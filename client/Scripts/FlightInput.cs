using Godot;
using RogueTrader.Simulation;

namespace RogueTrader.Client;

/// <summary>Only input capture and persisted bindings; never modifies ship state.</summary>
public static class FlightInput
{
    public readonly record struct Binding(string Action, string Label, Key[] Defaults);
    public static readonly Binding[] Bindings =
    [
        new("flight_forward", "Forward thrust", [Key.W, Key.Up]),
        new("flight_reverse", "Reverse thrust", [Key.S, Key.Down]),
        new("flight_left", "Rotate left", [Key.A, Key.Left]),
        new("flight_right", "Rotate right", [Key.D, Key.Right]),
        new("flight_fire", "Primary weapon", [Key.Space]),
        new("flight_pause", "Pause / controls", [Key.Escape]),
        new("flight_reset", "Reset sector", [Key.R]),
        new("original_content", "Original content viewer", [Key.F2])
    ];
    private const string SettingsPath = "user://controls.cfg";

    public static void Initialize()
    {
        RestoreDefaults(save: false);
        var settings = new ConfigFile();
        if (settings.Load(SettingsPath) != Error.Ok) return;
        foreach (var binding in Bindings)
        {
            if (!settings.HasSectionKey("controls", binding.Action)) continue;
            var keys = settings.GetValue("controls", binding.Action).AsString()
                .Split(',').Select(text => long.TryParse(text, out var number) ? (Key)number : Key.None)
                .Where(key => key != Key.None).ToArray();
            if (keys.Length != 0) SetKeys(binding.Action, keys);
        }
    }

    public static ShipInput Capture() => new(
        Input.GetAxis("flight_reverse", "flight_forward"),
        Input.GetAxis("flight_left", "flight_right"),
        Input.IsActionPressed("flight_fire"));

    public static string Display(string action) => string.Join(" / ", InputMap.ActionGetEvents(action)
        .OfType<InputEventKey>().Select(input => OS.GetKeycodeString(input.PhysicalKeycode)));

    public static string? TryRebind(string action, Key key)
    {
        if (key == Key.None) return "Choose a keyboard key.";
        foreach (var binding in Bindings)
        {
            if (binding.Action == action) continue;
            if (InputMap.ActionGetEvents(binding.Action).OfType<InputEventKey>().Any(input => input.PhysicalKeycode == key))
                return $"{OS.GetKeycodeString(key)} is assigned to {binding.Label.ToLowerInvariant()}.";
        }
        SetKeys(action, [key]);
        Save();
        return null;
    }

    public static void RestoreDefaults(bool save = true)
    {
        foreach (var binding in Bindings) SetKeys(binding.Action, binding.Defaults);
        if (save) Save();
    }

    private static void SetKeys(string action, IEnumerable<Key> keys)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        // Releasing a held old key after erasing its event cannot clear Godot's cached action state.
        Input.ActionRelease(action);
        InputMap.ActionEraseEvents(action);
        foreach (var key in keys) InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    private static void Save()
    {
        var settings = new ConfigFile();
        foreach (var binding in Bindings)
            settings.SetValue("controls", binding.Action, string.Join(',', InputMap.ActionGetEvents(binding.Action)
                .OfType<InputEventKey>().Select(input => ((long)input.PhysicalKeycode).ToString())));
        if (settings.Save(SettingsPath) != Error.Ok) GD.PushWarning("Could not save control bindings.");
    }
}



