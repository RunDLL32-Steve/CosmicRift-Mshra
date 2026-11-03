using System.Security.Cryptography;
using System.Text;
using RogueTrader.Persistence;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>Outcome of opening or reloading the session profile, phrased for a client status line.</summary>
public sealed record ProfileOpenOutcome(bool Succeeded, bool Created, ProfileLoadStatus Status, long Revision, string Message);

/// <summary>
/// Queue M8 client boundary: one local player profile, one station, and the authoritative services behind them. Every
/// state change goes through <see cref="TransactionProcessor"/> (which saves before acknowledging) and docking is decided
/// by <see cref="TradingService"/> from the ship position. The client renders <see cref="Profile"/>; it never edits wallet,
/// inventory or upgrades itself. Godot-free so the same flow is unit-tested.
/// </summary>
public sealed class StationProfileSession
{
    /// <summary>Reward source id prefix for cargo the ship collected in space; the pile id follows (NEW; original pickup ownership is open).</summary>
    public const string PickupSourcePrefix = "pickup:";

    private readonly TransactionProcessor _processor;
    private readonly TradingService _trading;
    private readonly IProfileRepository _repository;
    private readonly string? _shipHullId;

    /// <param name="cargoCapacity">The flown ship's hold weight cap (<c>ShipDefinition.Cargo.Capacity</c>).</param>
    public StationProfileSession(IProfileRepository repository, StationDefinition station, string? shipHullId, long cargoCapacity,
        PersistenceOptions? options = null, TimeProvider? time = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Station = station ?? throw new ArgumentNullException(nameof(station));
        _shipHullId = shipHullId;
        Cargo = station.CreateCargoCatalog(cargoCapacity);
        _processor = new TransactionProcessor(repository, station.CreatePriceTable(), new FlownShipCargoPolicy(this), options ?? new PersistenceOptions(), time);
        Stations = new StationDirectory(station.Sites);
        _trading = new TradingService(_processor, Stations);
    }

    public StationDefinition Station { get; }

    /// <summary>Credits paid per destroyed sector target (queue M7 slice); <see cref="KillRewardDefinition.None"/> pays nothing.</summary>
    public KillRewardDefinition KillReward { get; init; } = KillRewardDefinition.None;
    public StationDirectory Stations { get; }
    /// <summary>Cargo rules of the ship being flown; <see cref="FlyShip"/> replaces it when the player switches ships.</summary>
    public CargoCatalog Cargo { get; private set; }

    /// <summary>Applies the hold weight cap of the ship the player now flies (each ship has its own capacity, Steve, Nov 2 2026).</summary>
    public void FlyShip(long cargoCapacity) => Cargo = Station.CreateCargoCatalog(cargoCapacity);

    /// <summary>The cargo rule always checks against the ship flown at the time of the transaction.</summary>
    private sealed class FlownShipCargoPolicy(StationProfileSession session) : ICargoPolicy
    {
        public bool Fits(PlayerProfile current, IReadOnlyDictionary<string, int> proposedInventory) =>
            new CatalogCargoPolicy(session.Cargo).Fits(current, proposedInventory);
    }
    public string? ProfileId { get; private set; }

    /// <summary>Last committed authoritative profile; null until a profile opens successfully.</summary>
    public PlayerProfile? Profile => ProfileId is null ? null : _processor.GetSnapshot(ProfileId);

    public bool IsDocked(float shipX, float shipY) => Stations.IsDocked(Station.StationId, shipX, shipY);

    /// <summary>Loads the profile, creating it with the station's starting wallet only when it cleanly does not exist.</summary>
    public async Task<ProfileOpenOutcome> OpenOrCreateAsync(string profileId, CancellationToken ct = default)
    {
        ProfileId = profileId;
        var load = await _processor.OpenProfileAsync(profileId, ct).ConfigureAwait(false);
        if (load.Status != ProfileLoadStatus.NotFound) return Describe(load, created: false);
        var created = await _repository.CreateAsync(PlayerProfile.CreateNew(profileId, Station.StartingWallet, _shipHullId), ct).ConfigureAwait(false);
        if (created.Status != ProfileSaveStatus.Saved)
            return new ProfileOpenOutcome(false, false, ProfileLoadStatus.NotFound, -1, $"Could not create profile: {created.Status}.");
        return Describe(await _processor.OpenProfileAsync(profileId, ct).ConfigureAwait(false), created: true);
    }

