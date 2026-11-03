using RogueTrader.GameData;
using RogueTrader.Prediction;
using RogueTrader.Protocol;
using RogueTrader.Server;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests.Prediction;

// All identities, geometry, controls and timing in these tests are NEW/provisional fixtures.
public sealed class PredictionTests
{
    private const uint ControlledId = 17;

    private static GameCatalog Catalog(SectorDefinition? sector = null) => TestWorld.Catalog(sector: sector ?? new SectorDefinition
    {
        PlayerSpawn = new SpawnDefinition { Heading = 0, VelocityX = 23, VelocityY = -7 }
    });

    private static EntityState Ship(AuthoritativeSession session)
    {
        var ship = session.World.Player;
        return new EntityState(ControlledId, EntityKind.Ship, ship.Position.X, ship.Position.Y,
            ship.Velocity.X, ship.Velocity.Y, ship.Heading, ship.Hull, ship.Shield);
    }

    private static WorldSnapshot Snapshot(AuthoritativeSession session) =>
        new(checked((uint)session.ServerTick), session.LastProcessedInputSequence, [Ship(session)]);

    private static ShipPredictor Predictor(AuthoritativeSession session, int maxPending = 60) =>
        new(session.World.Catalog, ControlledId, Snapshot(session), maxPending);

    private static PlayerInput Issue(ShipPredictor predictor, float thrust = 1, float turn = 0, InputFlags flags = InputFlags.None)
    {
        Assert.True(predictor.TryPredict(thrust, turn, flags, out var input));
        return input!;
    }

    private static (EntityState Ship, uint Tick, uint ServerTick, uint Ack, uint Sequence, int Pending) State(ShipPredictor predictor) =>
        (predictor.Ship, predictor.PredictedTick, predictor.LastAcceptedServerTick, predictor.LastAcknowledgedSequence,
            predictor.LastIssuedSequence, predictor.PendingCount);

