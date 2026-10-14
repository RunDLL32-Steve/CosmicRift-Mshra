namespace RogueTrader.Simulation;

/// <summary>Converts elapsed rendering time to whole simulation ticks without changing tick duration.</summary>
public sealed class FixedStepRunner(WorldSimulation world)
{
    private const double Step = 1.0 / WorldSimulation.TickRate;
    private double _accumulator;
    public WorldSimulation World { get; } = world ?? throw new ArgumentNullException(nameof(world));
    public double InterpolationFraction => _accumulator / Step;

    public int Advance(double elapsedSeconds, ShipInput input) => Advance(elapsedSeconds, _ => input);

    public int Advance(double elapsedSeconds, Func<long, ShipInput> inputForTick)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        ArgumentNullException.ThrowIfNull(inputForTick);
        _accumulator += elapsedSeconds;
        var advanced = 0;
        while (_accumulator + 1e-12 >= Step)
        {
            World.Tick(inputForTick(World.TickNumber + 1));
            _accumulator = Math.Max(0, _accumulator - Step);
            advanced++;
        }
        return advanced;
    }
}
