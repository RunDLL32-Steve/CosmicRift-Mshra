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
    PersistenceOptions options, TimeProvider? time = null, IConductRules? conduct = null) : ITransactionProcessor
{
    /// <summary>
    /// Share of the credits held that a destroyed ship costs (Steve, Nov 3 2026: "the user loses money, which is 20% of what they
    /// own"). Rounded down to whole credits, so the wallet never goes negative. A server rule: no request can carry another rate.
    /// </summary>
    public const int DestructionPenaltyPercent = 20;

    private static readonly ImmutableSortedDictionary<string, int> NoItems = ImmutableSortedDictionary.Create<string, int>(StringComparer.Ordinal);

    private readonly IProfileRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IPriceSource _prices = prices ?? throw new ArgumentNullException(nameof(prices));
    private readonly ICargoPolicy _cargo = cargo ?? throw new ArgumentNullException(nameof(cargo));
    private readonly PersistenceOptions _options = Validate(options);
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly IConductRules? _conduct = conduct;
    private readonly ConcurrentDictionary<string, ProfileSlot> _slots = new(StringComparer.Ordinal);

    private static PersistenceOptions Validate(PersistenceOptions? o)
    {
        ArgumentNullException.ThrowIfNull(o);
        // A zero window would silently disable duplicate detection (master 9.3-6).
        if (o.IdempotencyWindow < 1) throw new ArgumentOutOfRangeException(nameof(options), "IdempotencyWindow must be >= 1.");
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
            if (result.Succeeded && !string.Equals(result.Profile?.ProfileId, profileId, StringComparison.Ordinal))
            {
                // Defence in depth: never run transactions for one id against another id's state.
                slot.NeedsReload = true;
                return new ProfileLoadResult(ProfileLoadStatus.Corrupt, null, result.Diagnostics.Append(new PersistenceDiagnostic(
                    DiagnosticCode.InvariantViolation, $"The repository returned profile '{result.Profile?.ProfileId}' for '{profileId}'.", null)).ToArray());
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
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.StationId) && IdOk(r.ItemId) && r.Quantity > 0;
        return ExecuteAsync(ctx, new Req(TransactionKind.Buy, r.TransactionId, r.ProfileId, r.StationId, r.ItemId, null, r.Quantity, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> SellAsync(TransactionContext ctx, SellRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.StationId) && IdOk(r.ItemId) && r.Quantity > 0;
        return ExecuteAsync(ctx, new Req(TransactionKind.Sell, r.TransactionId, r.ProfileId, r.StationId, r.ItemId, null, r.Quantity, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> BuyUpgradeAsync(TransactionContext ctx, BuyUpgradeRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.StationId) && IdOk(r.UpgradeId);
        return ExecuteAsync(ctx, new Req(TransactionKind.BuyUpgrade, r.TransactionId, r.ProfileId, r.StationId, null, r.UpgradeId, 0, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> RepairAsync(TransactionContext ctx, RepairRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.StationId) && IdOk(r.RepairId);
        return ExecuteAsync(ctx, new Req(TransactionKind.Repair, r.TransactionId, r.ProfileId, r.StationId, r.RepairId, null, 0, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> RefineAsync(TransactionContext ctx, RefineRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.StationId) && IdOk(r.RecipeId) && r.Quantity > 0;
        return ExecuteAsync(ctx, new Req(TransactionKind.Refine, r.TransactionId, r.ProfileId, r.StationId, r.RecipeId, null, r.Quantity, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> GrantRewardAsync(TransactionContext ctx, GrantRewardRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        // Snapshot the caller's dictionary once: shape check, fingerprint and application then see the same items
        // even if the caller mutates its collection while the request waits for the profile lock.
        var items = SnapshotItems(r.Items);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.SourceId) && r.Currency >= 0 && items is not null
            && (r.Currency > 0 || items.Count > 0);
        if (shapeOk)
            foreach (var (item, qty) in items!)
                if (!IdOk(item) || qty <= 0) { shapeOk = false; break; }
        return ExecuteAsync(ctx, new Req(TransactionKind.GrantReward, r.TransactionId, r.ProfileId, r.SourceId, null, null, 0, r.Currency, items, shapeOk), ct);
    }

    public Task<TransactionResult> RecordConductAsync(TransactionContext ctx, RecordConductRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.SourceId) && IdOk(r.EventId) && r.Count > 0;
        return ExecuteAsync(ctx, new Req(TransactionKind.RecordConduct, r.TransactionId, r.ProfileId, r.SourceId, r.EventId, null, r.Count, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> BuyShipAsync(TransactionContext ctx, BuyShipRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.StationId) && IdOk(r.HullId);
        return ExecuteAsync(ctx, new Req(TransactionKind.BuyShip, r.TransactionId, r.ProfileId, r.StationId, r.HullId, null, 0, 0, null, shapeOk), ct);
    }

    public Task<TransactionResult> RecordShipDestroyedAsync(TransactionContext ctx, ShipDestroyedRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(r);
        // The place is the hull itself: no station is involved and the fingerprint then names the destroyed hull.
        var shapeOk = IdsOk(r.TransactionId, r.ProfileId, r.HullId);
        return ExecuteAsync(ctx, new Req(TransactionKind.ShipDestroyed, r.TransactionId, r.ProfileId, r.HullId, r.HullId, null, 0, 0, null, shapeOk), ct);
    }

    public async Task<ShipConditionResult> SelectShipAsync(TransactionContext ctx, string profileId, string hullId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (!IdOk(profileId) || !IdOk(hullId)) return new ShipConditionResult(false, RejectionReason.InvalidRequest, -1);
        return await MutateAsync(ctx, profileId, committed =>
        {
            if (!committed.OwnedShips.TryGetValue(hullId, out var parked)) return (RejectionReason.ShipNotOwned, null);
            if (string.Equals(committed.ShipHullId, hullId, StringComparison.Ordinal)) return (RejectionReason.None, committed);
            var owned = committed.OwnedShips.SetItem(hullId, null);
            if (committed.ShipHullId is { } current) owned = owned.SetItem(current, committed.Ship);
            return (RejectionReason.None, committed with { ShipHullId = hullId, Ship = parked, OwnedShips = owned });
        }, ct).ConfigureAwait(false);
    }

    public async Task<ShipConditionResult> SaveShipConditionAsync(TransactionContext ctx, string profileId, ShipCondition ship, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        // A destroyed ship (hull 0) is never saved as a condition: destruction goes through RecordShipDestroyedAsync, which takes
        // the penalty, so a client cannot dodge it by saving the wreck and reloading.
        if (ship is null || !ship.IsValid || ship.Hull <= 0 || !IdOk(profileId)) return new ShipConditionResult(false, RejectionReason.InvalidRequest, -1);
        return await MutateAsync(ctx, profileId, committed =>
            (RejectionReason.None, committed.Ship == ship ? committed : committed with { Ship = ship }), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Shared path of the non-currency changes (ship saves and ship selection): identity check, per-profile lock, compare-and-swap
    /// save and acknowledge-after-save. <paramref name="change"/> returns the refusal, or the next state (the same instance when
    /// nothing changes, which saves nothing).
    /// </summary>
    private async Task<ShipConditionResult> MutateAsync(TransactionContext ctx, string profileId,
        Func<PlayerProfile, (RejectionReason Reason, PlayerProfile? Next)> change, CancellationToken ct)
    {
        if (!string.Equals(profileId, ctx.AuthenticatedProfileId, StringComparison.Ordinal))
            return new ShipConditionResult(false, RejectionReason.IdentityMismatch, -1);
        if (!_slots.TryGetValue(profileId, out var slot)) return new ShipConditionResult(false, RejectionReason.ProfileNotOpen, -1);
        await slot.Lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var committed = slot.Committed;
            if (committed is null || slot.NeedsReload) return new ShipConditionResult(false, RejectionReason.ProfileNotOpen, -1);
            var (reason, changed) = change(committed);
            if (reason != RejectionReason.None || changed is null) return new ShipConditionResult(false, reason, committed.Revision);
            if (ReferenceEquals(changed, committed)) return new ShipConditionResult(true, RejectionReason.None, committed.Revision);
            var next = changed with { Revision = committed.Revision + 1 };
            ProfileSaveResult save;
            try { save = await _repository.SaveAsync(next, committed.Revision, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception)
            {
                slot.NeedsReload = true;
                return new ShipConditionResult(false, RejectionReason.PersistenceFailed, committed.Revision);
            }
            if (save.Status != ProfileSaveStatus.Saved)
            {
                slot.NeedsReload = true;
                return new ShipConditionResult(false,
                    save.Status == ProfileSaveStatus.RevisionConflict ? RejectionReason.PersistenceConflict : RejectionReason.PersistenceFailed, committed.Revision);
            }
            slot.Committed = next;
            return new ShipConditionResult(true, RejectionReason.None, next.Revision);
        }
        finally { slot.Lock.Release(); }
    }

    /// <summary>Ordinal immutable copy; null when absent or when the caller's comparer let two ordinally equal keys in.</summary>
    private static ImmutableSortedDictionary<string, int>? SnapshotItems(IReadOnlyDictionary<string, int>? source)
    {
        if (source is null) return null;
        var builder = ImmutableSortedDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
        foreach (var (item, qty) in source)
        {
            if (item is null || builder.ContainsKey(item)) return null;
            builder.Add(item, qty);
        }
        return builder.ToImmutable();
    }

    private static bool IdsOk(string? transactionId, string? profileId, string? place) =>
        IdOk(transactionId) && transactionId!.Length <= ProfileInvariants.MaxTransactionIdLength
        && IdOk(profileId) && IdOk(place);

    /// <summary>Non-empty and well-formed UTF-16: ids with unpaired surrogates cannot be persisted verbatim.</summary>
    private static bool IdOk(string? id) => !string.IsNullOrEmpty(id) && ProfileInvariants.IsWellFormedUtf16(id);

    // ---- Pipeline ------------------------------------------------------------------------------------------

    private sealed record Req(TransactionKind Kind, string? TransactionId, string? ProfileId, string? Place, string? ItemId,
        string? UpgradeId, int Quantity, long Currency, IReadOnlyDictionary<string, int>? Items, bool ShapeOk)
    {
        public bool NeedsStation => Kind is not (TransactionKind.GrantReward or TransactionKind.RecordConduct or TransactionKind.ShipDestroyed);
    }

    private sealed record Plan(long CurrencyDelta, long WalletAfter, ImmutableSortedDictionary<string, int> NewInventory,
        ImmutableSortedDictionary<string, int> ItemDeltas, ImmutableSortedSet<string> NewUpgrades, string? UpgradeId, bool QuantityIncreases,
        long Bounty, ImmutableSortedDictionary<string, long> Reputation, string? RepairId = null, ConductDelta? Conduct = null,
        string? ShipHullId = null, ShipCondition? Ship = null, ImmutableSortedDictionary<string, ShipCondition?>? OwnedShips = null);

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
                        done.CurrencyDelta, done.ItemDeltas, done.UpgradeId, done.WalletAfter, done.Revision, done.RepairId, done.Conduct)
                    : Reject(q.TransactionId, RejectionReason.TransactionIdReused, committed);
            }

            // 3-7. Validation and planning; nothing is mutated.
            var reason = Evaluate(ctx, q, committed, out var plan);
            if (reason != RejectionReason.None) return Reject(q.TransactionId, reason, committed);

            // A conduct event that clamps to no change (for example bounty-cleared at bounty 0) is refused with NoConductChange: no
            // save, no new revision. It is a refusal, not an Applied acknowledgement, so no caller can read the id as done; recording
            // it would itself need a saved revision (review R2-1).
            if (plan!.Conduct is { BountyDelta: 0, ReputationDeltas.Count: 0 })
                return Reject(q.TransactionId, RejectionReason.NoConductChange, committed);

            // 8. Build the next immutable state.
            var now = _time.GetUtcNow();
            var revision = committed.Revision + 1;
            var applied = committed.AppliedTransactions.Add(new AppliedTransaction(q.TransactionId!, q.Kind, fingerprint,
                plan!.CurrencyDelta, plan.ItemDeltas, plan.UpgradeId, plan.WalletAfter, revision, now, plan.RepairId, plan.Conduct));
            if (applied.Count > _options.IdempotencyWindow) applied = applied.RemoveRange(0, applied.Count - _options.IdempotencyWindow);
            // Conduct moves no currency, so it writes no wallet ledger entry and cannot push currency history out of retention.
            var writesLedger = q.Kind != TransactionKind.RecordConduct;
            var ledger = writesLedger
                ? committed.Ledger.Add(new WalletLedgerEntry(committed.NextLedgerSequence, q.TransactionId!, q.Kind, plan.CurrencyDelta, plan.WalletAfter, now))
                : committed.Ledger;
            if (ledger.Count > _options.LedgerRetention) ledger = ledger.RemoveRange(0, ledger.Count - _options.LedgerRetention);
            var next = committed with
            {
                Revision = revision,
                Wallet = plan.WalletAfter,
                Inventory = plan.NewInventory,
                Upgrades = plan.NewUpgrades,
                Bounty = plan.Bounty,
                Reputation = plan.Reputation,
                ShipHullId = plan.ShipHullId,
                Ship = plan.Ship,
                OwnedShips = plan.OwnedShips!,
                AppliedTransactions = applied,
                Ledger = ledger,
                NextLedgerSequence = writesLedger ? committed.NextLedgerSequence + 1 : committed.NextLedgerSequence,
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
                plan.CurrencyDelta, plan.ItemDeltas, plan.UpgradeId, plan.WalletAfter, revision, plan.RepairId, plan.Conduct);
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
        string? repairId = null;
        var bounty = c.Bounty;
        var reputation = c.Reputation;
        ConductDelta? conduct = null;
        var shipHullId = c.ShipHullId;
        var ship = c.Ship;
        var owned = c.OwnedShips;
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
                case TransactionKind.Repair:
                {
                    if (!_prices.TryGetRepairPrice(q.Place!, q.ItemId!, out var price)) return RejectionReason.NotForSale;
                    if (price < 0) return RejectionReason.InvalidPrice;
                    if (c.Wallet < price) return RejectionReason.InsufficientFunds;
                    repairId = q.ItemId;
                    currencyDelta = -price;
                    walletAfter = c.Wallet - price;
                    break;
                }
                case TransactionKind.Refine:
                {
                    if (!_prices.TryGetRefineRecipe(q.Place!, q.ItemId!, out var input, out var output)
                        || !IdOk(input) || !IdOk(output) || input == output) return RejectionReason.NotForSale;
                    inventory.TryGetValue(input, out var held);
                    if (held < q.Quantity) return RejectionReason.InsufficientInventory;
                    if (held == q.Quantity) inventory.Remove(input);
                    else inventory[input] = held - q.Quantity;
                    inventory.TryGetValue(output, out var made);
                    inventory[output] = checked(made + q.Quantity);
                    deltas[input] = -q.Quantity;
                    deltas[output] = q.Quantity;
                    currencyDelta = 0;
                    walletAfter = c.Wallet;
                    increases = true;
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
                case TransactionKind.RecordConduct:
                {
                    if (_conduct is null || !_conduct.TryApply(q.ItemId!, q.Quantity, c.Bounty, c.Reputation, out bounty, out reputation)
                        || bounty < 0 || reputation is null || reputation.Values.Any(v => v == 0))
                        return RejectionReason.UnknownConductEvent;
                    var changes = ImmutableSortedDictionary.CreateBuilder<string, long>(StringComparer.Ordinal);
                    foreach (var category in c.Reputation.Keys.Union(reputation.Keys, StringComparer.Ordinal))
                    {
                        c.Reputation.TryGetValue(category, out var before);
                        reputation.TryGetValue(category, out var after);
                        if (after != before) changes[category] = checked(after - before);
                    }
                    conduct = new ConductDelta(q.ItemId!, q.Quantity, checked(bounty - c.Bounty), changes.ToImmutable(), bounty);
                    currencyDelta = 0;
                    walletAfter = c.Wallet;
                    break;
                }
                case TransactionKind.BuyShip:
                {
                    if (c.OwnedShips.ContainsKey(q.ItemId!)) return RejectionReason.ShipAlreadyOwned;
                    if (!_prices.TryGetShipPrice(q.Place!, q.ItemId!, out var price)) return RejectionReason.NotForSale;
                    if (price < 0) return RejectionReason.InvalidPrice;
                    if (_prices.TryGetShipRequirement(q.Place!, q.ItemId!, out var requiresOneOf) && !requiresOneOf.Any(c.OwnedShips.ContainsKey))
                        return RejectionReason.ShipRequirementNotMet;
                    if (c.Wallet < price) return RejectionReason.InsufficientFunds;
                    // A new hull waits at the spawn with a full hull. A player with no ship at all starts flying it.
                    owned = owned.Add(q.ItemId!, null);
                    shipHullId ??= q.ItemId;
                    currencyDelta = -price;
                    walletAfter = c.Wallet - price;
                    break;
                }
                case TransactionKind.ShipDestroyed:
                {
                    if (!string.Equals(c.ShipHullId, q.ItemId, StringComparison.Ordinal)) return RejectionReason.ShipNotActive;
                    // Steve, Nov 3 2026: the ship respawns with its weapons, and all purchased items remain; the player loses 20% of
                    // the credits they hold, and the cargo in the hold (minerals and every other hold item) is lost with the ship.
                    // Upgrades and owned ships are untouched; the ship returns to the spawn.
                    var penalty = c.Wallet / 100 * DestructionPenaltyPercent + c.Wallet % 100 * DestructionPenaltyPercent / 100;
                    foreach (var (item, held) in c.Inventory) deltas[item] = -held;
                    inventory.Clear();
                    ship = null;
                    currencyDelta = -penalty;
                    walletAfter = c.Wallet - penalty;
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

        plan = new Plan(currencyDelta, walletAfter, newInventory, deltas.ToImmutable(), upgrades, upgradeId, increases, bounty, reputation,
            repairId, conduct, shipHullId, ship, owned);
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
