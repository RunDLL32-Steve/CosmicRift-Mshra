using Godot;
using RogueTrader.GameData;
using RogueTrader.Persistence;
using RogueTrader.Trading;

namespace RogueTrader.Client;

/// <summary>
/// Queue M8 provisional station screen. It shows the pages of the one service the docked site offers (Steve, Nov 4 2026):
/// the Trading Outpost page at a Trading Post, Repairs at a Repair Dock, the Refinery page at a refinery, and Upgrades, Ship
/// Hulls and the Hangar (change ships) at a team's home store. It only raises requests and renders the authoritative profile it
/// is given; it never changes wallet, cargo, upgrades or ships itself. Layout and wording are NEW testbed UI, not the native
/// store (RT-F025/F026 stay open).
/// </summary>
public partial class StationScreen : CanvasLayer
{
    public event Action<string, int>? SellRequested;
    public event Action<string>? RepairRequested;
    public event Action<string>? UpgradeRequested;
    public event Action<string, int>? RefineRequested;
    public event Action<string>? BuyShipRequested;
    /// <summary>The pilot pressed FLY on an owned ship in the Hangar.</summary>
    public event Action<string>? SwitchShipRequested;
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
    private VBoxContainer _refinery = null!;
    private VBoxContainer _hulls = null!;
    private VBoxContainer _hangar = null!;
    private Label _title = null!;
    private string _service = SiteServices.Store;
    private string _siteName;
    private TabContainer _tabs = null!;

    // Page order in the tab container.
    private const int MarketTab = 0, RepairsTab = 1, UpgradesTab = 2, RefineryTab = 3, HullsTab = 4, HangarTab = 5;

    public void ShowTab(int index) => _tabs.CurrentTab = Math.Clamp(index, 0, _tabs.GetTabCount() - 1);

