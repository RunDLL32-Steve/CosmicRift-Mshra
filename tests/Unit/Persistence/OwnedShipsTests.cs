using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using RogueTrader.Persistence;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>
/// Schema 6 owned ships (Steve, Nov 3 2026): one of each hull on the account, the active ship chosen at load, and a destroyed
/// ship that respawns with everything it carried while the player pays 20% of their credits, exactly once per destruction.
/// </summary>
public sealed class OwnedShipsTests
{
    [Fact]
    public async Task ANewProfileOwnsItsStarterHullAndFliesIt()
    {
        var env = await Env.CreateAsync();
        Assert.Equal(Fx.Hull, env.Snapshot.ShipHullId);
        Assert.Equal(new[] { new KeyValuePair<string, ShipCondition?>(Fx.Hull, null) }, env.Snapshot.OwnedShips);
    }

    [Fact]
    public async Task BuyingAHullAddsItOnceAndChargesTheStationPrice()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        var bought = await env.BuyShip("buy-hull-2", Fx.Hull2);
        Assert.Equal(TransactionStatus.Applied, bought.Status);
        Assert.Equal(-600, bought.CurrencyDelta);
        Assert.Equal(400, env.Snapshot.Wallet);
        Assert.Equal(new[] { Fx.Hull, Fx.Hull2 }, env.Snapshot.OwnedShips.Keys);
        Assert.Equal(Fx.Hull, env.Snapshot.ShipHullId); // buying does not switch ships

