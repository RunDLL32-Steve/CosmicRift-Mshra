using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Prediction;
using RogueTrader.Protocol;
using RogueTrader.Server;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests.Prediction;

/// <summary>
/// Client prediction of the Shift boost (hyper thrust paid from energy) and of wall bounces on the sector's tile map. Every
/// input goes through the wire codec and the authoritative session, and the prediction must match the authority bit for bit.
/// The hulls, energies and maps here are NEW/provisional fixtures.
/// </summary>
public sealed class BoostAndWallPredictionTests
{
    private const uint ControlledId = 31;
    private static readonly int[] Expire = [720, .. Enumerable.Repeat(120, 15)];

    private static readonly ShipHyper Boost = new()
    {
        ForwardThrust = 30000, ReverseThrust = 20000, MaximumSpeed = 500, EnergyPerSecond = 60, RechargeWhileEngaged = false,
    };

    // 30 energy at 60/s lasts 30 ticks of boost; after that the 30/s recharge lets it re-engage every other tick.
    private static GameCatalog BoostCatalog(float x = 0, float y = 0, float energy = 30,
        IReadOnlyList<AsteroidDefinition>? asteroids = null) => TestWorld.Catalog(ship: new ShipDefinition
    {
        Physics = new ShipPhysics { ForwardThrust = 12000, ReverseThrust = 12000, MaximumSpeed = 360 },
        Reactor = new ShipReactor { MaximumEnergy = energy, EnergyRegenerationPerSecond = 30 },
        Hyper = Boost,
    }, sector: new SectorDefinition { PlayerSpawn = new SpawnDefinition { X = x, Y = y, Heading = 0 }, Asteroids = asteroids ?? [] });

    /// <summary>A 40 x 40 open map with a wall column at tile x = 30 (pixels 480 to 496).</summary>
    private static SectorMap WallMap()
    {
        var tiles = new byte[40 * 40];
        for (var y = 0; y < 40; y++) tiles[y * 40 + 30] = SectorMap.BlockedBit;
        return new SectorMap(40, 40, tiles, Expire);
    }

    private static EntityState Ship(AuthoritativeSession session)
    {
        var ship = session.World.Player;
        return new EntityState(ControlledId, EntityKind.Ship, ship.Position.X, ship.Position.Y,
            ship.Velocity.X, ship.Velocity.Y, ship.Heading, ship.Hull, ship.Shield, ship.Energy);
    }

    private static WorldSnapshot Snapshot(AuthoritativeSession session) =>
        Wire(new WorldSnapshot(checked((uint)session.ServerTick), session.LastProcessedInputSequence, [Ship(session)]));

    private static T Wire<T>(T message) where T : Message => Assert.IsType<T>(MessageCodec.Decode(MessageCodec.Encode(message)));

    private static PlayerInput Issue(ShipPredictor predictor, float thrust, float turn, InputFlags flags)
    {
        Assert.True(predictor.TryPredict(thrust, turn, flags, out var input));
        return Wire(input!);
    }