    public StationScreen(StationDefinition station)
    {
        _station = station;
        _siteName = station.Name;
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
        _title = FlightHud.Label(_station.Name.ToUpperInvariant(), 26, FlightHud.TextColor);
        _title.Name = "StationTitle";
        column.AddChild(_title);
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
        _refinery = Page(_tabs, "Refinery");
        _hulls = Page(_tabs, "Ship Hulls");
        _hangar = Page(_tabs, "Hangar");
        SetMode(_service, _siteName);

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

    /// <summary>The pages a site of <paramref name="service"/> offers, the first one shown when docking.</summary>
    public static IReadOnlyList<int> PagesOf(string service) => service switch
    {
        SiteServices.Trade => [MarketTab],
        SiteServices.Repair => [RepairsTab],
        SiteServices.Refine => [RefineryTab],
        _ => [UpgradesTab, HullsTab, HangarTab],
    };

    /// <summary>Shows only the pages of the docked site's service and its name as the title.</summary>
    public void SetMode(string service, string siteName)
    {
        _service = service;
        _siteName = siteName;
        if (_tabs is null) return;
        var pages = PagesOf(service);
        for (var i = 0; i < _tabs.GetTabCount(); i++) _tabs.SetTabHidden(i, !pages.Contains(i));
        _tabs.CurrentTab = pages[0];
        _title.Text = siteName.ToUpperInvariant();
    }

    public string Service => _service;
    public bool RefineryMode => _service == SiteServices.Refine;
    public string TitleText => _title.Text;
    public int CurrentTab => _tabs.CurrentTab;

    public void SetStatus(string text, bool error = false)
    {
        _status.Text = text;
        _status.AddThemeColorOverride("font_color", error ? new Color("f4b66b") : FlightHud.TextColor);
    }

    /// <summary>Rebuilds every row from the authoritative profile, the ship's hull and the active upgrade set.</summary>
    /// <param name="blockedUpgrade">Why an unowned upgrade cannot become active (null when it can); such rows are disabled.</param>
    /// <param name="canFly">Whether this build can fly a hull (the Hangar disables the others).</param>
    public void Refresh(PlayerProfile profile, long cargoWeight, long cargoCapacity, float hull, float maximumHull, IReadOnlySet<string> activeUpgrades,
        Func<string, string?> blockedUpgrade, Func<string, bool> canFly)
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

        Clear(_refinery);
        if (_station.Refinery is { } refinery)
        {
            _refinery.AddChild(FlightHud.Label("Original refinery recipes. One input unit becomes one output unit, free (provisional reading).", 12, FlightHud.Muted));
            _refinery.AddChild(Row(Cell("RECIPE", 150, true), Cell("INPUT", 120, true), Cell("HELD", 50, true), Cell("OUTPUT", 130, true)));
            foreach (var recipe in refinery.Recipes)
            {
                var held = profile.Inventory.GetValueOrDefault(recipe.InputItemId);
                _refinery.AddChild(Row(Cell(recipe.Name, 150), Cell(_station.NameOf(recipe.InputItemId), 120), Cell(held.ToString(), 50),
                    Cell(_station.NameOf(recipe.OutputItemId), 130),
                    ActionButton("REFINE 1", $"refine:{recipe.Id}", held == 0, () => RefineRequested?.Invoke(recipe.Id, 1)),
                    ActionButton("REFINE ALL", $"refineall:{recipe.Id}", held == 0, () => RefineRequested?.Invoke(recipe.Id, held))));
            }
        }

        Clear(_repairs);
        var canRepair = hull > 0 && hull < maximumHull;
        _repairs.AddChild(FlightHud.Label(canRepair
            ? "Original repair rows; the raw amount is read as hull points and the price is provisional."
            : hull <= 0 ? "The ship is destroyed; it respawns once the loss is saved." : "Hull is at maximum: nothing to repair.", 12, FlightHud.Muted));
        foreach (var repair in _station.Repairs)
            _repairs.AddChild(Row(Cell(repair.Name, 200), Cell(repair.RestoresFullHull ? "full hull" : $"+{repair.HullPoints:0} hull", 100), Cell($"{repair.Price:N0} cr", 90),
                ActionButton("REPAIR", $"repair:{repair.Id}", !canRepair, () => RepairRequested?.Invoke(repair.Id))));

        Clear(_hulls);
        _hulls.AddChild(FlightHud.Label("Original hull exchanges. You keep every ship you own, one of each; change ships in the Hangar or when the game loads. A hull needs one you own of the same race.", 12, FlightHud.Muted));
        foreach (var ship in _station.Ships ?? [])
        {
            var owned = profile.OwnedShips.ContainsKey(ship.HullId);
            var eligible = ship.RequiresOneOf is not { Count: > 0 } requires || requires.Any(profile.OwnedShips.ContainsKey);
            var state = owned ? "OWNED" : eligible ? "BUY" : "-";
            var button = ActionButton(state, $"ship:{ship.HullId}", owned || !eligible, () => BuyShipRequested?.Invoke(ship.HullId));
            var note = owned ? (ship.HullId == profile.ShipHullId ? "flying now" : "in your hangar") : eligible ? "" : "needs a same-race hull";
            _hulls.AddChild(Row(Cell(ship.Name, 160), Cell($"{ship.Price:N0} cr", 90), Cell(note, 200), button));
        }
        if (_station.Ships is not { Count: > 0 }) _hulls.AddChild(FlightHud.Label("No hulls are sold here.", 14, FlightHud.Muted));

        Clear(_hangar);
        _hangar.AddChild(FlightHud.Label("Your ships. Pick one to fly it out of this dock with the hull it was parked with; the ship you leave stays parked here.", 12, FlightHud.Muted));
        var names = (_station.Ships ?? []).ToDictionary(h => h.HullId, h => h.Name);
        foreach (var (hullId, parked) in profile.OwnedShips)
        {
            var flying = hullId == profile.ShipHullId;
            var condition = flying ? $"hull {hull:0} / {maximumHull:0}" : parked is null ? "full hull" : $"hull {parked.Hull:0}";
            var fly = ActionButton(flying ? "FLYING" : "FLY", $"hangar:{hullId}", flying || !canFly(hullId), () => SwitchShipRequested?.Invoke(hullId));
            _hangar.AddChild(Row(Cell(names.GetValueOrDefault(hullId, hullId), 160), Cell(condition, 120),
                Cell(flying ? "flying now" : canFly(hullId) ? "parked" : "not flyable in this build yet", 170), fly));
        }

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
                var effect = Cell(blocked ?? (upgrade.Effect.Armor is { } armor ? ArmorSummary(armor) : Summary(upgrade.Effect)), 200);
                if (blocked is null && upgrade.Effect.Armor is not null)
                {
                    // Armour rows show the protection; the full effect, speed penalty included, is in the tooltip.
                    effect.TooltipText = ArmorDetail(upgrade.Effect.Armor) + "\n" + Summary(upgrade.Effect with { Armor = null });
                    effect.MouseFilter = Control.MouseFilterEnum.Pass;
                }
                _upgrades.AddChild(Row(Cell(upgrade.Name, 190), Cell($"{upgrade.Price:N0} cr", 80), effect, button));
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

    /// <summary>Compact protection, for example "Kin 2+10% Exp 1+10% En 4%": ignored hull points, then the share of the rest stopped.</summary>
    private static string ArmorSummary(ShipArmor armor)
    {
        static string One(string name, ArmorProtection p) =>
            p.Ignore == 0 && p.Reduction == 0 ? ""
            : p.Ignore == 0 ? $"{name} {p.Reduction * 100:0.#}%"
            : p.Reduction == 0 ? $"{name} {p.Ignore:0.#}"
            : $"{name} {p.Ignore:0.#}+{p.Reduction * 100:0.#}%";
        var parts = new[] { One("Kin", armor.Kinetic), One("Exp", armor.Explosive), One("En", armor.Energy) }.Where(p => p.Length > 0).ToList();
        return parts.Count == 0 ? "no protection" : string.Join(" ", parts);
    }

    private static string ArmorDetail(ShipArmor armor)
    {
        static string One(string name, ArmorProtection p) =>
            $"{name}: ignores {p.Ignore:0.#} hull points of each hit, then stops {p.Reduction * 100:0.#}% of the rest";
        return string.Join("\n", One("Kinetic", armor.Kinetic), One("Explosive", armor.Explosive), One("Energy", armor.Energy));
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
