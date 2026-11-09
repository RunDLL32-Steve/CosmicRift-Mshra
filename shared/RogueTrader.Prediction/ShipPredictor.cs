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
    InvalidAcknowledgement,
    InvalidMotion
}

/// <summary>
/// NEW/provisional controlled-ship prediction. A single caller serializes issues and reconciliations.
/// Requires a fresh authoritative baseline and matching ship/sector catalog and tile map; recreate on context changes.
/// Motion is predicted through the authority's own <see cref="ShipMotion"/> step, including the Shift boost (paid from the
/// snapshot's energy, with the tick's recharge) and wall bounces on the supplied map. Hull/shield, energy spent by firing,
/// mining or hits, and all other gameplay side effects remain authoritative; the next snapshot corrects them.
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

    /// <param name="map">The authority's tile map (<see cref="WorldSimulation.Map"/>), so wall bounces are predicted; null for
    /// open space. Passing a different map than the authority's makes every wall contact a correction.</param>
    public ShipPredictor(GameCatalog catalog, uint controlledEntityId, WorldSnapshot initialSnapshot, int maxPending,
        SectorMap? map = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(initialSnapshot);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPending);
        if (!TryReadShip(initialSnapshot, controlledEntityId, out var ship) ||
            (initialSnapshot.ServerTick == 0 && initialSnapshot.LastProcessedInputSequence != 0))
            throw new ArgumentException("A fresh baseline requires one finite controlled ship and a valid initial acknowledgement.", nameof(initialSnapshot));
        _motion = new ShipMotion(catalog, map);
        ControlledEntityId = controlledEntityId;
        MaxPending = maxPending;
        Ship = ship;
        PredictedTick = LastAcceptedServerTick = initialSnapshot.ServerTick;
        LastIssuedSequence = LastAcknowledgedSequence = initialSnapshot.LastProcessedInputSequence;
    }

    /// <summary>
    /// Issues one immutable intent for the next tick and predicts its motion. Invalid controls, a full queue or exhausted
    /// uint identifiers, or nonfinite derived motion, return false without consuming identifiers or changing prediction state.
    /// Fire is carried in the message only; Hyper is carried and also predicted. Resubmit the returned message itself when transport requires a retry.
    /// </summary>
    public bool TryPredict(float thrust, float turn, InputFlags flags, [NotNullWhen(true)] out PlayerInput? input)
    {
        input = null;
        if (!float.IsFinite(thrust) || !float.IsFinite(turn) || Math.Abs(thrust) > 1 || Math.Abs(turn) > 1 ||
            (flags & ~MessageCodec.KnownFlags) != 0 || _pending.Count >= MaxPending ||
            PredictedTick == uint.MaxValue || LastIssuedSequence == uint.MaxValue)
            return false;
        var intent = new PlayerInput(PredictedTick + 1, LastIssuedSequence + 1, LastAcceptedServerTick, thrust, turn, flags);
        var ship = Predict(Ship, intent);
        if (!HasFiniteMotion(ship)) return false;
        _pending.Add(intent);
        Ship = ship;
        PredictedTick = intent.ClientTick;
        LastIssuedSequence = intent.Sequence;
        input = intent;
        return true;
    }

    /// <summary>
    /// Corrects to a newer server baseline, expires every completed tick (including lost inputs), then replays its suffix once.
    /// Rejection is atomic, including nonfinite derived replay motion (InvalidMotion). An acknowledgement may stay unchanged
    /// or name a retained issued intent whose tick has completed.
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
            if (!HasFiniteMotion(ship)) return ReconciliationVerdict.InvalidMotion;
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
        var result = _motion.Step(
            new ShipMotionState(new Vector2(ship.X, ship.Y), new Vector2(ship.VelX, ship.VelY), ship.Rotation, ship.Energy),
            new ShipInput(input.Thrust, input.Turn, false, Hyper: input.Flags.HasFlag(InputFlags.Hyper)));
        return ship with
        {
            X = result.Position.X, Y = result.Position.Y, VelX = result.Velocity.X, VelY = result.Velocity.Y,
            Rotation = result.Heading, Energy = result.Energy
        };
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
                !HasFiniteMotion(entity) || !float.IsFinite(entity.Hull) || !float.IsFinite(entity.Shield))
                return false;
            ship = entity;
            found = true;
        }
        return found;
    }

    private static bool HasFiniteMotion(EntityState ship) =>
        float.IsFinite(ship.X) && float.IsFinite(ship.Y) && float.IsFinite(ship.VelX) &&
        float.IsFinite(ship.VelY) && float.IsFinite(ship.Rotation) && float.IsFinite(ship.Energy);
}
