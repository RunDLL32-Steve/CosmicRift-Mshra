using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>
/// One cargo item the station knows. <see cref="UnitWeight"/> is the raw ITM mass for original items. Buy/sell are whole
/// currency units; null means not offered (original resources have raw buy 0 and sell 0/-1: no shop trades them).
/// </summary>
public sealed record StationGood(string Id, string Name, long UnitWeight, long? Buy, long? Sell);

/// <summary>
/// A Trading Outpost recipe (RT-F045): each unit of <see cref="InputItemId"/> handed in pays <see cref="CreditsPerUnit"/>.
/// The input, credit item and raw value are original; paying the raw f[31] value per unit is a NEW/provisional reading
/// (recipe quantities, f+4 and the award flow are undecoded).
/// </summary>
public sealed record StationTrade(string Id, string Name, string InputItemId, string CreditItemId, string CreditName, long CreditsPerUnit, int RawField4);

/// <summary>
/// A public refinery recipe (RT-F044): <see cref="InputItemId"/> becomes <see cref="OutputItemId"/>. Name and both items are
/// original (VEH id 400); converting one unit into one unit for free is a NEW/provisional reading (f+4 and quantities undecoded).
/// </summary>
public sealed record StationRefineRecipe(string Id, string Name, string InputItemId, string OutputItemId, int RawField4);

/// <summary>A refinery the ship docks at separately from the station (original refineries are their own sites).</summary>
public sealed record RefineryDefinition(string StationId, string Name, float X, float Y, float DockRadius)
{
    public IReadOnlyList<StationRefineRecipe> Recipes { get; init; } = [];
    [JsonIgnore] public StationSite Site => new(StationId, X, Y, DockRadius);
}

/// <summary>A repair service. <see cref="HullPoints"/> is restored on purchase; refused at full hull.</summary>
public sealed record StationRepair(string Id, string Name, long Price, float HullPoints, string? Source = null);

/// <summary>
/// Provisional effect of one upgrade, as fractions of the base ship value (0.1 = +10%) except <see cref="MaximumHull"/>
/// (hull points). See <see cref="StationUpgrade.EffectBasis"/> for where each number comes from.
/// </summary>
public sealed record UpgradeEffect
{
    public float ForwardThrust { get; init; }
    public float MaximumSpeed { get; init; }
    public float RotationSpeed { get; init; }
    public float MaximumEnergy { get; init; }
    public float EnergyRegeneration { get; init; }
    public float MaximumHull { get; init; }

    public static UpgradeEffect None { get; } = new();
}

/// <summary>
/// An upgrade module offered at the station. Name, price, family, held category and raw modifier block are original for
/// original rows; <see cref="Effect"/> is always NEW/provisional (RT-F048 effects are undecoded) and
/// <see cref="EffectBasis"/> says how it was derived.
/// </summary>
public sealed record StationUpgrade(string Id, string Name, long Price)
{
    public string Family { get; init; } = "";
    /// <summary>Raw ITM f[20]; upgrades sharing one are treated as one slot (only the best-priced owned one is active).</summary>
    public int HeldCategory { get; init; }
    public IReadOnlyList<int> RawModifiers { get; init; } = [];
    public UpgradeEffect Effect { get; init; } = UpgradeEffect.None;
    public string EffectBasis { get; init; } = "";
}

/// <summary>
/// Data-driven station offer for the testbed client: site, starting wallet, cargo rules, goods, Trading Outpost recipes,
/// repairs and upgrades. <c>data/stations/trading_outpost.json</c> is derived from the pinned original files by
/// <see cref="OriginalStationBuilder"/>; every value it does not take from them is listed in <see cref="NewValues"/>.
/// </summary>
public sealed class StationDefinition
{
    public required string StationId { get; init; }
    public required string Name { get; init; }
    public string EvidenceClassification { get; init; } = "NEW";
    public string Source { get; init; } = "";
    public IReadOnlyList<string> NewValues { get; init; } = [];
    public float X { get; init; }
    public float Y { get; init; }
    public float DockRadius { get; init; }
    public long StartingWallet { get; init; }
    public long CargoCapacity { get; init; }
    public IReadOnlyList<StationGood> Goods { get; init; } = [];
    public IReadOnlyList<StationTrade> Trades { get; init; } = [];
    public IReadOnlyList<StationRepair> Repairs { get; init; } = [];
    public IReadOnlyList<StationUpgrade> Upgrades { get; init; } = [];
    public IReadOnlyList<CollectibleSpawn> Collectibles { get; init; } = [];
    /// <summary>Optional refinery (queue M7 mining slice); null when the data has none.</summary>
    public RefineryDefinition? Refinery { get; init; }

