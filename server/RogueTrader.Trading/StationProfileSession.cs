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
    /// <summary>Reward source id for cargo the ship collected in space (NEW/provisional; original pickup ownership is open).</summary>
    public const string PickupSourceId = "world-pickup";

    private readonly TransactionProcessor _processor;
    private readonly TradingService _trading;
    private readonly IProfileRepository _repository;
    private readonly string? _shipHullId;

    public StationProfileSession(IProfileRepository repository, StationDefinition station, string? shipHullId,
        PersistenceOptions? options = null, TimeProvider? time = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Station = station ?? throw new ArgumentNullException(nameof(station));
        _shipHullId = shipHullId;
        Cargo = station.CreateCargoCatalog();
        _processor = new TransactionProcessor(repository, station.CreatePriceTable(), new CatalogCargoPolicy(Cargo), options ?? new PersistenceOptions(), time);
        Stations = new StationDirectory([station.Site]);
        _trading = new TradingService(_processor, Stations);
    }

    public StationDefinition Station { get; }
    public StationDirectory Stations { get; }
    public CargoCatalog Cargo { get; }
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

    public Task<TransactionResult> BuyUpgradeAsync(string upgradeId, float shipX, float shipY, CancellationToken ct = default) =>
        _trading.BuyUpgradeAsync(RequireProfile(), shipX, shipY, new BuyUpgradeRequest(NewId(), RequireProfile(), Station.StationId, upgradeId), ct);

    /// <summary>
    /// Commits units the ship's hold has gained over the profile inventory (in-space pickups) as one reward. Returns null when
    /// there is nothing new. The hold is simulation state; the profile stays the record the HUD and station show.
    /// </summary>
    public async Task<TransactionResult?> CommitPickupsAsync(IReadOnlyDictionary<string, int> hold, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(hold);
        var profile = Profile ?? throw new InvalidOperationException("No profile is open.");
        var gained = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var (item, quantity) in hold)
        {
            var delta = quantity - profile.Inventory.GetValueOrDefault(item);
            if (delta > 0) gained[item] = delta;
        }
        if (gained.Count == 0) return null;
        return await _processor.GrantRewardAsync(new TransactionContext(profile.ProfileId, StationAccessVerified: false),
            new GrantRewardRequest(NewId(), profile.ProfileId, PickupSourceId, 0, gained), ct).ConfigureAwait(false);
    }

    /// <summary>Saves the ship's hull and position to the profile (last write wins; no wallet or cargo change).</summary>
    public Task<ShipConditionResult> SaveShipAsync(float hull, float x, float y, float heading, CancellationToken ct = default) =>
        _processor.SaveShipConditionAsync(new TransactionContext(RequireProfile(), StationAccessVerified: false), RequireProfile(),
            new ShipCondition(hull, x, y, heading), ct);

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