    [Fact]
    public void PredictsReverseTurningAndNeutralDriftExactlyLikeAuthorityWithoutAdvancingIt()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority);
        Assert.NotEqual(0, predictor.Ship.VelX);
        var initial = predictor.Ship;
        for (uint tick = 1; tick <= 45; tick++)
        {
            var before = Ship(authority);
            var input = Issue(predictor, tick <= 15 ? -1 : tick <= 30 ? 1 : 0, tick <= 30 ? 0.4f : 0);
            Assert.Equal(tick, input.ClientTick);
            Assert.Equal(tick, input.Sequence);
            Assert.Equal(0u, input.LastSnapshotTick);
            Assert.Equal(before, Ship(authority));
            Assert.Equal(tick - 1, (uint)authority.ServerTick);
            Assert.Equal(InputVerdict.Accepted, authority.Submit(input));
            authority.Step();
            Assert.Equal(Ship(authority), predictor.Ship);
        }
        Assert.NotEqual(initial, predictor.Ship);
        Assert.Equal(initial.Hull, predictor.Ship.Hull);
        Assert.Equal(initial.Shield, predictor.Ship.Shield);
    }

    [Fact]
    public void SweptAsteroidContactUsesTheSameMotionAndOrderedGeometry()
    {
        AsteroidDefinition[] asteroids = [new() { Id = "near", X = 20, Radius = 2 }, new() { Id = "far", X = -40, Radius = 3 }];
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog(new SectorDefinition
        {
            PlayerSpawn = new SpawnDefinition { Heading = 0, VelocityX = 300 }, Asteroids = asteroids
        })));
        var predictor = Predictor(authority);
        // The predictor owns a copy; later mutation of the caller's static geometry list cannot change it.
        asteroids[0] = new AsteroidDefinition { Id = "moved", X = 10000, Radius = 2 };
        for (var tick = 0; tick < 12; tick++)
        {
            var input = Issue(predictor, 0);
            Assert.Equal(InputVerdict.Accepted, authority.Submit(input));
            authority.Step();
            Assert.Equal(Ship(authority), predictor.Ship);
        }
        Assert.True(authority.World.AsteroidCollisions > 0);
    }

    [Fact]
    public void LostInputExpiresAtItsTickDespiteUnchangedAcknowledgementAndSuffixReplaysOnce()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority);
        var first = Issue(predictor, 1, 0.5f);
        var lost = Issue(predictor, -1, -1);
        var third = Issue(predictor, 0.25f, 0.2f);
        Assert.Equal(InputVerdict.Accepted, authority.Submit(first));
        authority.Step();
        var delayedFirst = Snapshot(authority);
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(delayedFirst));
        authority.Step(); // Input 2 never arrives: authority drifts with neutral controls.
        var divergent = predictor.Ship;
        var secondSnapshot = Snapshot(authority);
        Assert.Equal(1u, secondSnapshot.LastProcessedInputSequence);
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(secondSnapshot));
        Assert.Equal(1, predictor.PendingCount);
        Assert.Equal(3u, predictor.PredictedTick);
        Assert.Equal(2u, predictor.LastAcceptedServerTick);
        Assert.NotEqual(divergent, predictor.Ship);
        var settledReplay = State(predictor);
        Assert.Equal(ReconciliationVerdict.Stale, predictor.Reconcile(secondSnapshot));
        Assert.Equal(ReconciliationVerdict.Stale, predictor.Reconcile(delayedFirst));
        Assert.Equal(settledReplay, State(predictor));
        Assert.Equal(InputVerdict.Late, authority.Submit(lost));
        Assert.Equal(InputVerdict.Accepted, authority.Submit(third));
        authority.Step();
        Assert.Equal(Ship(authority), predictor.Ship);
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        Assert.Equal(0, predictor.PendingCount);
        Assert.Equal(3u, predictor.LastAcknowledgedSequence);
    }

    [Fact]
    public void DelayedCorrectionReplaysMultiplePendingTicksAndConvergesAfterSkippedSnapshots()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority);
        var inputs = new[] { Issue(predictor, 1, 0.2f), Issue(predictor, -1, -0.3f), Issue(predictor, 0) };
        // A sanctioned server correction changes the baseline after the client has already predicted ahead.
        Assert.True(authority.RestorePlayer(new System.Numerics.Vector2(50, -30), 0.7f, 60));
        foreach (var input in inputs) Assert.Equal(InputVerdict.Accepted, authority.Submit(input));
        authority.Step();
        var corrected = Snapshot(authority);
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(corrected));
        Assert.Equal(2, predictor.PendingCount);
        Assert.Equal(60, predictor.Ship.Hull);
        authority.Step();
        authority.Step(); // Tick-2 snapshot is lost entirely.
        Assert.Equal(Ship(authority), predictor.Ship);
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        Assert.Equal(0, predictor.PendingCount);
        Assert.Equal(ReconciliationVerdict.Stale, predictor.Reconcile(corrected));
        var next = Issue(predictor, 0);
        Assert.Equal(4u, next.ClientTick);
        Assert.Equal(4u, next.Sequence);
        Assert.Equal(3u, next.LastSnapshotTick);
    }

    [Fact]
    public void JumpAcrossAllLostInputsRecoversCapacityAndNeverReusesASequence()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority, 2);
        Issue(predictor);
        Issue(predictor);
        var full = State(predictor);
        Assert.False(predictor.TryPredict(0, 0, InputFlags.None, out var rejected));
        Assert.Null(rejected);
        Assert.Equal(full, State(predictor));
        for (var tick = 0; tick < 10; tick++) authority.Step();
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        Assert.Equal(10u, predictor.PredictedTick);
        Assert.Equal(0, predictor.PendingCount);
        Assert.Equal(2u, predictor.LastIssuedSequence);
        var next = Issue(predictor);
        Assert.Equal(11u, next.ClientTick);
        Assert.Equal(3u, next.Sequence);
        Assert.Equal(10u, next.LastSnapshotTick);
        Assert.Equal(InputVerdict.Accepted, authority.Submit(next));
        authority.Step();
        Assert.Equal(Ship(authority), predictor.Ship);
    }

    [Fact]
    public void AdvancingAckMaySkipLostInputsButMustNameAKnownCompletedTick()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority);
        Issue(predictor);
        var second = Issue(predictor);
        Issue(predictor);
        Assert.Equal(InputVerdict.Accepted, authority.Submit(second));
        authority.Step();
        authority.Step();
        Assert.Equal(2u, authority.LastProcessedInputSequence);
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        Assert.Equal(1, predictor.PendingCount);
        Assert.Equal(2u, predictor.LastAcknowledgedSequence);
    }

    [Theory]
    [InlineData(0u)] // Regression after acknowledging 1.
    [InlineData(3u)] // Issued, but its tick has not completed.
    [InlineData(99u)] // Never issued.
    public void InvalidAcknowledgementsLeaveAllStateAndTheReplayQueueUntouched(uint badAck)
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority);
        var first = Issue(predictor);
        var second = Issue(predictor, -1, 0.5f);
        var third = Issue(predictor, 0, -0.5f);
        foreach (var input in new[] { first, second, third }) Assert.Equal(InputVerdict.Accepted, authority.Submit(input));
        authority.Step();
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        authority.Step();
        var valid = Snapshot(authority);
        var before = State(predictor);
        Assert.Equal(ReconciliationVerdict.InvalidAcknowledgement,
            predictor.Reconcile(valid with { LastProcessedInputSequence = badAck }));
        Assert.Equal(before, State(predictor));
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(valid));
        Assert.Equal(1, predictor.PendingCount);
        authority.Step();
        Assert.Equal(Ship(authority), predictor.Ship);
    }

    [Fact]
    public void AnExpiredUnacknowledgedSequenceIsNotAKnownRetainedAcknowledgement()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority);
        Issue(predictor);
        Issue(predictor);
        authority.Step();
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
        authority.Step();
        var valid = Snapshot(authority);
        var before = State(predictor);
        Assert.Equal(ReconciliationVerdict.InvalidAcknowledgement,
            predictor.Reconcile(valid with { LastProcessedInputSequence = 1 }));
        Assert.Equal(before, State(predictor));
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(valid));
    }

    [Theory]
    [InlineData(float.NaN, 0f, (byte)0)]
    [InlineData(0f, float.PositiveInfinity, (byte)0)]
    [InlineData(1.01f, 0f, (byte)0)]
    [InlineData(0f, -1.01f, (byte)0)]
    [InlineData(0f, 0f, (byte)2)]
    public void InvalidControlsConsumeNoIdentifiersOrMotion(float thrust, float turn, byte flags)
    {
        var predictor = Predictor(new AuthoritativeSession(new WorldSimulation(Catalog())));
        var before = State(predictor);
        Assert.False(predictor.TryPredict(thrust, turn, (InputFlags)flags, out var rejected));
        Assert.Null(rejected);
        Assert.Equal(before, State(predictor));
        Assert.Equal(1u, Issue(predictor).Sequence);
    }

    [Theory]
    [InlineData(uint.MaxValue, 0u)]
    [InlineData(1u, uint.MaxValue)]
    public void ExhaustedTickOrSequenceCannotWrap(uint tick, uint sequence)
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var baseline = Snapshot(authority) with { ServerTick = tick, LastProcessedInputSequence = sequence };
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, baseline, 2);
        var before = State(predictor);
        Assert.False(predictor.TryPredict(0, 0, InputFlags.None, out _));
        Assert.Equal(before, State(predictor));
    }

    [Fact]
    public void LastAvailableIdentifiersAreIssuedOnceThenStop()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var baseline = Snapshot(authority) with { ServerTick = uint.MaxValue - 1, LastProcessedInputSequence = uint.MaxValue - 1 };
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, baseline, 2);
        var input = Issue(predictor, 0);
        Assert.Equal(uint.MaxValue, input.ClientTick);
        Assert.Equal(uint.MaxValue, input.Sequence);
        var before = State(predictor);
        Assert.False(predictor.TryPredict(0, 0, InputFlags.None, out _));
        Assert.Equal(before, State(predictor));
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(baseline with
        {
            ServerTick = uint.MaxValue, LastProcessedInputSequence = uint.MaxValue
        }));
        Assert.Equal(0, predictor.PendingCount);
        Assert.False(predictor.TryPredict(0, 0, InputFlags.None, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaximumPendingMustBeExplicitAndPositive(int maximum)
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShipPredictor(authority.World.Catalog, ControlledId, Snapshot(authority), maximum));
    }

    [Fact]
    public void TickZeroCannotAcknowledgeAnInput()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        Assert.Throws<ArgumentException>(() => new ShipPredictor(authority.World.Catalog, ControlledId,
            Snapshot(authority) with { LastProcessedInputSequence = 1 }, 2));
    }

    public static IEnumerable<object[]> BadShips()
    {
        var ship = new EntityState(ControlledId, EntityKind.Ship, 1, 2, 3, 4, 0.5f, 60, 20);
        yield return [ship with { X = float.NaN }];
        yield return [ship with { Y = float.PositiveInfinity }];
        yield return [ship with { VelX = float.NegativeInfinity }];
        yield return [ship with { VelY = float.NaN }];
        yield return [ship with { Rotation = float.NaN }];
        yield return [ship with { Hull = float.PositiveInfinity }];
        yield return [ship with { Shield = float.NaN }];
    }

    [Theory]
    [MemberData(nameof(BadShips))]
    public void EveryControlledShipFieldMustBeFiniteAtBindingAndReconciliation(EntityState badShip)
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority);
        Issue(predictor);
        var malformed = new WorldSnapshot(1, 0, [badShip]);
        Assert.Throws<ArgumentException>(() => new ShipPredictor(authority.World.Catalog, ControlledId, malformed, 2));
        var before = State(predictor);
        Assert.Equal(ReconciliationVerdict.InvalidSnapshot, predictor.Reconcile(malformed));
        Assert.Equal(before, State(predictor));
        authority.Step();
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(Snapshot(authority)));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("ambiguous")]
    [InlineData("wrong-kind")]
    [InlineData("oversized")]
    [InlineData("null")]
    public void RequiresExactlyOneControlledShipAndBoundedSnapshotArray(string fault)
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority);
        Issue(predictor);
        var ship = Ship(authority);
        EntityState[] entities = fault switch
        {
            "missing" => [ship with { EntityId = ControlledId + 1 }],
            "ambiguous" => [ship, ship],
            "wrong-kind" => [ship with { Kind = EntityKind.Asteroid }],
            "oversized" => new EntityState[ProtocolLimits.MaxSnapshotEntities + 1],
            _ => null!
        };
        var malformed = new WorldSnapshot(1, 0, entities);
        Assert.Throws<ArgumentException>(() => new ShipPredictor(authority.World.Catalog, ControlledId, malformed, 2));
        var before = State(predictor);
        Assert.Equal(ReconciliationVerdict.InvalidSnapshot, predictor.Reconcile(malformed));
        Assert.Equal(before, State(predictor));
    }

    [Fact]
    public void CopiedSnapshotsAndImmutableOutboundIntentsCannotMutatePrediction()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var baseline = Snapshot(authority);
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, baseline, 3);
        var original = predictor.Ship;
        baseline.Entities[0] = original with { X = 999 };
        Assert.Equal(original, predictor.Ship);
        var first = Issue(predictor);
        var second = Issue(predictor, -1);
        var changedCopy = second with { Thrust = 0, Turn = 1, Flags = InputFlags.Fire };
        Assert.NotEqual(changedCopy, second);
        Assert.Equal(InputVerdict.Accepted, authority.Submit(first));
        authority.Step();
        var snapshot = Snapshot(authority);
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(snapshot));
        var before = State(predictor);
        snapshot.Entities[0] = original with { VelX = float.NaN, Hull = 0 };
        Assert.Equal(before, State(predictor));
        Assert.Equal(InputVerdict.Accepted, authority.Submit(second));
        authority.Step();
        Assert.Equal(Ship(authority), predictor.Ship);
    }

    [Fact]
    public void DestroyedShipRetainsAuthoritativePoseHullAndShieldThroughPredictionAndReplay()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var dead = Ship(authority) with { Hull = 0, Shield = 13 };
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, new WorldSnapshot(0, 0, [dead]), 4);
        Issue(predictor, 1, 1, InputFlags.Fire);
        Issue(predictor, -1, -1, InputFlags.Fire);
        Assert.Equal(dead, predictor.Ship);
        var corrected = dead with { X = 50, Rotation = 1 };
        Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(new WorldSnapshot(1, 1, [corrected])));
        Assert.Equal(corrected, predictor.Ship);
        Assert.Equal(2u, predictor.PredictedTick);
        Assert.Equal(1, predictor.PendingCount);
    }

    [Fact]
    public void FireIsOnlyIntentAndLocalMotionNeverCreatesGameplayEffects()
    {
        var authority = new AuthoritativeSession(new WorldSimulation(Catalog()));
        var predictor = Predictor(authority);
        var beforeEnergy = authority.World.Player.Energy;
        var input = Issue(predictor, 0, 0, InputFlags.Fire);
        Assert.Equal(InputFlags.Fire, input.Flags);
        Assert.Equal(0, authority.World.TickNumber);
        Assert.Empty(authority.World.Projectiles);
        Assert.Equal(0, authority.World.ShotsFired);
        Assert.Equal(beforeEnergy, authority.World.Player.Energy);
        Assert.Equal(InputVerdict.Accepted, authority.Submit(input));
        authority.Step();
        Assert.Equal(1, authority.World.ShotsFired);
        Assert.Equal(Ship(authority), predictor.Ship);
    }
}
