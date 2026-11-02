using System.Numerics;
using RogueTrader.Protocol;
using RogueTrader.Server;
using RogueTrader.Simulation;
using RogueTrader.Simulation.Replay;

namespace RogueTrader.UnitTests.Server;

/// <summary>Queue C8 boundary validation of <see cref="AuthoritativeSession"/>. Every threshold here is NEW/provisional.</summary>
public sealed class InputValidationTests
{
    private static WorldSimulation NewWorld() => new(TestWorld.ExternalCatalog());

    private static AuthoritativeSession NewSession(SessionOptions? options = null) => new(NewWorld(), options);

    private static PlayerInput Input(uint tick, uint sequence, float thrust = 1, float turn = 0, uint lastSnapshot = 0) =>
        new(tick, sequence, lastSnapshot, thrust, turn, InputFlags.None);

    [Theory]
    [InlineData(1.0001f, 0f)]
    [InlineData(-1.5f, 0f)]
    [InlineData(0f, 2f)]
    [InlineData(0f, -1.0001f)]
    [InlineData(float.MaxValue, 0f)]
    [InlineData(0f, float.MinValue)]
    public void ThrustAndTurnOutsideTheDocumentedRangeAreRejected(float thrust, float turn)
    {
        var session = NewSession();
        Assert.Equal(InputVerdict.OutOfRange, session.Submit(Input(1, 1, thrust, turn)));
        Assert.Equal(0, session.QueuedInputCount);
        Assert.Equal(1, session.Counters[InputVerdict.OutOfRange]);
        // The rejected input reserved nothing: the same sequence is still free.
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(1, 1)));
    }

    [Theory]
    [InlineData(1f, 1f)]
    [InlineData(-1f, -1f)]
    [InlineData(0f, 0f)]
    [InlineData(-0f, 0.5f)]
    public void TheRangeEndsAreAccepted(float thrust, float turn) =>
        Assert.Equal(InputVerdict.Accepted, NewSession().Submit(Input(1, 1, thrust, turn)));

    [Fact]
    public void AcknowledgingASnapshotTheServerHasNotProducedIsRejected()
    {
        var session = NewSession();
        Assert.Equal(InputVerdict.FutureSnapshot, session.Submit(Input(1, 1, lastSnapshot: 1)));
        for (var tick = 0; tick < 5; tick++) session.Step();
        Assert.Equal(InputVerdict.FutureSnapshot, session.Submit(Input(6, 2, lastSnapshot: 6)));
        Assert.Equal(InputVerdict.FutureSnapshot, session.Submit(Input(6, 3, lastSnapshot: uint.MaxValue)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(6, 4, lastSnapshot: 5)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(7, 5, lastSnapshot: 0)));
        Assert.Equal(3, session.Counters[InputVerdict.FutureSnapshot]);
    }

    [Fact]
    public void SubmissionsBeyondTheAllowanceAreRateLimitedWhateverTheirVerdict()
    {
        var session = NewSession(new SessionOptions(InputsPerTick: 2, InputBurst: 5));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(1, 1)));
        Assert.Equal(InputVerdict.Duplicate, session.Submit(Input(1, 1)));
        Assert.Equal(InputVerdict.Invalid, session.Submit(Input(1, 2, float.NaN)));
        Assert.Equal(InputVerdict.OutOfRange, session.Submit(Input(1, 3, 3)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(2, 4)));
        // The burst of five is spent; even a valid input is now refused and reserves nothing.
        Assert.Equal(InputVerdict.RateLimited, session.Submit(Input(3, 5)));
        Assert.Equal(2, session.QueuedInputCount);

        session.Step();
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(3, 5)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(4, 6)));
        Assert.Equal(InputVerdict.RateLimited, session.Submit(Input(5, 7)));
        Assert.Equal(2, session.Counters[InputVerdict.RateLimited]);
    }

    [Fact]
    public void AnIdleSessionRefillsOnlyUpToTheBurst()
    {
        var session = NewSession(new SessionOptions(InputsPerTick: 4, InputBurst: 6, MaxInputLeadTicks: 60));
        for (var tick = 0; tick < 1000; tick++) session.Step();
        var accepted = 0;
        for (uint i = 1; i <= 10; i++)
            if (session.Submit(Input(1000 + i, i)) == InputVerdict.Accepted) accepted++;
        Assert.Equal(6, accepted);
        Assert.Equal(4, session.Counters[InputVerdict.RateLimited]);
    }

    [Fact]
    public void ASteadyClientResendingEachInputThreeTimesIsNeverRateLimited()
    {
        // One input per tick, each sent four times in all (three redundant resends), plus a full lead window up front.
        var session = NewSession();
        uint sequence = 0;
        for (uint tick = 1; tick <= 59; tick++) Assert.Equal(InputVerdict.Accepted, session.Submit(Input(tick, ++sequence)));
        for (uint tick = 60; tick <= 659; tick++)
        {
            var input = Input(tick, ++sequence, 1, tick % 120 < 60 ? 1 : -1);
            Assert.Equal(InputVerdict.Accepted, session.Submit(input));
            for (var resend = 0; resend < 3; resend++) Assert.Equal(InputVerdict.Duplicate, session.Submit(input));
            session.Step();
        }
        Assert.Equal(0, session.Counters[InputVerdict.RateLimited]);
        Assert.Equal(1800, session.Counters[InputVerdict.Duplicate]);
        Assert.Equal(600, session.Counters.Applied);
    }

    [Fact]
    public void RejectedBoundaryInputsNeverChangeTheWorldQueueOrAcknowledgement()
    {
        var options = new SessionOptions(InputsPerTick: 6, InputBurst: 6);
        var clean = NewSession(options);
        var noisy = NewSession(options);
        uint sequence = 1;
        for (uint tick = 1; tick <= 120; tick++)
        {
            var valid = Input(tick, sequence++, tick % 3 == 0 ? -1 : 1, tick % 2 == 0 ? 1 : -1, (uint)noisy.ServerTick);
            Assert.Equal(InputVerdict.Accepted, clean.Submit(valid));
            Assert.Equal(InputVerdict.Accepted, noisy.Submit(valid));
            var hashBefore = WorldStateHasher.Hash(noisy.World);
            var queuedBefore = noisy.QueuedInputCount;
            var ackBefore = noisy.LastProcessedInputSequence;
            Assert.Equal(InputVerdict.OutOfRange, noisy.Submit(Input(tick, sequence + 1000, 50, 0)));
            Assert.Equal(InputVerdict.OutOfRange, noisy.Submit(Input(tick, sequence + 2000, 0, -9)));
            Assert.Equal(InputVerdict.FutureSnapshot, noisy.Submit(Input(tick, sequence + 3000, -1, 1, tick + 5)));
            // Spend the rest of this tick's allowance, then one more is refused.
            Assert.Equal(InputVerdict.Duplicate, noisy.Submit(valid));
            Assert.Equal(InputVerdict.Duplicate, noisy.Submit(valid));
            Assert.Equal(InputVerdict.RateLimited, noisy.Submit(Input(tick, sequence + 4000, -1, -1)));
            Assert.Equal(hashBefore, WorldStateHasher.Hash(noisy.World));
            Assert.Equal(queuedBefore, noisy.QueuedInputCount);
            Assert.Equal(ackBefore, noisy.LastProcessedInputSequence);

            clean.Step();
            noisy.Step();
            Assert.Equal(WorldStateHasher.Hash(clean.World), WorldStateHasher.Hash(noisy.World));
            Assert.Equal(clean.LastProcessedInputSequence, noisy.LastProcessedInputSequence);
        }
        TestWorld.SameWorld(clean.World, noisy.World);
        Assert.Equal(120, noisy.Counters.Applied);
        Assert.Equal(120, noisy.Counters[InputVerdict.RateLimited]);
    }

    [Fact]
    public void FullThrustFlightIntoAnAsteroidRaisesNoMotionFindings()
    {
        var session = NewSession();
        var asteroid = session.World.Asteroids[0];
        var standoff = session.World.Player.Radius + asteroid.Radius + 40;
        Assert.True(session.RestorePlayer(asteroid.Position - new Vector2(standoff, 0), 0, 100));
        uint sequence = 0;
        for (var tick = 0; tick < 600; tick++)
        {
            var turn = tick < 240 ? 0 : (tick / 90 % 2 == 0 ? 1 : -1);
            Assert.Equal(InputVerdict.Accepted, session.Submit(Input((uint)session.ServerTick + 1, ++sequence, 1, turn)));
            session.Step();
        }
        // Advance uses the same audit as Step.
        session.Advance(2.0);
        Assert.True(session.World.AsteroidCollisions > 0);
        Assert.Empty(session.MotionViolations);
        Assert.Equal(0, session.Counters[MotionViolationKind.SpeedExceeded]);
        Assert.Equal(0, session.Counters[MotionViolationKind.MovedOutsideTick]);
    }

    [Fact]
    public void AMoveThatBypassesTheSessionIsFlaggedButNotUndone()
    {
        var session = NewSession();
        for (var tick = 0; tick < 10; tick++) session.Step();
        var start = session.World.Player.Position;
        // Something outside the session (not a client: inputs carry no position) moves the ship 500 units.
        Assert.True(session.World.RestorePlayer(start + new Vector2(500, 0), 0, 100));
        session.Step();

        var finding = Assert.Single(session.MotionViolations);
        Assert.Equal(MotionViolationKind.MovedOutsideTick, finding.Kind);
        Assert.Equal(10, finding.Tick);
        Assert.Equal(500, finding.Distance, 0.01f);
        Assert.Equal(0, finding.Limit);
        Assert.Equal(1, session.Counters[MotionViolationKind.MovedOutsideTick]);
        // Reporting only: the moved ship stays where it was put.
        Assert.True(Vector2.Distance(start, session.World.Player.Position) > 499);

        // The same move made through the session is sanctioned.
        Assert.True(session.RestorePlayer(start, 0, 100));
        session.Step();
        Assert.Single(session.MotionViolations);
    }

    [Fact]
    public void AMoveBetweenAdvanceCallsIsFlagged()
    {
        var session = NewSession();
        session.Advance(0.5);
        Assert.True(session.World.RestorePlayer(new Vector2(1234, -321), 0, 100));
        session.Advance(0.5);
        Assert.Equal(MotionViolationKind.MovedOutsideTick, Assert.Single(session.MotionViolations).Kind);
    }

    [Fact]
    public void ADisplacementFasterThanTheMaximumSpeedIsFlagged()
    {
        // Restoring the ship onto an asteroid's center makes the next tick push it out by the sum of both radii, far more
        // than the ship can fly in one tick, so the audit reports it.
        var session = NewSession();
        var asteroid = session.World.Asteroids[0];
        Assert.True(session.RestorePlayer(asteroid.Position, 0, 100));
        session.Step();

        var finding = Assert.Single(session.MotionViolations);
        Assert.Equal(MotionViolationKind.SpeedExceeded, finding.Kind);
        Assert.Equal(1, finding.Tick);
        var physics = session.World.Player.Definition.Physics;
        Assert.Equal(physics.MaximumSpeed * WorldSimulation.TickDuration + SessionOptions.Default.DisplacementTolerance,
            finding.Limit, 0.0001f);
        Assert.True(finding.Distance >= session.World.Player.Radius + asteroid.Radius);
        Assert.Equal(1, session.Counters[MotionViolationKind.SpeedExceeded]);
    }

    [Fact]
    public void OnlyTheLatestFindingsAreKept()
    {
        var session = NewSession();
        for (var i = 1; i <= AuthoritativeSession.MotionViolationCapacity + 6; i++)
        {
            Assert.True(session.World.RestorePlayer(new Vector2(i * 1000, 0), 0, 100));
            session.Step();
        }
        Assert.Equal(AuthoritativeSession.MotionViolationCapacity, session.MotionViolations.Count);
        Assert.Equal(AuthoritativeSession.MotionViolationCapacity + 6, session.Counters[MotionViolationKind.MovedOutsideTick]);
        Assert.Equal(6, session.MotionViolations[0].Tick);
    }

    [Theory]
    [InlineData(0, 120, 0.01f)]
    [InlineData(4, 0, 0.01f)]
    [InlineData(4, 120, -0.01f)]
    [InlineData(4, 120, float.NaN)]
    public void ValidationOptionsMustBeSane(int perTick, int burst, float tolerance) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NewSession(new SessionOptions(InputsPerTick: perTick, InputBurst: burst, DisplacementTolerance: tolerance)));
}