    /// <summary>Discards in-memory state and reads the saved profile back from the repository.</summary>
    public async Task<ProfileOpenOutcome> ReloadAsync(CancellationToken ct = default)
    {
        if (ProfileId is null) throw new InvalidOperationException("No profile has been opened.");
        return Describe(await _processor.OpenProfileAsync(ProfileId, ct).ConfigureAwait(false), created: false);
    }

    public Task<TransactionResult> BuyAsync(string itemId, int quantity, float shipX, float shipY, CancellationToken ct = default) =>
        _trading.BuyAsync(RequireProfile(), shipX, shipY, new BuyRequest(NewId(), RequireProfile(), Station.StationId, itemId, quantity), ct);

    public Task<TransactionResult> SellAsync(string itemId, int quantity, float shipX, float shipY, CancellationToken ct = default) =>
        _trading.SellAsync(RequireProfile(), shipX, shipY, new SellRequest(NewId(), RequireProfile(), Station.StationId, itemId, quantity), ct);

    /// <summary>
    /// Buys a repair only when it can restore something: a ship at full hull or destroyed is refused locally with
    /// <see cref="RejectionReason.NothingToRepair"/> and the wallet is never charged (review R1-1).
    /// </summary>
    public Task<TransactionResult> RepairAsync(string repairId, float shipX, float shipY, float hull, float maximumHull, CancellationToken ct = default)
    {
        var profileId = RequireProfile();
        var transactionId = NewId();
        if (!float.IsFinite(hull) || hull <= 0 || hull >= maximumHull)
            return Task.FromResult(new TransactionResult(transactionId, TransactionStatus.Rejected, RejectionReason.NothingToRepair, false, 0,
                new Dictionary<string, int>(), null, Profile?.Wallet ?? 0, Profile?.Revision ?? -1, repairId));
        return _trading.RepairAsync(profileId, shipX, shipY, new RepairRequest(transactionId, profileId, Station.StationId, repairId), ct);
    }

    /// <summary>
    /// Buys an upgrade only when it would become active: one with no implemented effect, or one an owned upgrade in the same
    /// held category outranks, is refused locally and never charged (review R2-1). Already-owned ids still reach the core so
    /// it reports <see cref="RejectionReason.UpgradeAlreadyOwned"/>.
    /// </summary>
    public Task<TransactionResult> BuyUpgradeAsync(string upgradeId, float shipX, float shipY, CancellationToken ct = default)
    {
        var profileId = RequireProfile();
        var transactionId = NewId();
        var refusal = UpgradeRefusal(upgradeId);
        if (refusal != RejectionReason.None)
            return Task.FromResult(Refused(transactionId, refusal));
        return _trading.BuyUpgradeAsync(profileId, shipX, shipY, new BuyUpgradeRequest(transactionId, profileId, Station.StationId, upgradeId), ct);
    }

    /// <summary>Why buying this upgrade now would have no effect (<see cref="RejectionReason.None"/> when it would).</summary>
    public RejectionReason UpgradeRefusal(string upgradeId)
    {
        var profile = Profile;
        var upgrade = Station.Upgrades.FirstOrDefault(u => u.Id == upgradeId);
        if (profile is null || upgrade is null || profile.Upgrades.Contains(upgradeId)) return RejectionReason.None;
        if (upgrade.Effect == UpgradeEffect.None) return RejectionReason.UpgradeHasNoEffect;
        var active = UpgradeEffects.Active(Station, profile.Upgrades.Append(upgradeId));
        return active.Any(u => u.Id == upgradeId) ? RejectionReason.None : RejectionReason.UpgradeSlotHeld;
    }

    /// <summary>The owned upgrade that holds the same slot, if any.</summary>
    public StationUpgrade? SlotHolder(string upgradeId)
    {
        var upgrade = Station.Upgrades.FirstOrDefault(u => u.Id == upgradeId);
        if (upgrade is null || upgrade.HeldCategory == 0 || Profile is null) return null;
        return UpgradeEffects.Active(Station, Profile.Upgrades).FirstOrDefault(u => u.HeldCategory == upgrade.HeldCategory && u.Id != upgradeId);
    }

