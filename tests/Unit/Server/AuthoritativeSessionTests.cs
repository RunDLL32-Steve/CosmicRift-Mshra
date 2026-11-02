using RogueTrader.Protocol;
using RogueTrader.Server;
using RogueTrader.Simulation;
using RogueTrader.Simulation.Replay;

namespace RogueTrader.UnitTests.Server;

public sealed class AuthoritativeSessionTests
{
    private static WorldSimulation NewWorld() => new(TestWorld.ExternalCatalog());

    private static AuthoritativeSession NewSession(SessionOptions? options = null) => new(NewWorld(), options);

    private static PlayerInput Input(uint tick, uint sequence, float thrust = 1, float turn = 0, InputFlags flags = InputFlags.None) =>
        new(tick, sequence, 0, thrust, turn, flags);

    [Fact]
    public void AdvanceRunsFixedSixtyHertzTicks()
    {
        var session = NewSession();
        Assert.Equal(60, session.Advance(1.0));
        Assert.Equal(60, session.ServerTick);
        Assert.Equal(0, session.Advance(1.0 / 120));
        Assert.Equal(1, session.Advance(1.0 / 120));
        Assert.Equal(61, session.ServerTick);
        Assert.Equal(61, session.Counters.MissingInputTicks);
    }

    [Fact]
    public void AcceptedInputsRunOnTheTicksTheyNameExactlyLikeDirectTicks()
    {
        var session = NewSession();
        var direct = NewWorld();
        for (uint tick = 1; tick <= 30; tick++)
        {
            var flags = tick % 10 == 0 ? InputFlags.Fire : InputFlags.None;
            Assert.Equal(InputVerdict.Accepted, session.Submit(Input(tick, tick, 0.8f, 0.25f, flags)));
        }
        for (uint tick = 1; tick <= 30; tick++)
        {
            session.Step();
            direct.Tick(new ShipInput(0.8f, 0.25f, tick % 10 == 0));
        }
        TestWorld.SameWorld(direct, session.World);
        Assert.Equal(WorldStateHasher.Hash(direct), WorldStateHasher.Hash(session.World));
        Assert.Equal(30u, session.LastProcessedInputSequence);
        Assert.Equal(30, session.Counters.Applied);
        Assert.Equal(0, session.QueuedInputCount);
    }

    [Fact]
    public void CompetingInputsGiveTheSameWorldWhateverTheArrivalOrder()
    {
        // Ticks 2 and 4 each get two inputs; the higher sequence must win both times.
        PlayerInput[] inputs =
        [
            Input(1, 1, 1, 0), Input(2, 2, 1, 1), Input(2, 3, -1, -1), Input(3, 4, 0.5f, 0),
            Input(4, 5, 0, 1, InputFlags.Fire), Input(4, 6, 1, -0.5f), Input(5, 7, 0.2f, 0.2f, InputFlags.Fire),
        ];
        var forward = NewSession();
        var backward = NewSession();
        var shuffled = NewSession();
        foreach (var input in inputs) Assert.Equal(InputVerdict.Accepted, forward.Submit(input));
        foreach (var input in inputs.Reverse()) Assert.Equal(InputVerdict.Accepted, backward.Submit(input));
        foreach (var index in new[] { 3, 6, 0, 5, 1, 4, 2 }) Assert.Equal(InputVerdict.Accepted, shuffled.Submit(inputs[index]));
        foreach (var session in new[] { forward, backward, shuffled })
            for (var tick = 0; tick < 8; tick++)
                session.Step();

        var expected = NewWorld();
        foreach (var input in new[] { new ShipInput(1, 0, false), new ShipInput(-1, -1, false), new ShipInput(0.5f, 0, false),
                     new ShipInput(1, -0.5f, false), new ShipInput(0.2f, 0.2f, true), default, default, default })
            expected.Tick(input);
        foreach (var session in new[] { forward, backward, shuffled })
        {
            TestWorld.SameWorld(expected, session.World);
            Assert.Equal(WorldStateHasher.Hash(expected), WorldStateHasher.Hash(session.World));
            Assert.Equal(7u, session.LastProcessedInputSequence);
            Assert.Equal(5, session.Counters.Applied);
            Assert.Equal(2, session.Counters.Superseded);
            Assert.Equal(3, session.Counters.MissingInputTicks);
        }
    }

