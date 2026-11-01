using Godot;
using RogueTrader.Persistence;
using RogueTrader.Simulation;

namespace RogueTrader.Client;

/// <summary>
/// Queue M8 acceptance fixture: drives the real HUD, Trading Outpost screen and pause menu through native input and button
/// signals and checks every displayed value against the authoritative profile the services return, then reloads and reopens
/// the save folder and checks wallet, cargo, upgrades, hull and position. Runs against an isolated smoke save folder.
/// </summary>
public sealed class ClientStationSmokeTest(GameClient client, FlightHud hud, Func<WorldSimulation> readWorld, Action resetSector, Func<Task> nextPhysicsFrame)
{
    public async Task<bool> RunAsync()
    {
        try
        {
            var session = client.Session ?? throw new InvalidOperationException($"Profile did not open: {client.ProfileStatus}");
            var station = session.Station;
            var profile = session.Profile!;
            Require(profile.Wallet == station.StartingWallet && profile.Inventory.Count == 0,
                "A fresh smoke profile must start with the station's starting wallet and an empty hold.");
            await nextPhysicsFrame();
            Require(hud.CreditsText == $"CREDITS  {profile.Wallet:N0}", $"HUD credits '{hud.CreditsText}' must match the profile wallet {profile.Wallet}.");
            GD.Print("STATION SMOKE PASS: new profile created and the HUD shows the authoritative wallet");

            // In-space pickup, first with a simulated save failure (the cargo must stay in the hold), then committed.
            resetSector();
            var pile = station.Collectibles.First(p => Math.Abs(p.X) < 1);
            client.SimulatePickupSaveFailure();
            SendKey(Key.W, true);
            for (var frame = 0; frame < 240 && readWorld().Cargo.Quantity(pile.ItemId) == 0; frame++)
                await nextPhysicsFrame();
            SendKey(Key.W, false);
            Require(readWorld().Cargo.Quantity(pile.ItemId) == pile.Quantity && session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == 0,
                "While saving is unavailable the collected cargo must stay in the hold, uncommitted.");
            client.ClearPickupSaveFailure();
            await nextPhysicsFrame();
            Require(session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == pile.Quantity,
                $"Once saving works the {pile.Quantity} collected {pile.ItemId} must be committed to the profile.");
            await nextPhysicsFrame();
            Require(hud.CargoText.Contains($"x{pile.Quantity}"), $"HUD cargo '{hud.CargoText}' must list the committed pickup.");
            GD.Print("STATION SMOKE PASS: pickup survives a failed save and is then committed and shown on the HUD");

            // Dock with the native key from the spawn point (inside the provisional dock radius).
            resetSector();
            Require(readWorld().Cargo.Quantity(pile.ItemId) == pile.Quantity, "A sector reset must rebuild the hold from the saved profile.");
            SendKey(Key.F, true);
            SendKey(Key.F, false);
            await nextPhysicsFrame();
            Require(client.StationVisible && client.Station.Visible, "F at the station must open the station screen.");
            var dockedTick = readWorld().TickNumber;
            await nextPhysicsFrame();
            Require(readWorld().TickNumber == dockedTick, "The world must not advance while docked.");

            var trade = station.Trades.Single(t => t.InputItemId == pile.ItemId);
            Press($"sellall:{pile.ItemId}");
            Require(session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == 0 && readWorld().Cargo.Quantity(pile.ItemId) == 0,
                "Trade all must hand the whole input stack in.");
            Require(session.Profile.Wallet == station.StartingWallet + pile.Quantity * trade.CreditsPerUnit, "The recipe must pay its credits per unit.");
            Require(client.Station.CreditsText == $"CREDITS  {session.Profile.Wallet:N0}", "The station screen must show the saved wallet.");
            GD.Print($"STATION SMOKE PASS: {trade.Name} paid {pile.Quantity * trade.CreditsPerUnit} credits into the saved profile");

            // Repairs: refused at full hull without a charge, applied when damaged.
            var repair = station.Repairs[0];
            Require(FindAction($"repair:{repair.Id}").Disabled, "REPAIR must be disabled at full hull.");
            var beforeRefusal = session.Profile;
            Press($"repair:{repair.Id}");
            Require(ReferenceEquals(beforeRefusal, session.Profile), "A repair at full hull must not charge or save anything.");
            Require(client.Station.StatusText.Contains("nothing was charged"), $"The refusal must be explained, got '{client.Station.StatusText}'.");
            var player = readWorld().Player;
            readWorld().RestorePlayer(player.Position, player.Heading, player.Definition.Defense.MaximumHull - 20);
            var walletBefore = session.Profile!.Wallet;
            Press($"repair:{repair.Id}");
            Require(session.Profile!.Wallet == walletBefore - repair.Price, "A repair on a damaged ship must debit its price.");
            Require(Math.Abs(readWorld().Player.Hull - (player.Definition.Defense.MaximumHull - 20 + repair.HullPoints)) < 0.01f,
                "The repair must restore its original amount of hull.");
            GD.Print("STATION SMOKE PASS: repair refused without charge at full hull and applied when damaged");

            // Upgrades: original price, provisional effect applied to the flying ship, ownership saved.
            var baseSpeed = readWorld().Player.Definition.Physics.MaximumSpeed;
            var upgrade = station.Upgrades.Where(u => u.Price <= session.Profile.Wallet && u.Effect.MaximumSpeed > 0).OrderBy(u => u.Price).First();
            var hullBeforeUpgrade = readWorld().Player.Hull;
            Press($"upgrade:{upgrade.Id}");
            Require(session.Profile!.Upgrades.Contains(upgrade.Id), "Upgrade purchase must persist ownership.");
            Require(readWorld().Player.Definition.Physics.MaximumSpeed > baseSpeed, $"{upgrade.Name} must raise maximum speed.");
            Require(Math.Abs(readWorld().Player.Hull - hullBeforeUpgrade) < 0.01f, "Installing an upgrade must keep the ship's hull.");
            Require(FindAction($"upgrade:{upgrade.Id}").Text == "ACTIVE", "An owned upgrade must be shown as active.");
            var unaffordable = station.Upgrades.First(u => u.Price > session.Profile.Wallet);
            beforeRefusal = session.Profile;
            Press($"upgrade:{unaffordable.Id}");
            Require(ReferenceEquals(beforeRefusal, session.Profile), "A refused purchase must change nothing.");
            Require(client.Station.StatusText.Contains("not enough credits"), $"The refusal must be explained, got '{client.Station.StatusText}'.");
            GD.Print($"STATION SMOKE PASS: {upgrade.Name} bought at its original price and its effect applied; unaffordable purchase refused");

            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            Require(!client.StationVisible && !client.Paused, "Escape must undock without pausing.");

            // Fly a little so the saved position differs from spawn, then save/load.
            SendKey(Key.W, true);
            for (var frame = 0; frame < 20; frame++) await nextPhysicsFrame();
            SendKey(Key.W, false);
            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            Require(client.Paused, "Escape must pause (which saves the ship).");
            var beforeExit = session.Profile!;
            var ship = beforeExit.Ship ?? throw new InvalidOperationException("Pausing must save the ship condition.");
            Require(Math.Abs(ship.X - readWorld().Player.Position.X) < 0.01f && Math.Abs(ship.Y - readWorld().Player.Position.Y) < 0.01f
                && Math.Abs(ship.Hull - readWorld().Player.Hull) < 0.01f, "The saved ship condition must match the paused ship.");
            hud.FindChildren("ReloadProfile", "Button", true, false).Cast<Button>().Single().EmitSignal(Button.SignalName.Pressed);
            Require(Same(beforeExit, client.Session!.Profile!), "Reload from the pause menu must return the saved profile unchanged.");
            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            client.ReopenProfileFromDisk();
            var reopened = client.Session?.Profile ?? throw new InvalidOperationException($"Reopen failed: {client.ProfileStatus}");
            Require(Same(beforeExit, reopened), "Reopening the save folder must restore the exact wallet, inventory, upgrades, ship and revision.");
            var restored = readWorld().Player;
            Require(Math.Abs(restored.Position.X - ship.X) < 0.01f && Math.Abs(restored.Position.Y - ship.Y) < 0.01f && Math.Abs(restored.Hull - ship.Hull) < 0.01f,
                "The relaunched ship must be where it was saved, with the saved hull.");
            Require(restored.Definition.Physics.MaximumSpeed > baseSpeed, "The relaunched ship must keep its upgrade effect.");
            await nextPhysicsFrame();
            Require(hud.CreditsText == $"CREDITS  {reopened.Wallet:N0}", "The HUD must show the reopened wallet.");
            GD.Print($"STATION SMOKE PASS: reload and reopen restored revision {reopened.Revision}, wallet {reopened.Wallet}, hull {ship.Hull:0} at ({ship.X:0}, {ship.Y:0}), {reopened.Upgrades.Count} upgrade(s)");
            GD.Print("CLIENT_STATION_SMOKE_PASS: HUD, pickup save retry, docking, outpost trade, repair guard, upgrade effect, refusal, ship save, reload and reopen verified.");
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError($"CLIENT_STATION_SMOKE_FAILED: {exception.Message}");
            return false;
        }
        finally
        {
            foreach (var key in new[] { Key.W, Key.F, Key.Escape }) SendKey(key, false);
            client.ClearPickupSaveFailure();
            resetSector();
        }
    }

    private static bool Same(PlayerProfile a, PlayerProfile b) =>
        a.Revision == b.Revision && a.Wallet == b.Wallet && a.Inventory.SequenceEqual(b.Inventory) && a.Upgrades.SequenceEqual(b.Upgrades) && a.Ship == b.Ship;

    private void Press(string action) => FindAction(action).EmitSignal(Button.SignalName.Pressed);

    private Button FindAction(string action) =>
        client.Station.FindChildren("*", "Button", true, false).Cast<Button>()
            .Where(button => !button.IsQueuedForDeletion())
            .Single(button => button.HasMeta("station_action") && button.GetMeta("station_action").AsString() == action);

    private static void SendKey(Key key, bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed });
        Input.FlushBufferedEvents();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
