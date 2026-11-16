using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

/// <summary>A space mine lying in space (RT-F038). It stays where it was laid until it goes off or its life runs out.</summary>
public sealed class MineState
{
    public long Id { get; }
    public string OwnerShipId { get; }
    public MineDefinition Definition { get; }
    public Vector2 Position { get; }
    public int AgeTicks { get; internal set; }
    public int LifetimeTicks { get; }
    public int ArmTicks { get; }
    public bool IsArmed => AgeTicks >= ArmTicks;

    internal MineState(long id, string ownerShipId, MineDefinition definition, Vector2 position, int ageTicks = 0)
    {
        Id = id;
        OwnerShipId = ownerShipId;
        Definition = definition;
        Position = position;
        AgeTicks = ageTicks;
        LifetimeTicks = Math.Max(1, (int)MathF.Ceiling(definition.LifetimeSeconds * WorldSimulation.TickRate));
        ArmTicks = (int)MathF.Ceiling(definition.ArmSeconds * WorldSimulation.TickRate);
    }
}

/// <summary>One mine that went off: where, whose it was, what set it off and which ships its blast reached.</summary>
public sealed record MineDetonation(long MineId, string OwnerShipId, float X, float Y, string TriggeredByShipId, IReadOnlyList<string> ShipsHit, long Tick);

/// <summary>A mine still lying in space in a <see cref="SectorSnapshot"/>; <paramref name="AgeTicks"/> is how long it has lain there.</summary>
public sealed record MineSnapshot(long Id, string OwnerShipId, string MineId, float X, float Y, int AgeTicks);

public sealed partial class WorldSimulation
{
    /// <summary>Pile id of the record a laid mine leaves in <see cref="Pickups"/>: <c>mine:&lt;mine id&gt;</c>.</summary>
    public const string MineUsePilePrefix = "mine:";

    private readonly List<MineState> _mines = [];
    private readonly List<MineDetonation> _detonations = [];
    private long _nextMineId;
    private long _nextMineDropTick;

    /// <summary>The mine the player's ship lays from its hold; null when it carries no mine launcher (nothing is laid).</summary>
    public MineDefinition? PlayerMine { get; }

    /// <summary>Mines lying in space, oldest first.</summary>
    public IReadOnlyList<MineState> Mines => _mines;

    /// <summary>Every mine that went off in this world, in order.</summary>
    public IReadOnlyList<MineDetonation> Detonations => _detonations;

    public long MinesLaid { get; private set; }

    /// <summary>The mine's item must be a cargo item this world knows, since every mine laid comes out of the hold.</summary>
    private static MineDefinition? ValidatePlayerMine(MineDefinition? mine, CargoCatalog cargo)
    {
        if (mine is null) return null;
        mine.Validate();
        if (!cargo.IsKnown(mine.ItemId))
            throw new ArgumentException($"Mine '{mine.Id}' uses unknown cargo item '{mine.ItemId}'.", nameof(mine));
        return mine;
    }

    /// <summary>
    /// Lays one mine where the ship is, at rest (the Plasma Mine's original muzzle velocity and inherited speed are both 0). It
    /// needs a mine in the hold, the drop interval to have passed, and fewer than the per-pilot limit already lying in space.
    /// The unit leaves the hold at once and the world records it as a pickup of -1, so the profile is charged through the same
    /// saved queue as every pickup and a relaunch can never give a laid mine back.
    /// </summary>
    private void TryLayMine()
    {
        if (PlayerMine is not { } mine || TickNumber < _nextMineDropTick) return;
        if (_mines.Count(m => m.OwnerShipId == Player.Id && m.Definition.Id == mine.Id) >= mine.MaxLivePerPlayer) return;
        if (!Cargo.TryRemove(mine.ItemId, 1)) return;
        var id = ++_nextMineId;
        _mines.Add(new MineState(id, Player.Id, mine, Player.Position));
        _pickups.Add(new CargoPickup(_pickups.Count, MineUsePilePrefix + id.ToString(System.Globalization.CultureInfo.InvariantCulture), mine.ItemId, -1, TickNumber));
        _nextMineDropTick = TickNumber + Math.Max(1, (int)MathF.Ceiling(mine.DropIntervalSeconds * TickRate));
        MinesLaid++;
    }