    [Fact]
    public void BoostIsPredictedExactlyThroughEngageRunOutAndReengage()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(BoostCatalog()));
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, Snapshot(authority), 8);
        var engagedTicks = 0;
        var toggles = 0;
        var wasEngaged = false;
        for (var tick = 1; tick <= 90; tick++)
        {
            var input = Issue(predictor, 1, tick > 60 ? 0.3f : 0, InputFlags.Hyper);
            Assert.Equal(InputFlags.Hyper, input.Flags);
            Assert.Equal(InputVerdict.Accepted, authority.Submit(input));
            authority.Step();
            Assert.Equal(Ship(authority), predictor.Ship);
            if (authority.World.Player.IsHyperEngaged) engagedTicks++;
            if (authority.World.Player.IsHyperEngaged != wasEngaged) toggles++;
            wasEngaged = authority.World.Player.IsHyperEngaged;
            Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        }
        // The boost really ran, ran out, and came back on the trickle of recharge, all predicted.
        Assert.InRange(engagedTicks, 31, 89);
        Assert.True(toggles > 2, $"toggles {toggles}");
        Assert.True(predictor.Ship.VelX > 360 * 0.9f, $"vx {predictor.Ship.VelX}");
        Assert.Empty(authority.MotionViolations);
    }

    [Fact]
    public void BoostedPendingInputsReplayFromADelayedSnapshot()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(BoostCatalog(energy: 1000)));
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, Snapshot(authority), 32);
        var inFlight = new Queue<PlayerInput>();
        WorldSnapshot? delayed = null;
        for (var tick = 1; tick <= 120; tick++)
        {
            inFlight.Enqueue(Issue(predictor, tick % 40 < 30 ? 1 : -1, 0, tick % 3 == 0 ? InputFlags.None : InputFlags.Hyper));
            // Inputs arrive four ticks late; snapshots come back every fifth tick, applied five ticks later.
            if (inFlight.Count > 4)
            {
                Assert.Equal(InputVerdict.Accepted, authority.Submit(inFlight.Dequeue()));
                authority.Step();
                if (authority.ServerTick % 5 == 0)
                {
                    if (delayed is not null) Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(delayed));
                    delayed = Snapshot(authority);
                }
            }
        }
        while (inFlight.Count > 0)
        {
            Assert.Equal(InputVerdict.Accepted, authority.Submit(inFlight.Dequeue()));
            authority.Step();
        }
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        Assert.Equal(0, predictor.PendingCount);
        Assert.Equal(Ship(authority), predictor.Ship);
        // A replay that never corrected anything means the boost and its energy were right all along.
        Assert.True(authority.World.Player.Energy < 1000);
    }

    [Fact]
    public void BoostIsPredictedOffWhenTheSnapshotHasNoEnergyForIt()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(BoostCatalog()));
        var baseline = Snapshot(authority) with { Entities = [Ship(authority) with { Energy = 0 }] };
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, baseline, 4);
        Issue(predictor, 1, 0, InputFlags.Hyper);
        // Normal thrust only: 12000 / mass 100 for one tick, and the tick's recharge.
        Assert.Equal(12000 / 100f / 60, predictor.Ship.VelX, 4);
        Assert.Equal(0.5f, predictor.Ship.Energy, 4);
    }

    [Fact]
    public void WallBouncesArePredictedExactlyWhenThePredictorHasTheMap()
    {
        var map = WallMap();
        var authority = new AuthoritativeSession(new WorldSimulation(BoostCatalog(400, 320, 1000), map: map));
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, Snapshot(authority), 8, map);
        for (var tick = 1; tick <= 120; tick++)
        {
            var input = Issue(predictor, tick <= 40 ? 1 : 0, 0, tick <= 20 ? InputFlags.Hyper : InputFlags.None);
            Assert.Equal(InputVerdict.Accepted, authority.Submit(input));
            authority.Step();
            Assert.Equal(Ship(authority), predictor.Ship);
            Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        }
        Assert.True(authority.World.WallCollisions > 0);
        Assert.True(predictor.Ship.X < 480, $"x {predictor.Ship.X}");
        Assert.True(predictor.Ship.VelX < 0, "the ship bounced back off the wall");
    }

    [Fact]
    public void WithoutTheMapAWallContactIsAnAuthoritativeCorrection()
    {
        var map = WallMap();
        var authority = new AuthoritativeSession(new WorldSimulation(BoostCatalog(400, 320, 1000), map: map));
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, Snapshot(authority), 64);
        for (var tick = 1; tick <= 60; tick++)
        {
            Assert.Equal(InputVerdict.Accepted, authority.Submit(Issue(predictor, tick <= 40 ? 1 : 0, 0, InputFlags.Hyper)));
            authority.Step();
        }
        Assert.True(authority.World.WallCollisions > 0);
        Assert.True(predictor.Ship.X > 480, "a predictor with no map flies through the wall");
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        Assert.Equal(Ship(authority), predictor.Ship);
    }

    [Fact]
    public void TheMotionAuditAllowsTheBoostTopSpeed()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(BoostCatalog(energy: 100_000)));
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, Snapshot(authority), 4);
        for (var tick = 1; tick <= 300; tick++)
        {
            Assert.Equal(InputVerdict.Accepted, authority.Submit(Issue(predictor, 1, 0, InputFlags.Hyper)));
            authority.Step();
            Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        }
        Assert.Equal(500, authority.World.Player.Velocity.Length(), 2);
        Assert.Equal(Ship(authority), predictor.Ship);
        Assert.Empty(authority.MotionViolations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheAuditUsesTheBoostTopSpeedOnlyOnATickThatBoosted(bool boost)
    {
        // Review R1-1: restoring the ship onto an asteroid's centre pushes it out far faster than it can fly, so the tick is
        // flagged, and the finding's limit shows which top speed the audit used for that tick.
        var catalog = BoostCatalog(energy: 1000, asteroids: [new AsteroidDefinition { X = 1000, Y = 0, Radius = 40 }]);
        var authority = new AuthoritativeSession(new WorldSimulation(catalog));
        Assert.True(authority.RestorePlayer(new Vector2(1000, 0), 0, authority.World.Player.Hull));
        Assert.Equal(InputVerdict.Accepted, authority.Submit(new PlayerInput(1, 1, 0, 1, 0, boost ? InputFlags.Hyper : InputFlags.None)));
        authority.Step();
        Assert.Equal(boost, authority.World.Player.IsHyperEngaged);
        var finding = Assert.Single(authority.MotionViolations);
        Assert.Equal(MotionViolationKind.SpeedExceeded, finding.Kind);
        var topSpeed = boost ? Boost.MaximumSpeed : 360;
        Assert.Equal(topSpeed * WorldSimulation.TickDuration + SessionOptions.Default.DisplacementTolerance, finding.Limit, 4);
    }

    [Fact]
    public void ShipMotionStepCarriesEnergyAndBouncesOffWalls()
    {
        var motion = new ShipMotion(BoostCatalog(energy: 30), WallMap());
        var state = new ShipMotionState(new Vector2(478, 320), new Vector2(300, 0), 0, 30);
        var next = motion.Step(state, new ShipInput(1, 0, false, Hyper: true));
        Assert.Equal(29, next.Energy, 4); // one tick of boost at 60/s, no recharge while engaged
        Assert.True(next.Velocity.X < 0, "bounced");
        Assert.True(next.Position.X < 480);
    }

    [Fact]
    public void SnapshotEnergyAndTheHyperFlagCrossTheWire()
    {
        var snapshot = new WorldSnapshot(5, 4, [new EntityState(1, EntityKind.Ship, 1, 2, 3, 4, 0.5f, 90, 0, 1234.5f)]);
        Assert.Equal(1234.5f, Wire(snapshot).Entities[0].Energy);
        var input = new PlayerInput(1, 1, 0, 1, 0, InputFlags.Fire | InputFlags.Hyper);
        Assert.Equal(InputFlags.Fire | InputFlags.Hyper, Wire(input).Flags);
        var nonFinite = snapshot with { Entities = [snapshot.Entities[0] with { Energy = float.NaN }] };
        Assert.False(MessageCodec.TryEncode(nonFinite, new byte[256], out _, out var error));
        Assert.Equal(ProtocolError.NonFiniteValue, error);
    }
}
