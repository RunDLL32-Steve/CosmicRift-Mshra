using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Protocol;
using RogueTrader.Simulation;

namespace RogueTrader.Prediction;

public enum ReconciliationVerdict
{
    Accepted,
    Stale,
    InvalidSnapshot,
    InvalidAcknowledgement
}

/// <summary>
/// NEW/provisional controlled-ship prediction. A single caller serializes issues and reconciliations.
/// Requires a fresh authoritative baseline and matching ship/sector catalog; recreate on context changes.
/// Only motion is predicted. Hull/shield and all gameplay side effects remain authoritative.
/// </summary>
public sealed class ShipPredictor
{
    private readonly ShipMotion _motion;
    private readonly List<PlayerInput> _pending = [];

    public uint ControlledEntityId { get; }
    public int MaxPending { get; }
    public EntityState Ship { get; private set; }
    public uint PredictedTick { get; private set; }
    public uint LastAcceptedServerTick { get; private set; }
    public uint LastAcknowledgedSequence { get; private set; }
    public uint LastIssuedSequence { get; private set; }
    public int PendingCount => _pending.Count;

    public ShipPredictor(GameCatalog catalog, uint controlledEntityId, WorldSnapshot initialSnapshot, int maxPending)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(initialSnapshot);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPending);
        if (!TryReadShip(initialSnapshot, controlledEntityId, out var ship) ||
            (initialSnapshot.ServerTick == 0 && initialSnapshot.LastProcessedInputSequence != 0))
            throw new ArgumentException("A fresh baseline requires one finite controlled ship and a valid initial acknowledgement.", nameof(initialSnapshot));
        _motion = new ShipMotion(catalog);
        ControlledEntityId = controlledEntityId;
        MaxPending = maxPending;
        Ship = ship;
        PredictedTick = LastAcceptedServerTick = initialSnapshot.ServerTick;
        LastIssuedSequence = LastAcknowledgedSequence = initialSnapshot.LastProcessedInputSequence;
    }

    /// <summary>
    /// Issues one immutable intent for the next tick and predicts its motion. Invalid controls, a full queue or exhausted
    /// uint identifiers return false without consuming identifiers or changing any prediction state.
    /// Fire is carried in the message only. Resubmit the returned message itself when transport requires a retry.
    /// </summary>
    public bool TryPredict(float thrust, float turn, InputFlags flags, [NotNullWhen(true)] out PlayerInput? input)
    {
        input = null;
        if (!float.IsFinite(thrust) || !float.IsFinite(turn) || Math.Abs(thrust) > 1 || Math.Abs(turn) > 1 ||
            (flags & ~InputFlags.Fire) != 0 || _pending.Count >= MaxPending ||
            PredictedTick == uint.MaxValue || LastIssuedSequence == uint.MaxValue)
            return false;
        input = new PlayerInput(PredictedTick + 1, LastIssuedSequence + 1, LastAcceptedServerTick, thrust, turn, flags);
        var ship = Predict(Ship, input);
        _pending.Add(input);
        Ship = ship;
        PredictedTick = input.ClientTick;
        LastIssuedSequence = input.Sequence;
        return true;
    }

    /// <summary>
    /// Corrects to a newer server baseline, expires every completed tick (including lost inputs), then replays its suffix once.
    /// Rejection is atomic. An acknowledgement may stay unchanged or name a retained issued intent whose tick has completed.
    /// </summary>
    public ReconciliationVerdict Reconcile(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.ServerTick <= LastAcceptedServerTick) return ReconciliationVerdict.Stale;
        if (!TryReadShip(snapshot, ControlledEntityId, out var ship)) return ReconciliationVerdict.InvalidSnapshot;
        var ack = snapshot.LastProcessedInputSequence;
        if (ack < LastAcknowledgedSequence ||
            (ack != LastAcknowledgedSequence && !_pending.Any(p => p.Sequence == ack && p.ClientTick <= snapshot.ServerTick)))
            return ReconciliationVerdict.InvalidAcknowledgement;

        var completed = 0;
        while (completed < _pending.Count && _pending[completed].ClientTick <= snapshot.ServerTick) completed++;
        var tick = snapshot.ServerTick;
        for (var i = completed; i < _pending.Count; i++)
        {
            ship = Predict(ship, _pending[i]);
            tick = _pending[i].ClientTick;
        }
        _pending.RemoveRange(0, completed);
        Ship = ship;
        PredictedTick = tick;
        LastAcceptedServerTick = snapshot.ServerTick;
        LastAcknowledgedSequence = ack;
        return ReconciliationVerdict.Accepted;
    }

    private EntityState Predict(EntityState ship, PlayerInput input)
    {
        if (ship.Hull <= 0) return ship;
        var result = _motion.Step(new ShipMotionState(new Vector2(ship.X, ship.Y), new Vector2(ship.VelX, ship.VelY), ship.Rotation),
            new ShipInput(input.Thrust, input.Turn, false));
        return ship with { X = result.Position.X, Y = result.Position.Y, VelX = result.Velocity.X, VelY = result.Velocity.Y, Rotation = result.Heading };
    }

    private static bool TryReadShip(WorldSnapshot snapshot, uint id, out EntityState ship)
    {
        ship = default;
        if (snapshot.Entities is null || snapshot.Entities.Length > ProtocolLimits.MaxSnapshotEntities) return false;
        var found = false;
        foreach (var entity in snapshot.Entities)
        {
            if (entity.EntityId != id) continue;
            if (found || entity.Kind != EntityKind.Ship ||
                !float.IsFinite(entity.X) || !float.IsFinite(entity.Y) || !float.IsFinite(entity.VelX) ||
                !float.IsFinite(entity.VelY) || !float.IsFinite(entity.Rotation) || !float.IsFinite(entity.Hull) || !float.IsFinite(entity.Shield))
                return false;
            ship = entity;
            found = true;
        }
        return found;
    }
}
