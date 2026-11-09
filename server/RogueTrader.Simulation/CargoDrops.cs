using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

public sealed partial class WorldSimulation
{
    /// <summary>Id prefix of piles spilled by a destroyed ship: <c>wreck:&lt;drop id&gt;:&lt;item id&gt;</c>.</summary>
    public const string WreckPilePrefix = "wreck:";

    /// <summary>Open-space drop life, CFG [Terrain0] PrizeExpire = 720 s; on a map the terrain under the pile decides (120 s in docks and trade zones).</summary>
    public const long WreckDropExpireTicks = 720L * TickRate;

    /// <summary>Pickup radius of a spilled pile: CFG [Arena] ItemPickupDistance = 80 map pixels, as mineral drops use (NEW for wreck drops).</summary>
    public const float WreckPileRadius = 80f;

    /// <summary>Distance from the wreck at which the spilled piles lie, spread evenly around it (NEW placement).</summary>
    public const float WreckSpreadRadius = 12f;

    /// <summary>
    /// Spills a destroyed ship's cargo into space where it died (Steve, Nov 4 2026): one pile per item, in item-id order, spread
    /// around <paramref name="position"/>. Each pile expires after <see cref="WreckDropExpireTicks"/> on the mineral clock, so it
    /// keeps counting while nobody is in the sector, and is carried by <see cref="CaptureSector"/> like any other drop. Anyone
    /// can pick it up through the normal pickup grant. <paramref name="dropId"/> must be unique per destruction (the caller uses
    /// the destruction's world and tick), so the same spill can never be placed twice. Items with zero or negative quantity and
    /// items this world's cargo catalog does not know are skipped. Returns the piles placed.
    /// </summary>
    /// <summary>
    /// What a destroyed ship leaves where it died: the cargo it lost plus the hull's own wreck (VEH f[68..69], RT-F043), summed per
    /// item so a ship that carried Wreckage drops one pile holding both. The hull's wreck is produced by the destruction itself,
    /// so it drops even from an empty hold. Non-positive cargo quantities are left out.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, int>> DestructionDrop(IEnumerable<KeyValuePair<string, int>> lostCargo, ShipWreck? hullWreck)
    {
        ArgumentNullException.ThrowIfNull(lostCargo);
        var drop = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var (item, quantity) in lostCargo)
            if (quantity > 0) drop[item] = checked(drop.GetValueOrDefault(item) + quantity);
        if (hullWreck is { Quantity: > 0 } wreck && !string.IsNullOrEmpty(wreck.ItemId))
            drop[wreck.ItemId] = checked(drop.GetValueOrDefault(wreck.ItemId) + wreck.Quantity);
        return drop.ToArray();
    }

    public IReadOnlyList<CollectibleState> DropCargo(string dropId, Vector2 position, IEnumerable<KeyValuePair<string, int>> items)
    {
        ArgumentException.ThrowIfNullOrEmpty(dropId);
        ArgumentNullException.ThrowIfNull(items);
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
            throw new ArgumentOutOfRangeException(nameof(position), "The drop position must be finite.");
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
}
