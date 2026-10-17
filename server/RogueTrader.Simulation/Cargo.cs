using System.Text.Json;

namespace RogueTrader.Simulation;

/// <param name="Id">Opaque ordinal item id (shared with the persistence inventory keys).</param>
/// <param name="UnitWeight">Weight of one unit in raw, unscaled weight units; zero is allowed.</param>
public sealed record CargoItemDefinition(string Id, long UnitWeight);

/// <summary>A pile of one item lying in space. Position is in world units.</summary>
public sealed record CollectibleSpawn(string Id, string ItemId, int Quantity, float X, float Y, float Radius);

/// <summary>
/// Data-driven cargo rules: one hard weight limit and per-item unit weights, plus the collectible piles of a sector.
/// The native per-hull threshold/overload formula (RT-F030) is unresolved, so this models only the plan section 8.4 rule
/// that a ship cannot hold more than its capacity. Capacity and weights are supplied by data (original values or NEW fixtures).
/// </summary>
public sealed class CargoCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    private readonly Dictionary<string, long> _weights = new(StringComparer.Ordinal);

    public long Capacity { get; }
    public IReadOnlyList<CargoItemDefinition> Items { get; }
    public IReadOnlyList<CollectibleSpawn> Collectibles { get; }

    public CargoCatalog(long capacity, IEnumerable<CargoItemDefinition> items, IEnumerable<CollectibleSpawn>? collectibles = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (capacity < 0)
            throw new ArgumentException("Cargo capacity must not be negative.", nameof(capacity));
        Capacity = capacity;
        Items = Array.AsReadOnly(items.ToArray());
        foreach (var item in Items)
        {
            if (item is null || string.IsNullOrEmpty(item.Id))
                throw new ArgumentException("Cargo item needs an id.", nameof(items));
            if (item.UnitWeight < 0)
                throw new ArgumentException($"Cargo item '{item.Id}' has a negative weight.", nameof(items));
            if (!_weights.TryAdd(item.Id, item.UnitWeight))
                throw new ArgumentException($"Duplicate cargo item '{item.Id}'.", nameof(items));
        }
        Collectibles = Array.AsReadOnly((collectibles ?? []).ToArray());
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var spawn in Collectibles)
        {
            if (spawn is null || string.IsNullOrEmpty(spawn.Id) || !ids.Add(spawn.Id))
                throw new ArgumentException("Collectible needs a unique id.", nameof(collectibles));
            if (!_weights.ContainsKey(spawn.ItemId ?? ""))
                throw new ArgumentException($"Collectible '{spawn.Id}' names unknown item '{spawn.ItemId}'.", nameof(collectibles));
            if (spawn.Quantity <= 0)
                throw new ArgumentException($"Collectible '{spawn.Id}' needs a positive quantity.", nameof(collectibles));
            if (!float.IsFinite(spawn.X) || !float.IsFinite(spawn.Y) || !float.IsFinite(spawn.Radius) || spawn.Radius <= 0)
                throw new ArgumentException($"Collectible '{spawn.Id}' needs a finite position and positive radius.", nameof(collectibles));
        }
    }

    /// <summary>Document shape: <c>{ "capacity": n, "items": [{ "id", "unitWeight" }], "collectibles": [{ "id", "itemId", "quantity", "x", "y", "radius" }] }</c>.</summary>
    public static CargoCatalog FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<CargoDocument>(json, JsonOptions)
            ?? throw new JsonException("Empty cargo definition.");
        return new CargoCatalog(document.Capacity, document.Items ?? [], document.Collectibles);
    }

    public bool IsKnown(string itemId) => _weights.ContainsKey(itemId);

    public long UnitWeight(string itemId) => _weights.TryGetValue(itemId, out var weight)
        ? weight
        : throw new ArgumentException($"Unknown cargo item '{itemId}'.", nameof(itemId));

    /// <summary>Total weight of an inventory; unknown items or negative quantities throw. Overflow throws.</summary>
    public long WeightOf(IEnumerable<KeyValuePair<string, int>> inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        long total = 0;
        foreach (var (id, quantity) in inventory)
        {
            if (quantity < 0)
                throw new ArgumentException($"Negative quantity for '{id}'.", nameof(inventory));
            total = checked(total + checked(UnitWeight(id) * quantity));
        }
        return total;
    }

    /// <summary>True when every item is known, no quantity is negative and the total weight is within capacity.</summary>
    public bool Fits(IReadOnlyDictionary<string, int> inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        try
        {
            return WeightOf(inventory) <= Capacity;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private sealed class CargoDocument
    {
        public long Capacity { get; set; }
        public List<CargoItemDefinition>? Items { get; set; }
        public List<CollectibleSpawn>? Collectibles { get; set; }
    }
}

public enum CargoPickupOutcome { Full, Partial, Refused }

/// <summary>Result of one add attempt. Accepted + Refused always equals Requested.</summary>
public readonly record struct CargoPickupResult(int Requested, int Accepted)
{
    public int Refused => Requested - Accepted;
    public CargoPickupOutcome Outcome => Accepted == Requested ? CargoPickupOutcome.Full
        : Accepted == 0 ? CargoPickupOutcome.Refused : CargoPickupOutcome.Partial;
}

/// <summary>Server-authoritative ship cargo. Quantities are never negative and total weight never exceeds capacity through pickup.</summary>
public sealed class CargoHold
{
    private readonly CargoCatalog _catalog;
    private readonly SortedDictionary<string, int> _items = new(StringComparer.Ordinal);

    public long Capacity => _catalog.Capacity;
    public long TotalWeight { get; private set; }
    /// <summary>Unused capacity; zero (never negative) when an initial inventory already exceeds it.</summary>
    public long FreeWeight => Math.Max(0, Capacity - TotalWeight);
    /// <summary>Contents in ordinal id order.</summary>
    public IReadOnlyDictionary<string, int> Items => _items;

    /// <param name="initial">Starting contents (for example a persisted inventory); unknown items or negative quantities throw.</param>
    public CargoHold(CargoCatalog catalog, IEnumerable<KeyValuePair<string, int>>? initial = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
        if (initial is null)
            return;
        var list = initial.ToArray();
        TotalWeight = catalog.WeightOf(list);
        foreach (var (id, quantity) in list.Where(p => p.Value != 0))
            _items[id] = _items.GetValueOrDefault(id) + quantity;
    }

    public int Quantity(string itemId) => _items.GetValueOrDefault(itemId);

    /// <summary>The most units of the item that can be added now (capacity and int range).</summary>
    public int MaxAccept(string itemId)
    {
        var weight = _catalog.UnitWeight(itemId);
        var byRange = int.MaxValue - Quantity(itemId);
        if (weight == 0)
            return byRange;
        return (int)Math.Min(byRange, FreeWeight / weight);
    }

    /// <summary>Adds as many units as fit (partial pickup); the rest is refused and nothing is created or lost.</summary>
    public CargoPickupResult Add(string itemId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        var accepted = Math.Min(quantity, MaxAccept(itemId));
        if (accepted > 0)
        {
            _items[itemId] = Quantity(itemId) + accepted;
            TotalWeight += _catalog.UnitWeight(itemId) * accepted;
        }
        return new CargoPickupResult(quantity, accepted);
    }
}