    /// <summary>
    /// Ages every mine, removes those whose life ran out, and sets off each armed mine that another ship has come within its
    /// proximity radius of (measured to the ship's edge). A blast reaches every ship other than the mine's owner whose centre is
    /// inside the blast radius, with drain and damage falling linearly from inner to outer over that radius.
    /// </summary>
    private void StepMines()
    {
        for (var index = 0; index < _mines.Count;)
        {
            var mine = _mines[index];
            mine.AgeTicks++;
            if (mine.AgeTicks >= mine.LifetimeTicks)
            {
                _mines.RemoveAt(index);
                continue;
            }
            var trigger = mine.IsArmed ? Ships().FirstOrDefault(s => s.Id != mine.OwnerShipId && !s.IsDestroyed
                && Vector2.Distance(s.Position, mine.Position) <= mine.Definition.ProximityRadius + s.Radius) : null;
            if (trigger is null)
            {
                index++;
                continue;
            }
            _mines.RemoveAt(index);
            Detonate(mine, trigger.Id);
        }
    }

    private IEnumerable<ShipState> Ships() => Targets.Prepend(Player);

    private void Detonate(MineState mine, string triggeredBy)
    {
        var d = mine.Definition;
        var hit = new List<string>();
        foreach (var ship in Ships())
        {
            if (ship.Id == mine.OwnerShipId || ship.IsDestroyed) continue;
            var distance = Vector2.Distance(ship.Position, mine.Position);
            if (distance > d.BlastRadius) continue;
            var t = distance / d.BlastRadius;
            var drain = d.ShieldDrainInner + (d.ShieldDrainOuter - d.ShieldDrainInner) * t;
            var damage = d.DamageInner + (d.DamageOuter - d.DamageInner) * t;
            ApplyDamage(ship, mine.OwnerShipId, d.Id, drain, damage);
            hit.Add(ship.Id);
        }
        _detonations.Add(new MineDetonation(mine.Id, mine.OwnerShipId, mine.Position.X, mine.Position.Y, triggeredBy, hit, TickNumber));
    }

    /// <summary>Time passing with no ticks (docked, paused, closed) ages the mines too, so a mine never outlives its life offline.</summary>
    private void ElapseMines(int ticks)
    {
        foreach (var mine in _mines) mine.AgeTicks = (int)Math.Min(int.MaxValue, (long)mine.AgeTicks + ticks);
        _mines.RemoveAll(m => m.AgeTicks >= m.LifetimeTicks);
    }

    private IReadOnlyList<MineSnapshot> CaptureMines() =>
        _mines.Select(m => new MineSnapshot(m.Id, m.OwnerShipId, m.Definition.Id, m.Position.X, m.Position.Y, m.AgeTicks)).ToArray();

    /// <summary>
    /// Puts saved mines back. Only mines of this world's player mine are restored (a mine needs its definition); one whose life
    /// has run out, or with a bad position or a repeated id, is dropped.
    /// </summary>
    private void RestoreMines(IReadOnlyList<MineSnapshot>? saved)
    {
        _mines.Clear();
        foreach (var m in saved ?? [])
        {
            if (m is null || PlayerMine is not { } mine || m.MineId != mine.Id || string.IsNullOrEmpty(m.OwnerShipId)
                || !float.IsFinite(m.X) || !float.IsFinite(m.Y) || m.AgeTicks < 0 || _mines.Any(x => x.Id == m.Id))
                continue;
            var state = new MineState(m.Id, m.OwnerShipId, mine, new Vector2(m.X, m.Y), m.AgeTicks);
            if (state.AgeTicks < state.LifetimeTicks) _mines.Add(state);
            _nextMineId = Math.Max(_nextMineId, m.Id);
        }
    }
}
