using RogueTrader.Persistence;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>
/// Production <see cref="ICargoPolicy"/>: the proposed inventory must fit the simulation's <see cref="CargoCatalog"/>
/// (known items, no negative quantities, total weight within capacity). Unknown items are refused, never ignored.
/// A hold already over capacity, such as a save made under the older 4,000,000 cap, may still change as long as it gets no heavier
/// (<see cref="CargoCatalog.AllowsChange"/>), so it can refine ore into lighter goods without selling first.
/// </summary>
public sealed class CatalogCargoPolicy(CargoCatalog catalog) : ICargoPolicy
{
    private readonly CargoCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public bool Fits(PlayerProfile current, IReadOnlyDictionary<string, int> proposedInventory)
    {
        ArgumentNullException.ThrowIfNull(current);
        return _catalog.AllowsChange(current.Inventory, proposedInventory);
    }
}
