using Godot;
using RogueTrader.GameData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.Client;

/// <summary>Thin bridge between the pure C# world and Godot input, view, camera, and HUD.</summary>
public partial class GameClient : Node2D
{
    private GameCatalog _catalog = null!;
    private WorldSimulation _world = null!;
    private WorldView _view = null!;
    private FlightHud _hud = null!;
    private Camera2D _camera = null!;
    private OriginalContentInspector _inspector = null!;
    private OriginalSpriteLibrary? _originals;
    private bool _inspectionVisible;
    private bool _verifyOriginals;
    private long _inspectionStartTick;
    private ClientSmokeTest? _smokeTest;
    private string? _capturePath;
    private bool _paused;
    private bool _initialized;
    private bool _captureStarted;
    private int _renderedFrames;
    private const string LocalProfileId = "local-pilot";
    private StationDefinition _stationDefinition = null!;
    private CargoCatalog _cargo = null!;
    private StationScreen _stationScreen = null!;
    private FileProfileRepository? _repository;
    private StationProfileSession? _session;
    private string _profileRoot = "";
    private string? _smokeProfileRoot;
    private string _profileStatus = "";
    private bool _stationVisible;
    private GameCatalog _playCatalog = null!;
    private string _profileOutcome = "";
    private long _nextPickupRetryTick;
    private readonly PickupCommitQueue _pickups = new();
    private MineralFieldCatalog? _minerals;
    private bool _atRefinery;
    /// <summary>One id per WorldSimulation: pickup ids and mineral placement key on it, never on the sector instance (review R1-1).</summary>
    private string _worldInstanceId = "";
    private int _smokeWorlds;
    private long _lastShipSaveTick;
    private const int ShipSaveIntervalTicks = WorldSimulation.TickRate * 10;
    private KillRewardDefinition _killReward = KillRewardDefinition.None;
    private string _sectorInstanceId = "";
    private int _killsSeen;
    private readonly List<(string SectorInstanceId, TargetKill Kill)> _pendingKills = [];
    private long _nextKillRetryTick;
    private ulong _noticeExpiresMs;
    // Sector state outlives every world (owner rule, Nov 2 2026): it is saved before any reward leaves it for a profile.
    private SectorStateStore? _sectorStore;
    private WorldSimulation? _sectorSavedWorld;
    private int _sectorSavedPickups;
    private int _sectorSavedKills;
    private long _sectorSavedTick;
    /// <summary>A sector save flushes about 90 KB to disk (median ~5 ms, worst cases tens of ms), so pickups and kills share at
    /// most one save every half second; their rewards wait for it (review R1-3).</summary>
    private const int SectorSaveIntervalTicks = WorldSimulation.TickRate / 2;
    private IReadOnlyList<PendingPickupGrant> _otherPilotsPickups = [];
    private IReadOnlyList<PendingKillGrant> _otherPilotsKills = [];
    private const ulong NoticeDurationMs = 5000;

