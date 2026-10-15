using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace RogueTrader.Persistence;

/// <summary>
/// Authoritative buy/sell/upgrade/reward processing. Per profile, every transaction runs inside one lock; the
/// repository compare-and-swap additionally rejects a second processor or stale writer. A transaction is acknowledged
/// only after the repository reports the new state saved.
/// </summary>
public sealed class TransactionProcessor(IProfileRepository repository, IPriceSource prices, ICargoPolicy cargo,
    PersistenceOptions options, TimeProvider? time = null) : ITransactionProcessor
{
    private static readonly ImmutableSortedDictionary<string, int> NoItems = ImmutableSortedDictionary.Create<string, int>(StringComparer.Ordinal);

    private readonly IProfileRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IPriceSource _prices = prices ?? throw new ArgumentNullException(nameof(prices));
    private readonly ICargoPolicy _cargo = cargo ?? throw new ArgumentNullException(nameof(cargo));
    private readonly PersistenceOptions _options = Validate(options);
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, ProfileSlot> _slots = new(StringComparer.Ordinal);

    private static PersistenceOptions Validate(PersistenceOptions? o)
    {
        ArgumentNullException.ThrowIfNull(o);
        if (o.IdempotencyWindow < 0) throw new ArgumentOutOfRangeException(nameof(options), "IdempotencyWindow must be >= 0.");
        if (o.LedgerRetention < 0) throw new ArgumentOutOfRangeException(nameof(options), "LedgerRetention must be >= 0.");
        return o;
    }

    // ---- Open / snapshot -----------------------------------------------------------------------------------

    public async Task<ProfileLoadResult> OpenProfileAsync(string profileId, CancellationToken ct = default)
    {
        ProfileIdRules.ThrowIfInvalid(profileId, nameof(profileId));
        var slot = _slots.GetOrAdd(profileId, static _ => new ProfileSlot());
        await slot.Lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ProfileLoadResult result;
            try { result = await _repository.LoadAsync(profileId, CancellationToken.None).ConfigureAwait(false); }
            catch
            {
                slot.NeedsReload = true;
                throw;
            }
            if (result.Succeeded)
            {
                slot.Committed = result.Profile;
                slot.NeedsReload = false;
            }
            else
            {
                // Disk truth is unknown or unusable: refuse further transactions until a successful reopen.
                slot.NeedsReload = true;
            }
            return result;
        }
        finally { slot.Lock.Release(); }
    }

    public PlayerProfile? GetSnapshot(string profileId) =>
        profileId is not null && _slots.TryGetValue(profileId, out var slot) ? slot.Committed : null;

    // ---- Public transactions -------------------------------------------------------------------------------

    public Task<TransactionResult> BuyAsync(TransactionContext ctx, BuyRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.StationId) && !string.IsNullOrEmpty(r.ItemId) && r.Quantity > 0;
        return ExecuteAsync(ctx, new Req(TransactionKind.Buy, r.TransactionId, r.ProfileId, r.StationId, r.ItemId, null, r.Quantity, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> SellAsync(TransactionContext ctx, SellRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.StationId) && !string.IsNullOrEmpty(r.ItemId) && r.Quantity > 0;
        return ExecuteAsync(ctx, new Req(TransactionKind.Sell, r.TransactionId, r.ProfileId, r.StationId, r.ItemId, null, r.Quantity, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> BuyUpgradeAsync(TransactionContext ctx, BuyUpgradeRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.StationId) && !string.IsNullOrEmpty(r.UpgradeId);
        return ExecuteAsync(ctx, new Req(TransactionKind.BuyUpgrade, r.TransactionId, r.ProfileId, r.StationId, null, r.UpgradeId, 0, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> GrantRewardAsync(TransactionContext ctx, GrantRewardRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.SourceId) && r.Currency >= 0 && r.Items is not null
            && (r.Currency > 0 || r.Items.Count > 0);
        if (shapeOk)
            foreach (var (item, qty) in r.Items!)
                if (string.IsNullOrEmpty(item) || qty <= 0) { shapeOk = false; break; }
        return ExecuteAsync(ctx, new Req(TransactionKind.GrantReward, r.TransactionId, r.ProfileId, r.SourceId, null, null, 0, r.Currency, r.Items, shapeOk), ct);
    }

    private static bool IdsOk(string? transactionId, string? profileId, string? place) =>
        !string.IsNullOrEmpty(transactionId) && transactionId.Length <= ProfileInvariants.MaxTransactionIdLength
        && !string.IsNullOrEmpty(profileId) && !string.IsNullOrEmpty(place);

    // ---- Pipeline ------------------------------------------------------------------------------------------

    private sealed record Req(TransactionKind Kind, string? TransactionId, string? ProfileId, string? Place, string? ItemId,
        string? UpgradeId, int Quantity, long Currency, IReadOnlyDictionary<string, int>? Items, bool ShapeOk)
    {
        public bool NeedsStation => Kind != TransactionKind.GrantReward;
    }

    private sealed record Plan(long CurrencyDelta, long WalletAfter, ImmutableSortedDictionary<string, int> NewInventory,
        ImmutableSortedDictionary<string, int> ItemDeltas, ImmutableSortedSet<string> NewUpgrades, string? UpgradeId, bool QuantityIncreases);

    private async Task<TransactionResult> ExecuteAsync(TransactionContext ctx, Req q, CancellationToken ct)
    {
        // 0. Shape.
        if (!q.ShapeOk) return Reject(q.TransactionId, RejectionReason.InvalidRequest, null);
        // 1. Identity.
        if (!string.Equals(q.ProfileId, ctx.AuthenticatedProfileId, StringComparison.Ordinal))
            return Reject(q.TransactionId, RejectionReason.IdentityMismatch, null);
        if (!_slots.TryGetValue(q.ProfileId!, out var slot)) return Reject(q.TransactionId, RejectionReason.ProfileNotOpen, null);

        var fingerprint = Fingerprint(q);
        await slot.Lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var committed = slot.Committed;
            if (committed is null || slot.NeedsReload) return Reject(q.TransactionId, RejectionReason.ProfileNotOpen, null);

            // 2. Duplicate lookup (precedes state checks so a replay returns the original outcome).
            foreach (var done in committed.AppliedTransactions)
            {
                if (!string.Equals(done.TransactionId, q.TransactionId, StringComparison.Ordinal)) continue;
                return string.Equals(done.RequestFingerprint, fingerprint, StringComparison.Ordinal)
                    ? new TransactionResult(done.TransactionId, TransactionStatus.Applied, RejectionReason.None, true,
                        done.CurrencyDelta, done.ItemDeltas, done.UpgradeId, done.WalletAfter, done.Revision)
                    : Reject(q.TransactionId, RejectionReason.TransactionIdReused, committed);
            }

            // 3-7. Validation and planning; nothing is mutated.
            var reason = Evaluate(ctx, q, committed, out var plan);
            if (reason != RejectionReason.None) return Reject(q.TransactionId, reason, committed);

            // 8. Build the next immutable state.
            var now = _time.GetUtcNow();
            var revision = committed.Revision + 1;
            var applied = committed.AppliedTransactions.Add(new AppliedTransaction(q.TransactionId!, q.Kind, fingerprint,
                plan!.CurrencyDelta, plan.ItemDeltas, plan.UpgradeId, plan.WalletAfter, revision, now));
            if (applied.Count > _options.IdempotencyWindow) applied = applied.RemoveRange(0, applied.Count - _options.IdempotencyWindow);
            var ledger = committed.Ledger.Add(new WalletLedgerEntry(committed.NextLedgerSequence, q.TransactionId!, q.Kind,
                plan.CurrencyDelta, plan.WalletAfter, now));
            if (ledger.Count > _options.LedgerRetention) ledger = ledger.RemoveRange(0, ledger.Count - _options.LedgerRetention);
            var next = committed with
            {
                Revision = revision,
                Wallet = plan.WalletAfter,
                Inventory = plan.NewInventory,
                Upgrades = plan.NewUpgrades,
                AppliedTransactions = applied,
                Ledger = ledger,
                NextLedgerSequence = committed.NextLedgerSequence + 1,
            };

            // 9. Persist; acknowledge only after the save returns.
            ProfileSaveResult save;
            try { save = await _repository.SaveAsync(next, committed.Revision, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception)
            {
                slot.NeedsReload = true;
                return Reject(q.TransactionId, RejectionReason.PersistenceFailed, committed);
            }
            if (save.Status != ProfileSaveStatus.Saved)
            {
                slot.NeedsReload = true;
                return Reject(q.TransactionId,
                    save.Status == ProfileSaveStatus.RevisionConflict ? RejectionReason.PersistenceConflict : RejectionReason.PersistenceFailed,
                    committed);
            }
            slot.Committed = next;
            return new TransactionResult(q.TransactionId!, TransactionStatus.Applied, RejectionReason.None, false,
                plan.CurrencyDelta, plan.ItemDeltas, plan.UpgradeId, plan.WalletAfter, revision);
        }
        finally { slot.Lock.Release(); }
    }

    private RejectionReason Evaluate(TransactionContext ctx, Req q, PlayerProfile c, out Plan? plan)
    {
        plan = null;
        // 3. Proximity.
        if (q.NeedsStation && !ctx.StationAccessVerified) return RejectionReason.StationAccessDenied;

        var inventory = c.Inventory.ToBuilder();
        var deltas = ImmutableSortedDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
        var upgrades = c.Upgrades;
        string? upgradeId = null;
        long currencyDelta;
        long walletAfter;
        var increases = false;

        try
        {
            switch (q.Kind)
            {
                case TransactionKind.Buy:
                {
                    if (!_prices.TryGetBuyUnitPrice(q.Place!, q.ItemId!, out var unit)) return RejectionReason.NotForSale;
                    if (unit < 0) return RejectionReason.InvalidPrice;
                    var total = checked(unit * (long)q.Quantity);
                    if (c.Wallet < total) return RejectionReason.InsufficientFunds;
                    inventory.TryGetValue(q.ItemId!, out var held);
                    inventory[q.ItemId!] = checked(held + q.Quantity);
                    deltas[q.ItemId!] = q.Quantity;
                    currencyDelta = -total;
                    walletAfter = c.Wallet - total;
                    increases = true;
                    break;
                }
                case TransactionKind.Sell:
                {
                    inventory.TryGetValue(q.ItemId!, out var held);
                    if (held < q.Quantity) return RejectionReason.InsufficientInventory;
                    if (!_prices.TryGetSellUnitPrice(q.Place!, q.ItemId!, out var unit)) return RejectionReason.NotForSale;
                    if (unit < 0) return RejectionReason.InvalidPrice;
                    var total = checked(unit * (long)q.Quantity);
                    walletAfter = checked(c.Wallet + total);
                    if (held == q.Quantity) inventory.Remove(q.ItemId!);
                    else inventory[q.ItemId!] = held - q.Quantity;
                    deltas[q.ItemId!] = -q.Quantity;
                    currencyDelta = total;
                    break;
                }
                case TransactionKind.BuyUpgrade:
                {
                    if (c.Upgrades.Contains(q.UpgradeId!)) return RejectionReason.UpgradeAlreadyOwned;
                    if (!_prices.TryGetUpgradePrice(q.Place!, q.UpgradeId!, out var price)) return RejectionReason.NotForSale;
                    if (price < 0) return RejectionReason.InvalidPrice;
                    if (c.Wallet < price) return RejectionReason.InsufficientFunds;
                    upgrades = upgrades.Add(q.UpgradeId!);
                    upgradeId = q.UpgradeId;
                    currencyDelta = -price;
                    walletAfter = c.Wallet - price;
                    break;
                }
                case TransactionKind.GrantReward:
                {
                    walletAfter = checked(c.Wallet + q.Currency);
                    currencyDelta = q.Currency;
                    foreach (var (item, qty) in q.Items!)
                    {
                        inventory.TryGetValue(item, out var held);
                        inventory[item] = checked(held + qty);
                        deltas[item] = qty;
                        increases = true;
                    }
                    break;
                }
                default:
                    return RejectionReason.InvalidRequest;
            }
        }
        catch (OverflowException)
        {
            return RejectionReason.ArithmeticOverflow;
        }

        var newInventory = inventory.ToImmutable();
        // 7. Cargo (all-or-nothing).
        if (increases && !_cargo.Fits(c, newInventory)) return RejectionReason.CargoCapacityExceeded;

        plan = new Plan(currencyDelta, walletAfter, newInventory, deltas.ToImmutable(), upgrades, upgradeId, increases);
        return RejectionReason.None;
    }

    private static TransactionResult Reject(string? transactionId, RejectionReason reason, PlayerProfile? profile) =>
        new(transactionId ?? string.Empty, TransactionStatus.Rejected, reason, false, 0, NoItems, null,
            profile?.Wallet ?? 0, profile?.Revision ?? -1);

    /// <summary>
    /// Lowercase hex SHA-256 over kind, profile, station/source, item/upgrade, quantity, currency and ordinally sorted reward items.
    /// Variable-length fields are length-prefixed so different payloads cannot collide through separator characters.
    /// </summary>
    private static string Fingerprint(Req q)
    {
        static string L(string? s) => s is null ? "-" : s.Length.ToString(CultureInfo.InvariantCulture) + ":" + s;
        var sb = new StringBuilder();
        sb.Append(q.Kind).Append('|').Append(L(q.ProfileId)).Append('|').Append(L(q.Place)).Append('|')
          .Append(L(q.ItemId ?? q.UpgradeId)).Append('|').Append(q.Quantity.ToString(CultureInfo.InvariantCulture)).Append('|')
          .Append(q.Currency.ToString(CultureInfo.InvariantCulture)).Append('|');
        if (q.Items is not null)
        {
            var first = true;
            foreach (var (item, qty) in q.Items.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (!first) sb.Append(';');
                first = false;
                sb.Append(L(item)).Append('=').Append(qty.ToString(CultureInfo.InvariantCulture));
            }
        }
        return ProfileSerializer.Sha256Hex(sb.ToString());
    }
}
