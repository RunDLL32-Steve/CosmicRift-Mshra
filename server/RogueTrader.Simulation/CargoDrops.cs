using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

public sealed partial class WorldSimulation
{
    /// <summary>Id prefix of piles spilled by a destroyed ship: <c>wreck:&lt;drop id&gt;:&lt;item id&gt;</c>.</summary>
    public const string WreckPilePrefix = "wreck:";

    /// <summary>Id segment of the hull's own wreck pile: <c>wreck:&lt;drop id&gt;:hull:&lt;item id&gt;</c>, apart from the cargo piles.</summary>
    public const string HullWreckSegment = "hull:";

    /// <summary>Open-space drop life, CFG [Terrain0] PrizeExpire = 720 s; on a map the terrain under the pile decides (120 s in docks and trade zones).</summary>
    public const long WreckDropExpireTicks = 720L * TickRate;

    /// <summary>Pickup radius of a spilled pile: CFG [Arena] ItemPickupDistance = 80 map pixels, as mineral drops use (NEW for wreck drops).</summary>
    public const float WreckPileRadius = 80f;

    /// <summary>Distance from the wreck at which the spilled piles lie, spread evenly around it (NEW placement).</summary>
    public const float WreckSpreadRadius = 12f;

    /// <summary>
    /// The pilot flying this world's ship (the profile id). A pile whose <see cref="CollectibleState.DroppedBy"/> names this
    /// pilot is never collected by this world's ship; null means the pilot is unnamed, and such a world collects every pile.
    /// </summary>
    public string? PilotId { get; init; }

    /// <summary>
    /// Where this world's pilot plays from: the install (one computer's game) on the local client, later a connection's account
    /// or machine on a server. A hull wreck whose <see cref="CollectibleState.DroppedFrom"/> is this origin is never collected by
    /// this world's ship either, so a second profile on the same computer cannot feed itself a wreck (review R2, Nov 10 2026).
    /// Null means unknown, and then only <see cref="PilotId"/> is checked.
    /// </summary>
    public string? PilotOrigin { get; init; }

    /// <summary>
    /// Spills a destroyed ship's cargo into space where it died (Steve, Nov 4 2026): one pile per item, in item-id order, spread
    /// around <paramref name="position"/>. Each pile expires after <see cref="WreckDropExpireTicks"/> on the mineral clock, so it
    /// keeps counting while nobody is in the sector, and is carried by <see cref="CaptureSector"/> like any other drop. Anyone
    /// can pick it up through the normal pickup grant. <paramref name="dropId"/> must be unique per destruction (the caller uses
    /// the destruction's world and tick), so the same spill can never be placed twice. Items with zero or negative quantity and
    /// items this world's cargo catalog does not know are skipped. Returns the piles placed.
    /// </summary>
    public IReadOnlyList<CollectibleState> DropCargo(string dropId, Vector2 position, IEnumerable<KeyValuePair<string, int>> items)
    {
        ArgumentException.ThrowIfNullOrEmpty(dropId);
        ArgumentNullException.ThrowIfNull(items);
        RequireFinite(position);
        var spill = items.Where(i => i.Value > 0 && Cargo.Catalog.IsKnown(i.Key))
            .OrderBy(i => i.Key, StringComparer.Ordinal).ToArray();
        var placed = new List<CollectibleState>(spill.Length);
        for (var n = 0; n < spill.Length; n++)
        {
            var id = $"{WreckPilePrefix}{dropId}:{spill[n].Key}";
            if (!_collectibleIds.Add(id)) continue; // already spilled
            var angle = spill.Length == 1 ? 0 : MathF.Tau * n / spill.Length;
            var offset = spill.Length == 1 ? Vector2.Zero : new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * WreckSpreadRadius;
            var at = position + offset;
            var pile = new CollectibleState(new CollectibleSpawn(id, spill[n].Key, spill[n].Value, at.X, at.Y, WreckPileRadius))
            {
                ExpiresAt = checked(MineralClock + DropLifeAt(at, WreckDropExpireTicks)),
            };
            _collectibles.Add(pile);
            placed.Add(pile);
        }
        return placed;
    }

    /// <summary>
    /// Leaves the destroyed hull's own wreck (VEH f[68..69], RT-F043: 1 Wreckage on every original hull) where it died, as its own
    /// pile apart from the spilled cargo. The destruction produces it, so it drops even from an empty hold. It is marked
    /// <paramref name="droppedBy"/>: that pilot can never collect it, while anyone else can (review R1-1, Nov 9 2026). Otherwise
    /// dying would pay, since the 20% penalty is below the wreck's 300-credit trade value under 1,500 credits; in the original,
    /// other pilots collected your wreck. Placed at most once per <paramref name="dropId"/>, and it expires like the cargo piles.
    /// <paramref name="droppedFrom"/> is the dying pilot's <see cref="PilotOrigin"/>: no pilot from that origin can collect it
    /// either, so another profile on the same computer cannot collect it for them.
    /// Returns the pile, or null when the hull drops nothing, the item is unknown here or this drop was already placed.
    /// </summary>
    public CollectibleState? DropHullWreck(string dropId, Vector2 position, ShipWreck? wreck, string? droppedBy, string? droppedFrom = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(dropId);
        RequireFinite(position);
        if (wreck is not { Quantity: > 0 } || string.IsNullOrEmpty(wreck.ItemId) || !Cargo.Catalog.IsKnown(wreck.ItemId)) return null;
        var id = $"{WreckPilePrefix}{dropId}:{HullWreckSegment}{wreck.ItemId}";
        if (!_collectibleIds.Add(id)) return null; // already dropped
        var pile = new CollectibleState(new CollectibleSpawn(id, wreck.ItemId, wreck.Quantity, position.X, position.Y, WreckPileRadius))
        {
            ExpiresAt = checked(MineralClock + DropLifeAt(position, WreckDropExpireTicks)),
            DroppedBy = string.IsNullOrEmpty(droppedBy) ? null : droppedBy,
            DroppedFrom = string.IsNullOrEmpty(droppedFrom) ? null : droppedFrom,
        };
        _collectibles.Add(pile);
        return pile;
    }

    /// <summary>
    /// True when this world's ship may collect <paramref name="pile"/>: every pile except a hull wreck its own pilot dropped, or
    /// that any pilot from the same origin (the same computer) dropped.
    /// </summary>
    public bool CanCollect(CollectibleState pile)
    {
        ArgumentNullException.ThrowIfNull(pile);
        if (pile.DroppedBy is not null && PilotId is not null && string.Equals(pile.DroppedBy, PilotId, StringComparison.Ordinal)) return false;
        return pile.DroppedFrom is null || PilotOrigin is null || !string.Equals(pile.DroppedFrom, PilotOrigin, StringComparison.Ordinal);
    }

    private static void RequireFinite(Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
            throw new ArgumentOutOfRangeException(nameof(position), "The drop position must be finite.");
    }
}
