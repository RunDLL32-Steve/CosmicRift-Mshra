using Godot;

namespace RogueTrader.Client;

/// <summary>Native UI/input regression only. The caller pauses gameplay before running this fixture.</summary>
public sealed class OriginalContentInputCheck(
    OriginalContentInspector inspector,
    OriginalSpriteLibrary library,
    Func<bool> readVisible,
    Func<long> readTick,
    Func<bool> readPaused,
    Func<Task> nextFrame,
    Func<bool>? readMenuVisible = null)
{
    public async Task<bool> RunAsync()
    {
        const string settingsPath = "user://controls.cfg";
        var originalSettings = Godot.FileAccess.FileExists(settingsPath) ? Godot.FileAccess.GetFileAsString(settingsPath) : null;
        var startTick = readTick();
        var passed = false;
        try
        {
            Require(readPaused() && (readMenuVisible?.Invoke() ?? true), "Inspector fixture must start with gameplay and controls paused.");
            FlightInput.RestoreDefaults();
            Send(Key.F2);
            await nextFrame();
            Require(readVisible(), "Native F2 must open the inspector.");
            var row = Widget<SpinBox>("Inspect_row");
            var column = Widget<SpinBox>("Inspect_column");
            var selector = Widget<OptionButton>("Inspect_selector");
            Require(selector.ItemCount == library.Sprites.Count && library.Sprites.Count == 231, "All 231 staged sprites must be selectable.");
            Press("ships");
            for (var direction = 0; direction < 64; direction++)
                for (var state = 0; state < 11; state++)
                {
                    row.Value = direction;
                    column.Value = state;
                    Require(inspector.Row == direction && inspector.Column == state, "Hull frame control signals did not update the display.");
                    foreach (var hull in library.Hulls)
                    {
                        var sprite = library.Find(hull.AssetKey);
                        var frame = library.Frame(sprite, inspector.Row, inspector.Column);
                        var texture = library.Texture(sprite);
                        Require(frame.End.X <= texture.GetWidth() && frame.End.Y <= texture.GetHeight(), $"Hull {hull.Name} frame exceeds its texture.");
                    }
                }
            row.Value = 47; column.Value = 9;
            Press("map"); await nextFrame(); Press("ships");
            Require(inspector.Row == 47 && inspector.Column == 9, "Map navigation lost the hull direction/state selection.");
            // Route real keyboard events through Godot's GUI, not just direct Value setters.
            row.GetLineEdit().GrabFocus();
            row.GetLineEdit().Text = "31";
            Send(Key.Enter);
            await nextFrame();
            Require(inspector.Row == 31, "Native Enter did not commit the focused row editor.");
            row.GetLineEdit().ReleaseFocus();
            GD.Print("ORIGINAL_INPUT_PHASE_PASS: 12 hulls x 64 directions x 11 states; map return; native frame editor.");

            Press("sprites");
            for (var index = 0; index < selector.ItemCount; index++)
            {
                Select(selector, index);
                var sprite = library.Sprites[index];
                row.Value = sprite.RowCount - 1; column.Value = sprite.ColumnCount - 1;
                Require(inspector.SelectedSprite == index && inspector.Row == sprite.RowCount - 1 && inspector.Column == sprite.ColumnCount - 1,
                    $"Cannot select the final frame of {sprite.AssetKey}.");
                library.Frame(sprite, inspector.Row, inspector.Column);
            }
            var multiple = library.Sprites.Select((sprite, index) => (sprite, index)).First(pair => pair.sprite.RowCount > 1 && pair.sprite.ColumnCount > 1);
            var single = library.Sprites.Select((sprite, index) => (sprite, index)).First(pair => pair.sprite.RowCount == 1);
            Select(selector, multiple.index); row.Value = multiple.sprite.RowCount - 1; column.Value = multiple.sprite.ColumnCount - 1;
            Select(selector, single.index); Press("map"); await nextFrame(); Press("sprites"); Select(selector, multiple.index);
            Require(inspector.Row == multiple.sprite.RowCount - 1 && inspector.Column == multiple.sprite.ColumnCount - 1,
                "Selecting a narrow sprite/map destroyed another sprite's retained frame.");
            Press("ships"); Require(inspector.Row == 31 && inspector.Column == 9, "Archive inspection lost the independent hull frame.");
            GD.Print("ORIGINAL_INPUT_PHASE_PASS: 231 sprite selection signals, final frames and independent page/member state.");

            Send(Key.R); Send(Key.W); Send(Key.Space); await nextFrame();
            Require(readVisible() && readTick() == startTick, "Inspector keys advanced or reset gameplay.");
            Send(Key.Escape); await nextFrame(); Require(!readVisible() && readPaused() && (readMenuVisible?.Invoke() ?? true), "Escape must close inspection while preserving pause and the controls menu.");
            Send(Key.F2); await nextFrame(); Require(readVisible(), "F2 must reopen inspection.");
            Send(Key.F2); await nextFrame(); Require(!readVisible(), "F2 must close inspection.");
            Require(FlightInput.TryRebind("original_content", Key.F3) is null, "Could not remap inspector input.");
            Require(FlightInput.TryRebind("flight_pause", Key.P) is null, "Could not remap pause input.");
            FlightInput.Initialize();
            Send(Key.F2); await nextFrame(); Require(!readVisible(), "Old F2 opened inspector after persisted remap.");
            Send(Key.F3); await nextFrame(); Require(readVisible(), "Persisted F3 must open inspection.");
            Send(Key.Escape); await nextFrame(); Require(!readVisible(), "Visible ESC close hint must remain correct after pause remap.");
            Send(Key.F3); await nextFrame(); Send(Key.P); await nextFrame(); Require(!readVisible() && readPaused(), "Remapped pause must close inspection without resuming flight.");
            Send(Key.F3); await nextFrame(); Press("close"); await nextFrame(); Require(!readVisible(), "Close button signal must close inspection.");
            Require(readTick() == startTick && (readMenuVisible?.Invoke() ?? true), "Original input verification advanced the world or lost the controls menu.");
            GD.Print($"ORIGINAL_INPUT_PHASE_PASS: native open/close, input isolation, persisted remaps, close button; world tick {startTick} unchanged.");
            passed = true;
        }
        catch (Exception exception) { GD.PushError($"ORIGINAL_INPUT_FAILED: {exception.Message}"); }
        finally
        {
            try
            {
                foreach (var key in new[] { Key.F2, Key.F3, Key.Enter, Key.Escape, Key.R, Key.W, Key.Space, Key.P }) KeyEvent(key, false);
                if (originalSettings is not null)
                {
                    using var file = Godot.FileAccess.Open(settingsPath, Godot.FileAccess.ModeFlags.Write);
                    if (file is null) throw new InvalidOperationException("Cannot restore original control settings.");
                    file.StoreString(originalSettings);
                }
                else if (Godot.FileAccess.FileExists(settingsPath) && DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(settingsPath)) != Error.Ok)
                    throw new InvalidOperationException("Cannot remove temporary inspector settings.");
                FlightInput.Initialize();
            }
            catch (Exception exception) { GD.PushError($"ORIGINAL_INPUT_FAILED: settings restoration: {exception.Message}"); passed = false; }
        }
        if (passed) GD.Print("ORIGINAL_INPUT_PASS: native inspector navigation/input and retained frames verified without world simulation.");
        return passed;
    }

    private T Widget<T>(string name) where T : Node => inspector.FindChildren(name, typeof(T).Name, true, false).Cast<T>().Single();
    private void Press(string page) => Widget<Button>("Inspect_" + page).EmitSignal(Button.SignalName.Pressed);
    private static void Select(OptionButton selector, int index) { selector.Select(index); selector.EmitSignal(OptionButton.SignalName.ItemSelected, index); }
    private static void Send(Key key) { KeyEvent(key, true); KeyEvent(key, false); }
    private static void KeyEvent(Key key, bool pressed) { Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed }); Input.FlushBufferedEvents(); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
