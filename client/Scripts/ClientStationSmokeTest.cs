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

            // Kill reward (queue M7 slice): the spawn faces target_alpha, so holding fire destroys it.
            resetSector();
            Require(readWorld().Collectibles.All(p => p.Id != pile.Id), "A sector reset must not refill a collected pile.");
            var walletBeforeKill = session.Profile!.Wallet;
            SendKey(Key.Space, true);
            for (var frame = 0; frame < 900 && readWorld().Kills.Count == 0; frame++)
                await nextPhysicsFrame();
            SendKey(Key.Space, false);
            Require(readWorld().Kills.Count == 1, "Holding fire from spawn must destroy the practice target ahead.");
            await nextPhysicsFrame();
            var reward = session.KillReward.Credits;
            Require(reward > 0 && session.Profile!.Wallet == walletBeforeKill + reward && client.PendingKillRewards == 0,
                $"The kill must pay {reward} credits into the saved profile, got wallet {session.Profile!.Wallet} from {walletBeforeKill}.");
            var replay = await session.GrantKillRewardAsync(client.SectorInstanceId, readWorld().Kills[0]);
            Require(replay is { Status: TransactionStatus.Applied, IsReplay: true } && session.Profile!.Wallet == walletBeforeKill + reward,
                "Replaying the same kill must be recognised as a duplicate and pay nothing.");
            await nextPhysicsFrame();
            Require(hud.NoticeText.Contains($"+{reward:N0} credits") && hud.CreditsText == $"CREDITS  {session.Profile.Wallet:N0}",
                $"The HUD must show the kill reward and the new wallet, got '{hud.NoticeText}' / '{hud.CreditsText}'.");
            GD.Print($"STATION SMOKE PASS: destroying a target paid {reward} credits once; the replayed kill was a duplicate");

            // A sector reset must not bring the destroyed target back early (targets respawn only as a group, on a timer).
            var killedTarget = readWorld().Kills[0].TargetId;
            var instanceBeforeReset = client.SectorInstanceId;
            var worldBeforeReset = client.WorldInstanceId;
            resetSector();
            await nextPhysicsFrame();
            Require(client.WorldInstanceId != worldBeforeReset,
                "A reset that keeps the sector instance must still start a new world id, so pickup ids never repeat (review R1-1).");
            Require(readWorld().Targets.Single(t => t.Id == killedTarget).IsDestroyed && client.SectorInstanceId == instanceBeforeReset,
                "A sector reset must keep destroyed targets down and keep the sector instance, so the kill cannot pay again.");
            Require(session.Profile!.Wallet == walletBeforeKill + reward, "A sector reset must not pay any kill again.");
            GD.Print("STATION SMOKE PASS: a sector reset keeps the destroyed target down and pays nothing again");

            // Mining (queue M7 slice): fly onto an original Tellerium spawn's pile while saving fails, reload, then let it save.
            resetSector();
            await nextPhysicsFrame();
            var refinery = station.Refinery ?? throw new InvalidOperationException("The shipped station must carry the refinery.");
            var recipe = refinery.Recipes.Single(r => r.InputItemId == "2003");
            var mineral = readWorld().Collectibles.FirstOrDefault(p => p.ItemId == "2003" && p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal))
                ?? throw new InvalidOperationException("A fresh sector must hold a Tellerium pile from an original mineral spawn.");
            client.SimulatePickupSaveFailure();
            var pickupsBefore = readWorld().Pickups.Count;
            Teleport(mineral.Position);
            for (var frame = 0; frame < 30 && readWorld().Pickups.Count == pickupsBefore; frame++) await nextPhysicsFrame();
            // Piles are 1 unit and lie close together, so the ship may sweep up several Tellerium piles at once; count them all.
            var mined = readWorld().Pickups.Skip(pickupsBefore).Where(p => p.IsMineral && p.ItemId == "2003").Sum(p => p.Quantity);
            Require(readWorld().Pickups.Skip(pickupsBefore).Any(p => p.PileId == mineral.Id), "The ship must have collected the Tellerium pile it was sent to.");
            var savedOre = session.Profile!.Inventory.GetValueOrDefault("2003");
            Require(mined > 0 && readWorld().Cargo.Quantity("2003") == savedOre + mined && client.PendingPickups > 0,
                $"The mined Tellerium must be in the hold but not yet saved (mined {mined}, pending {client.PendingPickups}).");
            client.ReloadProfile();
            Require(readWorld().Cargo.Quantity("2003") == savedOre + mined && client.PendingPickups > 0,
                "A reload must keep unsaved pickups queued and in the hold.");
            client.ClearPickupSaveFailure();
            await nextPhysicsFrame();
            Require(session.Profile!.Inventory.GetValueOrDefault("2003") == savedOre + mined && client.PendingPickups == 0,
                $"Once saving works the {mined} mined Tellerium must be saved exactly once.");
            var minedPickup = readWorld().Pickups.Skip(pickupsBefore).First(p => p.PileId == mineral.Id);
            Require((await session.GrantPickupAsync(client.WorldInstanceId, minedPickup)).IsReplay,
                "Granting the same pickup again must be recognised as a duplicate.");
            await nextPhysicsFrame();
            Require(hud.NoticeText.Contains("Mined") && hud.NoticeText.Contains("Tellerium"), $"The HUD must report the mined ore, got '{hud.NoticeText}'.");
            GD.Print($"STATION SMOKE PASS: mined {mined} Tellerium from an original spawn; it survived a failed save and a reload and was saved once");

            // Refine it at the refinery (F docks there; only the Refinery page is offered).
            Teleport(new System.Numerics.Vector2(refinery.X, refinery.Y));
            await nextPhysicsFrame();
            SendKey(Key.F, true);
            SendKey(Key.F, false);
            await nextPhysicsFrame();
            Require(client.AtRefinery && client.Station.RefineryMode && client.Station.TitleText == refinery.Name.ToUpperInvariant(),
                "F at the refinery must open the refinery page.");
            Press($"refineall:{recipe.Id}");
            var refinedUnits = session.Profile!.Inventory.GetValueOrDefault(recipe.OutputItemId);
            Require(refinedUnits == savedOre + mined && !session.Profile.Inventory.ContainsKey("2003") && readWorld().Cargo.Quantity(recipe.OutputItemId) == refinedUnits,
                $"Refine all must turn the {savedOre + mined} Tellerium into Refined Tellerium in profile and hold.");
            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            Require(!client.StationVisible, "Escape must undock from the refinery.");
            GD.Print($"STATION SMOKE PASS: refined {refinedUnits} Tellerium into {station.NameOf(recipe.OutputItemId)} at the {refinery.Name}");

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
            Require(session.Profile.Wallet == walletBeforeKill + reward + pile.Quantity * trade.CreditsPerUnit, "The recipe must pay its credits per unit.");
            Require(client.Station.CreditsText == $"CREDITS  {session.Profile.Wallet:N0}", "The station screen must show the saved wallet.");
            GD.Print($"STATION SMOKE PASS: {trade.Name} paid {pile.Quantity * trade.CreditsPerUnit} credits into the saved profile");
            var refinedTrade = station.Trades.Single(t => t.InputItemId == recipe.OutputItemId);
            var walletBeforeRefined = session.Profile.Wallet;
            Press($"sellall:{recipe.OutputItemId}");
            Require(session.Profile!.Wallet == walletBeforeRefined + refinedUnits * refinedTrade.CreditsPerUnit && !session.Profile.Inventory.ContainsKey(recipe.OutputItemId),
                "Trading the refined ore must pay its recipe value per unit.");
            GD.Print($"STATION SMOKE PASS: {refinedTrade.Name} paid {refinedUnits * refinedTrade.CreditsPerUnit} credits for the refined ore");

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
            var worldBeforeUpgrade = readWorld();
            var pilesBeforeUpgrade = worldBeforeUpgrade.Collectibles.Select(p => p.Id).ToArray();
            Press($"upgrade:{upgrade.Id}");
            Require(session.Profile!.Upgrades.Contains(upgrade.Id), "Upgrade purchase must persist ownership.");
            Require(readWorld().Player.Definition.Physics.MaximumSpeed > baseSpeed, $"{upgrade.Name} must raise maximum speed.");
            Require(Math.Abs(readWorld().Player.Hull - hullBeforeUpgrade) < 0.01f, "Installing an upgrade must keep the ship's hull.");
            Require(FindAction($"upgrade:{upgrade.Id}").Text == "ACTIVE", "An owned upgrade must be shown as active.");
            Require(ReferenceEquals(worldBeforeUpgrade, readWorld()) && readWorld().Collectibles.Select(p => p.Id).SequenceEqual(pilesBeforeUpgrade),
                "Installing an upgrade must change the ship in place, not rebuild the world or respawn piles.");
            var noEffect = station.Upgrades.First(u => u.Effect == RogueTrader.Trading.UpgradeEffect.None);
            Require(FindAction($"upgrade:{noEffect.Id}").Disabled, "An upgrade with no effect must not be offered.");
            beforeRefusal = session.Profile;
            Press($"upgrade:{noEffect.Id}");
            Require(ReferenceEquals(beforeRefusal, session.Profile), "An upgrade with no effect must not be charged.");
            var unaffordable = station.Upgrades.First(u => u.Price > session.Profile.Wallet && session.UpgradeRefusal(u.Id) == RejectionReason.None);
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
            var pilesBeforeReload = readWorld().Collectibles.Select(p => p.Id).ToArray();
            var ship = beforeExit.Ship ?? throw new InvalidOperationException("Pausing must save the ship condition.");
            Require(Math.Abs(ship.X - readWorld().Player.Position.X) < 0.01f && Math.Abs(ship.Y - readWorld().Player.Position.Y) < 0.01f
                && Math.Abs(ship.Hull - readWorld().Player.Hull) < 0.01f, "The saved ship condition must match the paused ship.");
            hud.FindChildren("ReloadProfile", "Button", true, false).Cast<Button>().Single().EmitSignal(Button.SignalName.Pressed);
            Require(Same(beforeExit, client.Session!.Profile!), "Reload from the pause menu must return the saved profile unchanged.");
            Require(readWorld().Collectibles.Select(p => p.Id).SequenceEqual(pilesBeforeReload), "A profile reload must not respawn collected piles.");
            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            var sectorBeforeExit = readWorld().CaptureSector();
            var instanceBeforeExit = client.SectorInstanceId;
            client.ReopenProfileFromDisk();
            Require(readWorld().Collectibles.Select(p => p.Id).SequenceEqual(sectorBeforeExit.Piles.Select(p => p.Id)) && readWorld().Collectibles.All(p => p.Id != pile.Id),
                "A relaunch must continue the saved sector: no collected pile may come back.");
            Require(client.SectorInstanceId == instanceBeforeExit && readWorld().TargetWave == sectorBeforeExit.TargetWave
                && (sectorBeforeExit.TargetRespawnTicksRemaining is not > 600 || readWorld().Targets.All(t => t.IsDestroyed)),
                "A relaunch must keep the target group, its wave and its sector instance, so no kill can pay again.");
            var reopened = client.Session?.Profile ?? throw new InvalidOperationException($"Reopen failed: {client.ProfileStatus}");
            Require(Same(beforeExit, reopened), "Reopening the save folder must restore the exact wallet, inventory, upgrades, ship and revision.");
            var restored = readWorld().Player;
            Require(Math.Abs(restored.Position.X - ship.X) < 0.01f && Math.Abs(restored.Position.Y - ship.Y) < 0.01f && Math.Abs(restored.Hull - ship.Hull) < 0.01f,
                "The relaunched ship must be where it was saved, with the saved hull.");
            Require(restored.Definition.Physics.MaximumSpeed > baseSpeed, "The relaunched ship must keep its upgrade effect.");
            await nextPhysicsFrame();
            Require(hud.CreditsText == $"CREDITS  {reopened.Wallet:N0}", "The HUD must show the reopened wallet.");
            GD.Print($"STATION SMOKE PASS: reload and reopen restored revision {reopened.Revision}, wallet {reopened.Wallet}, hull {ship.Hull:0} at ({ship.X:0}, {ship.Y:0}), {reopened.Upgrades.Count} upgrade(s)");
            GD.Print("CLIENT_STATION_SMOKE_PASS: HUD, pickup save retry, kill reward and duplicate, no target re-arm on reset with a new world id, original mineral pickup kept across a failed save and reload then saved once, refinery dock and refine, outpost trade of wreckage and refined ore, repair guard, upgrade effect, inactive-upgrade and funds refusals, no pile respawn on reset, reload or relaunch, sector kept across relaunch, ship save, reload and reopen verified.");
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError($"CLIENT_STATION_SMOKE_FAILED: {exception.Message}");
            return false;
        }
        finally
        {
            foreach (var key in new[] { Key.W, Key.F, Key.Escape, Key.Space }) SendKey(key, false);
            client.ClearPickupSaveFailure();
            resetSector();
        }
    }

    private static bool Same(PlayerProfile a, PlayerProfile b) =>
        a.Revision == b.Revision && a.Wallet == b.Wallet && a.Inventory.SequenceEqual(b.Inventory) && a.Upgrades.SequenceEqual(b.Upgrades) && a.Ship == b.Ship;

    private void Teleport(System.Numerics.Vector2 position)
    {
        var player = readWorld().Player;
        readWorld().RestorePlayer(position, player.Heading, player.Hull);
    }

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