    private TransactionResult Refused(string transactionId, RejectionReason reason) =>
        new(transactionId, TransactionStatus.Rejected, reason, false, 0, new Dictionary<string, int>(), null, Profile?.Wallet ?? 0, Profile?.Revision ?? -1);

    /// <summary>
    /// Stable transaction id for one pickup (queue M7 mining slice): the same world instance and pickup sequence always give the
    /// same id, so a retried grant after a lost acknowledgement, reload or relaunch is recognised as a duplicate and applied once.
    /// Sequences restart at 0 in every <see cref="WorldSimulation"/>, so the id must be unique per world, not per sector (R1-1).
    /// </summary>
    public static string PickupTransactionId(string worldInstanceId, int sequence)
    {
        ArgumentException.ThrowIfNullOrEmpty(worldInstanceId);
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);
        return StableId("pickup", $"{worldInstanceId}-{sequence.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    /// <summary>
    /// Commits one pickup (a mined mineral pile or any other pile in space) to the profile through the idempotent GrantReward
    /// core. The source id names the pile, so the ledger shows where cargo came from. Retry with the same arguments after a
    /// refusal; the stable id makes the grant safe to repeat across retries, reloads and relaunches.
    /// </summary>
    public Task<TransactionResult> GrantPickupAsync(string worldInstanceId, CargoPickup pickup, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        var profileId = RequireProfile();
        var transactionId = PickupTransactionId(worldInstanceId, pickup.Sequence);
        return _processor.GrantRewardAsync(new TransactionContext(profileId, StationAccessVerified: false),
            new GrantRewardRequest(transactionId, profileId, $"{PickupSourcePrefix}{pickup.PileId}", 0,
                new Dictionary<string, int> { [pickup.ItemId] = pickup.Quantity }), ct);
    }

    /// <summary>Refines at the refinery: one unit of the recipe's input becomes one unit of its output (NEW reading, RT-F044 open).</summary>
    public Task<TransactionResult> RefineAsync(string recipeId, int quantity, float shipX, float shipY, CancellationToken ct = default)
    {
        var profileId = RequireProfile();
        var refinery = Station.Refinery?.StationId ?? Station.StationId; // without a refinery the core refuses: no recipes there
        return _trading.RefineAsync(profileId, shipX, shipY, new RefineRequest(NewId(), profileId, refinery, recipeId, quantity), ct);
    }

    public bool IsDockedAtRefinery(float shipX, float shipY) =>
        Station.Refinery is { } refinery && Stations.IsDocked(refinery.StationId, shipX, shipY);

    /// <summary>
    /// Stable transaction id for one kill: the same sector instance, target wave and target always give the same id, so a
    /// retried or replayed grant is recognised as a duplicate by the transaction core (persisted with the profile) and paid
    /// once, while the same target in a later respawn wave is a new kill.
    /// </summary>
    public static string KillTransactionId(string sectorInstanceId, int wave, string targetId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sectorInstanceId);
        ArgumentException.ThrowIfNullOrEmpty(targetId);
        ArgumentOutOfRangeException.ThrowIfNegative(wave);
        return StableId("kill", $"{sectorInstanceId}-{wave}-{targetId}");
    }

    private static string StableId(string kind, string identity)
    {
        var id = $"{kind}-{identity}";
        return id.Length <= ProfileInvariants.MaxTransactionIdLength
            ? id
            : $"{kind}-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id))).ToLowerInvariant();
    }

    /// <summary>
    /// Pays <see cref="KillReward"/> for a target the player destroyed (queue M7 slice, NEW reading of the raw [Cash]
    /// KillReward). Returns null when nothing is owed: no credits configured or another ship made the kill. Retry with the
    /// same arguments after a refusal; the stable id makes the grant idempotent across retries, reloads and relaunches.
    /// </summary>
    public Task<TransactionResult?> GrantKillRewardAsync(string sectorInstanceId, TargetKill kill, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(kill);
        var profileId = RequireProfile();
        if (KillReward.Credits <= 0 || kill.KillerShipId != WorldSimulation.PlayerShipId) return Task.FromResult<TransactionResult?>(null);
        return GrantAsync();

        async Task<TransactionResult?> GrantAsync() => await _processor.GrantRewardAsync(new TransactionContext(profileId, StationAccessVerified: false),
            new GrantRewardRequest(KillTransactionId(sectorInstanceId, kill.Wave, kill.TargetId), profileId, $"kill:{kill.TargetId}", KillReward.Credits,
                new Dictionary<string, int>()), ct).ConfigureAwait(false);
    }

