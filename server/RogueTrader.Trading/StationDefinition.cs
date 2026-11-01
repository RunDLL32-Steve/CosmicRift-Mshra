using System.Text.Json;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>One tradeable cargo item of a station definition. Prices are whole currency units; null means not offered.</summary>
public sealed record StationGood(string Id, string Name, long UnitWeight, long? Buy, long? Sell);

/// <summary>A repair service. <see cref="HullRestoreFraction"/> is the share of maximum hull the client restores (NEW/provisional).</summary>
public sealed record StationRepair(string Id, string Name, long Price, float HullRestoreFraction);

/// <summary>An upgrade module offered at the station. Effects are not modelled; ownership is persisted only.</summary>
public sealed record StationUpgrade(string Id, string Name, long Price);

/// <summary>
/// Data-driven station offer for the testbed client: station site, starting wallet, cargo rules, goods, repairs and upgrades.
/// Everything loaded from <c>data/stations/*.json</c> is NEW/provisional unless the file says otherwise: original station
/// inventories, dock rules, repair amounts and upgrade effects are unresolved (RT-F045/F047/F048/F049).
/// </summary>
public sealed class StationDefinition
{
    public required string StationId { get; init; }
    public required string Name { get; init; }
    public string EvidenceClassification { get; init; } = "NEW";
    public float X { get; init; }
    public float Y { get; init; }
    public float DockRadius { get; init; }
    public long StartingWallet { get; init; }
    public long CargoCapacity { get; init; }
    public IReadOnlyList<StationGood> Goods { get; init; } = [];
    public IReadOnlyList<StationRepair> Repairs { get; init; } = [];
    public IReadOnlyList<StationUpgrade> Upgrades { get; init; } = [];
    public IReadOnlyList<CollectibleSpawn> Collectibles { get; init; } = [];

    public StationSite Site => new(StationId, X, Y, DockRadius);

    /// <summary>Cargo rules for the world and for the transaction core; both must use the same catalog.</summary>
    public CargoCatalog CreateCargoCatalog() =>
        new(CargoCapacity, Goods.Select(g => new CargoItemDefinition(g.Id, g.UnitWeight)), Collectibles);

    public StationPriceTable CreatePriceTable() =>
        new StationPriceTable(StationId, Goods.Select(g => (g.Id, g.Buy, g.Sell)))
            .WithRepairPrices(Repairs.Select(r => (r.Id, r.Price)))
            .WithUpgradePrices(Upgrades.Select(u => (u.Id, u.Price)));

    public string NameOf(string itemId) =>
        Goods.FirstOrDefault(g => g.Id == itemId)?.Name
        ?? Upgrades.FirstOrDefault(u => u.Id == itemId)?.Name
        ?? Repairs.FirstOrDefault(r => r.Id == itemId)?.Name
        ?? itemId;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };

    public static StationDefinition FromJson(string json)
    {
        var definition = JsonSerializer.Deserialize<StationDefinition>(json, Json)
            ?? throw new InvalidDataException("Station definition is empty.");
        definition.Validate();
        return definition;
    }

    private void Validate()
    {
        if (string.IsNullOrEmpty(StationId)) throw new InvalidDataException("Station definition needs a stationId.");
        if (StartingWallet < 0) throw new InvalidDataException("startingWallet must not be negative.");
        if (Goods.Concat<object>(Repairs).Concat(Upgrades).Any(x => x is null)) throw new InvalidDataException("Null station entry.");
        foreach (var repair in Repairs)
            if (!float.IsFinite(repair.HullRestoreFraction) || repair.HullRestoreFraction <= 0 || repair.HullRestoreFraction > 1)
                throw new InvalidDataException($"Repair '{repair.Id}' hullRestoreFraction must be in (0, 1].");
        // Constructing the site, catalog and table runs their own checks (radius, weights, duplicates, negative prices).
        _ = Site;
        _ = CreateCargoCatalog();
        _ = CreatePriceTable();
    }
}
