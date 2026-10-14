using Godot;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.Client;

/// <summary>Flight telemetry, objective hints, and a working persisted controls editor.</summary>
public partial class FlightHud : CanvasLayer
{
    public event Action? PauseRequested;
    public event Action? ResetRequested;
    public event Action? QuitRequested;
    public event Action? OriginalContentRequested;
    private Label _contentCaption = null!;
    private readonly Dictionary<string, Button> _bindingButtons = [];
    private Label _speed = null!;
    private Label _telemetry = null!;
    private Label _objective = null!;
    private Label _controls = null!;
    private Label _status = null!;
    private Label _bindingHint = null!;
    private ProgressBar _hull = null!;
    private ProgressBar _shield = null!;
    private ProgressBar _energy = null!;
    private Control _menu = null!;
    private string? _captureAction;
    public bool ControlsVisible => _menu.Visible;
    public bool IsCapturingKey => _captureAction is not null;
    private static readonly Color Teal = new("62dfd1");
    private static readonly Color TextColor = new("cedce5");
    private static readonly Color Muted = new("7997a9");

    public override void _Ready()
    {
        var root = new Control { Name = "Hud", MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        var left = Panel();
        left.Position = new Vector2(22, 20);
        left.CustomMinimumSize = new Vector2(310, 0);
        root.AddChild(left);
        var metrics = Column(left);
        metrics.AddChild(Label("ROGUE TRADER 2.0", 23, TextColor));
        _contentCaption = Label("FIRST CONTACT  /  PROVISIONAL INTERCEPTOR", 12, Teal);
        metrics.AddChild(_contentCaption);
        metrics.AddChild(new HSeparator());
        _hull = Meter(metrics, "HULL", new Color("e3b57d"));
        _shield = Meter(metrics, "SHIELD", Teal);
        _energy = Meter(metrics, "ENERGY", new Color("88a9f1"));
        _speed = Label("", 21, TextColor);
        metrics.AddChild(_speed);
        _telemetry = Label("", 12, Muted);
        metrics.AddChild(_telemetry);

        var right = Panel();
        right.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        right.OffsetLeft = -292;
        right.OffsetRight = -22;
        right.OffsetTop = 20;
        right.CustomMinimumSize = new Vector2(270, 0);
        root.AddChild(right);
        var mission = Column(right);
        mission.AddChild(Label("FLIGHT & COMBAT RANGE", 13, Teal));
        _objective = Label("", 16, TextColor);
        _objective.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        mission.AddChild(_objective);
        mission.AddChild(Label("Drift freely. Aim with your heading.\nYour velocity carries into plasma shots.", 12, Muted));
        var pause = Button("PAUSE / CONTROLS");
        pause.Pressed += () => PauseRequested?.Invoke();
        mission.AddChild(pause);
        var original = Button("ORIGINAL CONTENT  /  F2");
        original.Pressed += () => OriginalContentRequested?.Invoke();
        mission.AddChild(original);

        var bottom = Panel();
        bottom.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        bottom.OffsetLeft = 22;
        bottom.OffsetRight = -22;
        bottom.OffsetTop = -58;
        bottom.OffsetBottom = -20;
        root.AddChild(bottom);
        _controls = Label("", 13, Muted);
        bottom.AddChild(_controls);
        _controls.HorizontalAlignment = HorizontalAlignment.Center;
        _controls.VerticalAlignment = VerticalAlignment.Center;

        _status = Label("", 22, TextColor);
        _status.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        _status.OffsetLeft = -390;
        _status.OffsetRight = 390;
        _status.OffsetTop = -125;
        _status.OffsetBottom = -80;
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.VerticalAlignment = VerticalAlignment.Center;
        root.AddChild(_status);
        BuildMenu(root);
        RefreshBindings();
    }

    public void SetOriginalContentAvailable(bool available) =>
        _contentCaption.Text = available ? "ASP ART  /  PROVISIONAL FLIGHT RANGE" : "FIRST CONTACT  /  PROVISIONAL INTERCEPTOR";

    public void Update(WorldSimulation world, GameCatalog catalog)
    {
        var player = world.Player;
        _hull.MaxValue = catalog.Ship.Defense.MaximumHull;
        _hull.Value = player.Hull;
        _shield.MaxValue = Math.Max(1, catalog.Ship.Defense.MaximumShield);
        _shield.Value = player.Shield;
        _energy.MaxValue = Math.Max(1, catalog.Ship.Reactor.MaximumEnergy);
        _energy.Value = player.Energy;
        _speed.Text = $"{player.Velocity.Length():000.0}  u/s";
        _telemetry.Text = $"H {player.Hull:0}   S {player.Shield:0}   E {player.Energy:0}\n60 Hz  |  Tick {world.TickNumber:N0}  |  FPS {Engine.GetFramesPerSecond()}\nPosition  {player.Position.X:0}, {player.Position.Y:0}\n{catalog.Weapon.Name}  /  {world.Projectiles.Count} shots";
        var liveTargets = world.Targets.Count(target => !target.IsDestroyed);
        _objective.Text = liveTargets > 0
            ? $"{catalog.Sector.Name}\nDestroy the practice target.\n{liveTargets} target{(liveTargets == 1 ? "" : "s")} remaining"
            : $"{catalog.Sector.Name}\nTarget destroyed.\nPractice flight through the belt.";
        _status.Text = player.IsDestroyed ? $"SHIP DESTROYED  -  {FlightInput.Display("flight_reset")} to reset"
            : world.DestroyedTargets > 0 ? $"TARGET DESTROYED  /  {FlightInput.Display("flight_reset")} to restart the range" : "";
        _status.Modulate = player.IsDestroyed ? new Color("f4b66b") : TextColor;
    }

    public void SetMenuVisible(bool visible)
    {
        _menu.Visible = visible;
        _captureAction = null;
        _bindingHint.Text = "Click a binding, then press a new key. Changes save automatically.";
        RefreshBindings();
    }

    public void CaptureKey(InputEventKey keyEvent)
    {
        if (_captureAction is null || !keyEvent.Pressed || keyEvent.Echo) return;
        var action = _captureAction;
        _captureAction = null;
        if (keyEvent.PhysicalKeycode == Key.Escape && action != "flight_pause")
            _bindingHint.Text = "Binding change cancelled.";
        else
            _bindingHint.Text = FlightInput.TryRebind(action, keyEvent.PhysicalKeycode) ?? "Binding saved.";
        RefreshBindings();
    }

    private void BuildMenu(Control root)
    {
        _menu = new Control { Visible = false };
        _menu.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(_menu);
        var shade = new ColorRect { Color = new Color(0.015f, 0.03f, 0.05f, 0.88f), MouseFilter = Control.MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _menu.AddChild(shade);
        var box = Panel();
        box.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        box.OffsetLeft = -290;
        box.OffsetRight = 290;
        box.OffsetTop = -300;
        box.OffsetBottom = 300;
        _menu.AddChild(box);
        var column = Column(box);
        column.AddChild(Label("FLIGHT PAUSED", 26, TextColor));
        column.AddChild(Label("CONTROLS", 13, Teal));
        _bindingHint = Label("Click a binding, then press a new key. Changes save automatically.", 13, Muted);
        _bindingHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _bindingHint.CustomMinimumSize = new Vector2(0, 32);
        column.AddChild(_bindingHint);
        foreach (var binding in FlightInput.Bindings)
        {
            var row = new HBoxContainer();
            column.AddChild(row);
            var label = Label(binding.Label, 16, TextColor);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(label);
            var button = Button(FlightInput.Display(binding.Action));
            button.SetMeta("binding_action", binding.Action);
            button.CustomMinimumSize = new Vector2(185, 31);
            button.Pressed += () =>
            {
                _captureAction = binding.Action;
                _bindingHint.Text = $"Press a key for {binding.Label.ToLowerInvariant()}. Escape cancels.";
                button.Text = "PRESS A KEY...";
            };
            _bindingButtons.Add(binding.Action, button);
            row.AddChild(button);
        }
        var defaults = Button("RESTORE DEFAULT BINDINGS");
        defaults.Name = "RestoreDefaultBindings";
        defaults.Pressed += () =>
        {
            FlightInput.RestoreDefaults();
            _captureAction = null;
            _bindingHint.Text = "Default bindings restored and saved.";
            RefreshBindings();
        };
        column.AddChild(defaults);
        var actions = new HBoxContainer();
        column.AddChild(actions);
        var resume = Button("RESUME FLIGHT");
        resume.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        resume.Pressed += () => PauseRequested?.Invoke();
        actions.AddChild(resume);
        var reset = Button("RESET RANGE");
        reset.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        reset.Pressed += () => ResetRequested?.Invoke();
        actions.AddChild(reset);
        var quit = Button("QUIT");
        quit.Pressed += () => QuitRequested?.Invoke();
        column.AddChild(quit);
    }

    private void RefreshBindings()
    {
        foreach (var binding in FlightInput.Bindings)
            if (_bindingButtons.TryGetValue(binding.Action, out var button)) button.Text = FlightInput.Display(binding.Action);
        _controls.Text = $"{FlightInput.Display("flight_forward")} thrust   ·   {FlightInput.Display("flight_reverse")} reverse   ·   {FlightInput.Display("flight_left")} / {FlightInput.Display("flight_right")} turn   ·   {FlightInput.Display("flight_fire")} fire   ·   {FlightInput.Display("flight_reset")} reset   ·   {FlightInput.Display("flight_pause")} controls";
    }

    private static ProgressBar Meter(VBoxContainer parent, string caption, Color color)
    {
        var row = new HBoxContainer();
        parent.AddChild(row);
        var label = Label(caption, 12, Muted);
        label.CustomMinimumSize = new Vector2(58, 0);
        row.AddChild(label);
        var bar = new ProgressBar { ShowPercentage = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 9) };
        bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color("203445"), CornerRadiusTopLeft = 3, CornerRadiusBottomLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomRight = 3 });
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = color, CornerRadiusTopLeft = 3, CornerRadiusBottomLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomRight = 3 });
        row.AddChild(bar);
        return bar;
    }

    private static PanelContainer Panel()
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.031f, 0.066f, 0.1f, 0.94f), BorderColor = new Color("263f50"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7, CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7,
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 13, ContentMarginBottom = 13
        });
        return panel;
    }

    private static VBoxContainer Column(Node parent)
    {
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 6);
        parent.AddChild(column);
        return column;
    }

    private static Label Label(string text, int fontSize, Color color)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Button Button(string text)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 32), FocusMode = Control.FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", TextColor);
        button.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color("1b3446"), ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        button.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color("28566a"), ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8 });
        button.AddThemeStyleboxOverride("pressed", new StyleBoxFlat { BgColor = new Color("38717c"), ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8 });
        return button;
    }
}


