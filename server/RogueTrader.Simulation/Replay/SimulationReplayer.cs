using RogueTrader.GameData;

namespace RogueTrader.Simulation.Replay;

/// <param name="Matched">Every checkpoint and the final digest equalled the recording.</param>
/// <param name="FirstMismatchTick">Tick of the first differing digest, or null when matched.</param>
/// <param name="FinalHash">Digest of the replayed world after the last recorded tick.</param>
/// <param name="World">The replayed world, for further inspection.</param>
public sealed record ReplayResult(bool Matched, long? FirstMismatchTick, ulong FinalHash, WorldSimulation World);

public static class SimulationReplayer
{
    /// <summary>Replays the recorded inputs on a fresh world built from <paramref name="catalog"/> and compares digests.</summary>
    public static ReplayResult Replay(GameCatalog catalog, SimulationRecording recording)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(recording);
        var world = new WorldSimulation(catalog);
        long? mismatch = null;
        var next = 0;
        foreach (var input in recording.Inputs)
        {
            world.Tick(input);
            while (next < recording.Checkpoints.Count && recording.Checkpoints[next].Tick == world.TickNumber)
            {
                if (mismatch is null && recording.Checkpoints[next].Hash != WorldStateHasher.Hash(world))
                    mismatch = world.TickNumber;
                next++;
            }
        }
        var finalHash = WorldStateHasher.Hash(world);
        if (mismatch is null && finalHash != recording.FinalHash)
            mismatch = world.TickNumber;
        // A checkpoint past the last tick can never be satisfied, so the recording is inconsistent.
        if (mismatch is null && next < recording.Checkpoints.Count)
            mismatch = recording.Checkpoints[next].Tick;
        return new ReplayResult(mismatch is null, mismatch, finalHash, world);
    }
}