    public override void _Ready()
    {
        try
        {
            FlightInput.Initialize();
            _catalog = GameCatalog.FromJson(
                ReadDefinition("ships/starter_interceptor.json"),
                ReadDefinition("weapons/plasma_cannon.json"),
                ReadDefinition("sectors/first_contact.json"));
            _stationDefinition = StationDefinition.FromJson(ReadDefinition("stations/trading_outpost.json"));
            _cargo = _stationDefinition.CreateCargoCatalog(_catalog.Ship.Cargo.Capacity);
            _killReward = KillRewardDefinition.FromJson(ReadDefinition("rewards/kill_reward.json"));
            _minerals = MineralFieldCatalog.FromJson(ReadDefinition("minerals/mineral_spawns.json"));
            var userArgs = OS.GetCmdlineUserArgs();
            _profileRoot = ProjectSettings.GlobalizePath("user://profiles");
            foreach (var arg in userArgs)
                if (arg.StartsWith("--profile-dir=", StringComparison.Ordinal)) _profileRoot = arg[14..];
            if (userArgs.Contains("--smoke-test"))
            {
                // Acceptance runs never touch the player's real save.
                _smokeProfileRoot = ProjectSettings.GlobalizePath($"user://smoke-profiles/{Guid.NewGuid():N}");
                _profileRoot = _smokeProfileRoot;
            }
            OpenProfile();
            _world = NewWorld(restoreSavedShip: true, loadSector: true);
            _originals = OriginalSpriteLibrary.LoadIfAvailable();
            _view = new WorldView { Originals = _originals, TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
            _view.Station = (new System.Numerics.Vector2(_stationDefinition.X, _stationDefinition.Y), _stationDefinition.DockRadius, _stationDefinition.Name);
            if (_stationDefinition.Refinery is { } refinery)
                _view.Refinery = (new System.Numerics.Vector2(refinery.X, refinery.Y), refinery.DockRadius, refinery.Name);
            AddChild(_view);
            _view.SetWorld(_world);
            _camera = new Camera2D { Enabled = true };
            AddChild(_camera);
            _hud = new FlightHud();
            _hud.PauseRequested += TogglePause;
            _hud.ResetRequested += ResetSector;
            _hud.QuitRequested += () => GetTree().Quit();
            _hud.OriginalContentRequested += () => SetInspectionVisible(true);
            _hud.ReloadProfileRequested += () => ReloadProfile();
            AddChild(_hud);
            _stationScreen = new StationScreen(_stationDefinition) { Visible = false };
            _stationScreen.SellRequested += (item, quantity) => StationTransaction(s => s.SellAsync(item, quantity, PlayerX, PlayerY), $"Traded {quantity} {_stationDefinition.NameOf(item)} for {quantity * (_stationDefinition.SellPrice(item) ?? 0):N0} credits.");
            _stationScreen.RefineRequested += (recipe, quantity) => StationTransaction(s => s.RefineAsync(recipe, quantity, PlayerX, PlayerY), $"Refined {quantity} {_stationDefinition.NameOf(_stationDefinition.Refinery!.Recipes.Single(r => r.Id == recipe).InputItemId)}.");
            _stationScreen.UpgradeRequested += upgrade => StationTransaction(s => s.BuyUpgradeAsync(upgrade, PlayerX, PlayerY), $"Installed {_stationDefinition.NameOf(upgrade)}.");
            _stationScreen.RepairRequested += repair => StationTransaction(s => s.RepairAsync(repair, PlayerX, PlayerY, _world.Player.Hull, _playCatalog.Ship.Defense.MaximumHull), null, repair);
            _stationScreen.CloseRequested += () => SetStationVisible(false);
            AddChild(_stationScreen);
            _hud.SetOriginalContentAvailable(_originals is not null);
            _inspector = new OriginalContentInspector(_originals) { Visible = false };
            _inspector.CloseRequested += () => SetInspectionVisible(false);
            AddChild(_inspector);
            _hud.Update(_world, _playCatalog);
            RefreshProfileViews();
            var args = userArgs;
            var smokeRequested = args.Contains("--smoke-test");
            var originalInputRequested = args.Contains("--verify-original-input");
            for (var i = 0; i < args.Length; i++)
                if (args[i] == "--capture" && i + 1 < args.Length) _capturePath = args[i + 1];
                else if (args[i].StartsWith("--capture=", StringComparison.Ordinal)) _capturePath = args[i][10..];
            if (args.Contains("--capture-controls")) TogglePause();
            if (args.Contains("--capture-station")) SetStationVisible(true);
            foreach (var arg in args)
                if (arg.StartsWith("--station-tab=", StringComparison.Ordinal) && int.TryParse(arg[14..], out var tab)) _stationScreen.ShowTab(tab);
            _verifyOriginals = args.Contains("--verify-original-content");
            if (args.Contains("--inspect-original-content") || _verifyOriginals) SetInspectionVisible(true);
            foreach (var arg in args)
                if (arg.StartsWith("--original-page=", StringComparison.Ordinal)) _inspector.ShowPage(arg[16..]);
            if (_verifyOriginals && (_originals is null || smokeRequested))
                throw new InvalidOperationException("Original resource verification requires staged content and cannot run a gameplay smoke.");
            if (originalInputRequested)
            {
                if (_originals is null || smokeRequested || _verifyOriginals || _capturePath is not null || _inspectionVisible)
                    throw new InvalidOperationException("Original input verification requires staged content and an isolated paused UI fixture.");
                _paused = true;
                _hud.SetMenuVisible(true);
            }
            _camera.Position = WorldView.ToGodot(_world.Player.Position) + new Vector2(0, -35);
            _initialized = true;
            GD.Print($"CLIENT_READY: {_catalog.Sector.Name}; {WorldSimulation.TickRate} Hz fixed simulation; {_world.Targets.Count} targets; {_world.Asteroids.Count} asteroids.");
            GD.Print($"CLIENT_PROFILE: {_profileStatus}");
            if (smokeRequested) RunInputSmokeTest();
            else if (originalInputRequested) RunOriginalInputCheck();
        }
        catch (Exception exception)
        {
            GD.PushError($"CLIENT_STARTUP_FAILED: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized) return;
        try
        {
            if (_inspectionVisible || _stationVisible)
            {
                _view.InputState = default;
                _world.ElapseWithoutTicking(1); // the target respawn timer keeps running while docked or inspecting
                return;
            }
            if (_smokeTest is not null)
            {
                // Acceptance mode accelerates elapsed wall time while retaining identical 60 Hz ticks.
                for (var i = 0; i < 8 && !_smokeTest.IsComplete; i++) _smokeTest.Step();
                if (_world != _smokeTest.World)
                {
                    _world = _smokeTest.World;
                    _view.SetWorld(_world);
                }
                if (_smokeTest.IsComplete) GetTree().Quit(0);
            }
            else if (!_paused)
            {
                var input = FlightInput.Capture();
                _world.Tick(input);
                _view.InputState = input;
                CommitPickups();
                CommitKills();
                if (_world.TickNumber - _lastShipSaveTick >= ShipSaveIntervalTicks) SaveShip();
            }
            else
            {
                _view.InputState = default;
                _world.ElapseWithoutTicking(1); // the target respawn timer keeps running while paused
            }
        }
        catch (Exception exception)
        {
            GD.PushError($"CLIENT_SMOKE_FAILED: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    public override void _Process(double delta)
    {
        if (!_initialized) return;
        _camera.Position = WorldView.ToGodot(_world.Player.Position) + new Vector2(0, -35);
        _hud.Update(_world, _playCatalog);
        RefreshProfileViews();
        if (_noticeExpiresMs != 0 && Time.GetTicksMsec() >= _noticeExpiresMs)
        {
            _noticeExpiresMs = 0;
            _hud.SetNotice("");
        }
        _renderedFrames++;
        if (_verifyOriginals && _renderedFrames == 30)
        {
            try
            {
                _originals!.VerifyPackagedResources();
                if (_world.TickNumber != _inspectionStartTick) throw new InvalidOperationException("Original inspector advanced the world.");
                GD.Print($"ORIGINAL_INSPECTION_PASS: page={_inspector.Page}; world ticks unchanged at {_world.TickNumber}.");
                if (_capturePath is null) GetTree().Quit(0);
            }
            catch (Exception exception)
            {
                GD.PushError($"ORIGINAL_INSPECTION_FAILED: {exception.Message}");
                GetTree().Quit(1);
                return;
            }
        }
        if (_capturePath is not null && !_captureStarted && _renderedFrames >= 30)
        {
            _captureStarted = true;
            CaptureFrame();
        }
    }

    public override void _Input(InputEvent input)
    {
        if (!_initialized || input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (_stationVisible)
        {
            if (key.PhysicalKeycode == Key.Escape || input.IsActionPressed("flight_pause") || input.IsActionPressed("station_dock"))
            {
                SetStationVisible(false);
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (_inspectionVisible)
        {
            if (key.PhysicalKeycode == Key.Escape || input.IsActionPressed("flight_pause") || input.IsActionPressed("original_content"))
            {
                SetInspectionVisible(false);
                GetViewport().SetInputAsHandled();
            }
            // Other keys must reach the inspector's native numeric editors.
            return;
        }
        if (_hud.IsCapturingKey)
        {
            _hud.CaptureKey(key);
            GetViewport().SetInputAsHandled();
        }
        else if (input.IsActionPressed("original_content"))
        {
            SetInspectionVisible(true);
            GetViewport().SetInputAsHandled();
        }
        else if (input.IsActionPressed("flight_pause"))
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
        }
        else if (input.IsActionPressed("station_dock") && !_paused)
        {
            SetStationVisible(true);
            GetViewport().SetInputAsHandled();
        }
        else if (input.IsActionPressed("flight_reset"))
        {
            ResetSector();
            GetViewport().SetInputAsHandled();
        }
    }

    private void SetInspectionVisible(bool visible)
    {
        _inspectionVisible = visible;
        _inspector.Visible = visible;
        _hud.Visible = !visible;
        if (visible) _inspectionStartTick = _world.TickNumber;
    }

    private void TogglePause()
    {
        _paused = !_paused;
        if (_paused) SaveShip();
        _hud.SetMenuVisible(_paused);
    }

    private void ResetSector()
    {
        // A reset only puts the player's ship back at the spawn. The sector itself (targets, their respawn timer, piles, mineral
        // field) carries on exactly as it was, so resetting cannot re-arm kill rewards or refill cargo (owner rule, Nov 2 2026).
        // The ship keeps its hull damage too, so a reset is not a free repair (Steve, Nov 3 2026). A destroyed ship (hull 0)
        // still respawns with a full hull, as before (NEW/provisional).
        var hull = _world.Player.IsDestroyed ? 0 : _world.Player.Hull;
        _world = NewWorld(restoreSavedShip: false, loadSector: false);
        if (hull > 0) _world.RestorePlayer(_world.Player.Position, _world.Player.Heading, hull);
        _view.SetWorld(_world);
        _paused = false;
        _hud.SetMenuVisible(false);
        SaveShip();
    }

    // ---- Profile and station (queue M8) ------------------------------------------------------------------

    private float PlayerX => _world.Player.Position.X;
    private float PlayerY => _world.Player.Position.Y;
    public StationProfileSession? Session => _session;
    public StationScreen Station => _stationScreen;
    public bool StationVisible => _stationVisible;
    public bool AtRefinery => _stationVisible && _atRefinery;
    public string ProfileStatus => _profileStatus;
    public bool Paused => _paused;

    /// <summary>Opens the single-owner local save and the authoritative station services; failures leave services offline.</summary>
    private void OpenProfile()
    {
        _session = null;
        try
        {
            _repository = FileProfileRepository.Open(_profileRoot, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn);
            // The sector file shares the save folder's single-owner lock: a second client never reads or writes it.
            _sectorStore = new SectorStateStore(_profileRoot);
            var session = new StationProfileSession(_repository, _stationDefinition, _catalog.Ship.Id, _catalog.Ship.Cargo.Capacity) { KillReward = _killReward };
            var outcome = Await(session.OpenOrCreateAsync(LocalProfileId));
            _profileOutcome = outcome.Message;
            if (outcome.Succeeded) _session = session;
        }
        catch (ProfileRepositoryLockedException)
        {
            _profileOutcome = "Another client owns this save folder; station services are offline.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _profileOutcome = $"Save folder unavailable ({exception.Message}); station services are offline.";
        }
        _profileStatus = _profileOutcome;
    }

    /// <summary>Exit/relaunch boundary used by the smoke test: drops every in-memory service and reopens the save folder.</summary>
    public void ReopenProfileFromDisk()
    {
        if (_stationVisible) SetStationVisible(false); // undocking saves the ship; otherwise reopen exactly what is on disk
        // What a quit does (see _ExitTree), then forget everything held in memory, as a relaunched process would.
        CommitKills(force: true);
        CommitPickups(force: true);
        SaveSector();
        // Drop the old world before the observers restart, so NewWorld's final commits cannot re-observe its pickups and kills
        // from sequence 0 (review R1-2); everything owed comes back from the sector file instead.
        _world = null!;
        _pickups.StartNewWorld();
        _pendingKills.Clear();
        _killsSeen = 0;
        _repository?.Dispose();
        _repository = null;
        _sectorStore = null;
        OpenProfile();
        _world = NewWorld(restoreSavedShip: true, loadSector: true);
        _view.SetWorld(_world);
    }

    public void ReloadProfile()
    {
        if (_session is null)
        {
            ReopenProfileFromDisk();
            return;
        }
        var outcome = Await(_session.ReloadAsync());
        _profileOutcome = _profileStatus = outcome.Message;
        if (!outcome.Succeeded) _session = null;
        // Unsaved pickups stay queued (and in the hold) and are granted with their own ids, so a reload loses nothing.
        ReconcileHold();
        ApplyUpgrades();
    }

    /// <summary>Ship definition with the profile's active upgrades applied (NEW/provisional effects, see UpgradeEffects).</summary>
    private GameCatalog EffectiveCatalog() => new(
        UpgradeEffects.Apply(_catalog.Ship, _stationDefinition, _session?.Profile?.Upgrades ?? Enumerable.Empty<string>()),
        _catalog.Weapon, _catalog.Sector);

    /// <summary>Tells the player when cargo collected in a sector could not be saved before that sector instance ended.</summary>
    private void WarnLostPickups(IReadOnlyList<CargoPickup> lost, string why)
    {
        if (lost.Count == 0) return;
        var units = lost.Sum(p => p.Quantity);
        var names = string.Join(", ", lost.GroupBy(p => p.ItemId).Select(g => $"{_stationDefinition.NameOf(g.Key)} x{g.Sum(p => p.Quantity)}"));
        var message = $"{units} collected cargo unit{(units == 1 ? "" : "s")} {why} ({names}).";
        _profileStatus = $"{_profileStatus}\n{message}";
        Notice(message, warning: true);
    }

    private void Notice(string text, bool warning = false)
    {
        _hud?.SetNotice(text, warning);
        _noticeExpiresMs = Time.GetTicksMsec() + NoticeDurationMs;
    }

    /// <summary>
    /// Pays the kill reward for every target the player destroyed, oldest first, through the idempotent transaction core.
    /// On a refusal the kill stays queued and is retried once a second with the same transaction id, so a retry after a lost
    /// acknowledgement is recognised as a duplicate and never paid twice.
    /// </summary>
    private void CommitKills(bool force = false)
    {
        if (_world is null) return;
        if (!SaveSectorIfChanged(force)) return; // a kill is paid only once the sector that lost the target is saved
        if (_session?.Profile is null || _pendingKills.Count == 0 || (!force && _world.TickNumber < _nextKillRetryTick)) return;
        while (_pendingKills.Count > 0)
        {
            var (instance, kill) = _pendingKills[0];
            var result = Await(_session.GrantKillRewardAsync(instance, kill));
            if (result is null || result.Status == TransactionStatus.Applied || result.Reason == RejectionReason.TransactionIdReused)
            {
                _pendingKills.RemoveAt(0);
                if (result is { Status: TransactionStatus.Applied, IsReplay: false })
                    Notice($"Target destroyed: +{result.CurrencyDelta:N0} credits (saved at revision {result.Revision}).");
                continue;
            }
            Notice($"Kill reward is not saved yet ({result.Reason}); retrying.", warning: true);
            _nextKillRetryTick = _world.TickNumber + WorldSimulation.TickRate;
            if (result.Reason is RejectionReason.PersistenceFailed or RejectionReason.PersistenceConflict or RejectionReason.ProfileNotOpen)
            {
                var reload = Await(_session.ReloadAsync());
                if (!reload.Succeeded) _profileOutcome = reload.Message;
            }
            return;
        }
    }

    public int PendingKillRewards => _pendingKills.Count;
    public string SectorInstanceId => _sectorInstanceId;

    /// <summary>
    /// Builds a new world that continues the sector (owner rule, November 2, 2026: the world keeps its state for as long as the
    /// server runs). A reset (<paramref name="loadSector"/> false) carries the live world's sector over; a launch or reopen
    /// loads the saved sector from the save folder, lets the target respawn timer run for the real time that passed since it
    /// was saved, and requeues the rewards that were taken from it but not yet saved to the profile. Only a save folder with
    /// no sector file (or an unreadable one, which is quarantined) starts a fresh sector with a new sector instance id.
    /// </summary>
    private WorldSimulation NewWorld(bool restoreSavedShip, bool loadSector)
    {
        // Give the outgoing world's unpaid kills and unsaved pickups one last try; whatever is still unsaved stays queued.
        CommitKills(force: true);
        CommitPickups(force: true);
        SectorSnapshot? sector = null;
        var offline = TimeSpan.Zero;
        if (!loadSector && _world is not null) sector = _world.CaptureSector();
        else if (LoadSector() is { } saved)
        {
            sector = saved.Sector;
            _sectorInstanceId = saved.SectorInstanceId;
            offline = _sectorStore!.UtcNow - saved.SavedAtUtc;
            _pickups.Restore(saved.PendingPickups.Where(p => p.ProfileId == LocalProfileId).Select(p => (p.WorldInstanceId, p.Pickup)));
            foreach (var kill in saved.PendingKills.Where(k => k.ProfileId == LocalProfileId))
                if (!_pendingKills.Contains((kill.SectorInstanceId, kill.Kill))) _pendingKills.Add((kill.SectorInstanceId, kill.Kill));
            _otherPilotsPickups = saved.PendingPickups.Where(p => p.ProfileId != LocalProfileId).ToArray();
            _otherPilotsKills = saved.PendingKills.Where(k => k.ProfileId != LocalProfileId).ToArray();
        }
        else
        {
            _sectorInstanceId = Guid.NewGuid().ToString("N");
            _otherPilotsPickups = [];
            _otherPilotsKills = [];
        }
        _pickups.ContinueInNewWorld();
        _nextPickupRetryTick = _nextPickupRetryTick == long.MaxValue ? long.MaxValue : 0;
        _nextKillRetryTick = 0;
        _killsSeen = 0;
        _playCatalog = EffectiveCatalog();
        _lastShipSaveTick = 0;
        // Pickup ids key on a new id for every world, never on the sector instance (pickup sequences restart per world, R1-1).
        // The id also seeds the mineral placement of a fresh sector; a continued sector keeps its own random state. The salt
        // comes from the id's own bytes, so a logged world id replays its placement. Smoke runs number their worlds instead of
        // drawing random ids, so every acceptance run lays the field out the same way.
        var worldId = _smokeProfileRoot is null ? Guid.NewGuid() : SmokeWorldId(++_smokeWorlds);
        _worldInstanceId = worldId.ToString("N");
        var minerals = _minerals?.ForWorld(BitConverter.ToUInt64(worldId.ToByteArray(), 0));
        // The hold is the saved inventory plus the pickups still owed to the profile.
        var hold = _pickups.HoldContents(new Dictionary<string, int>(AuthoritativeHold()), _session?.Profile?.AppliedTransactions.Select(t => t.TransactionId));
        var world = new WorldSimulation(_playCatalog, _cargo, hold, minerals: minerals);
        if (sector is not null && !world.RestoreSector(sector))
            Notice("The saved sector belongs to another sector and was not used.", warning: true);
        // Real time keeps running toward a pending target respawn while nobody is in the sector (owner rule, Nov 1 2026).
        if (offline > TimeSpan.Zero) world.ElapseWithoutTicking((int)Math.Min(int.MaxValue, Math.Ceiling(offline.TotalSeconds * WorldSimulation.TickRate)));
        _sectorSavedWorld = null;
        if (restoreSavedShip && _session?.Profile?.Ship is { } ship)
            world.RestorePlayer(new System.Numerics.Vector2(ship.X, ship.Y), ship.Heading, ship.Hull);
        return world;
    }

    /// <summary>
    /// After an upgrade purchase or reload: swaps the player's statistics in place, so piles, targets, position and a destroyed
    /// ship's state are untouched (review R2-2).
    /// </summary>
    private void ApplyUpgrades()
    {
        _playCatalog = EffectiveCatalog();
        _world.ApplyPlayerDefinition(_playCatalog.Ship);
    }

    /// <summary>The saved sector, or null (with a notice when it was unreadable) when the sector must start afresh.</summary>
    private SectorStateDocument? LoadSector()
    {
        if (_sectorStore is null) return null;
        try
        {
            var result = _sectorStore.Load(_catalog.Sector.Id);
            if (result.Status is SectorStateLoadStatus.Corrupt or SectorStateLoadStatus.RecoveredFromBackup)
            {
                _profileStatus = $"{_profileStatus}\n{result.Message}";
                Notice(result.Message, warning: true);
            }
            return result.Document;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Notice($"Sector state could not be read ({exception.Message}); the sector starts afresh.", warning: true);
            return null;
        }
    }

    /// <summary>Queues the pickups and kills the world recorded since the last call.</summary>
    private void ObserveWorld()
    {
        _pickups.Observe(_worldInstanceId, _world.Pickups);
        for (; _killsSeen < _world.Kills.Count; _killsSeen++) _pendingKills.Add((_sectorInstanceId, _world.Kills[_killsSeen]));
    }

    /// <summary>
    /// Saves the sector when the world has recorded a pickup or kill since the last sector save. Rewards are granted only after
    /// this returns true, so a crash between the two saves can neither refill the sector nor lose the reward (it is requeued
    /// from the sector file). Without a save folder nothing can be granted, so there is nothing to protect.
    /// </summary>
    private bool SaveSectorIfChanged(bool force = false)
    {
        if (_world is null) return true;
        ObserveWorld();
        if (ReferenceEquals(_sectorSavedWorld, _world) && _sectorSavedPickups == _world.Pickups.Count && _sectorSavedKills == _world.Kills.Count)
            return true;
        // Coalesce: inside the interval the new rewards simply wait (they are in the hold and queued) for the next save.
        if (!force && ReferenceEquals(_sectorSavedWorld, _world) && _world.TickNumber - _sectorSavedTick < SectorSaveIntervalTicks)
            return false;
        return SaveSector();
    }

    /// <summary>Writes the sector state (on every pickup and kill, pause, dock, undock, reset, quit and every ten seconds of flight).</summary>
    private bool SaveSector()
    {
        if (_world is null || _sectorStore is null) return _sectorStore is null;
        ObserveWorld();
        try
        {
            _sectorStore.Save(new SectorStateDocument(_sectorInstanceId, _sectorStore.UtcNow, _world.CaptureSector(),
                _otherPilotsPickups.Concat(_pickups.Pending.Select(p => new PendingPickupGrant(LocalProfileId, p.WorldInstanceId, p.Pickup))).ToArray(),
                _otherPilotsKills.Concat(_pendingKills.Select(k => new PendingKillGrant(LocalProfileId, k.SectorInstanceId, k.Kill))).ToArray()));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _profileStatus = $"Sector state could not be saved ({exception.Message}); rewards wait until it is.";
            return false;
        }
        _sectorSavedWorld = _world;
        _sectorSavedPickups = _world.Pickups.Count;
        _sectorSavedKills = _world.Kills.Count;
        _sectorSavedTick = _world.TickNumber;
        return true;
    }

    /// <summary>Saves the sector, then hull and position to the profile (on pause, dock, undock, repair, reset, quit and every ten seconds of flight).</summary>
    public void SaveShip()
    {
        SaveSector();
        if (_session?.Profile is null || _world is null) return;
        var player = _world.Player;
        var result = Await(_session.SaveShipAsync(player.Hull, player.Position.X, player.Position.Y, player.Heading));
        if (!result.Saved) _profileStatus = $"Ship condition could not be saved ({result.Reason}).";
        _lastShipSaveTick = _world.TickNumber;
    }

    private IEnumerable<KeyValuePair<string, int>> AuthoritativeHold() =>
        (_session?.Profile?.Inventory ?? Enumerable.Empty<KeyValuePair<string, int>>())
            .Where(entry => _cargo.IsKnown(entry.Key) && entry.Value > 0);

    /// <summary>The world hold is always the saved inventory plus the pickups still waiting to be saved.</summary>
    private void ReconcileHold() =>
        _world.Cargo.ReplaceContents(_pickups.HoldContents(new Dictionary<string, int>(AuthoritativeHold()),
            _session?.Profile?.AppliedTransactions.Select(t => t.TransactionId)).ToArray());

    /// <summary>
    /// Saves every pickup the world recorded (queue M7 mining slice) through the idempotent transaction core, one grant per
    /// pickup with a stable id. On a refusal the pickup stays queued and in the hold and is retried once a second (after
    /// reloading the profile when the save itself failed), so neither a failed save nor a reload loses or duplicates cargo.
    /// </summary>
    private void CommitPickups(bool force = false)
    {
        if (_world is null) return;
        if (!SaveSectorIfChanged(force)) return; // cargo is granted only once the sector that lost the pile is saved
        if (_session?.Profile is null || _pickups.Count == 0 || _nextPickupRetryTick == long.MaxValue
            || (!force && _world.TickNumber < _nextPickupRetryTick)) return;
        var outcome = Await(_pickups.CommitAsync(_session));
        if (outcome.Replayed.Count > 0 || outcome.Refused.Count > 0) ReconcileHold();
        WarnLostPickups(outcome.Refused.Select(r => r.Pickup).ToArray(), $"could not be stored ({string.Join(", ", outcome.Refused.Select(r => r.Reason).Distinct())})");
        var mined = outcome.Applied.Where(p => p.IsMineral).ToArray();
        if (mined.Length > 0)
            Notice($"Mined {string.Join(", ", mined.GroupBy(p => p.ItemId).Select(g => $"{g.Sum(p => p.Quantity)} {_stationDefinition.NameOf(g.Key)}"))} (saved at revision {_session.Profile!.Revision}).");
        if (outcome.Blocked is not { } reason)
        {
            if (_nextPickupRetryTick > 0) _profileStatus = _profileOutcome;
            _nextPickupRetryTick = 0;
            return;
        }
        _profileStatus = $"Collected cargo is not saved yet ({reason}); it stays in the hold and saving is retried.";
        _nextPickupRetryTick = _world.TickNumber + WorldSimulation.TickRate;
        if (reason is RejectionReason.PersistenceFailed or RejectionReason.PersistenceConflict or RejectionReason.ProfileNotOpen)
        {
            var reload = Await(_session.ReloadAsync());
            if (!reload.Succeeded) _profileOutcome = reload.Message;
        }
    }

    public int PendingPickups => _pickups.Count;
    public string WorldInstanceId => _worldInstanceId;

    /// <summary>Fixed world ids for smoke runs; each smoke run uses a fresh profile, so they never repeat inside one save.</summary>
    private static Guid SmokeWorldId(int number) => new(number, 0x534D, 0x4B45, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>Test hook: makes the next pickup commits fail as if the save folder were unavailable.</summary>
    public void SimulatePickupSaveFailure() => _nextPickupRetryTick = long.MaxValue;
    public void ClearPickupSaveFailure() => _nextPickupRetryTick = 0;

    private void SetStationVisible(bool visible)
    {
        if (visible)
        {
            if (_session?.Profile is null)
            {
                _profileStatus = $"Station services are offline: {_profileOutcome}";
                return;
            }
            _atRefinery = !_session.IsDocked(PlayerX, PlayerY) && _session.IsDockedAtRefinery(PlayerX, PlayerY);
            if (!_atRefinery && !_session.IsDocked(PlayerX, PlayerY)) return;
            _stationScreen.SetRefineryMode(_atRefinery);
            _stationScreen.SetStatus($"Welcome to the {(_atRefinery ? _stationDefinition.Refinery!.Name : _stationDefinition.Name)}.");
        }
        _stationVisible = visible;
        _stationScreen.Visible = visible;
        SaveShip();
        _hud.Visible = !visible && !_inspectionVisible;
        RefreshProfileViews(rebuildStation: true);
    }

    private void StationTransaction(Func<StationProfileSession, Task<TransactionResult>> run, string? success, string? repairId = null)
    {
        if (_session is null) return;
        var result = Await(run(_session));
        if (result.Status == TransactionStatus.Applied)
        {
            if (repairId is not null)
            {
                var restored = _world.RepairPlayerHull(_session.RepairHullPoints(repairId));
                success = $"{_stationDefinition.NameOf(repairId)}: {restored:0} hull restored.";
                SaveShip();
            }
            if (result.UpgradeId is not null) ApplyUpgrades();
            _stationScreen.SetStatus($"{success}  Saved at revision {result.Revision}.");
        }
        else
        {
            _stationScreen.SetStatus($"Refused: {Describe(result.Reason)}", error: true);
            if (result.Reason is RejectionReason.PersistenceFailed or RejectionReason.PersistenceConflict) ReloadProfile();
        }
        ReconcileHold();
        RefreshProfileViews(rebuildStation: true);
    }

    private string? BlockedUpgradeText(string upgradeId) => _session?.UpgradeRefusal(upgradeId) switch
    {
        RejectionReason.UpgradeHasNoEffect => "no effect yet",
        RejectionReason.UpgradeSlotHeld => $"slot held by {_session!.SlotHolder(upgradeId)?.Name ?? "an installed upgrade"}",
        _ => null,
    };

    private static string Describe(RejectionReason reason) => reason switch
    {
        RejectionReason.InsufficientFunds => "not enough credits.",
        RejectionReason.InsufficientInventory => "not enough cargo to sell.",
        RejectionReason.CargoCapacityExceeded => "the hold is full.",
        RejectionReason.UpgradeAlreadyOwned => "upgrade already installed.",
        RejectionReason.StationAccessDenied => "you are no longer docked.",
        RejectionReason.NotForSale => "not traded here.",
        RejectionReason.UpgradeHasNoEffect => "this upgrade has no effect yet; nothing was charged.",
        RejectionReason.UpgradeSlotHeld => "an installed upgrade already holds that slot; nothing was charged.",
        RejectionReason.NothingToRepair => "the hull is already at maximum (or the ship is destroyed); nothing was charged.",
        _ => reason.ToString(),
    };

    private void RefreshProfileViews(bool rebuildStation = false)
    {
        var profile = _session?.Profile;
        var docked = profile is not null && (_session!.IsDocked(PlayerX, PlayerY) || _session.IsDockedAtRefinery(PlayerX, PlayerY));
        _hud.UpdateProfile(profile, _session?.IsDocked(PlayerX, PlayerY) == false && _stationDefinition.Refinery is { } site ? site.Name : _stationDefinition.Name,
            _stationDefinition, _world.Cargo.TotalWeight, _world.Cargo.Capacity, docked && !_stationVisible,
            $"{_profileStatus}\nSave folder: {_profileRoot}");
        if (rebuildStation && _stationVisible && profile is not null)
            _stationScreen.Refresh(profile, _world.Cargo.TotalWeight, _world.Cargo.Capacity, _world.Player.Hull, _playCatalog.Ship.Defense.MaximumHull,
                UpgradeEffects.Active(_stationDefinition, profile.Upgrades).Select(u => u.Id).ToHashSet(), BlockedUpgradeText);
    }

    /// <summary>The local services do file I/O without capturing a context, so blocking here cannot deadlock the main thread.</summary>
    private static T Await<T>(Task<T> task) => task.GetAwaiter().GetResult();

    public override void _ExitTree()
    {
        if (_initialized && _smokeTest is null)
        {
            CommitKills(force: true);
            CommitPickups(force: true);
            SaveShip();
        }
        _repository?.Dispose();
        _repository = null;
        if (_smokeProfileRoot is not null && DirAccess.DirExistsAbsolute(_smokeProfileRoot))
            try { Directory.Delete(_smokeProfileRoot, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static string ReadDefinition(string relativePath)
    {
        var path = $"res://GameData/{relativePath}";
        if (!Godot.FileAccess.FileExists(path)) throw new InvalidOperationException($"Missing packaged game definition: {path}. Build the C# project to stage JSON data.");
        return Godot.FileAccess.GetFileAsString(path);
    }

    private async void CaptureFrame()
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.PushError("CAPTURE_FAILED: a rendered display is required for screenshot capture.");
            GetTree().Quit(1);
            return;
        }
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = GetViewport().GetTexture().GetImage();
        var error = image.SavePng(_capturePath!);
        if (error == Error.Ok) GD.Print($"CLIENT_CAPTURE_SAVED: {_capturePath}");
        else GD.PushError($"CAPTURE_FAILED: {error}");
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }
    private async void RunOriginalInputCheck()
    {
        try
        {
            var check = new OriginalContentInputCheck(_inspector, _originals!, () => _inspectionVisible,
                () => _world.TickNumber, () => _paused, NextPhysicsFrame, () => _hud.ControlsVisible);
            GetTree().Quit(await check.RunAsync() ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"ORIGINAL_INPUT_FAILED: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private async void RunInputSmokeTest()
    {
        var inputTest = new ClientInputSmokeTest(_catalog, _hud, () => _world, () => _paused, ResetSector, NextPhysicsFrame);
        if (!await inputTest.RunAsync())
        {
            GetTree().Quit(1);
            return;
        }
        var stationTest = new ClientStationSmokeTest(this, _hud, () => _world, ResetSector, NextPhysicsFrame);
        if (!await stationTest.RunAsync())
        {
            GetTree().Quit(1);
            return;
        }
        _smokeTest = new ClientSmokeTest(_catalog) { Report = message => GD.Print(message) };
        _world = _smokeTest.World;
        _view.SetWorld(_world);
    }
    private async Task NextPhysicsFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

}

