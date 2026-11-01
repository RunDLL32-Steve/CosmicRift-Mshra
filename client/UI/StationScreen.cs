using Godot;
using RogueTrader.Persistence;
using RogueTrader.Trading;

namespace RogueTrader.Client;

/// <summary>
/// Queue M8 provisional station screen: market, repairs and upgrades. It only raises requests and renders the authoritative
/// profile it is given; it never changes wallet, cargo or upgrades itself. Layout and wording are NEW testbed UI, not the
/// native store (RT-F025/F026 stay open).
/// </summary>
public partial class StationScreen : CanvasLayer
{
    public event Action<string, int>? SellRequested;
    public event Action<string>? RepairRequested;
    public event Action<string>? UpgradeRequested;
    public event Action? CloseRequested;

    private readonly StationDefinition _station;
    private Label _credits = null!;
    private Label _cargo = null!;
    private Label _hull = null!;
    private Label _revision = null!;
    private Label _status = null!;
    private VBoxContainer _market = null!;
    private VBoxContainer _repairs = null!;
    private VBoxContainer _upgrades = null!;
    private TabContainer _tabs = null!;

    public void ShowTab(int index) => _tabs.CurrentTab = Math.Clamp(index, 0, _tabs.GetTabCount() - 1);

    public StationScreen(StationDefinition station)
    {
        _station = station;
        Layer = 5;
    }

    public string CreditsText => _credits.Text;
    public string StatusText => _status.Text;

