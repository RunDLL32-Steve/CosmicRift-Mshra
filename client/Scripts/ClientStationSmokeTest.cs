using Godot;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;

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
            var spawn = readWorld().Catalog.Sector.PlayerSpawn;
            var pile = station.Collectibles.First(p => Math.Abs(p.X - spawn.X) < 1 && p.Y < spawn.Y); // straight ahead of the spawn
            client.SimulatePickupSaveFailure();
            SendKey(Key.W, true);
            for (var frame = 0; frame < 240 && readWorld().Cargo.Quantity(pile.ItemId) == 0; frame++)
                await nextPhysicsFrame();
            SendKey(Key.W, false);
            Require(readWorld().Cargo.Quantity(pile.ItemId) == pile.Quantity && session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == 0,
                "While saving is unavailable the collected cargo must stay in the hold, uncommitted.");
            client.ClearPickupSaveFailure();
            await RewardsSaved();
            Require(session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == pile.Quantity,
                $"Once saving works the {pile.Quantity} collected {pile.ItemId} must be committed to the profile.");
            await nextPhysicsFrame();
            Require(hud.CargoText.Contains($"x{pile.Quantity}"), $"HUD cargo '{hud.CargoText}' must list the committed pickup.");
            GD.Print("STATION SMOKE PASS: pickup survives a failed save and is then committed and shown on the HUD");

            // Kill reward (queue M7 slice): from the practice range centre the ship faces target_alpha, so holding fire destroys it.
            resetSector();
            Require(readWorld().Collectibles.All(p => p.Id != pile.Id), "A sector reset must not refill a collected pile.");
            Teleport(readWorld().Targets[0].Position + new System.Numerics.Vector2(0, 280));
            var walletBeforeKill = session.Profile!.Wallet;
            SendKey(Key.Space, true);
            for (var frame = 0; frame < 900 && readWorld().Kills.Count == 0; frame++)
                await nextPhysicsFrame();
            SendKey(Key.Space, false);
            Require(readWorld().Kills.Count == 1, "Holding fire from the range centre must destroy the practice target ahead.");
            await RewardsSaved();
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
            var damaged = readWorld().Player.Definition.Defense.MaximumHull - 15;
            readWorld().RestorePlayer(readWorld().Player.Position, readWorld().Player.Heading, damaged);
            resetSector();
            await nextPhysicsFrame();
            Require(client.WorldInstanceId != worldBeforeReset,
                "A reset that keeps the sector instance must still start a new world id, so pickup ids never repeat (review R1-1).");
            Require(readWorld().Targets.Single(t => t.Id == killedTarget).IsDestroyed && client.SectorInstanceId == instanceBeforeReset,
                "A sector reset must keep destroyed targets down and keep the sector instance, so the kill cannot pay again.");
            Require(session.Profile!.Wallet == walletBeforeKill + reward, "A sector reset must not pay any kill again.");
            Require(Math.Abs(readWorld().Player.Hull - damaged) < 0.01f && session.Profile.Ship is { } resetShip && Math.Abs(resetShip.Hull - damaged) < 0.01f,
                $"A sector reset must keep the ship's hull damage (Steve, Nov 3 2026), got {readWorld().Player.Hull}.");
            readWorld().RestorePlayer(readWorld().Player.Position, readWorld().Player.Heading, readWorld().Player.Definition.Defense.MaximumHull);
            GD.Print("STATION SMOKE PASS: a sector reset keeps the destroyed target down, keeps hull damage and pays nothing again");

            // Owned ships (Steve, Nov 3 2026): the picker offers the owned hull, the world waits while it is open, and choosing
            // the active ship keeps it exactly where it is.
            var activeHull = session.Profile!.ShipHullId ?? throw new InvalidOperationException("A new profile must fly its starter hull.");
            Require(session.Profile.OwnedShips.Keys.SequenceEqual(new[] { activeHull }) && client.ActiveHullId == activeHull,
                "A new profile must own exactly its starter hull and fly it.");
            readWorld().DamagePlayer(5);
            client.ShowShipPicker();
            var pickerTick = readWorld().TickNumber;
            var pickerWorld = client.WorldInstanceId;
            await nextPhysicsFrame();
            Require(client.ShipPickerVisible && client.Picker.OfferedHulls.SequenceEqual(new[] { activeHull }) && readWorld().TickNumber == pickerTick,
                "The ship picker must list the owned hull and hold the world still while it is open.");
            Require(client.Picker.Press(activeHull) && !client.ShipPickerVisible && client.WorldInstanceId == pickerWorld,
                "Choosing the ship already flown must close the picker and keep the same world.");
            var damagedHull = readWorld().Player.Definition.Defense.MaximumHull - 5;
            Require(Math.Abs(readWorld().Player.Hull - damagedHull) < 0.01f
                && session.Profile!.Ship is { } pickedShip && Math.Abs(pickedShip.Hull - damagedHull) < 0.01f,
                $"Picking a damaged ship must not repair it for free (Steve, Nov 4 2026), got {readWorld().Player.Hull}.");
            readWorld().RestorePlayer(readWorld().Player.Position, readWorld().Player.Heading, readWorld().Player.Definition.Defense.MaximumHull);
            GD.Print("STATION SMOKE PASS: the ship picker lists the owned hull, pauses the world and flies the chosen ship with its saved hull");

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
            // At full map size a clump spreads over its 350-pixel box, so visit the rest of this spawn's clump as a pilot would.
            var clump = mineral.Id[..(mineral.Id.LastIndexOf(':') + 1)];
            foreach (var rest in readWorld().Collectibles.Where(p => p.Id.StartsWith(clump, StringComparison.Ordinal)).Select(p => p.Position).ToArray())
            {
                Teleport(rest);
                await nextPhysicsFrame();
            }
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
            await RewardsSaved();
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

            // Goods sell only at the original Trading Posts (Steve, Nov 4 2026): fly to the first one and dock with the native key.
            resetSector();
            Require(readWorld().Cargo.Quantity(pile.ItemId) == pile.Quantity, "A sector reset must rebuild the hold from the saved profile.");
            var post = station.SitesOffering(SiteServices.Trade)[0];
            await DockAt(post);
            var dockedTick = readWorld().TickNumber;
            await nextPhysicsFrame();
            Require(readWorld().TickNumber == dockedTick, "The world must not advance while docked.");

            var trade = station.Trades.Single(t => t.InputItemId == pile.ItemId);
            var walletBeforeTrade = session.Profile!.Wallet; // after the kill reward above
            Press($"sellall:{pile.ItemId}");
            Require(session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == 0 && readWorld().Cargo.Quantity(pile.ItemId) == 0,
                "Trade all must hand the whole input stack in.");
            Require(session.Profile.Wallet == walletBeforeTrade + pile.Quantity * trade.CreditsPerUnit, "The recipe must pay its credits per unit.");
            Require(client.Station.CreditsText == $"CREDITS  {session.Profile.Wallet:N0}", "The station screen must show the saved wallet.");
            GD.Print($"STATION SMOKE PASS: {trade.Name} paid {pile.Quantity * trade.CreditsPerUnit} credits into the saved profile");
            var refinedTrade = station.Trades.Single(t => t.InputItemId == recipe.OutputItemId);
            var walletBeforeRefined = session.Profile.Wallet;
            Press($"sellall:{recipe.OutputItemId}");
            Require(session.Profile!.Wallet == walletBeforeRefined + refinedUnits * refinedTrade.CreditsPerUnit && !session.Profile.Inventory.ContainsKey(recipe.OutputItemId),
                "Trading the refined ore must pay its recipe value per unit.");
            GD.Print($"STATION SMOKE PASS: {refinedTrade.Name} paid {refinedUnits * refinedTrade.CreditsPerUnit} credits for the refined ore at {post.Name}");
            await Undock();

            // Repairs are sold only at the Repair Docks.
            var repairDock = station.SitesOffering(SiteServices.Repair)[0];
            await DockAt(repairDock);

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
            // Full Repair: 500 credits back to a full hull (Steve, Nov 4 2026).
            var fullRepair = station.Repairs.Single(r => r.Name == "Full Repair");
            readWorld().RestorePlayer(player.Position, player.Heading, 1);
            walletBefore = session.Profile!.Wallet;
            Press($"repair:{fullRepair.Id}");
            Require(fullRepair.Price == 500 && session.Profile!.Wallet == walletBefore - 500
                && Math.Abs(readWorld().Player.Hull - player.Definition.Defense.MaximumHull) < 0.01f,
                $"Full Repair must cost 500 credits and restore a full hull, got wallet {session.Profile.Wallet} and hull {readWorld().Player.Hull}.");
            GD.Print($"STATION SMOKE PASS: repair refused without charge at full hull and applied when damaged at {repairDock.Name}; Full Repair costs 500 for a full hull");
            await Undock();

            // Upgrades, hulls and the hangar are at the team's home store: Cold Harbor, where the first pilot spawns.
            Require(client.Team is { Index: 0, Name: "Cold Harbor" }, $"The first pilot must join Cold Harbor, got '{client.Team?.Name}'.");
            resetSector();
            SendKey(Key.F, true);
            SendKey(Key.F, false);
            await nextPhysicsFrame();
            Require(client.StationVisible && client.DockedSite?.StationId == station.StationId && client.Station.Service == SiteServices.Store
                && client.Station.TitleText == station.Name.ToUpperInvariant(),
                $"F at the Cold Harbor spawn must open its store, got '{client.Station.TitleText}'.");

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

            // Ship Hulls page: the twelve original exchanges at their original price, open only to a pilot of the same race.
            var hulls = station.Ships ?? throw new InvalidOperationException("The shipped station must sell the original hulls.");
            var active = session.Profile!.ShipHullId!;
            var sameRace = hulls.First(h => h.HullId != active && h.RequiresOneOf!.Contains(active));
            var otherRace = hulls.First(h => !h.RequiresOneOf!.Any(session.Profile.OwnedShips.ContainsKey));
            Require(hulls.Count == 12 && FindAction($"ship:{active}").Disabled && FindAction($"ship:{otherRace.HullId}").Disabled
                && !FindAction($"ship:{sameRace.HullId}").Disabled,
                "The Ship Hulls page must list 12 hulls, mark the owned one and close hulls of another race.");
            beforeRefusal = session.Profile;
            Press($"ship:{sameRace.HullId}");
            Require(sameRace.Price > beforeRefusal.Wallet && ReferenceEquals(beforeRefusal, session.Profile) && client.Station.StatusText.Contains("not enough credits"),
                $"An unaffordable hull must be refused and change nothing, got '{client.Station.StatusText}'.");
            GD.Print($"STATION SMOKE PASS: the Ship Hulls page sells the original hulls at {sameRace.Price:N0} credits to a same-race pilot only");
            var flying = FindAction($"hangar:{active}");
            Require(flying.Disabled && flying.Text == "FLYING", "The Hangar must list the ship being flown as FLYING.");
            GD.Print("STATION SMOKE PASS: the Hangar at Cold Harbor lists the owned ships (switching is covered by the unit tests)");

            SendKey(Key.Escape, true);
            SendKey(Key.Escape, false);
            await nextPhysicsFrame();
            Require(!client.StationVisible && !client.Paused, "Escape must undock without pausing.");

            // A destroyed ship costs 20% of the credits held, saved once, then respawns at the spawn with a full hull, keeping
            // weapons and upgrades; the cargo in the hold leaves the profile and spills into space where the ship died (Steve, Nov 3-4 2026).
            // Load the hold first through the same server grant a pickup uses, so there is cargo to lose.
            var lootResult = await session.GrantPickupAsync("smoke-destruction", new CargoPickup(1, "smoke-loot", pile.ItemId, pile.Quantity, 0));
            Require(lootResult.Status == TransactionStatus.Applied && session.Profile!.Inventory.GetValueOrDefault(pile.ItemId) == pile.Quantity,
                "The smoke cargo grant must be saved before the destruction.");
            var beforeDeath = session.Profile!;
            var worldBeforeDeath = client.WorldInstanceId;
            // Die away from the spawn, so the respawned ship does not sweep its own spilled cargo straight back up.
            var deathPoint = readWorld().Player.Position + new System.Numerics.Vector2(0, 300);
            readWorld().RestorePlayer(deathPoint, readWorld().Player.Heading, readWorld().Player.Hull);
            readWorld().DamagePlayer(float.MaxValue);
            for (var frame = 0; frame < 10 && client.WorldInstanceId == worldBeforeDeath; frame++) await nextPhysicsFrame();
            var penalty = beforeDeath.Wallet / 100 * TransactionProcessor.DestructionPenaltyPercent + beforeDeath.Wallet % 100 * TransactionProcessor.DestructionPenaltyPercent / 100;
            var afterDeath = session.Profile!;
            Require(beforeDeath.Wallet > 0 && afterDeath.Wallet == beforeDeath.Wallet - penalty && afterDeath.Ledger[^1].Kind == TransactionKind.ShipDestroyed,
                $"A destroyed ship must cost {penalty} credits (20% of {beforeDeath.Wallet}), got wallet {afterDeath.Wallet}.");
            var lostUnits = beforeDeath.Inventory.Values.Sum();
            // The hull itself also leaves its original wreck, VEH f[68..69] = 2008 x1 (Wreckage) on every original hull (RT-F043).
            var hullWreck = readWorld().Player.Definition.Wreck;
            var expectedDrop = WorldSimulation.DestructionDrop(beforeDeath.Inventory, hullWreck);
            var wreck = readWorld().Collectibles.Where(c => c.Id.StartsWith(WorldSimulation.WreckPilePrefix, StringComparison.Ordinal)).ToArray();
            Require(lostUnits > 0 && hullWreck is { ItemId: "2008", Quantity: 1 } && wreck.All(c => c.ExpiresAt is not null)
                && wreck.OrderBy(c => c.ItemId, StringComparer.Ordinal).Select(c => KeyValuePair.Create(c.ItemId, c.Quantity)).SequenceEqual(expectedDrop)
                && wreck.All(c => System.Numerics.Vector2.Distance(c.Position, deathPoint) < WorldSimulation.WreckSpreadRadius + 1),
                $"The destroyed ship's {lostUnits} cargo units and its hull's Wreckage must spill into space as expiring piles, got {wreck.Sum(c => c.Quantity)}.");
            Require(afterDeath.Inventory.Count == 0 && readWorld().Cargo.Quantity(pile.ItemId) == 0 && client.PendingPickups == 0
                && afterDeath.Upgrades.SequenceEqual(beforeDeath.Upgrades)
                && afterDeath.OwnedShips.Keys.SequenceEqual(beforeDeath.OwnedShips.Keys) && !client.DestructionPending,
                "A destroyed ship must lose its cargo and keep its upgrades and owned ships.");
            Require(client.WorldInstanceId != worldBeforeDeath && !readWorld().Player.IsDestroyed
                && Math.Abs(readWorld().Player.Hull - readWorld().Player.Definition.Defense.MaximumHull) < 0.01f,
                "A destroyed ship must respawn with a full hull.");
            await nextPhysicsFrame();
            Require(hud.NoticeText.Contains("Ship destroyed") && hud.CreditsText == $"CREDITS  {afterDeath.Wallet:N0}",
                $"The HUD must report the loss and the new wallet, got '{hud.NoticeText}' / '{hud.CreditsText}'.");
            GD.Print($"STATION SMOKE PASS: a destroyed ship cost {penalty:N0} credits (20%) once, dropped its cargo and 1 Wreckage where it died and respawned with its upgrades");

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
            Require(client.PendingPickups == 0 && client.PendingKillRewards == 0,
                "A reopen must not re-queue pickups or kills that were already saved (review R1-2).");
            Require(Same(beforeExit, reopened), "Reopening the save folder must restore the exact wallet, inventory, upgrades, ship and revision.");
            var restored = readWorld().Player;
            Require(Math.Abs(restored.Position.X - ship.X) < 0.01f && Math.Abs(restored.Position.Y - ship.Y) < 0.01f && Math.Abs(restored.Hull - ship.Hull) < 0.01f,
                "The relaunched ship must be where it was saved, with the saved hull.");
            Require(restored.Definition.Physics.MaximumSpeed > baseSpeed, "The relaunched ship must keep its upgrade effect.");
            await nextPhysicsFrame();
            Require(hud.CreditsText == $"CREDITS  {reopened.Wallet:N0}", "The HUD must show the reopened wallet.");
            GD.Print($"STATION SMOKE PASS: reload and reopen restored revision {reopened.Revision}, wallet {reopened.Wallet}, hull {ship.Hull:0} at ({ship.X:0}, {ship.Y:0}), {reopened.Upgrades.Count} upgrade(s)");
            GD.Print("CLIENT_STATION_SMOKE_PASS: HUD, pickup save retry, kill reward and duplicate, no target re-arm on reset with a new world id, original mineral pickup kept across a failed save and reload then saved once, refinery dock and refine, Trading Post trade of wreckage and refined ore, Repair Dock repair guard, Cold Harbor store and hangar for the first pilot's team, upgrade effect, inactive-upgrade and funds refusals, no pile respawn on reset, reload or relaunch, sector kept across relaunch, ship picker keeping hull damage, Full Repair for 500, destruction penalty with the cargo dropped into space, ship save, reload and reopen verified.");
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

    /// <summary>Sector saves are coalesced to one per half second and rewards wait for them, so allow up to a second.</summary>
    private async Task RewardsSaved()
    {
        await nextPhysicsFrame();
        for (var frame = 0; frame < 60 && (client.PendingPickups > 0 || client.PendingKillRewards > 0); frame++) await nextPhysicsFrame();
    }

    private static bool Same(PlayerProfile a, PlayerProfile b) =>
        a.Revision == b.Revision && a.Wallet == b.Wallet && a.Inventory.SequenceEqual(b.Inventory) && a.Upgrades.SequenceEqual(b.Upgrades) && a.Ship == b.Ship;

    /// <summary>Flies straight onto a site (a teleport stands in for the flight) and docks there with the native key.</summary>
    private async Task DockAt(RogueTrader.Trading.ServiceSite site)
    {
        Teleport(new System.Numerics.Vector2(site.X, site.Y));
        await nextPhysicsFrame();
        SendKey(Key.F, true);
        SendKey(Key.F, false);
        await nextPhysicsFrame();
        Require(client.StationVisible && client.DockedSite?.StationId == site.StationId && client.Station.Service == site.Service
            && client.Station.TitleText == site.Name.ToUpperInvariant(),
            $"F at {site.Name} must open its {site.Service} page, got '{client.Station.TitleText}'.");
    }

    private async Task Undock()
    {
        SendKey(Key.Escape, true);
        SendKey(Key.Escape, false);
        await nextPhysicsFrame();
        Require(!client.StationVisible, "Escape must undock.");
    }

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
