using Godot;
using RogueTrader.GameData;
using RogueTrader.Persistence;

namespace RogueTrader.Client;

/// <summary>
/// Ship picker shown when the client and map load (Steve, Nov 3 2026): one row per hull the player owns, with the condition the
/// server saved for it. It only raises <see cref="ShipChosen"/>; the server makes the switch. Layout and wording are NEW testbed
/// UI, not the native F11 hull selection (RT-F001-F012 stay open).
/// </summary>
public partial class ShipPicker : CanvasLayer
{
    public event Action<string>? ShipChosen;

    private VBoxContainer _rows = null!;
    private Label _status = null!;

    public ShipPicker() => Layer = 6;

    public string StatusText => _status.Text;
    /// <summary>Hull ids of the rows offered, in display order (for the acceptance fixture).</summary>
    public IReadOnlyList<string> OfferedHulls { get; private set; } = [];

    public override void _Ready()
    {
        var root = new Control { Name = "ShipPicker" };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        var shade = new ColorRect { Color = new Color(0.015f, 0.03f, 0.05f, 0.88f), MouseFilter = Control.MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(shade);
        var box = FlightHud.Panel();
        box.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        box.OffsetLeft = -330;
        box.OffsetRight = 330;
        box.OffsetTop = -220;
        box.OffsetBottom = 220;
        root.AddChild(box);
        var column = FlightHud.Column(box);
        column.AddChild(FlightHud.Label("CHOOSE YOUR SHIP", 26, FlightHud.TextColor));
        column.AddChild(FlightHud.Label("Ships you own are saved to your account. Each one stays exactly as you left it.", 12, FlightHud.Teal));
        column.AddChild(new HSeparator());
        _rows = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _rows.AddThemeConstantOverride("separation", 8);
        column.AddChild(_rows);
        _status = FlightHud.Label("", 14, FlightHud.TextColor);
        _status.Name = "ShipPickerStatus";
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _status.CustomMinimumSize = new Vector2(0, 36);
        column.AddChild(_status);
    }

    /// <summary>Rebuilds the rows from the authoritative profile and shows the picker, focusing the active ship.</summary>
    /// <param name="definitionOf">The flyable ship definition of a hull, or null when it has none yet.</param>
    public void Show(PlayerProfile profile, ShipRoster roster, Func<string, ShipDefinition?> definitionOf, string activeHullId)
    {
        foreach (var child in _rows.GetChildren()) { _rows.RemoveChild(child); child.QueueFree(); }
        _status.Text = "";
        // Roster order first, then any owned hull this build cannot fly yet.
        var order = roster.Ships.Select(s => s.HullId).Where(profile.OwnedShips.ContainsKey)
            .Concat(profile.OwnedShips.Keys.Where(h => roster.Find(h) is null)).ToArray();
        OfferedHulls = order;
        Button? focus = null;
        foreach (var hullId in order)
        {
            var definition = definitionOf(hullId);
            var active = hullId == profile.ShipHullId;
            var condition = active ? profile.Ship : profile.OwnedShips[hullId];
            var maximum = definition?.Defense.MaximumHull ?? 0;
            var hull = condition is null ? "full hull, at the spawn"
                : maximum > 0 ? $"hull {Math.Min(condition.Hull, maximum):0} / {maximum:0}" : $"hull {condition.Hull:0}";
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 14);
            var name = FlightHud.Label(definition?.Name ?? hullId, 18, FlightHud.TextColor);
            name.CustomMinimumSize = new Vector2(200, 0);
            row.AddChild(name);
            var detail = FlightHud.Label(definition is null ? "not flyable in this build yet" : (active ? $"{hull}  ·  last flown" : hull), 13, FlightHud.Muted);
            detail.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(detail);
            var fly = FlightHud.Button("FLY");
            fly.Name = $"Fly_{hullId}";
            fly.Disabled = definition is null;
            var chosen = hullId;
            fly.Pressed += () => ShipChosen?.Invoke(chosen);
            row.AddChild(fly);
            _rows.AddChild(row);
            if (hullId == activeHullId && definition is not null) focus = fly;
            focus ??= definition is null ? null : fly;
        }
        Visible = true;
        focus?.CallDeferred(Control.MethodName.GrabFocus);
    }

    /// <summary>Presses the FLY button of a hull, as a player would (acceptance fixture).</summary>
    public bool Press(string hullId)
    {
        if (_rows.FindChild($"Fly_{hullId}", recursive: true, owned: false) is not Button { Disabled: false } button) return false;
        button.EmitSignal(BaseButton.SignalName.Pressed);
        return true;
    }

    public void SetStatus(string text, bool error = false)
    {
        _status.Text = text;
        _status.AddThemeColorOverride("font_color", error ? new Color("f4b66b") : FlightHud.TextColor);
    }
}
