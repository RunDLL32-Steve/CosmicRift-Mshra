using RogueTrader.Simulation;
using RogueTrader.Simulation.Replay;

namespace RogueTrader.UnitTests;

public sealed class ReplayHarnessTests
{
    // Fixed seed: the generated input script is part of the test, never wall-clock or ambient randomness.
    private static ShipInput[] Script(int ticks, int seed = 20261016)
    {
        var random = new Random(seed);
        var inputs = new ShipInput[ticks];
        var thrust = 0f;
        var turn = 0f;
        for (var tick = 0; tick < ticks; tick++)
        {
            if (tick % 45 == 0)
            {
                thrust = random.Next(-1, 2);
                turn = random.Next(-1, 2) * (random.Next(2) == 0 ? 1f : 0.5f);
            }
            inputs[tick] = new ShipInput(thrust, turn, tick % 7 < 4);
        }
        return inputs;
    }

    private static SimulationRecording Record(ShipInput[] inputs, int interval = 60)
    {
        var recorder = new SimulationRecorder(new WorldSimulation(TestWorld.ExternalCatalog()), interval);
        foreach (var input in inputs)
            recorder.Tick(input);
        return recorder.Finish();
    }

    [Fact]
    public void RecordedRunReplaysToIdenticalStateAtEveryCheckpoint()
    {
        var recording = Record(Script(3600));
        var result = SimulationReplayer.Replay(TestWorld.ExternalCatalog(), recording);
        Assert.True(result.Matched);
        Assert.Null(result.FirstMismatchTick);
        Assert.Equal(recording.FinalHash, result.FinalHash);
        Assert.Equal(3600, result.World.TickNumber);
        Assert.Equal(60, recording.Checkpoints.Count);
        Assert.Equal(60, recording.Checkpoints[0].Tick);
        Assert.True(result.World.ShotsFired > 0);
    }

    [Fact]
    public void ReplayHashesMatchLiveWorldComparedFieldByField()
    {
        var inputs = Script(1800);
        var live = new WorldSimulation(TestWorld.ExternalCatalog());
        foreach (var input in inputs)
            live.Tick(input);
        var result = SimulationReplayer.Replay(TestWorld.ExternalCatalog(), Record(inputs));
        TestWorld.SameWorld(live, result.World);
        Assert.Equal(WorldStateHasher.Hash(live), result.FinalHash);
    }

    [Fact]
    public void IndependentIdenticalWorldsHashEqualAtEveryTick()
    {
        var first = new WorldSimulation(TestWorld.ExternalCatalog());
        var second = new WorldSimulation(TestWorld.ExternalCatalog());
        Assert.Equal(WorldStateHasher.Hash(first), WorldStateHasher.Hash(second));
        foreach (var input in Script(600))
        {
            first.Tick(input);
            second.Tick(input);
            Assert.Equal(WorldStateHasher.Hash(first), WorldStateHasher.Hash(second));
        }
    }

    [Fact]
    public void HashReactsToEveryKindOfStateChange()
    {
        var idle = new WorldSimulation(TestWorld.ExternalCatalog());
        var thrusting = new WorldSimulation(TestWorld.ExternalCatalog());
        var turning = new WorldSimulation(TestWorld.ExternalCatalog());
        var firing = new WorldSimulation(TestWorld.ExternalCatalog());
        idle.Tick(default);
        thrusting.Tick(new ShipInput(1, 0, false));
        turning.Tick(new ShipInput(0, 1, false));
        firing.Tick(new ShipInput(0, 0, true));
        var hashes = new[] { idle, thrusting, turning, firing }.Select(WorldStateHasher.Hash).ToHashSet();
        Assert.Equal(4, hashes.Count);
        Assert.NotEmpty(firing.Projectiles);
        var before = WorldStateHasher.Hash(firing);
        firing.Tick(default);
        Assert.NotEqual(before, WorldStateHasher.Hash(firing));
    }

    [Fact]
    public void DifferentInputsProduceDifferentFinalStates()
    {
        var inputs = Script(900);
        var altered = (ShipInput[])inputs.Clone();
        altered[500] = altered[500] with { Turn = altered[500].Turn + 0.25f };
        Assert.NotEqual(Record(inputs).FinalHash, Record(altered).FinalHash);
    }

    [Fact]
    public void ChangedInputIsDetectedAtTheNextCheckpoint()
    {
        var inputs = Script(900);
        var recording = Record(inputs);
        var altered = (ShipInput[])inputs.Clone();
        altered[500] = altered[500] with { Thrust = altered[500].Thrust > 0 ? -1 : 1 };
        var tampered = new SimulationRecording(recording.CheckpointInterval, altered, recording.Checkpoints,
            recording.FinalHash);
        var result = SimulationReplayer.Replay(TestWorld.ExternalCatalog(), tampered);
        Assert.False(result.Matched);
        // Input index 500 is tick 501; checkpoints fall every 60 ticks, so tick 540 is the first to differ.
        Assert.Equal(540, result.FirstMismatchTick);
    }

