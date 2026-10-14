using Godot;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.Client;

/// <summary>Native-input and controls regression fixture; preserves the user's saved bindings.</summary>
public sealed class ClientInputSmokeTest(
    GameCatalog catalog, FlightHud hud, Func<WorldSimulation> readWorld,
    Func<bool> readPaused, Action resetSector, Func<Task> nextPhysicsFrame)
{
    public async Task<bool> RunAsync()
    {
        const string settingsPath = "user://controls.cfg";
        var originalSettings = Godot.FileAccess.FileExists(settingsPath)
            ? Godot.FileAccess.GetFileAsString(settingsPath) : null;
        var passed = false;
        try
        {
            FlightInput.RestoreDefaults(save: false);
            resetSector();
            SendKey(Key.W, true);
            await nextPhysicsFrame();
            RequireInput(FlightInput.Capture().Thrust == 1, "The original held W key must produce thrust.");
            RequireInput(readWorld().Player.Velocity.Length() > 0, "Native keyboard input must reach the gameplay bridge.");
            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            RequireInput(readPaused() && hud.ControlsVisible, "Escape must pause and show the controls menu.");
            var pausedTick = readWorld().TickNumber;
            await nextPhysicsFrame();
            await nextPhysicsFrame();
            RequireInput(readWorld().TickNumber == pausedTick, "The world must stop advancing while paused.");

            var bindingButton = hud.FindChildren("*", "Button", true, false).Cast<Button>()
                .Single(button => button.HasMeta("binding_action") && button.GetMeta("binding_action").AsString() == "flight_forward");
            bindingButton.EmitSignal(Button.SignalName.Pressed);
            RequireInput(hud.IsCapturingKey, "The remapping button must enter key capture.");
            SendKey(Key.Q, true);
            await nextPhysicsFrame();
            RequireInput(!hud.IsCapturingKey, "The native replacement key must complete UI key capture.");
            RequireInput(FlightInput.Display("flight_forward") == "Q", "The remapping UI must bind forward thrust to Q.");
            SendKey(Key.W, false);
            await nextPhysicsFrame();
            RequireInput(FlightInput.Capture().Thrust == 0, "Hold W, pause, rebind Q, release W must not leave cached thrust pressed.");
            SendKey(Key.Q, false);
            await nextPhysicsFrame();
            GD.Print("INPUT SMOKE PASS: held-key UI rebinding releases cached action state");

            FlightInput.RestoreDefaults(save: false);
            RequireInput(FlightInput.Display("flight_forward").Contains("W"), "The memory-only reset must change the binding.");
            FlightInput.Initialize();
            RequireInput(FlightInput.Display("flight_forward") == "Q", "Saved remapped bindings must survive initialization.");
            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            RequireInput(!readPaused() && !hud.ControlsVisible, "Escape must resume and close controls.");
            var resumeTick = readWorld().TickNumber;
            var coastVelocity = readWorld().Player.Velocity;
            await nextPhysicsFrame();
            RequireInput(readWorld().TickNumber > resumeTick, "The world must resume advancing after pause.");
            RequireInput(System.Numerics.Vector2.Distance(readWorld().Player.Velocity, coastVelocity) < 0.01f,
                "Resuming after key remapping must coast without phantom acceleration.");
            SendKey(Key.W, true);
            await nextPhysicsFrame();
            RequireInput(FlightInput.Capture().Thrust == 0, "The removed W binding must no longer produce thrust.");
            SendKey(Key.W, false);
            SendKey(Key.Q, true);
            await nextPhysicsFrame();
            RequireInput(FlightInput.Capture().Thrust == 1 && readWorld().Player.Velocity.Length() > coastVelocity.Length(),
                "The persisted Q binding must accelerate the gameplay ship.");
            GD.Print("INPUT SMOKE PASS: persisted remap, pause, resume, and native gameplay input");

            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            var defaultsButton = hud.FindChildren("RestoreDefaultBindings", "Button", true, false).Cast<Button>().Single();
            defaultsButton.EmitSignal(Button.SignalName.Pressed);
            SendKey(Key.Q, false);
            await nextPhysicsFrame();
            RequireInput(FlightInput.Capture().Thrust == 0, "Restoring defaults while Q is held must release cached thrust.");
            FlightInput.Initialize();
            RequireInput(FlightInput.Display("flight_forward").Contains("W") && FlightInput.Display("flight_forward").Contains("Up"),
                "Restored letter and arrow defaults must persist.");
            SendKey(Key.Up, true);
            await nextPhysicsFrame();
            RequireInput(FlightInput.Capture().Thrust == 1, "Restored Up key must produce thrust.");
            SendKey(Key.Up, false);
            await nextPhysicsFrame();
            RequireInput(FlightInput.Capture().Thrust == 0, "Releasing restored Up must stop thrust.");
            var oldWorld = readWorld();
            SendKey(Key.R, true);
            SendKey(Key.R, false);
            await nextPhysicsFrame();
            RequireInput(readWorld() != oldWorld && !readPaused() && !hud.ControlsVisible,
                "Native reset must replace the world, resume flight, and close the controls menu.");
            var spawn = catalog.Sector.PlayerSpawn;
            RequireInput(System.Numerics.Vector2.Distance(readWorld().Player.Position, new(spawn.X, spawn.Y)) < 0.01f
                && readWorld().Player.Velocity.Length() < 0.01f && readWorld().DestroyedTargets == 0,
                "Reset must restore a stationary fresh ship and live targets.");
            GD.Print("INPUT SMOKE PASS: held-key default restoration, persisted defaults, and native sector reset");
            passed = true;
        }
        catch (Exception exception)
        {
            GD.PushError($"CLIENT_INPUT_SMOKE_FAILED: {exception.Message}");
        }
        finally
        {
            foreach (var key in new[] { Key.W, Key.Q, Key.Up, Key.Escape, Key.R }) SendKey(key, false);
            try
            {
                if (originalSettings is not null)
                {
                    using var settingsFile = Godot.FileAccess.Open(settingsPath, Godot.FileAccess.ModeFlags.Write);
                    if (settingsFile is null) throw new InvalidOperationException("Could not restore pre-test control settings.");
                    settingsFile.StoreString(originalSettings);
                }
                else if (Godot.FileAccess.FileExists(settingsPath))
                {
                    if (DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(settingsPath)) != Error.Ok)
                        throw new InvalidOperationException("Could not remove temporary input-test settings.");
                }
                FlightInput.Initialize();
                resetSector();
            }
            catch (Exception exception)
            {
                GD.PushError($"CLIENT_INPUT_SMOKE_FAILED: {exception.Message}");
                passed = false;
            }
        }
        if (passed) GD.Print("CLIENT_INPUT_SMOKE_PASS: native key rebinding, cached action release, settings reload, defaults, pause/resume and reset verified.");
        return passed;
    }
    private static void SendKey(Key key, bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed });
        Input.FlushBufferedEvents();
    }

    private static void RequireInput(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}


