using System.Text;

namespace RogueTrader.Simulation.Replay;

/// <summary>A state digest taken after the given tick completed.</summary>
public readonly record struct StateCheckpoint(long Tick, ulong Hash);

/// <summary>
/// Everything needed to reproduce a run on the same catalog: the player input for every tick (tick N is
/// <c>Inputs[N-1]</c>) plus periodic state digests and the final digest. The catalog itself is not embedded;
/// the caller must replay against the same game data.
/// </summary>
public sealed class SimulationRecording
{
    public const int FormatVersion = 1;
    // NEW/provisional: default digest cadence of once per simulated second; no original value exists.
    public const int DefaultCheckpointInterval = WorldSimulation.TickRate;
    private static readonly byte[] Magic = "RTRP"u8.ToArray();
    private const int InputBytes = 9;
    private const int CheckpointBytes = 16;

    public int CheckpointInterval { get; }
    public IReadOnlyList<ShipInput> Inputs { get; }
    public IReadOnlyList<StateCheckpoint> Checkpoints { get; }
    public ulong FinalHash { get; }
    public long TickCount => Inputs.Count;

    public SimulationRecording(int checkpointInterval, IEnumerable<ShipInput> inputs,
        IEnumerable<StateCheckpoint> checkpoints, ulong finalHash)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(checkpoints);
        if (checkpointInterval < 1)
            throw new ArgumentOutOfRangeException(nameof(checkpointInterval));
        CheckpointInterval = checkpointInterval;
        Inputs = Array.AsReadOnly(inputs.ToArray());
        Checkpoints = Array.AsReadOnly(checkpoints.ToArray());
        FinalHash = finalHash;
    }

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Magic);
            writer.Write((ushort)FormatVersion);
            writer.Write((ushort)WorldSimulation.TickRate);
            writer.Write(CheckpointInterval);
            writer.Write(Inputs.Count);
            foreach (var input in Inputs)
            {
                writer.Write(input.Thrust);
                writer.Write(input.Turn);
                writer.Write(input.Fire ? (byte)1 : (byte)0);
            }
            writer.Write(Checkpoints.Count);
            foreach (var checkpoint in Checkpoints)
            {
                writer.Write(checkpoint.Tick);
                writer.Write(checkpoint.Hash);
            }
            writer.Write(FinalHash);
        }
        return stream.ToArray();
    }

    /// <exception cref="InvalidDataException">The bytes are truncated, oversized or not a version-1 recording.</exception>
    public static SimulationRecording FromBytes(ReadOnlySpan<byte> bytes)
    {
        using var stream = new MemoryStream(bytes.ToArray(), writable: false);
        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic))
                throw new InvalidDataException("Not a simulation recording.");
            if (reader.ReadUInt16() != FormatVersion)
                throw new InvalidDataException("Unsupported recording version.");
            if (reader.ReadUInt16() != WorldSimulation.TickRate)
                throw new InvalidDataException("Recording tick rate differs from the simulation tick rate.");
            var interval = reader.ReadInt32();
            if (interval < 1)
                throw new InvalidDataException("Invalid checkpoint interval.");
            var inputCount = reader.ReadInt32();
            RequireRemaining(stream, inputCount, InputBytes);
            var inputs = new ShipInput[inputCount];
            for (var index = 0; index < inputCount; index++)
            {
                var thrust = reader.ReadSingle();
                var turn = reader.ReadSingle();
                var fire = reader.ReadByte();
                if (fire > 1)
                    throw new InvalidDataException("Invalid fire flag.");
                inputs[index] = new ShipInput(thrust, turn, fire == 1);
            }
            var checkpointCount = reader.ReadInt32();
            RequireRemaining(stream, checkpointCount, CheckpointBytes);
            var checkpoints = new StateCheckpoint[checkpointCount];
            for (var index = 0; index < checkpointCount; index++)
                checkpoints[index] = new StateCheckpoint(reader.ReadInt64(), reader.ReadUInt64());
            var finalHash = reader.ReadUInt64();
            if (stream.Position != stream.Length)
                throw new InvalidDataException("Trailing bytes after recording.");
            return new SimulationRecording(interval, inputs, checkpoints, finalHash);
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException("Truncated recording.", exception);
        }
    }

    private static void RequireRemaining(MemoryStream stream, int count, int bytesEach)
    {
        if (count < 0 || (stream.Length - stream.Position) / bytesEach < count)
            throw new InvalidDataException("Recording count exceeds the available data.");
    }
}