    [Fact]
    public void TamperedFinalHashAndCheckpointHashAreRejected()
    {
        var recording = Record(Script(300));
        var badFinal = new SimulationRecording(recording.CheckpointInterval, recording.Inputs, recording.Checkpoints,
            recording.FinalHash ^ 1);
        var finalResult = SimulationReplayer.Replay(TestWorld.ExternalCatalog(), badFinal);
        Assert.False(finalResult.Matched);
        Assert.Equal(300, finalResult.FirstMismatchTick);

        var checkpoints = recording.Checkpoints.ToArray();
        checkpoints[2] = checkpoints[2] with { Hash = checkpoints[2].Hash ^ 1 };
        var badCheckpoint = new SimulationRecording(recording.CheckpointInterval, recording.Inputs, checkpoints,
            recording.FinalHash);
        var checkpointResult = SimulationReplayer.Replay(TestWorld.ExternalCatalog(), badCheckpoint);
        Assert.False(checkpointResult.Matched);
        Assert.Equal(checkpoints[2].Tick, checkpointResult.FirstMismatchTick);
    }

    [Fact]
    public void CheckpointBeyondTheLastTickMakesTheRecordingInconsistent()
    {
        var recording = Record(Script(120));
        var extra = recording.Checkpoints.Append(new StateCheckpoint(180, 1)).ToArray();
        var inconsistent = new SimulationRecording(recording.CheckpointInterval, recording.Inputs, extra,
            recording.FinalHash);
        var result = SimulationReplayer.Replay(TestWorld.ExternalCatalog(), inconsistent);
        Assert.False(result.Matched);
        Assert.Equal(180, result.FirstMismatchTick);
    }

    [Fact]
    public void SerializationRoundTripsBytesAndReplays()
    {
        var recording = Record(Script(1200), interval: 30);
        var bytes = recording.ToBytes();
        var restored = SimulationRecording.FromBytes(bytes);
        Assert.Equal(recording.CheckpointInterval, restored.CheckpointInterval);
        Assert.Equal(recording.Inputs, restored.Inputs);
        Assert.Equal(recording.Checkpoints, restored.Checkpoints);
        Assert.Equal(recording.FinalHash, restored.FinalHash);
        Assert.Equal(bytes, restored.ToBytes());
        Assert.True(SimulationReplayer.Replay(TestWorld.ExternalCatalog(), restored).Matched);
    }

    [Fact]
    public void EveryTruncationOfARecordingIsRejected()
    {
        var bytes = Record(Script(130), interval: 50).ToBytes();
        for (var length = 0; length < bytes.Length; length++)
            Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(bytes.AsSpan(0, length)));
    }

    [Fact]
    public void MalformedRecordingsAreRejected()
    {
        var bytes = Record(Script(60)).ToBytes();

        var badMagic = (byte[])bytes.Clone();
        badMagic[0] ^= 0xFF;
        Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(badMagic));

        var badVersion = (byte[])bytes.Clone();
        badVersion[4] = 9;
        Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(badVersion));

        var badTickRate = (byte[])bytes.Clone();
        badTickRate[6] = 30;
        Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(badTickRate));

        var zeroInterval = (byte[])bytes.Clone();
        Array.Clear(zeroInterval, 8, 4);
        Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(zeroInterval));

        var hugeCount = (byte[])bytes.Clone();
        BitConverter.GetBytes(int.MaxValue).CopyTo(hugeCount, 12);
        Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(hugeCount));

        var negativeCount = (byte[])bytes.Clone();
        BitConverter.GetBytes(-1).CopyTo(negativeCount, 12);
        Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(negativeCount));

        var badFire = (byte[])bytes.Clone();
        badFire[16 + 8] = 2;
        Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(badFire));

        Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes([.. bytes, 0]));
    }

    [Fact]
    public void RecorderRequiresAFreshWorldAndValidInterval()
    {
        var started = new WorldSimulation(TestWorld.ExternalCatalog());
        started.Tick(default);
        Assert.Throws<ArgumentException>(() => new SimulationRecorder(started));
        var fresh = new WorldSimulation(TestWorld.ExternalCatalog());
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationRecorder(fresh, 0));
    }

    [Fact]
    public void EmptyRecordingReplaysToTheInitialState()
    {
        var recording = Record([]);
        Assert.Empty(recording.Checkpoints);
        var result = SimulationReplayer.Replay(TestWorld.ExternalCatalog(), recording);
        Assert.True(result.Matched);
        Assert.Equal(0, result.World.TickNumber);
    }
}