    [JsonIgnore] public StationSite Site => new(StationId, X, Y, DockRadius);

    /// <summary>The station and, when present, the refinery.</summary>
    [JsonIgnore] public IReadOnlyList<StationSite> Sites => Refinery is { } refinery ? [Site, refinery.Site] : [Site];

    /// <summary>Cargo rules for the world and for the transaction core; both must use the same catalog.</summary>
    public CargoCatalog CreateCargoCatalog() =>
        new(CargoCapacity, Goods.Select(g => new CargoItemDefinition(g.Id, g.UnitWeight)), Collectibles);

    /// <summary>Goods at their own prices; a Trading Outpost recipe is a sell price for its input item.</summary>
    public StationPriceTable CreatePriceTable() =>
        new StationPriceTable(StationId, Goods.Select(g =>
                (g.Id, g.Buy, Trades.FirstOrDefault(t => t.InputItemId == g.Id) is { } trade ? trade.CreditsPerUnit : g.Sell)))
            .WithRepairPrices(Repairs.Select(r => (r.Id, r.Price)))
            .WithUpgradePrices(Upgrades.Select(u => (u.Id, u.Price)))
            .WithRefineRecipes(Refinery?.StationId ?? "", Refinery?.Recipes.Select(r => (r.Id, r.InputItemId, r.OutputItemId)) ?? []);

    public long? SellPrice(string itemId) =>
        Trades.FirstOrDefault(t => t.InputItemId == itemId)?.CreditsPerUnit ?? Goods.FirstOrDefault(g => g.Id == itemId)?.Sell;

    public string NameOf(string itemId) =>
        Goods.FirstOrDefault(g => g.Id == itemId)?.Name
        ?? Upgrades.FirstOrDefault(u => u.Id == itemId)?.Name
        ?? Repairs.FirstOrDefault(r => r.Id == itemId)?.Name
        ?? Refinery?.Recipes.FirstOrDefault(r => r.Id == itemId)?.Name
        ?? itemId;

    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static StationDefinition FromJson(string json)
    {
        var definition = JsonSerializer.Deserialize<StationDefinition>(json, Json)
            ?? throw new InvalidDataException("Station definition is empty.");
        definition.Validate();
        return definition;
    }

    public string ToJson() => JsonSerializer.Serialize(this, Json) + "\n";

    private void Validate()
    {
        if (string.IsNullOrEmpty(StationId)) throw new InvalidDataException("Station definition needs a stationId.");
        if (StartingWallet < 0) throw new InvalidDataException("startingWallet must not be negative.");
        if (Goods.Concat<object>(Repairs).Concat(Upgrades).Concat(Trades).Any(x => x is null)) throw new InvalidDataException("Null station entry.");
        foreach (var repair in Repairs)
            if (!float.IsFinite(repair.HullPoints) || repair.HullPoints <= 0)
                throw new InvalidDataException($"Repair '{repair.Id}' hullPoints must be positive.");
        foreach (var trade in Trades)
        {
            if (Goods.All(g => g.Id != trade.InputItemId)) throw new InvalidDataException($"Trade '{trade.Id}' input '{trade.InputItemId}' is not a station good.");
            if (trade.CreditsPerUnit <= 0) throw new InvalidDataException($"Trade '{trade.Id}' must pay a positive amount.");
        }
        if (Trades.GroupBy(t => t.InputItemId).Any(g => g.Count() > 1)) throw new InvalidDataException("Two trades share one input item.");
        if (Refinery is { } refinery)
        {
            if (refinery.StationId == StationId) throw new InvalidDataException("The refinery needs its own station id.");
            foreach (var recipe in refinery.Recipes)
            {
                if (recipe is null) throw new InvalidDataException("Null refinery recipe.");
                if (Goods.All(g => g.Id != recipe.InputItemId) || Goods.All(g => g.Id != recipe.OutputItemId) || recipe.InputItemId == recipe.OutputItemId)
                    throw new InvalidDataException($"Refinery recipe '{recipe.Id}' must turn one station good into another.");
            }
            _ = refinery.Site;
        }
        foreach (var upgrade in Upgrades)
            if (upgrade.Effect is null) throw new InvalidDataException($"Upgrade '{upgrade.Id}' has no effect entry.");
        // Constructing the site, catalog and table runs their own checks (radius, weights, duplicates, negative prices).
        _ = Site;
        _ = CreateCargoCatalog();
        _ = CreatePriceTable();
    }
}