    /// <summary>Saves the ship's hull and position to the profile (last write wins; no wallet or cargo change).</summary>
    public Task<ShipConditionResult> SaveShipAsync(float hull, float x, float y, float heading, CancellationToken ct = default) =>
        _processor.SaveShipConditionAsync(new TransactionContext(RequireProfile(), StationAccessVerified: false), RequireProfile(),
            new ShipCondition(hull, x, y, heading), ct);

    /// <summary>Buys a hull sold at the station (one of each); the player keeps flying the current ship until they select it.</summary>
    public Task<TransactionResult> BuyShipAsync(string hullId, float shipX, float shipY, CancellationToken ct = default) =>
        _trading.BuyShipAsync(RequireProfile(), shipX, shipY, new BuyShipRequest(NewId(), RequireProfile(), Station.StationId, hullId), ct);

    /// <summary>Makes an owned hull the active ship (the ship picker at load). The ship left behind keeps its saved condition.</summary>
    public Task<ShipConditionResult> SelectShipAsync(string hullId, CancellationToken ct = default) =>
        _processor.SelectShipAsync(new TransactionContext(RequireProfile(), StationAccessVerified: false), RequireProfile(), hullId, ct);

    /// <summary>
    /// Stable transaction id for one destruction of the active ship: the same world instance and destruction tick always give the
    /// same id, so a retry after a refusal, lost acknowledgement or reload is recognised as a duplicate and charged once.
    /// </summary>
    public static string DestructionTransactionId(string worldInstanceId, long tick)
    {
        ArgumentException.ThrowIfNullOrEmpty(worldInstanceId);
        ArgumentOutOfRangeException.ThrowIfNegative(tick);
        return StableId("destroyed", $"{worldInstanceId}-{tick.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    /// <summary>
    /// Records that the active ship was destroyed (Steve, Nov 3 2026): the core takes 20% of the credits held, once per
    /// destruction id, and puts the ship back at the spawn with a full hull; weapons, upgrades and cargo stay. Retry with the
    /// same arguments after a refusal.
    /// </summary>
    public Task<TransactionResult> RecordShipDestroyedAsync(string worldInstanceId, long tick, CancellationToken ct = default)
    {
        var profileId = RequireProfile();
        var hull = Profile?.ShipHullId ?? "";
        return _processor.RecordShipDestroyedAsync(new TransactionContext(profileId, StationAccessVerified: false),
            new ShipDestroyedRequest(DestructionTransactionId(worldInstanceId, tick), profileId, hull), ct);
    }

    /// <summary>Hull points the client restores for an applied repair (raw original amount read as hull points, NEW).</summary>
    public float RepairHullPoints(string repairId) => Station.Repairs.FirstOrDefault(r => r.Id == repairId)?.HullPoints ?? 0f;

    private string RequireProfile() => ProfileId ?? throw new InvalidOperationException("No profile has been opened.");

    private static string NewId() => Guid.NewGuid().ToString("N");

    private static ProfileOpenOutcome Describe(ProfileLoadResult load, bool created)
    {
        var revision = load.Profile?.Revision ?? -1;
        var message = load.Status switch
        {
            ProfileLoadStatus.Loaded when created => "New profile created and saved.",
            ProfileLoadStatus.Loaded => $"Profile loaded (revision {revision}).",
            ProfileLoadStatus.RecoveredFromBackup => $"Primary save was unreadable; recovered revision {revision} from backup.",
            ProfileLoadStatus.NotFound => "No saved profile exists.",
            ProfileLoadStatus.Corrupt => "Saved profile is unreadable and was quarantined; station services are disabled.",
            ProfileLoadStatus.UnsupportedNewerVersion => "Saved profile was written by a newer build; station services are disabled.",
            ProfileLoadStatus.MigrationMissing => "Saved profile needs a migration this build lacks; station services are disabled.",
            _ => "Saved profile could not be read; station services are disabled.",
        };
        var detail = load.Succeeded || load.Diagnostics.Count == 0 ? "" : " " + load.Diagnostics[^1].Message;
        return new ProfileOpenOutcome(load.Succeeded, created, load.Status, revision, message + detail);
    }
}