    public override void _Ready()
    {
        var root = new Control { Name = "Station" };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        var shade = new ColorRect { Color = new Color(0.015f, 0.03f, 0.05f, 0.82f), MouseFilter = Control.MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(shade);
        var box = FlightHud.Panel();
        box.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        box.OffsetLeft = -380;
        box.OffsetRight = 380;
        box.OffsetTop = -290;
        box.OffsetBottom = 290;
        root.AddChild(box);
        var column = FlightHud.Column(box);
        column.AddChild(FlightHud.Label(_station.Name.ToUpperInvariant(), 26, FlightHud.TextColor));
        column.AddChild(FlightHud.Label("DOCKED  /  ORIGINAL DATA, PROVISIONAL RULES", 12, FlightHud.Teal));

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 22);
        column.AddChild(header);
        _credits = FlightHud.Label("", 18, FlightHud.TextColor);
        _credits.Name = "StationCredits";
        header.AddChild(_credits);
        _cargo = FlightHud.Label("", 14, FlightHud.Muted);
        header.AddChild(_cargo);
        _hull = FlightHud.Label("", 14, FlightHud.Muted);
        header.AddChild(_hull);
        _revision = FlightHud.Label("", 12, FlightHud.Muted);
        column.AddChild(_revision);
        column.AddChild(new HSeparator());

        _tabs = new TabContainer { CustomMinimumSize = new Vector2(0, 330), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(_tabs);
        _market = Page(_tabs, "Trading Outpost");
        _repairs = Page(_tabs, "Repairs");
        _upgrades = Page(_tabs, "Upgrades");

        _status = FlightHud.Label("", 14, FlightHud.TextColor);
        _status.Name = "StationStatus";
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _status.CustomMinimumSize = new Vector2(0, 36);
        column.AddChild(_status);
        var close = FlightHud.Button("UNDOCK  /  ESC");
        close.Name = "StationClose";
        close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(close);
    }

    public void SetStatus(string text, bool error = false)
    {
        _status.Text = text;
        _status.AddThemeColorOverride("font_color", error ? new Color("f4b66b") : FlightHud.TextColor);
    }

    /// <summary>Rebuilds every row from the authoritative profile, the ship's hull and the active upgrade set.</summary>
    /// <param name="blockedUpgrade">Why an unowned upgrade cannot become active (null when it can); such rows are disabled.</param>
    public void Refresh(PlayerProfile profile, long cargoWeight, long cargoCapacity, float hull, float maximumHull, IReadOnlySet<string> activeUpgrades,
        Func<string, string?> blockedUpgrade)
    {
        _credits.Text = $"CREDITS  {profile.Wallet:N0}";
        _cargo.Text = $"CARGO  {cargoWeight:N0} / {cargoCapacity:N0}";
        _hull.Text = $"HULL  {hull:0} / {maximumHull:0}";
        _revision.Text = $"Profile {profile.ProfileId}  ·  revision {profile.Revision}  ·  every purchase is saved before it is shown";

        Clear(_market);
        _market.AddChild(FlightHud.Label("Original Trading Outpost recipes. Credits per unit are the raw original value (provisional reading).", 12, FlightHud.Muted));
        _market.AddChild(Row(Cell("RECIPE", 150, true), Cell("INPUT", 150, true), Cell("HELD", 50, true), Cell("CR / UNIT", 70, true)));
        foreach (var trade in _station.Trades)
        {
            var held = profile.Inventory.GetValueOrDefault(trade.InputItemId);
            _market.AddChild(Row(Cell(trade.Name, 150), Cell(_station.NameOf(trade.InputItemId), 150), Cell(held.ToString(), 50),
                Cell(trade.CreditsPerUnit.ToString("N0"), 70),
                ActionButton("TRADE 1", $"sell:{trade.InputItemId}", held == 0, () => SellRequested?.Invoke(trade.InputItemId, 1)),
                ActionButton("TRADE ALL", $"sellall:{trade.InputItemId}", held == 0, () => SellRequested?.Invoke(trade.InputItemId, held))));
        }
        var untraded = profile.Inventory.Where(p => p.Value > 0 && _station.Trades.All(t => t.InputItemId != p.Key)).ToArray();
        if (untraded.Length > 0)
            _market.AddChild(FlightHud.Label("Not traded here: " + string.Join(", ", untraded.Select(p => $"{_station.NameOf(p.Key)} x{p.Value}")), 12, FlightHud.Muted));

        Clear(_repairs);
        var canRepair = hull > 0 && hull < maximumHull;
        _repairs.AddChild(FlightHud.Label(canRepair
            ? "Original repair rows; the raw amount is read as hull points and the price is provisional."
            : hull <= 0 ? "The ship is destroyed: reset to launch a new one." : "Hull is at maximum: nothing to repair.", 12, FlightHud.Muted));
        foreach (var repair in _station.Repairs)
            _repairs.AddChild(Row(Cell(repair.Name, 200), Cell($"+{repair.HullPoints:0} hull", 100), Cell($"{repair.Price:N0} cr", 90),
                ActionButton("REPAIR", $"repair:{repair.Id}", !canRepair, () => RepairRequested?.Invoke(repair.Id))));

        Clear(_upgrades);
        _upgrades.AddChild(FlightHud.Label("Original modules and prices. Effects are provisional; one active item per slot.", 12, FlightHud.Muted));
        foreach (var family in _station.Upgrades.GroupBy(u => u.Family))
        {
            _upgrades.AddChild(FlightHud.Label(family.Key.ToUpperInvariant(), 11, FlightHud.Teal));
            foreach (var upgrade in family)
            {
                var owned = profile.Upgrades.Contains(upgrade.Id);
                var blocked = owned ? null : blockedUpgrade(upgrade.Id);
                var state = owned ? (activeUpgrades.Contains(upgrade.Id) ? "ACTIVE" : "OWNED") : blocked is null ? "BUY" : "-";
                var button = ActionButton(state, $"upgrade:{upgrade.Id}", owned || blocked is not null, () => UpgradeRequested?.Invoke(upgrade.Id));
                button.TooltipText = blocked ?? "";
                _upgrades.AddChild(Row(Cell(upgrade.Name, 190), Cell($"{upgrade.Price:N0} cr", 80), Cell(blocked ?? Summary(upgrade.Effect), 200), button));
            }
        }
    }

    private static string Summary(UpgradeEffect e)
    {
        var parts = new List<string>();
        void Add(string name, float value) { if (value != 0) parts.Add($"{name} {value * 100:+0.#;-0.#}%"); }
        Add("thrust", e.ForwardThrust);
        Add("speed", e.MaximumSpeed);
        Add("turn", e.RotationSpeed);
        Add("energy", e.MaximumEnergy);
        Add("regen", e.EnergyRegeneration);
        if (e.MaximumHull != 0) parts.Add($"hull +{e.MaximumHull:0}");
        return parts.Count == 0 ? "no effect yet" : string.Join(", ", parts);
    }

    private static VBoxContainer Page(TabContainer tabs, string name)
    {
        var scroll = new ScrollContainer { Name = name, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        tabs.AddChild(scroll);
        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(column);
        return column;
    }

    private static void Clear(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static HBoxContainer Row(params Control[] cells)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        foreach (var cell in cells) row.AddChild(cell);
        return row;
    }

    private static Label Cell(string text, float width, bool heading = false)
    {
        var label = FlightHud.Label(text, heading ? 11 : 14, heading ? FlightHud.Teal : FlightHud.TextColor);
        label.CustomMinimumSize = new Vector2(width, 0);
        return label;
    }

    private static Button ActionButton(string text, string action, bool disabled, Action pressed)
    {
        var button = FlightHud.Button(text);
        button.CustomMinimumSize = new Vector2(78, 30);
        button.Disabled = disabled;
        button.SetMeta("station_action", action);
        button.Pressed += pressed;
        return button;
    }
}
