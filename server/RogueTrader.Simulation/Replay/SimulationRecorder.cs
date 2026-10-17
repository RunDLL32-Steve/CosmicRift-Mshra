namespace RogueTrader.Simulation.Replay;

/// <summary>Drives a <see cref="WorldSimulation"/> while capturing its per-tick input and periodic state digests.</summary>
public sealed class SimulationRecorder
{
    private readonly List<ShipInput> _inputs = [];
    private readonly List<StateCheckpoint> _checkpoints = [];
    private readonly int _interval;

    public WorldSimulation World { get; }

    /// <param name="world">A world that has not yet ticked; replays start from a freshly constructed world.</param>
    public SimulationRecorder(WorldSimulation world, int checkpointInterval = SimulationRecording.DefaultCheckpointInterval)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.TickNumber != 0)
            throw new ArgumentException("Recording must begin at tick 0.", nameof(world));
        if (checkpointInterval < 1)
            throw new ArgumentOutOfRangeException(nameof(checkpointInterval));
        World = world;
        _interval = checkpointInterval;
    }

    public void Tick(ShipInput input)
    {
        World.Tick(input);
        _inputs.Add(input);
        if (World.TickNumber % _interval == 0)
            _checkpoints.Add(new StateCheckpoint(World.TickNumber, WorldStateHasher.Hash(World)));
    }

    public SimulationRecording Finish() =>
        new(_interval, _inputs, _checkpoints, WorldStateHasher.Hash(World));
}
