using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

public sealed partial class WorldSimulation
{
    private readonly MiningEquipmentDefinition? _miningEquipment;
    internal long NextMineTick { get; private set; }
    internal long NextMiningPileId { get; private set; }
    private readonly HashSet<string> _collectibleIds = new(StringComparer.Ordinal);

    private void ValidateMining(MiningCatalog mining, CargoCatalog cargo)
    {
        var asteroids = Asteroids.ToDictionary(a => a.Id, StringComparer.Ordinal);
        foreach (var deposit in mining.Deposits)
        {
            if (!asteroids.TryGetValue(deposit.AsteroidId, out var asteroid) || !cargo.IsKnown(deposit.ResourceItemId))
                throw new ArgumentException("Mining deposit names an unknown asteroid or cargo item.", nameof(mining));
            asteroid.ResourceItemId = deposit.ResourceItemId;
            asteroid.ResourceQuantity = deposit.Quantity;
        }
        if (mining.Equipment is not { } equipment)
            return;
        if (equipment.CompatibleItemIds.Any(id => !cargo.IsKnown(id)))
            throw new ArgumentException("Mining equipment names an unknown cargo item.", nameof(mining));
        // Sweep uses float squared radii; reject individually finite values whose combined geometry overflows.
        foreach (var asteroid in Asteroids)
        {
            var reach = Player.Radius + asteroid.Radius + equipment.Range;
            var placement = asteroid.Radius + equipment.DropRadius;
            var pickup = Player.Radius + equipment.DropRadius;
            if (!float.IsFinite(reach * reach) || !float.IsFinite(placement * placement)
                || !float.IsFinite(pickup * pickup)
                || !float.IsFinite(MathF.Abs(asteroid.Position.X) + placement)
                || !float.IsFinite(MathF.Abs(asteroid.Position.Y) + placement))
                throw new ArgumentException("Combined mining geometry must remain finite.", nameof(mining));
        }
    }

    private bool TryMine()
    {
        if (_miningEquipment is not { } equipment || TickNumber < NextMineTick)
            return false;
        // NEW/provisional: first compatible, reachable, nonempty asteroid in sector catalog order wins.
        foreach (var asteroid in Asteroids)
        {
            if (asteroid.ResourceQuantity == 0 || !equipment.CompatibleItemIds.Contains(asteroid.ResourceItemId, StringComparer.Ordinal))
                continue;
            var dx = (double)Player.Position.X - asteroid.Position.X;
            var dy = (double)Player.Position.Y - asteroid.Position.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var reach = (double)Player.Radius + asteroid.Radius + equipment.Range;
            if (distance > reach)
                continue;
            // NEW/provisional: drop just outside the asteroid surface toward the ship (+X if coincident).
            var offset = (double)asteroid.Radius + equipment.DropRadius;
            var position = distance == 0 ? asteroid.Position + new Vector2((float)offset, 0)
                : new Vector2((float)(asteroid.Position.X + dx / distance * offset),
                    (float)(asteroid.Position.Y + dy / distance * offset));
            string id;
            do { id = $"mining:{++NextMiningPileId}"; } while (!_collectibleIds.Add(id));
            var quantity = Math.Min(asteroid.ResourceQuantity, equipment.UnitsPerCycle);
            _collectibles.Add(new CollectibleState(new CollectibleSpawn(id, asteroid.ResourceItemId!, quantity,
                position.X, position.Y, equipment.DropRadius)));
            asteroid.ResourceQuantity -= quantity;
            NextMineTick = checked(TickNumber + equipment.CycleTicks);
            return true;
        }
        return false;
    }
}
