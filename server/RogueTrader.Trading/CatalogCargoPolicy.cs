using RogueTrader.Persistence;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>
/// Production <see cref="ICargoPolicy"/>: the proposed inventory must fit the simulation's <see cref="CargoCatalog"/>
/// (known items, no negative quantities, total weight within capacity). Unknown items are refused, never ignored.
/// </summary>
public sealed class CatalogCargoPolicy(CargoCatalog catalog) : ICargoPolicy
{
    private readonly CargoCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public bool Fits(PlayerProfile current, IReadOnlyDictionary<string, int> proposedInventory) =>
        _catalog.Fits(proposedInventory);
}
