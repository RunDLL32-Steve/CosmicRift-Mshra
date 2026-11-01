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

    public override void _Ready()
    {
        try
        {
            FlightInput.Initialize();
            _catalog = GameCatalog.FromJson(
                ReadDefinition("ships/starter_interceptor.json"),
                ReadDefinition("weapons/plasma_cannon.json"),
                ReadDefinition("sectors/first_contact.json"));
            _stationDefinition = StationDefinition.FromJson(ReadDefinition("stations/first_contact_station.json"));
            _cargo = _stationDefinition.CreateCargoCatalog();
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
            _world = NewWorld();
            _originals = OriginalSpriteLibrary.LoadIfAvailable();
            _view = new WorldView { Originals = _originals, TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
            _view.Station = (new System.Numerics.Vector2(_stationDefinition.X, _stationDefinition.Y), _stationDefinition.DockRadius, _stationDefinition.Name);
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
            _stationScreen.BuyRequested += (item, quantity) => StationTransaction(s => s.BuyAsync(item, quantity, PlayerX, PlayerY), $"Bought {quantity} {_stationDefinition.NameOf(item)}.");
            _stationScreen.SellRequested += (item, quantity) => StationTransaction(s => s.SellAsync(item, quantity, PlayerX, PlayerY), $"Sold {quantity} {_stationDefinition.NameOf(item)}.");
            _stationScreen.UpgradeRequested += upgrade => StationTransaction(s => s.BuyUpgradeAsync(upgrade, PlayerX, PlayerY), $"Installed {_stationDefinition.NameOf(upgrade)}.");
            _stationScreen.RepairRequested += repair => StationTransaction(s => s.RepairAsync(repair, PlayerX, PlayerY), null, repair);
            _stationScreen.CloseRequested += () => SetStationVisible(false);
            AddChild(_stationScreen);
            _hud.SetOriginalContentAvailable(_originals is not null);
            _inspector = new OriginalContentInspector(_originals) { Visible = false };
            _inspector.CloseRequested += () => SetInspectionVisible(false);
            AddChild(_inspector);
            _hud.Update(_world, _catalog);
            RefreshProfileViews();
            var args = userArgs;
            var smokeRequested = args.Contains("--smoke-test");
            var originalInputRequested = args.Contains("--verify-original-input");
            for (var i = 0; i < args.Length; i++)
                if (args[i] == "--capture" && i + 1 < args.Length) _capturePath = args[i + 1];
                else if (args[i].StartsWith("--capture=", StringComparison.Ordinal)) _capturePath = args[i][10..];
            if (args.Contains("--capture-controls")) TogglePause();
            if (args.Contains("--capture-station")) SetStationVisible(true);
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
            if (_inspectionVisible || _stationVisible) { _view.InputState = default; return; }
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
            }
            else _view.InputState = default;
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
        _hud.Update(_world, _catalog);
        RefreshProfileViews();
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
        _hud.SetMenuVisible(_paused);
    }

    private void ResetSector()
    {
        _world = NewWorld();
        _view.SetWorld(_world);
        _paused = false;
        _hud.SetMenuVisible(false);
    }

    // ---- Profile and station (queue M8) ------------------------------------------------------------------

    private float PlayerX => _world.Player.Position.X;
    private float PlayerY => _world.Player.Position.Y;
    public StationProfileSession? Session => _session;
    public StationScreen Station => _stationScreen;
    public bool StationVisible => _stationVisible;
    public string ProfileStatus => _profileStatus;
    public bool Paused => _paused;

    /// <summary>Opens the single-owner local save and the authoritative station services; failures leave services offline.</summary>
    private void OpenProfile()
    {
        _session = null;
        try
        {
            _repository = FileProfileRepository.Open(_profileRoot, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn);
            var session = new StationProfileSession(_repository, _stationDefinition, _catalog.Ship.Id);
            var outcome = Await(session.OpenOrCreateAsync(LocalProfileId));
            _profileStatus = outcome.Message;
            if (outcome.Succeeded) _session = session;
        }
        catch (ProfileRepositoryLockedException)
        {
            _profileStatus = "Another client owns this save folder; station services are offline.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _profileStatus = $"Save folder unavailable ({exception.Message}); station services are offline.";
        }
    }

    /// <summary>Exit/relaunch boundary used by the smoke test: drops every in-memory service and reopens the save folder.</summary>
    public void ReopenProfileFromDisk()
    {
        SetStationVisible(false);
        _repository?.Dispose();
        _repository = null;
        OpenProfile();
        ReconcileHold();
    }

    public void ReloadProfile()
    {
        if (_session is null)
        {
            ReopenProfileFromDisk();
            return;
        }
        var outcome = Await(_session.ReloadAsync());
        _profileStatus = outcome.Message;
        if (!outcome.Succeeded) _session = null;
        ReconcileHold();
    }

    private WorldSimulation NewWorld() => new(_catalog, _cargo, AuthoritativeHold());

    private IEnumerable<KeyValuePair<string, int>> AuthoritativeHold() =>
        (_session?.Profile?.Inventory ?? Enumerable.Empty<KeyValuePair<string, int>>())
            .Where(entry => _cargo.IsKnown(entry.Key) && entry.Value > 0);

    /// <summary>The world hold always mirrors the saved inventory after a station transaction or reload.</summary>
    private void ReconcileHold() => _world.Cargo.ReplaceContents(AuthoritativeHold().ToArray());

    private void CommitPickups()
    {
        if (_session?.Profile is null) return;
        var result = Await(_session.CommitPickupsAsync(_world.Cargo.Items));
        if (result is null) return;
        if (result.Status != TransactionStatus.Applied)
        {
            _profileStatus = $"Pickup could not be saved ({result.Reason}); cargo restored to the saved hold.";
            ReconcileHold();
        }
    }

    private void SetStationVisible(bool visible)
    {
        if (visible)
        {
            if (_session?.Profile is null)
            {
                _profileStatus = "Station services are offline: " + _profileStatus;
                return;
            }
            if (!_session.IsDocked(PlayerX, PlayerY)) return;
            _stationScreen.SetStatus($"Welcome to {_stationDefinition.Name}.");
        }
        _stationVisible = visible;
        _stationScreen.Visible = visible;
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
                var restored = _world.RepairPlayerHull(_session.RepairHullPoints(repairId, _catalog.Ship.Defense.MaximumHull));
                success = $"{_stationDefinition.NameOf(repairId)}: {restored:0} hull restored.";
            }
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

    private static string Describe(RejectionReason reason) => reason switch
    {
        RejectionReason.InsufficientFunds => "not enough credits.",
        RejectionReason.InsufficientInventory => "not enough cargo to sell.",
        RejectionReason.CargoCapacityExceeded => "the hold is full.",
        RejectionReason.UpgradeAlreadyOwned => "upgrade already installed.",
        RejectionReason.StationAccessDenied => "you are no longer docked.",
        RejectionReason.NotForSale => "not traded here.",
        _ => reason.ToString(),
    };

    private void RefreshProfileViews(bool rebuildStation = false)
    {
        var profile = _session?.Profile;
        var docked = profile is not null && _session!.IsDocked(PlayerX, PlayerY);
        _hud.UpdateProfile(profile, _stationDefinition, _world.Cargo.TotalWeight, _world.Cargo.Capacity, docked && !_stationVisible,
            $"{_profileStatus}\nSave folder: {_profileRoot}");
        if (rebuildStation && _stationVisible && profile is not null)
            _stationScreen.Refresh(profile, _world.Cargo.TotalWeight, _world.Cargo.Capacity, _world.Player.Hull, _catalog.Ship.Defense.MaximumHull);
    }

    /// <summary>The local services do file I/O without capturing a context, so blocking here cannot deadlock the main thread.</summary>
    private static T Await<T>(Task<T> task) => task.GetAwaiter().GetResult();

    public override void _ExitTree()
    {
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

