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

/// <summary>The services a site offers (Steve, November 4, 2026: goods sell at the Trading Posts, ships repair at the Repair Docks).</summary>
public static class SiteServices
{
    /// <summary>A home station's store zone: upgrades, hulls and the hangar where the pilot changes ships.</summary>
    public const string Store = "store";
    /// <summary>A Trading Post: the Trading Outpost recipes and goods.</summary>
    public const string Trade = "trade";
    /// <summary>A Repair Dock: the repair rows.</summary>
    public const string Repair = "repair";
    /// <summary>A refinery: the refinery recipes.</summary>
    public const string Refine = "refine";

    public static bool IsKnown(string? service) => service is Store or Trade or Repair or Refine;
}

/// <summary>
/// One more place the ship can dock at besides the main station (an original Trading Post, Repair Dock or team home station),
/// with the one service it offers. <see cref="LioRow"/> names the original LIO row it stands on (0 when hand-authored).
/// </summary>
public sealed record ServiceSite(string StationId, string Name, string Service, float X, float Y, float DockRadius)
{
    public int LioRow { get; init; }
    [JsonIgnore] public StationSite Site => new(StationId, X, Y, DockRadius);
}

/// <summary>
/// A repair service. <see cref="HullPoints"/> is restored on purchase, or the whole hull when <see cref="RestoresFullHull"/>
/// is set; refused at full hull.
/// </summary>
public sealed record StationRepair(string Id, string Name, long Price, float HullPoints, string? Source = null)
{
    /// <summary>Restores the flown ship to its maximum hull whatever that is (Full Repair, Steve's 500-credit repair, Nov 4 2026).</summary>
    public bool RestoresFullHull { get; init; }
}

