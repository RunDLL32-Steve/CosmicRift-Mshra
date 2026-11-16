using System.Text.Json;

namespace RogueTrader.Simulation;

/// <param name="Id">Opaque ordinal item id (shared with the persistence inventory keys).</param>
/// <param name="UnitWeight">Weight of one unit in raw, unscaled weight units; zero is allowed.</param>
public sealed record CargoItemDefinition(string Id, long UnitWeight)
{
    /// <summary>Most units one hold may carry (for example a mine's original carry limit); null means only weight limits it.</summary>
    public int? MaxQuantity { get; init; }

    /// <summary>
    /// True for a bought item that stays with the pilot when the ship is destroyed (Steve, Nov 3 2026: "all purchased items remain"),
    /// such as space mines; false for cargo, which is lost with the ship.
    /// </summary>
    public bool KeptOnDestruction { get; init; }
}

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
    private readonly Dictionary<string, int> _limits = new(StringComparer.Ordinal);
    private readonly HashSet<string> _kept = new(StringComparer.Ordinal);

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
            if (item.MaxQuantity is < 0)
                throw new ArgumentException($"Cargo item '{item.Id}' has a negative carry limit.", nameof(items));
            if (item.MaxQuantity is { } max) _limits[item.Id] = max;
            if (item.KeptOnDestruction) _kept.Add(item.Id);
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

    /// <summary>The most units of the item one hold may carry, or null when only weight limits it.</summary>
    public int? MaxQuantity(string itemId) => _limits.TryGetValue(itemId, out var max) ? max : null;

    /// <summary>True when the item stays with the pilot when the ship is destroyed (see <see cref="CargoItemDefinition.KeptOnDestruction"/>).</summary>
    public bool KeptOnDestruction(string itemId) => _kept.Contains(itemId);

    /// <summary>True when no item is over its carry limit.</summary>
    private bool WithinLimits(IEnumerable<KeyValuePair<string, int>> inventory) =>
        inventory.All(p => !_limits.TryGetValue(p.Key, out var max) || p.Value <= max);

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
            return WeightOf(inventory) <= Capacity && WithinLimits(inventory);
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

    /// <summary>
    /// Whether a change from <paramref name="current"/> to <paramref name="proposed"/> is allowed: every proposed item is known with
    /// no negative quantity, and the proposed weight is within capacity or no heavier than the current hold. The second clause keeps a
    /// hold that is already over capacity (a save made under an older, larger cap) usable: it may refine to lighter goods or otherwise
    /// shed weight, but nothing may make it heavier. A current inventory that cannot be weighed falls back to <see cref="Fits"/>.
    /// </summary>
    public bool AllowsChange(IEnumerable<KeyValuePair<string, int>> current, IReadOnlyDictionary<string, int> proposed)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(proposed);
        long after;
        try
        {
            after = WeightOf(proposed);
        }
        catch (Exception e) when (e is ArgumentException or OverflowException)
        {
            return false;
        }
        // A carry limit works like the weight cap: a hold already over it (data changed since the save) may only shrink there.
        foreach (var (id, quantity) in proposed)
            if (_limits.TryGetValue(id, out var max) && quantity > max && quantity > current.Where(p => p.Key == id).Sum(p => p.Value))
                return false;
        if (after <= Capacity) return true;
        try
        {
            return after <= WeightOf(current);
        }
        catch (Exception e) when (e is ArgumentException or OverflowException)
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
    /// <summary>The cargo rules this hold follows.</summary>
    public CargoCatalog Catalog => _catalog;
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

    /// <summary>
    /// Replaces the contents with an authoritative inventory (for example the persisted profile after a station sale or a
    /// reload). Same rules as the constructor: unknown items or negative quantities throw and leave the hold unchanged.
    /// </summary>
    public void ReplaceContents(IEnumerable<KeyValuePair<string, int>> inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var list = inventory.ToArray();
        var weight = _catalog.WeightOf(list);
        _items.Clear();
        foreach (var (id, quantity) in list.Where(p => p.Value != 0))
            _items[id] = _items.GetValueOrDefault(id) + quantity;
        TotalWeight = weight;
    }

    /// <summary>The most units of the item that can be added now (capacity, the item's carry limit and int range).</summary>
    public int MaxAccept(string itemId)
    {
        var weight = _catalog.UnitWeight(itemId);
        var byRange = int.MaxValue - Quantity(itemId);
        if (_catalog.MaxQuantity(itemId) is { } max) byRange = Math.Min(byRange, Math.Max(0, max - Quantity(itemId)));
        if (weight == 0)
            return byRange;
        return (int)Math.Min(byRange, FreeWeight / weight);
    }

    /// <summary>Takes units out of the hold (for example a mine laid in space). Returns false, changing nothing, when it holds fewer.</summary>
    public bool TryRemove(string itemId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        var held = Quantity(itemId);
        if (held < quantity) return false;
        if (held == quantity) _items.Remove(itemId);
        else _items[itemId] = held - quantity;
        TotalWeight -= _catalog.UnitWeight(itemId) * quantity;
        return true;
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
