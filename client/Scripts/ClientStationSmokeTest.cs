using Godot;
using RogueTrader.Persistence;
using RogueTrader.Simulation;

namespace RogueTrader.Client;

/// <summary>
/// Queue M8 acceptance fixture: drives the real HUD, station screen and pause menu through native input and button signals and
/// checks every displayed value against the authoritative profile the services return, then reloads and reopens the save
/// folder. Runs against an isolated smoke save folder, never the player's profile.
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
            Require(profile.Revision == 0 && profile.Wallet == station.StartingWallet && profile.Inventory.Count == 0,
                "A fresh smoke profile must start at revision 0 with the station's starting wallet and an empty hold.");
            await nextPhysicsFrame();
            Require(hud.CreditsText == $"CREDITS  {profile.Wallet:N0}", $"HUD credits '{hud.CreditsText}' must match the profile wallet {profile.Wallet}.");
            GD.Print("STATION SMOKE PASS: new profile created and the HUD shows the authoritative wallet");

            // In-space pickup: fly forward from spawn through the launch pile, then confirm the reward reached the profile.
            resetSector();
            var pile = station.Collectibles.First(p => Math.Abs(p.X) < 1);
            SendKey(Key.W, true);
            for (var frame = 0; frame < 240 && session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == 0; frame++)
                await nextPhysicsFrame();
            SendKey(Key.W, false);
            Require(session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == pile.Quantity,
                $"Flying through the launch pile must commit {pile.Quantity} {pile.ItemId} to the profile.");
            Require(readWorld().Cargo.Quantity(pile.ItemId) == pile.Quantity, "The ship hold must match the committed pickup.");
            await nextPhysicsFrame();
            Require(hud.CargoText.Contains($"x{pile.Quantity}"), $"HUD cargo '{hud.CargoText}' must list the committed pickup.");
            GD.Print("STATION SMOKE PASS: in-space pickup committed through the transaction core and shown on the HUD");

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

            var buyGood = station.Goods.First(g => g.Buy is not null && g.Sell is not null && g.Id != pile.ItemId);
            var walletBefore = session.Profile!.Wallet;
            Press($"buy:{buyGood.Id}");
            Require(session.Profile!.Wallet == walletBefore - buyGood.Buy!.Value && session.Profile.Inventory.GetValueOrDefault(buyGood.Id) == 1,
                "Buy must debit the price and add one unit to the saved profile.");
            Require(readWorld().Cargo.Quantity(buyGood.Id) == 1, "The hold must mirror the purchase.");
            Require(client.Station.CreditsText == $"CREDITS  {session.Profile.Wallet:N0}", "The station screen must show the saved wallet after a purchase.");

            Press($"sellall:{pile.ItemId}");
            Require(session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == 0 && readWorld().Cargo.Quantity(pile.ItemId) == 0,
                "Sell all must empty that item from the profile and the hold.");
            var pileGood = station.Goods.First(g => g.Id == pile.ItemId);
            Require(session.Profile.Wallet == walletBefore - buyGood.Buy.Value + pile.Quantity * pileGood.Sell!.Value, "Sell all must credit the sale price.");
            GD.Print("STATION SMOKE PASS: market buy and sell update the saved profile, the hold and the station screen");

            var repair = station.Repairs[0];
            walletBefore = session.Profile.Wallet;
            Press($"repair:{repair.Id}");
            Require(session.Profile!.Wallet == walletBefore - repair.Price, "Repair must debit its price.");
            Require(readWorld().Player.Hull == readWorld().Player.Definition.Defense.MaximumHull, "Repair must never exceed maximum hull.");

            var upgrade = station.Upgrades.Where(u => u.Price <= session.Profile.Wallet).OrderBy(u => u.Price).First();
            Press($"upgrade:{upgrade.Id}");
            Require(session.Profile!.Upgrades.Contains(upgrade.Id), "Upgrade purchase must persist ownership.");
            Require(FindAction($"upgrade:{upgrade.Id}").Disabled, "An owned upgrade must be shown as owned.");

            var unaffordable = station.Upgrades.FirstOrDefault(u => u.Price > session.Profile.Wallet && !session.Profile.Upgrades.Contains(u.Id));
            if (unaffordable is not null)
            {
                var beforeRefusal = session.Profile;
                Press($"upgrade:{unaffordable.Id}");
                Require(ReferenceEquals(beforeRefusal, session.Profile), "A refused purchase must change nothing.");
                Require(client.Station.StatusText.Contains("not enough credits"), $"The refusal must be explained, got '{client.Station.StatusText}'.");
            }
            GD.Print("STATION SMOKE PASS: repair, upgrade ownership and an explained refusal");

            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            Require(!client.StationVisible && !client.Paused, "Escape must undock without pausing.");

            // Save/load: reload from disk via the pause menu, then drop every service and reopen the folder (exit/relaunch).
            var beforeExit = session.Profile!;
            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            hud.FindChildren("ReloadProfile", "Button", true, false).Cast<Button>().Single().EmitSignal(Button.SignalName.Pressed);
            Require(Same(beforeExit, client.Session!.Profile!), "Reload from the pause menu must return the saved profile unchanged.");
            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            client.ReopenProfileFromDisk();
            var reopened = client.Session?.Profile ?? throw new InvalidOperationException($"Reopen failed: {client.ProfileStatus}");
            Require(Same(beforeExit, reopened), "Reopening the save folder must restore the exact wallet, inventory, upgrades and revision.");
            Require(readWorld().Cargo.Quantity(buyGood.Id) == reopened.Inventory.GetValueOrDefault(buyGood.Id), "The hold must be rebuilt from the reopened profile.");
            await nextPhysicsFrame();
            Require(hud.CreditsText == $"CREDITS  {reopened.Wallet:N0}", "The HUD must show the reopened wallet.");
            GD.Print($"STATION SMOKE PASS: reload and reopen restored revision {reopened.Revision}, wallet {reopened.Wallet}, {reopened.Upgrades.Count} upgrade(s)");
            GD.Print("CLIENT_STATION_SMOKE_PASS: HUD wallet/cargo, pickup commit, docking, market, repair, upgrade, refusal, reload and reopen verified.");
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
            resetSector();
        }
    }

    private static bool Same(PlayerProfile a, PlayerProfile b) =>
        a.Revision == b.Revision && a.Wallet == b.Wallet && a.Inventory.SequenceEqual(b.Inventory) && a.Upgrades.SequenceEqual(b.Upgrades);

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