/// <summary>
/// A hull the station sells (schema 6 owned ships). A player owns at most one of each; <see cref="Source"/> says where the
/// price comes from. <see cref="HullId"/> must name a ship definition the client can fly.
/// </summary>
public sealed record StationShip(string HullId, string Name, long Price, string? Source = null)
{
    /// <summary>The buyer must already own one of these hulls (the original same-race exchange rule); null means no requirement.</summary>
    public IReadOnlyList<string>? RequiresOneOf { get; init; }
}

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
    /// <summary>Protection against each hull damage type (armour, RT-F041); null for an upgrade that gives none.</summary>
    public RogueTrader.GameData.ShipArmor? Armor { get; init; }

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
    public IReadOnlyList<StationGood> Goods { get; init; } = [];
    public IReadOnlyList<StationTrade> Trades { get; init; } = [];
    public IReadOnlyList<StationRepair> Repairs { get; init; } = [];
    public IReadOnlyList<StationUpgrade> Upgrades { get; init; } = [];
    /// <summary>Hulls sold here; null (absent from the JSON) sells none.</summary>
    public IReadOnlyList<StationShip>? Ships { get; init; }
    public IReadOnlyList<CollectibleSpawn> Collectibles { get; init; } = [];
    /// <summary>Optional refinery (queue M7 mining slice); null when the data has none.</summary>
    public RefineryDefinition? Refinery { get; init; }
    /// <summary>
    /// The Trading Posts, Repair Docks and other team stores. When there is no Trading Post (or no Repair Dock) the main station
    /// trades (or repairs) itself, as the single testbed station did.
    /// </summary>
    public IReadOnlyList<ServiceSite> Outposts { get; init; } = [];

    [JsonIgnore] public StationSite Site => new(StationId, X, Y, DockRadius);

    /// <summary>The main station, the refinery when present, and every outpost.</summary>
    [JsonIgnore]
    public IReadOnlyList<StationSite> Sites =>
        [Site, .. Refinery is { } refinery ? [refinery.Site] : Array.Empty<StationSite>(), .. Outposts.Select(o => o.Site)];

    /// <summary>Every dockable place with its service: the main station is a store, plus the refinery and the outposts.</summary>
    [JsonIgnore]
    public IReadOnlyList<ServiceSite> ServiceSites =>
    [
        new ServiceSite(StationId, Name, SiteServices.Store, X, Y, DockRadius),
        .. Refinery is { } refinery ? [new ServiceSite(refinery.StationId, refinery.Name, SiteServices.Refine, refinery.X, refinery.Y, refinery.DockRadius)] : Array.Empty<ServiceSite>(),
        .. Outposts,
    ];

    /// <summary>Sites that offer <paramref name="service"/>; trade and repair fall back to the main station when no outpost offers them.</summary>
    public IReadOnlyList<ServiceSite> SitesOffering(string service)
    {
        var sites = ServiceSites.Where(s => s.Service == service).ToArray();
        return sites.Length > 0 || service is not (SiteServices.Trade or SiteServices.Repair)
            ? sites
            : [ServiceSites[0]];
    }

    /// <summary>The site whose dock radius holds the ship, or null. Sites never overlap in the shipped data; the first listed wins.</summary>
    public ServiceSite? DockedAt(float shipX, float shipY) => ServiceSites.FirstOrDefault(s => Docking.IsDocked(s.Site, shipX, shipY));

    /// <summary>
    /// Cargo rules for the world and for the transaction core; both must use the same catalog. The weight cap belongs to the ship
    /// (<c>ShipDefinition.Cargo</c>), the item weights to the station's goods.
    /// </summary>
    public CargoCatalog CreateCargoCatalog(long cargoCapacity) =>
        new(cargoCapacity, Goods.Select(g => new CargoItemDefinition(g.Id, g.UnitWeight)), Collectibles);

    /// <summary>Goods at their own prices; a Trading Outpost recipe is a sell price for its input item.</summary>
    /// <remarks>Goods and recipes are priced at the Trading Posts, repairs at the Repair Docks, upgrades and hulls at the stores.</remarks>
    public StationPriceTable CreatePriceTable()
    {
        string[] Ids(string service) => SitesOffering(service).Select(s => s.StationId).ToArray();
        var stores = Ids(SiteServices.Store);
        return new StationPriceTable(Ids(SiteServices.Trade), Goods.Select(g =>
                (g.Id, g.Buy, Trades.FirstOrDefault(t => t.InputItemId == g.Id) is { } trade ? trade.CreditsPerUnit : g.Sell)))
            .WithRepairPrices(Repairs.Select(r => (r.Id, r.Price)), Ids(SiteServices.Repair))
            .WithUpgradePrices(Upgrades.Select(u => (u.Id, u.Price)), stores)
            .WithShipPrices((Ships ?? []).Select(h => (h.HullId, h.Price)), stores)
            .WithShipRequirements((Ships ?? []).Where(h => h.RequiresOneOf is { Count: > 0 }).Select(h => (h.HullId, (IReadOnlyCollection<string>)h.RequiresOneOf!)), stores)
            .WithRefineRecipes(Refinery?.StationId ?? "", Refinery?.Recipes.Select(r => (r.Id, r.InputItemId, r.OutputItemId)) ?? []);
    }

    public long? SellPrice(string itemId) =>
        Trades.FirstOrDefault(t => t.InputItemId == itemId)?.CreditsPerUnit ?? Goods.FirstOrDefault(g => g.Id == itemId)?.Sell;

    public string NameOf(string itemId) =>
        Goods.FirstOrDefault(g => g.Id == itemId)?.Name
        ?? Upgrades.FirstOrDefault(u => u.Id == itemId)?.Name
        ?? Repairs.FirstOrDefault(r => r.Id == itemId)?.Name
        ?? Refinery?.Recipes.FirstOrDefault(r => r.Id == itemId)?.Name
        ?? Ships?.FirstOrDefault(h => h.HullId == itemId)?.Name
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

    /// <summary>LF line endings on every OS, so the derived file and its oracle test match on Windows too (review R3-2).</summary>
    public string ToJson() => JsonSerializer.Serialize(this, Json).ReplaceLineEndings("\n") + "\n";

    private void Validate()
    {
        if (string.IsNullOrEmpty(StationId)) throw new InvalidDataException("Station definition needs a stationId.");
        if (StartingWallet < 0) throw new InvalidDataException("startingWallet must not be negative.");
        if (Goods.Concat<object>(Repairs).Concat(Upgrades).Concat(Trades).Concat(Ships ?? []).Any(x => x is null)) throw new InvalidDataException("Null station entry.");
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
        foreach (var outpost in Outposts)
        {
            if (outpost is null || string.IsNullOrEmpty(outpost.StationId)) throw new InvalidDataException("An outpost needs a station id.");
            if (!SiteServices.IsKnown(outpost.Service) || outpost.Service == SiteServices.Refine)
                throw new InvalidDataException($"Outpost '{outpost.StationId}' offers an unknown service '{outpost.Service}'.");
        }
        _ = new StationDirectory(Sites); // duplicate ids, positions and radii
        foreach (var upgrade in Upgrades)
            if (upgrade.Effect is null) throw new InvalidDataException($"Upgrade '{upgrade.Id}' has no effect entry.");
        // Constructing the site, catalog and table runs their own checks (radius, weights, duplicates, negative prices).
        _ = Site;
        _ = CreateCargoCatalog(long.MaxValue);
        _ = CreatePriceTable();
    }
}