        // One of each: a second purchase is refused and charges nothing; a replay of the first returns its original outcome.
        var again = await env.BuyShip(null, Fx.Hull2);
        Assert.Equal(RejectionReason.ShipAlreadyOwned, again.Reason);
        var replay = await env.BuyShip("buy-hull-2", Fx.Hull2);
        Assert.True(replay.IsReplay);
        Assert.Equal(400, env.Snapshot.Wallet);
        Assert.Equal(RejectionReason.ShipAlreadyOwned, (await env.BuyShip(null, Fx.Hull)).Reason);
    }

    [Fact]
    public async Task BuyingAHullNeedsTheStationTheFundsAndAHullForSale()
    {
        var env = await Env.CreateAsync(wallet: 599);
        Assert.Equal(RejectionReason.StationAccessDenied, (await env.BuyShip(null, Fx.Hull2, stationAccess: false)).Reason);
        Assert.Equal(RejectionReason.InsufficientFunds, (await env.BuyShip(null, Fx.Hull2)).Reason);
        Assert.Equal(RejectionReason.NotForSale, (await env.BuyShip(null, "fixture-hull-unsold")).Reason);
        Assert.Equal(new[] { Fx.Hull }, env.Snapshot.OwnedShips.Keys);
        Assert.Equal(599, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task SwitchingShipsParksEachOneExactlyAsItWas()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        await env.BuyShip(null, Fx.Hull2);
        Assert.True((await env.SaveShip(35, 100, 200, 1)).Saved);

        var switched = await env.Select(Fx.Hull2);
        Assert.True(switched.Saved);
        Assert.Equal(Fx.Hull2, env.Snapshot.ShipHullId);
        Assert.Null(env.Snapshot.Ship); // never flown: starts at the spawn with a full hull
        Assert.Equal(new ShipCondition(35, 100, 200, 1), env.Snapshot.OwnedShips[Fx.Hull]);
        Assert.Null(env.Snapshot.OwnedShips[Fx.Hull2]);

        Assert.True((await env.SaveShip(80, -5, -6, 2)).Saved);
        Assert.True((await env.Select(Fx.Hull)).Saved);
        // The damaged first ship comes back damaged: switching is never a free repair.
        Assert.Equal(new ShipCondition(35, 100, 200, 1), env.Snapshot.Ship);
        Assert.Equal(new ShipCondition(80, -5, -6, 2), env.Snapshot.OwnedShips[Fx.Hull2]);
        Assert.Null(env.Snapshot.OwnedShips[Fx.Hull]);

        var revision = env.Snapshot.Revision;
        Assert.True((await env.Select(Fx.Hull)).Saved); // already active: nothing saved
        Assert.Equal(revision, env.Snapshot.Revision);
        Assert.Equal(RejectionReason.ShipNotOwned, (await env.Select(Fx.Hull3)).Reason);
        Assert.Equal(revision, env.Snapshot.Revision);
    }

    [Theory]
    [InlineData(1000, 200)]
    [InlineData(1234, 246)] // 20% rounded down to whole credits
    [InlineData(4, 0)]
    [InlineData(0, 0)]
    [InlineData(long.MaxValue, long.MaxValue / 5)]
    public async Task ADestroyedShipCostsTwentyPercentOfTheCreditsHeld(long wallet, long penalty)
    {
        var env = await Env.CreateAsync(wallet: wallet);
        var result = await env.Destroyed("destroyed-1");
        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(-penalty, result.CurrencyDelta);
        Assert.Equal(wallet - penalty, env.Snapshot.Wallet);
        Assert.Equal(TransactionKind.ShipDestroyed, env.Snapshot.Ledger[^1].Kind);
    }

    [Fact]
    public async Task ADestroyedShipLosesItsCargoKeepsWeaponsUpgradesAndOwnedShipsAndRespawnsAtFullHull()
    {
        var env = await Env.CreateAsync(wallet: 2000);
        await env.Buy(null, Fx.OreA, 3);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        await env.BuyShip(null, Fx.Hull2);
        await env.SaveShip(12, 50, 60, 0.5f);
        var before = env.Snapshot;

        var result = await env.Destroyed("destroyed-1");

        var after = env.Snapshot;
        Assert.Empty(after.Inventory); // the cargo is lost with the ship (Steve, Nov 3 2026)
        Assert.Equal(-3, result.ItemDeltas[Fx.OreA]);
        Assert.Equal(before.Upgrades, after.Upgrades);
        Assert.Equal(before.OwnedShips, after.OwnedShips);
        Assert.Equal(before.ShipHullId, after.ShipHullId);
        Assert.Null(after.Ship); // back at the spawn with a full hull
        Assert.Equal(before.Wallet - before.Wallet / 5, after.Wallet);
    }

    [Fact]
    public async Task OneDestructionIsChargedExactlyOnceAcrossRetriesAndReloads()
    {
        var repo = new InMemoryProfileRepository(new PersistenceOptions());
        var env = await Env.CreateAsync(wallet: 1000, repo: repo);
        Assert.Equal(-200, (await env.Destroyed("destroyed-world-a-77")).CurrencyDelta);
        var replay = await env.Destroyed("destroyed-world-a-77");
        Assert.True(replay.IsReplay);
        Assert.Equal(800, env.Snapshot.Wallet);

        // A reload (or a relaunch: the id window is saved with the profile) still recognises the same destruction.
        Assert.True((await env.Proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
        Assert.True((await env.Destroyed("destroyed-world-a-77")).IsReplay);
        Assert.Equal(800, env.Snapshot.Wallet);

        // A later destruction is a new one and is charged on what is left.
        Assert.Equal(-160, (await env.Destroyed("destroyed-world-a-900")).CurrencyDelta);
        Assert.Equal(640, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task APenaltyThatFailsToSaveChargesNothingAndRetriesWithTheSameId()
    {
        var faulty = new FaultInjectingProfileRepository(new InMemoryProfileRepository(new PersistenceOptions()));
        var env = await Env.CreateAsync(wallet: 1000, repo: faulty);
        faulty.Mode = FaultMode.FailBeforeWrite;
        Assert.Equal(RejectionReason.PersistenceFailed, (await env.Destroyed("destroyed-1")).Reason);
        faulty.Mode = FaultMode.None;
        Assert.True((await env.Proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
        Assert.Equal(1000, env.Snapshot.Wallet);
        Assert.Equal(-200, (await env.Destroyed("destroyed-1")).CurrencyDelta);

        // Saved, then the acknowledgement is lost: the retry is a replay, not a second charge.
        faulty.Mode = FaultMode.ThrowAfterWrite;
        Assert.Equal(RejectionReason.PersistenceFailed, (await env.Destroyed("destroyed-2")).Reason);
        faulty.Mode = FaultMode.None;
        Assert.True((await env.Proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
        Assert.True((await env.Destroyed("destroyed-2")).IsReplay);
        Assert.Equal(640, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task OnlyTheActiveShipCanBeDestroyedAndAWreckCannotBeSavedToDodgeThePenalty()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        await env.BuyShip(null, Fx.Hull2);
        Assert.Equal(RejectionReason.ShipNotActive, (await env.Destroyed("destroyed-1", Fx.Hull2)).Reason);
        Assert.Equal(400, env.Snapshot.Wallet);

        // Saving hull 0 would let a client reload a "destroyed" ship without paying: refused.
        var wreck = await env.SaveShip(0);
        Assert.False(wreck.Saved);
        Assert.Equal(RejectionReason.InvalidRequest, wreck.Reason);
    }

    [Fact]
    public async Task ASchemaFiveSaveMigratesItsHullIntoOwnedShipsAndKeepsEverythingElse()
    {
        using var dir = new TempDirectory();
        var env = await Env.CreateAsync(wallet: 700);
        await env.Buy(null, Fx.OreA, 2);
        await env.SaveShip(44, 10, 20, 0.25f);
        var current = env.Snapshot;
        var schema5 = AsSchemaFive(current);
        var primary = dir.File(Fx.ProfileId + ".profile.json");
        await File.WriteAllBytesAsync(primary, schema5);

        using (var repo = FileProfileRepository.Open(dir.Path, new PersistenceOptions(), Array.Empty<IProfileMigration>()))
        {
            var loaded = await repo.LoadAsync(Fx.ProfileId);
            Assert.Equal(ProfileLoadStatus.Loaded, loaded.Status);
            Assert.Contains(loaded.Diagnostics, d => d.Code == DiagnosticCode.MigrationApplied);
            ProfileAssert.Equivalent(current, loaded.Profile!);

            // The schema 5 file is kept byte for byte before anything is written at schema 6, and survives later saves.
            var copy = dir.File(Fx.ProfileId + ".profile.schema5.json");
            Assert.Equal(schema5, await File.ReadAllBytesAsync(copy));
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.SaveAsync(loaded.Profile! with { Revision = loaded.Profile!.Revision + 1, Wallet = 1 }, loaded.Profile.Revision)).Status);
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.SaveAsync(loaded.Profile! with { Revision = loaded.Profile!.Revision + 2, Wallet = 2 }, loaded.Profile.Revision + 1)).Status);
            Assert.Equal(schema5, await File.ReadAllBytesAsync(copy));
        }
    }

    [Fact]
    public void ASchemaFiveProfileWithNoHullOwnsNoShips()
    {
        var bytes = AsSchemaFive(PlayerProfile.CreateNew(Fx.ProfileId, 5, null));
        var decoded = ProfileSerializer.Decode(bytes, Array.Empty<IProfileMigration>());
        Assert.True(decoded.Succeeded);
        Assert.Null(decoded.Profile!.ShipHullId);
        Assert.Empty(decoded.Profile.OwnedShips);
    }

    [Fact]
    public void InvariantsKeepTheActiveShipOwnedAndItsConditionInShip()
    {
        var options = new PersistenceOptions();
        var good = PlayerProfile.CreateNew(Fx.ProfileId, 0, Fx.Hull);
        Assert.Empty(ProfileInvariants.Validate(good, options));
        Assert.NotEmpty(ProfileInvariants.Validate(good with { ShipHullId = Fx.Hull2 }, options));
        Assert.NotEmpty(ProfileInvariants.Validate(good with { ShipHullId = null }, options));
        Assert.NotEmpty(ProfileInvariants.Validate(good with { OwnedShips = good.OwnedShips.SetItem(Fx.Hull, new ShipCondition(1, 0, 0, 0)) }, options));
        Assert.NotEmpty(ProfileInvariants.Validate(good with { OwnedShips = good.OwnedShips.Add(Fx.Hull2, new ShipCondition(float.NaN, 0, 0, 0)) }, options));
        Assert.NotEmpty(ProfileInvariants.Validate(good with { OwnedShips = good.OwnedShips.Add("", null) }, options));
        Assert.Empty(ProfileInvariants.Validate(good with { OwnedShips = good.OwnedShips.Add(Fx.Hull2, new ShipCondition(3, 4, 5, 6)) }, options));
    }

    /// <summary>Rewrites a current profile as the schema 5 file an older build wrote: no <c>ownedShips</c>, version 5.</summary>
    private static byte[] AsSchemaFive(PlayerProfile profile)
    {
        var envelope = JsonNode.Parse(ProfileSerializer.Serialize(profile))!.AsObject();
        var payload = JsonNode.Parse(envelope["payload"]!.GetValue<string>())!.AsObject();
        payload.Remove("ownedShips");
        var text = payload.ToJsonString();
        envelope["payload"] = text;
        envelope["sha256"] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
        envelope["schemaVersion"] = 5;
        return Encoding.UTF8.GetBytes(envelope.ToJsonString());
    }
}
