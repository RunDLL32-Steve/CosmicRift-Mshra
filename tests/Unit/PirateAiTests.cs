using System.Numerics;
using RogueTrader.AI;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests;

/// <summary>M6 initial NEW pirate AI (plan 12.1/12.2). All tuning is provisional; none of this is original-controller evidence.</summary>
public sealed class PirateAiTests
{
    private const string Pirates = "pirates";
    private const string Others = "traders";

    private static PirateAiConfig LoadedConfig() => PirateAiConfig.FromJson(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "ai", "pirate_ai.json")));

    /// <summary>Fixed timings so transition ticks are exact; the ranges still come from the external JSON.</summary>
    private static PirateAiConfig FixedTiming() => LoadedConfig() with
    {
        IdleTicksMin = 30, IdleTicksMax = 30,
        AcquisitionTicksMin = 10, AcquisitionTicksMax = 10,
        EvadeTicksMin = 20, EvadeTicksMax = 20
    };

    private static readonly AiShipCapabilities Caps = new(360, 3.3f, 120, 120, 700, 2.5f, 6, true);

    private static AiShipView Ship(string id, string team, float x, float y, float heading = 0, float vx = 0, float vy = 0,
        float hull = 100, float shield = 50, float energy = 100, bool destroyed = false, string? damagedBy = null) =>
        new(id, team, new Vector2(x, y), new Vector2(vx, vy), heading, 14, hull, 100, shield, 50, energy, destroyed, damagedBy);

    private static AiWorldView View(long tick, AiShipView self, params AiShipView[] others) =>
        new(tick, self, Caps, others, []);

    private static PirateController Controller(PirateAiConfig? config = null, ulong seed = 1, params Vector2[] waypoints) =>
        new(config ?? FixedTiming(), waypoints, seed);

    /// <summary>Runs a controller against a static scene and returns the first tick of each state change.</summary>
    private static List<(long Tick, PirateState From, PirateState To)> Record(PirateController controller)
    {
        var log = new List<(long, PirateState, PirateState)>();
        controller.StateChanged += (tick, from, to) => log.Add((tick, from, to));
        return log;
    }

    private static void Run(PirateController controller, long from, long to, Func<long, AiWorldView> scene)
    {
        for (var tick = from; tick < to; tick++)
            controller.Think(scene(tick));
    }

    // ---- configuration ----

    [Fact]
    public void ExternalConfigurationLoadsAndIsMarkedNew()
    {
        var config = LoadedConfig();
        Assert.Equal("NEW", config.EvidenceClassification);
        Assert.Equal(Pirates, config.Team);
        Assert.True(config.MinimumRange < config.PreferredRange && config.ChaseToAttackRange < config.AttackToChaseRange);
    }

    [Fact]
    public void InvalidConfigurationIsRejected()
    {
        Assert.Throws<ArgumentException>(() => (LoadedConfig() with { MinimumRange = 500 }).Validate());
        Assert.Throws<ArgumentException>(() => (LoadedConfig() with { FleeHealthFraction = 0.9f }).Validate());
        Assert.Throws<ArgumentException>(() => (LoadedConfig() with { DetectionRange = float.NaN }).Validate());
        Assert.Throws<ArgumentException>(() => (LoadedConfig() with { IdleTicksMin = 10, IdleTicksMax = 5 }).Validate());
        Assert.Throws<System.Text.Json.JsonException>(() => PirateAiConfig.FromJson("""{ "unknownField": 1 }"""));
    }

    // ---- pure helpers ----

    [Fact]
    public void SeededGeneratorMatchesPublishedSplitMix64VectorAndRepeats()
    {
        Assert.Equal(0xE220A8397B1DCDAFUL, new DeterministicRng(0).NextUInt64());
        var a = new DeterministicRng(42);
        var b = new DeterministicRng(42);
        for (var index = 0; index < 100; index++)
            Assert.Equal(a.NextUInt64(), b.NextUInt64());
        var ranged = new DeterministicRng(7);
        for (var index = 0; index < 200; index++)
            Assert.InRange(ranged.NextInt(3, 9), 3, 9);
    }

    [Fact]
    public void InterceptSolvesStationaryAndMovingTargets()
    {
        Assert.True(Intercept.TrySolve(new Vector2(700, 0), Vector2.Zero, 700, out var stationary));
        Assert.Equal(1f, stationary, 4);

        var position = new Vector2(400, 300);
        var velocity = new Vector2(-50, 120);
        Assert.True(Intercept.TrySolve(position, velocity, 700, out var time));
        Assert.Equal(700f * time, (position + velocity * time).Length(), 2);

        Assert.False(Intercept.TrySolve(new Vector2(100, 0), new Vector2(900, 0), 700, out _));
        Assert.False(Intercept.TrySolve(position, velocity, 0, out _));
    }

    [Fact]
    public void AngleDifferenceTakesTheShortWay()
    {
        Assert.Equal(0.2f, Intercept.AngleDifference(MathF.PI - 0.1f, -MathF.PI + 0.1f), 4);
        Assert.Equal(-0.2f, Intercept.AngleDifference(-MathF.PI + 0.1f, MathF.PI - 0.1f), 4);
    }

    // ---- state machine ----

    [Fact]
    public void IdleBecomesPatrolAfterTheSeededDwell()
    {
        var controller = Controller(waypoints: [new Vector2(1000, 0)]);
        var log = Record(controller);
        var self = Ship("pirate_1", Pirates, 0, 0);
        Run(controller, 0, 30, tick => View(tick, self));
        Assert.Equal(PirateState.Idle, controller.State);
        Run(controller, 30, 31, tick => View(tick, self));
        Assert.Equal(PirateState.Patrol, controller.State);
        Assert.Equal([(30L, PirateState.Idle, PirateState.Patrol)], log);
    }

    [Fact]
    public void DifferentSeedsGiveDifferentBoundedDwellTimes()
    {
        var config = LoadedConfig();
        var dwell = new HashSet<long>();
        for (ulong seed = 1; seed <= 8; seed++)
        {
            var controller = new PirateController(config, [], seed);
            var log = Record(controller);
            var self = Ship("pirate_1", Pirates, 0, 0);
            Run(controller, 0, config.IdleTicksMax + 5, tick => View(tick, self));
            var change = Assert.Single(log);
            Assert.InRange(change.Tick, config.IdleTicksMin, config.IdleTicksMax);
            dwell.Add(change.Tick);
        }
        Assert.True(dwell.Count > 1);
    }

    [Fact]
    public void PatrolCruisesTowardTheWaypointWithoutFiring()
    {
        var controller = Controller(waypoints: [new Vector2(1000, 0)]);
        var self = Ship("pirate_1", Pirates, 0, 0);
        ShipInput last = default;
        for (long tick = 0; tick < 40; tick++)
            last = controller.Think(View(tick, self));
        Assert.Equal(PirateState.Patrol, controller.State);
        Assert.Equal(1f, last.Thrust);
        Assert.Equal(0f, last.Turn);
        Assert.False(last.Fire);

        // Too fast: coast instead of accelerating further.
        var fast = Ship("pirate_1", Pirates, 0, 0, vx: 300);
        Assert.Equal(0f, controller.Think(View(41, fast)).Thrust);
    }

    [Fact]
    public void PatrolAdvancesThroughWaypoints()
    {
        var controller = Controller(waypoints: [new Vector2(0, 0), new Vector2(500, 0)]);
        Run(controller, 0, 32, tick => View(tick, Ship("pirate_1", Pirates, 0, 0)));
        Assert.Equal(PirateState.Patrol, controller.State);
        Assert.Equal(1, controller.WaypointIndex);
    }

    [Fact]
    public void HostileInRangeIsAcquiredChasedThenAttacked()
    {
        var controller = Controller();
        var log = Record(controller);
        var self = Ship("pirate_1", Pirates, 0, 0);
        var hostile = Ship("target", Others, 400, 0);
        Run(controller, 0, 14, tick => View(tick, self, hostile));
        Assert.Equal(PirateState.Attack, controller.State);
        Assert.Equal("target", controller.TargetId);
        Assert.Equal(
            [(0L, PirateState.Idle, PirateState.DetectTarget), (10L, PirateState.DetectTarget, PirateState.Chase),
             (11L, PirateState.Chase, PirateState.Attack)], log);
    }

    [Fact]
    public void FriendlyOutOfRangeAndDestroyedShipsAreIgnored()
    {
        var controller = Controller();
        var self = Ship("pirate_1", Pirates, 0, 0);
        Run(controller, 0, 60, tick => View(tick, self,
            Ship("ally", Pirates, 100, 0),
            Ship("far", Others, 700, 0),
            Ship("wreck", Others, 200, 0, destroyed: true)));
        Assert.Equal(PirateState.Patrol, controller.State);
        Assert.Null(controller.TargetId);
    }

    [Fact]
    public void ClosestHostileWinsAndTiesBreakByOrdinalId()
    {
        var controller = Controller();
        var self = Ship("pirate_1", Pirates, 0, 0);
        controller.Think(View(0, self, Ship("b", Others, 0, 300), Ship("a", Others, 300, 0), Ship("c", Others, 500, 0)));
        Assert.Equal("a", controller.TargetId);
    }

    [Fact]
    public void AttackPointsAndFiresOnlyWhenAlignedInRangeWithEnergy()
    {
        var controller = Controller();
        var hostile = Ship("target", Others, 300, 0);
        Run(controller, 0, 20, tick => View(tick, Ship("pirate_1", Pirates, 0, 0), hostile));
        Assert.Equal(PirateState.Attack, controller.State);

        var aligned = controller.Think(View(20, Ship("pirate_1", Pirates, 0, 0), hostile));
        Assert.True(aligned.Fire);
        Assert.Equal(0f, aligned.Turn, 3);

        var off = controller.Think(View(21, Ship("pirate_1", Pirates, 0, 0, heading: 0.5f), hostile));
        Assert.False(off.Fire);
        Assert.True(off.Turn < 0);

        var dry = controller.Think(View(22, Ship("pirate_1", Pirates, 0, 0, energy: 2), hostile));
        Assert.False(dry.Fire);
    }

    [Fact]
    public void AttackLeadsAMovingTarget()
    {
        var controller = Controller();
        var mover = Ship("target", Others, 300, 0, vy: 150);
        Run(controller, 0, 20, tick => View(tick, Ship("pirate_1", Pirates, 0, 0), mover));
        var input = controller.Think(View(20, Ship("pirate_1", Pirates, 0, 0), mover));
        Assert.True(input.Turn > 0, "Aim should lead toward the direction the target is moving (positive heading).");
    }

    [Fact]
    public void AttackManagesRangeAndBrakesWhenClosingTooFast()
    {
        var controller = Controller();
        Run(controller, 0, 20, tick => View(tick, Ship("pirate_1", Pirates, 0, 0), Ship("target", Others, 300, 0)));
        Assert.Equal(PirateState.Attack, controller.State);

        var tooClose = controller.Think(View(20, Ship("pirate_1", Pirates, 0, 0), Ship("target", Others, 100, 0)));
        Assert.Equal(-1f, tooClose.Thrust);

        var tooFar = controller.Think(View(21, Ship("pirate_1", Pirates, 0, 0), Ship("target", Others, 500, 0)));
        Assert.Equal(PirateState.Attack, controller.State);
        Assert.Equal(1f, tooFar.Thrust);

        var closingFast = controller.Think(View(22, Ship("pirate_1", Pirates, 0, 0, vx: 320), Ship("target", Others, 400, 0)));
        Assert.Equal(-1f, closingFast.Thrust);

        var inBand = controller.Think(View(23, Ship("pirate_1", Pirates, 0, 0), Ship("target", Others, 250, 0)));
        Assert.Equal(0f, inBand.Thrust);
    }

    [Fact]
    public void AttackFallsBackToChaseBeyondHysteresisAndLosesDistantTargets()
    {
        var controller = Controller();
        var log = Record(controller);
        Run(controller, 0, 20, tick => View(tick, Ship("pirate_1", Pirates, 0, 0), Ship("target", Others, 300, 0)));
        controller.Think(View(20, Ship("pirate_1", Pirates, 0, 0), Ship("target", Others, 600, 0)));
        Assert.Equal(PirateState.Chase, controller.State);
        controller.Think(View(21, Ship("pirate_1", Pirates, 0, 0), Ship("target", Others, 1000, 0)));
        Assert.Equal(PirateState.ReturnToPatrol, controller.State);
        Assert.Null(controller.TargetId);
        Assert.Contains(log, change => change.To == PirateState.ReturnToPatrol);
    }

    [Fact]
    public void DestroyedTargetSendsThePirateBackToPatrol()
    {
        var controller = Controller(waypoints: [new Vector2(0, 0)]);
        Run(controller, 0, 20, tick => View(tick, Ship("pirate_1", Pirates, 0, 0), Ship("target", Others, 300, 0)));
        controller.Think(View(20, Ship("pirate_1", Pirates, 0, 0), Ship("target", Others, 300, 0, destroyed: true)));
        Assert.Equal(PirateState.ReturnToPatrol, controller.State);
        controller.Think(View(21, Ship("pirate_1", Pirates, 0, 0)));
        Assert.Equal(PirateState.Patrol, controller.State);
    }

    [Fact]
    public void CriticalDamageEvadesThenReturnsToPatrol()
    {
        var controller = Controller(waypoints: [new Vector2(0, 0)]);
        var log = Record(controller);
        var hostile = Ship("target", Others, 300, 0);
        Run(controller, 0, 20, tick => View(tick, Ship("pirate_1", Pirates, 0, 0), hostile));
        Assert.Equal(PirateState.Attack, controller.State);

        var hurt = controller.Think(View(20, Ship("pirate_1", Pirates, 0, 0, hull: 20, shield: 0, damagedBy: "target"), hostile));
        Assert.Equal(PirateState.Evade, controller.State);
        Assert.False(hurt.Fire);
        Assert.Equal(0f, hurt.Thrust);
        Assert.True(MathF.Abs(hurt.Turn) > 0.5f, "Should turn away from the threat.");

        for (long tick = 21; tick <= 40; tick++)
            controller.Think(View(tick, Ship("pirate_1", Pirates, 0, 0, hull: 20, shield: 0), Ship("target", Others, 1500, 0)));
        Assert.Equal(PirateState.ReturnToPatrol, controller.State);
        controller.Think(View(41, Ship("pirate_1", Pirates, 0, 0, hull: 20, shield: 0)));
        Assert.Equal(PirateState.Patrol, controller.State);
        Assert.Equal(
            [PirateState.DetectTarget, PirateState.Chase, PirateState.Attack, PirateState.Evade, PirateState.ReturnToPatrol, PirateState.Patrol],
            log.Select(change => change.To));
    }

    [Fact]
    public void EvadeFlightIsPlausible()
    {
        var controller = Controller();
        var hostile = Ship("target", Others, 300, 0);
        Run(controller, 0, 20, tick => View(tick, Ship("pirate_1", Pirates, 0, 0), hostile));
        controller.Think(View(20, Ship("pirate_1", Pirates, 0, 0, hull: 20, shield: 0, damagedBy: "target"), hostile));
        // Already pointing away from the threat: thrust at full and fire nothing.
        var input = controller.Think(View(21, Ship("pirate_1", Pirates, 0, 0, heading: MathF.PI, hull: 20, shield: 0), hostile));
        Assert.Equal(PirateState.Evade, controller.State);
        Assert.Equal(1f, input.Thrust);
        Assert.False(input.Fire);
    }

    [Fact]
    public void DamageFromBeyondDetectionRangePullsAHealthyPirateIntoTheFight()
    {
        var controller = Controller();
        var hostile = Ship("sniper", Others, 800, 0);
        controller.Think(View(0, Ship("pirate_1", Pirates, 0, 0), hostile));
        Assert.Equal(PirateState.Idle, controller.State);
        controller.Think(View(1, Ship("pirate_1", Pirates, 0, 0, hull: 80, shield: 30, damagedBy: "sniper"), hostile));
        Assert.Equal(PirateState.DetectTarget, controller.State);
        Assert.Equal("sniper", controller.TargetId);
    }

    [Fact]
    public void WeakenedPirateRunsWhenProvokedButIgnoresDistantStrangers()
    {
        var provoked = Controller();
        var hostile = Ship("target", Others, 400, 0);
        provoked.Think(View(0, Ship("pirate_1", Pirates, 0, 0), Ship("far", Others, 2000, 0)));
        provoked.Think(View(1, Ship("pirate_1", Pirates, 0, 0, hull: 40, shield: 10, damagedBy: "target"), hostile));
        Assert.Equal(PirateState.Evade, provoked.State);

        var cautious = Controller();
        cautious.Think(View(0, Ship("pirate_1", Pirates, 0, 0, hull: 50, shield: 10), hostile));
        Assert.Equal(PirateState.Idle, cautious.State);
        Assert.Null(cautious.TargetId);
    }

    [Fact]
    public void DestroyedPirateStopsAndStaysDead()
    {
        var controller = Controller();
        var hostile = Ship("target", Others, 300, 0);
        controller.Think(View(0, Ship("pirate_1", Pirates, 0, 0), hostile));
        var input = controller.Think(View(1, Ship("pirate_1", Pirates, 0, 0, hull: 0, destroyed: true), hostile));
        Assert.Equal(default, input);
        Assert.Equal(PirateState.Dead, controller.State);
        Assert.Equal(default, controller.Think(View(2, Ship("pirate_1", Pirates, 0, 0), hostile)));
        Assert.Equal(PirateState.Dead, controller.State);
    }

    [Fact]
    public void OutputIsAlwaysFiniteAndWithinPlayerInputLimits()
    {
        var controller = Controller(waypoints: [new Vector2(50, 50), new Vector2(-300, 400)]);
        var rng = new DeterministicRng(99);
        for (long tick = 0; tick < 2000; tick++)
        {
            var self = Ship("pirate_1", Pirates, rng.NextInt(-800, 800), rng.NextInt(-800, 800),
                heading: rng.NextInt(-300, 300) / 100f, vx: rng.NextInt(-300, 300), vy: rng.NextInt(-300, 300),
                hull: rng.NextInt(1, 100), shield: rng.NextInt(0, 50), energy: rng.NextInt(0, 100),
                damagedBy: rng.NextBool() ? "target" : null);
            var input = controller.Think(View(tick, self, Ship("target", Others, rng.NextInt(-800, 800), rng.NextInt(-800, 800),
                vx: rng.NextInt(-300, 300), vy: rng.NextInt(-300, 300))));
            Assert.True(float.IsFinite(input.Thrust) && float.IsFinite(input.Turn));
            Assert.InRange(input.Thrust, -1f, 1f);
            Assert.InRange(input.Turn, -1f, 1f);
        }
    }

    [Fact]
    public void SameSeedAndSameViewsReproduceIdenticalOutputsAndTransitions()
    {
        (List<ShipInput>, List<(long, PirateState, PirateState)>) Play(ulong seed)
        {
            var controller = new PirateController(LoadedConfig(), [new Vector2(0, 0), new Vector2(300, 100)], seed);
            var log = Record(controller);
            var inputs = new List<ShipInput>();
            var rng = new DeterministicRng(5);
            for (long tick = 0; tick < 1500; tick++)
            {
                var self = Ship("pirate_1", Pirates, tick % 400, 0, heading: tick / 200f,
                    hull: tick > 700 && tick < 900 ? 20 : 100, damagedBy: tick == 700 ? "target" : null);
                inputs.Add(controller.Think(View(tick, self,
                    Ship("target", Others, 250 + rng.NextInt(0, 200), rng.NextInt(-50, 50)))));
            }
            return (inputs, log);
        }

        var (inputsA, logA) = Play(123);
        var (inputsB, logB) = Play(123);
        Assert.Equal(inputsA, inputsB);
        Assert.Equal(logA, logB);
        Assert.NotEmpty(logA);
    }

    // ---- closed loop through the real simulation ----

    private static WorldSimulation PirateWorld(params TargetDefinition[] targets) => new(TestWorld.Catalog(
        sector: new SectorDefinition
        {
            PlayerSpawn = new SpawnDefinition { X = 0, Y = 0, Heading = 0 },
            Targets = targets
        }));

    private static (WorldSimulation World, List<PirateState> Visited, int Ticks) FlyPirate(ulong seed, params TargetDefinition[] targets)
    {
        var world = PirateWorld(targets);
        var controller = new PirateController(LoadedConfig(), [Vector2.Zero], seed);
        var visited = new List<PirateState> { controller.State };
        controller.StateChanged += (_, _, to) => visited.Add(to);
        var ticks = 0;
        // The pirate occupies the player slot of the simulation, so it flies under the exact player movement, energy and weapon rules.
        while (ticks < 3600 && world.DestroyedTargets < targets.Length && !world.Player.IsDestroyed)
        {
            var view = SimulationAiViews.Build(world, world.Player, Pirates, world.Targets, _ => Others);
            world.Tick(controller.Think(view));
            ticks++;
        }
        return (world, visited, ticks);
    }

    [Fact]
    public void ControllerAcquiresPursuesFiresAtAndDestroysATargetUnderPlayerRules()
    {
        var (world, visited, ticks) = FlyPirate(1, new TargetDefinition { Id = "target_a", X = 400, Y = 300 });
        var target = Assert.Single(world.Targets);
        Assert.True(target.IsDestroyed, $"Target survived {ticks} ticks.");
        Assert.Equal(world.Player.Id, target.DestroyedByShipId);
        Assert.Equal(world.Player.Id, target.LastDamagedByShipId);
        Assert.Equal(1, world.DestroyedTargets);
        Assert.True(world.ShotsFired >= 5);
        Assert.True(world.Player.Energy >= 0);
        Assert.Contains(PirateState.DetectTarget, visited);
        Assert.Contains(PirateState.Chase, visited);
        Assert.Contains(PirateState.Attack, visited);
        Assert.True(ticks < 1800);
    }

    [Fact]
    public void ControllerReturnsToPatrolAndEngagesASecondTarget()
    {
        var (world, visited, ticks) = FlyPirate(3,
            new TargetDefinition { Id = "target_a", X = 400, Y = 300 },
            new TargetDefinition { Id = "target_b", X = -300, Y = -450 });
        Assert.All(world.Targets, target => Assert.True(target.IsDestroyed, $"{target.Id} survived {ticks} ticks."));
        Assert.Equal(2, world.DestroyedTargets);
        Assert.All(world.Targets, target => Assert.Equal(world.Player.Id, target.DestroyedByShipId));
        Assert.Contains(PirateState.ReturnToPatrol, visited);
    }

    [Fact]
    public void ClosedLoopRunsAreBitIdenticalForTheSameSeed()
    {
        var targets = new[] { new TargetDefinition { Id = "target_a", X = 400, Y = 300 } };
        var first = FlyPirate(11, targets);
        var second = FlyPirate(11, targets);
        TestWorld.SameWorld(first.World, second.World);
        Assert.Equal(first.Visited, second.Visited);
        Assert.Equal(first.Ticks, second.Ticks);
    }

    [Fact]
    public void ControllerCannotExceedThePlayerTurnRateOrSpeedLimits()
    {
        var world = PirateWorld(new TargetDefinition { Id = "target_a", X = -500, Y = 200 });
        var controller = new PirateController(LoadedConfig(), [Vector2.Zero], 2);
        var maxTurn = world.Catalog.Ship.Physics.RotationSpeed * WorldSimulation.TickDuration + 1e-4f;
        for (var tick = 0; tick < 900; tick++)
        {
            var before = world.Player.Heading;
            world.Tick(controller.Think(SimulationAiViews.Build(world, world.Player, Pirates, world.Targets, _ => Others)));
            Assert.True(MathF.Abs(MathF.IEEERemainder(world.Player.Heading - before, MathF.Tau)) <= maxTurn);
            Assert.True(world.Player.Velocity.Length() <= world.Catalog.Ship.Physics.MaximumSpeed + 1e-3f);
        }
    }
}