    [Fact]
    public void LateStaleAndDuplicateInputsAreRejected()
    {
        var session = NewSession();
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(3, 10)));
        Assert.Equal(InputVerdict.Duplicate, session.Submit(Input(3, 10)));
        Assert.Equal(InputVerdict.Duplicate, session.Submit(Input(4, 10)));
        for (var tick = 0; tick < 3; tick++) session.Step();

        Assert.Equal(10u, session.LastProcessedInputSequence);
        Assert.Equal(InputVerdict.Duplicate, session.Submit(Input(3, 10)));
        Assert.Equal(InputVerdict.Late, session.Submit(Input(3, 11)));
        Assert.Equal(InputVerdict.Late, session.Submit(Input(1, 12)));
        Assert.Equal(InputVerdict.Stale, session.Submit(Input(5, 9)));
        Assert.Equal(InputVerdict.Stale, session.Submit(Input(5, 0)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(4, 11)));
        Assert.Equal(2, session.Counters[InputVerdict.Late]);
        Assert.Equal(3, session.Counters[InputVerdict.Duplicate]);
        Assert.Equal(2, session.Counters[InputVerdict.Stale]);
    }

    [Fact]
    public void InputsTooFarAheadOrBeyondTheQueueBoundAreRejected()
    {
        var session = NewSession(new SessionOptions(InputQueueCapacity: 3, MaxInputLeadTicks: 10));
        Assert.Equal(InputVerdict.TooFarAhead, session.Submit(Input(11, 1)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(10, 2)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(1, 3)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(2, 4)));
        Assert.Equal(InputVerdict.QueueFull, session.Submit(Input(3, 5)));
        Assert.Equal(3, session.QueuedInputCount);
        session.Step();
        Assert.Equal(InputVerdict.TooFarAhead, session.Submit(Input(12, 6)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(11, 6)));
    }

    [Theory]
    [InlineData(0u, 1f, 0f, (byte)0)]
    [InlineData(1u, float.NaN, 0f, (byte)0)]
    [InlineData(1u, 0f, float.PositiveInfinity, (byte)0)]
    [InlineData(1u, 0f, 0f, (byte)2)]
    public void MalformedInputsAreInvalid(uint tick, float thrust, float turn, byte flags)
    {
        var session = NewSession();
        Assert.Equal(InputVerdict.Invalid, session.Submit(new PlayerInput(tick, 1, 0, thrust, turn, (InputFlags)flags)));
        Assert.Equal(0, session.QueuedInputCount);
        // An invalid input does not reserve its sequence.
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(1, 1)));
    }

    [Fact]
    public void RejectedInputsNeverChangeTheWorldQueueOrAcknowledgement()
    {
        var options = new SessionOptions(InputQueueCapacity: 8, MaxInputLeadTicks: 20);
        var clean = NewSession(options);
        var noisy = NewSession(options);
        uint sequence = 100;
        for (uint tick = 1; tick <= 40; tick++)
        {
            var valid = Input(tick + 2, sequence++, tick % 3 == 0 ? -1 : 1, tick % 2 == 0 ? 1 : -1,
                tick % 7 == 0 ? InputFlags.Fire : InputFlags.None);
            Assert.Equal(InputVerdict.Accepted, clean.Submit(valid));
            Assert.Equal(InputVerdict.Accepted, noisy.Submit(valid));
            var hashBefore = WorldStateHasher.Hash(noisy.World);
            var queuedBefore = noisy.QueuedInputCount;
            var ackBefore = noisy.LastProcessedInputSequence;
            List<PlayerInput> rejected =
            [
                valid, // duplicate
                Input((uint)noisy.ServerTick, sequence + 1000, -1, 1, InputFlags.Fire), // late
                Input((uint)noisy.ServerTick + 21, sequence + 2000, -1, -1, InputFlags.Fire), // too far ahead
                new PlayerInput(tick + 2, sequence + 3000, 0, float.NaN, 1, InputFlags.Fire), // invalid
            ];
            // Below every sequence sent so far, once one has run.
            if (ackBefore > 0) rejected.Add(Input(tick + 3, 50, -1, 1, InputFlags.Fire)); // stale
            foreach (var input in rejected) Assert.NotEqual(InputVerdict.Accepted, noisy.Submit(input));
            Assert.Equal(hashBefore, WorldStateHasher.Hash(noisy.World));
            Assert.Equal(queuedBefore, noisy.QueuedInputCount);
            Assert.Equal(ackBefore, noisy.LastProcessedInputSequence);

            clean.Step();
            noisy.Step();
            Assert.Equal(WorldStateHasher.Hash(clean.World), WorldStateHasher.Hash(noisy.World));
            Assert.Equal(clean.LastProcessedInputSequence, noisy.LastProcessedInputSequence);
        }
        TestWorld.SameWorld(clean.World, noisy.World);
        Assert.True(noisy.World.ShotsFired > 0);
    }

    [Fact]
    public void QueueFullLeavesTheQueueUntouchedUntilTicksDrainIt()
    {
        var session = NewSession(new SessionOptions(InputQueueCapacity: 2, MaxInputLeadTicks: 60));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(1, 1)));
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(2, 2)));
        Assert.Equal(InputVerdict.QueueFull, session.Submit(Input(3, 3)));
        session.Step();
        Assert.Equal(InputVerdict.Accepted, session.Submit(Input(3, 3)));
        Assert.Equal(2, session.QueuedInputCount);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(8, 0)]
    public void OptionsMustBePositive(int capacity, int lead) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NewSession(new SessionOptions(capacity, lead)));
}
