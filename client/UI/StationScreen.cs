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
    public event Action<string, int>? BuyRequested;
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
        box.OffsetLeft = -330;
        box.OffsetRight = 330;
        box.OffsetTop = -290;
        box.OffsetBottom = 290;
        root.AddChild(box);
        var column = FlightHud.Column(box);
        column.AddChild(FlightHud.Label(_station.Name.ToUpperInvariant(), 26, FlightHud.TextColor));
        column.AddChild(FlightHud.Label("DOCKED  /  PROVISIONAL STATION SERVICES", 12, FlightHud.Teal));

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

        var tabs = new TabContainer { CustomMinimumSize = new Vector2(0, 330), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(tabs);
        _market = Page(tabs, "Market");
        _repairs = Page(tabs, "Repairs");
        _upgrades = Page(tabs, "Upgrades");

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

    /// <summary>Rebuilds every row from the authoritative profile and the ship's current hull.</summary>
    public void Refresh(PlayerProfile profile, long cargoWeight, long cargoCapacity, float hull, float maximumHull)
    {
        _credits.Text = $"CREDITS  {profile.Wallet:N0}";
        _cargo.Text = $"CARGO  {cargoWeight} / {cargoCapacity}";
        _hull.Text = $"HULL  {hull:0} / {maximumHull:0}";
        _revision.Text = $"Profile {profile.ProfileId}  ·  revision {profile.Revision}  ·  every purchase is saved before it is shown";

        Clear(_market);
        _market.AddChild(Row(Cell("ITEM", 150, true), Cell("HELD", 50, true), Cell("BUY", 60, true), Cell("SELL", 60, true)));
        foreach (var good in _station.Goods)
        {
            var held = profile.Inventory.GetValueOrDefault(good.Id);
            var buy = ActionButton(good.Buy is null ? "-" : "BUY 1", $"buy:{good.Id}", good.Buy is null, () => BuyRequested?.Invoke(good.Id, 1));
            var sell = ActionButton(good.Sell is null ? "-" : "SELL 1", $"sell:{good.Id}", good.Sell is null || held == 0, () => SellRequested?.Invoke(good.Id, 1));
            var sellAll = ActionButton("SELL ALL", $"sellall:{good.Id}", good.Sell is null || held == 0, () => SellRequested?.Invoke(good.Id, held));
            _market.AddChild(Row(Cell($"{good.Name}  ({good.UnitWeight} wt)", 150), Cell(held.ToString(), 50),
                Cell(good.Buy?.ToString("N0") ?? "-", 60), Cell(good.Sell?.ToString("N0") ?? "-", 60), buy, sell, sellAll));
        }

        Clear(_repairs);
        _repairs.AddChild(FlightHud.Label("Repair amounts are provisional; the original repair rule is undecoded.", 12, FlightHud.Muted));
        foreach (var repair in _station.Repairs)
            _repairs.AddChild(Row(Cell(repair.Name, 250), Cell($"{repair.Price:N0} cr", 90),
                ActionButton("REPAIR", $"repair:{repair.Id}", false, () => RepairRequested?.Invoke(repair.Id))));

        Clear(_upgrades);
        _upgrades.AddChild(FlightHud.Label("Ownership is saved; upgrade effects are not modelled yet.", 12, FlightHud.Muted));
        foreach (var upgrade in _station.Upgrades)
        {
            var owned = profile.Upgrades.Contains(upgrade.Id);
            _upgrades.AddChild(Row(Cell(upgrade.Name, 250), Cell($"{upgrade.Price:N0} cr", 90),
                ActionButton(owned ? "OWNED" : "BUY", $"upgrade:{upgrade.Id}", owned, () => UpgradeRequested?.Invoke(upgrade.Id))));
        }
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
