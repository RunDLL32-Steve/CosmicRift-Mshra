using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// NEW practice-range rule: once every target of the group is destroyed, the whole group respawns at its configured
/// positions after <see cref="SectorDefinition.TargetRespawnSeconds"/> as the next wave. Fixture sectors are NEW test data.
/// </summary>
public sealed class TargetRespawnTests
{
    private static WorldSimulation World(float respawnSeconds, params TargetDefinition[] targets) =>
        new(TestWorld.Catalog(sector: new SectorDefinition
        {
            Targets = targets.Length > 0 ? targets : [new TargetDefinition { Id = "t1" }],
            TargetRespawnSeconds = respawnSeconds,
        }));

    private static void FireUntil(WorldSimulation world, int kills)
    {
        for (var tick = 0; tick < 3000 && world.Kills.Count < kills; tick++) world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(kills, world.Kills.Count);
    }

    [Fact]
    public void ShippedSector_RespawnsAfterOneMinute()
    {
        Assert.Equal(60, TestWorld.ExternalCatalog().Sector.TargetRespawnSeconds);
    }

    [Fact]
    public void WholeGroupDestroyed_RespawnsAtFullStrengthAfterTheDelay()
    {
        var world = World(2);
        var definition = world.Catalog.Sector.Targets[0];
        FireUntil(world, 1);
        var killTick = world.Kills[0].Tick;
        Assert.Equal(killTick + 2 * WorldSimulation.TickRate, world.TargetRespawnTick);

        while (world.TickNumber < world.TargetRespawnTick - 1) world.Tick(default);
        Assert.True(world.Targets[0].IsDestroyed);
        world.Tick(default);

        var target = world.Targets[0];
        Assert.False(target.IsDestroyed);
        Assert.Equal(definition.Hull, target.Hull);
        Assert.Equal(definition.Shield, target.Shield);
        Assert.Equal(new Vector2(definition.X, definition.Y), target.Position);
        Assert.Null(target.DestroyedByShipId);
        Assert.Equal(1, world.TargetWave);
        Assert.Null(world.TargetRespawnTick);
    }

    [Fact]
    public void GroupWithASurvivor_DoesNotRespawn()
    {
        var world = World(1, new TargetDefinition { Id = "near" }, new TargetDefinition { Id = "far", X = 900, Y = 900 });
        FireUntil(world, 1);
        TestWorld.Tick(world, 3 * WorldSimulation.TickRate);

        Assert.Null(world.TargetRespawnTick);
        Assert.True(world.Targets[0].IsDestroyed);
        Assert.Equal(0, world.TargetWave);
    }

    [Fact]
    public void RespawnOff_TargetsStayDestroyed()
    {
        var world = World(0);
        FireUntil(world, 1);
        TestWorld.Tick(world, 10 * WorldSimulation.TickRate);

        Assert.Null(world.TargetRespawnTick);
        Assert.True(world.Targets[0].IsDestroyed);
    }

    [Fact]
    public void KillsInTheNextWave_CarryTheWaveNumber()
    {
        var world = World(1);
        FireUntil(world, 1);
        while (world.TargetWave == 0) world.Tick(default);
        FireUntil(world, 2);

        Assert.Equal([0, 1], world.Kills.Select(k => k.Wave));
        Assert.All(world.Kills, k => Assert.Equal("t1", k.TargetId));
    }

    [Fact]
    public void CarryTargetsFrom_KeepsDestroyedTargetsAndTheTimeLeft()
    {
        var old = World(2);
        FireUntil(old, 1);
        TestWorld.Tick(old, WorldSimulation.TickRate / 2);
        var left = old.TargetRespawnTick!.Value - old.TickNumber;

        var rebuilt = World(2);
        rebuilt.CarryTargetsFrom(old);

        Assert.True(rebuilt.Targets[0].IsDestroyed);
        Assert.Equal(rebuilt.TickNumber + left, rebuilt.TargetRespawnTick);
        Assert.Empty(rebuilt.Kills);
        TestWorld.Tick(rebuilt, (int)left);
        Assert.False(rebuilt.Targets[0].IsDestroyed);
        Assert.Equal(1, rebuilt.TargetWave);
    }

    [Fact]
    public void TimeWithoutTicking_CountsTowardTheRespawn()
    {
        var world = World(2);
        FireUntil(world, 1);
        var tick = world.TickNumber;
        var due = world.TargetRespawnTick!.Value;

        world.ElapseWithoutTicking(WorldSimulation.TickRate); // one second docked or paused
        Assert.Equal(due - WorldSimulation.TickRate, world.TargetRespawnTick);
        Assert.True(world.Targets[0].IsDestroyed);
        Assert.Equal(tick, world.TickNumber);

        world.ElapseWithoutTicking(10 * WorldSimulation.TickRate); // more than the time left
        Assert.False(world.Targets[0].IsDestroyed);
        Assert.Equal(1, world.TargetWave);
        Assert.Null(world.TargetRespawnTick);
        Assert.Equal(tick, world.TickNumber);
    }

    [Fact]
    public void TimeWithoutTicking_DoesNothingWithoutAPendingRespawn()
    {
        var world = World(2, new TargetDefinition { Id = "near" }, new TargetDefinition { Id = "far", X = 900, Y = 900 });
        FireUntil(world, 1);
        world.ElapseWithoutTicking(100 * WorldSimulation.TickRate);

        Assert.True(world.Targets[0].IsDestroyed);
        Assert.Equal(0, world.TargetWave);
        Assert.Throws<ArgumentOutOfRangeException>(() => world.ElapseWithoutTicking(-1));
    }

    [Fact]
    public void CarryTargetsFrom_RefusesAnotherSector()
    {
        var other = new WorldSimulation(TestWorld.Catalog(sector: new SectorDefinition { Id = "elsewhere", Targets = [new TargetDefinition { Id = "t1" }] }));
        Assert.Throws<ArgumentException>(() => World(1).CarryTargetsFrom(other));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Catalog_RejectsBadRespawnDelay(float seconds) =>
        Assert.Throws<ArgumentException>(() => TestWorld.Catalog(sector: new SectorDefinition { TargetRespawnSeconds = seconds }));

    [Fact]
    public async Task SameTargetInTheNextWave_PaysAgain_ButEachWavePaysOnce()
    {
        var station = StationDefinition.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "stations", "trading_outpost.json")));
        var session = new StationProfileSession(new InMemoryProfileRepository(), station, "starter_interceptor") { KillReward = new KillRewardDefinition("r", 75) };
        Assert.True((await session.OpenOrCreateAsync("respawn-pilot")).Succeeded);

        Assert.False((await session.GrantKillRewardAsync("sector", new TargetKill("t1", WorldSimulation.PlayerShipId, 5, 0)))!.IsReplay);
        Assert.True((await session.GrantKillRewardAsync("sector", new TargetKill("t1", WorldSimulation.PlayerShipId, 5, 0)))!.IsReplay);
        Assert.False((await session.GrantKillRewardAsync("sector", new TargetKill("t1", WorldSimulation.PlayerShipId, 99, 1)))!.IsReplay);
        Assert.Equal(150, session.Profile!.Wallet);
    }
}
