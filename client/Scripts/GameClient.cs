using Godot;
using RogueTrader.GameData;
using RogueTrader.Simulation;

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

    public override void _Ready()
    {
        try
        {
            FlightInput.Initialize();
            _catalog = GameCatalog.FromJson(
                ReadDefinition("ships/starter_interceptor.json"),
                ReadDefinition("weapons/plasma_cannon.json"),
                ReadDefinition("sectors/first_contact.json"));
            _world = new WorldSimulation(_catalog);
            _originals = OriginalSpriteLibrary.LoadIfAvailable();
            _view = new WorldView { Originals = _originals, TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
            AddChild(_view);
            _view.SetWorld(_world);
            _camera = new Camera2D { Enabled = true };
            AddChild(_camera);
            _hud = new FlightHud();
            _hud.PauseRequested += TogglePause;
            _hud.ResetRequested += ResetSector;
            _hud.QuitRequested += () => GetTree().Quit();
            _hud.OriginalContentRequested += () => SetInspectionVisible(true);
            AddChild(_hud);
            _hud.SetOriginalContentAvailable(_originals is not null);
            _inspector = new OriginalContentInspector(_originals) { Visible = false };
            _inspector.CloseRequested += () => SetInspectionVisible(false);
            AddChild(_inspector);
            _hud.Update(_world, _catalog);
            var args = OS.GetCmdlineUserArgs();
            var smokeRequested = args.Contains("--smoke-test");
            var originalInputRequested = args.Contains("--verify-original-input");
            for (var i = 0; i < args.Length; i++)
                if (args[i] == "--capture" && i + 1 < args.Length) _capturePath = args[i + 1];
                else if (args[i].StartsWith("--capture=", StringComparison.Ordinal)) _capturePath = args[i][10..];
            if (args.Contains("--capture-controls")) TogglePause();
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
            if (_inspectionVisible) { _view.InputState = default; return; }
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
        _world = new WorldSimulation(_catalog);
        _view.SetWorld(_world);
        _paused = false;
        _hud.SetMenuVisible(false);
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

