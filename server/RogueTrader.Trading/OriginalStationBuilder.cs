using System.Globalization;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>
/// Derives the testbed Trading Outpost from the pinned original files (queue M8 follow-up). Taken from the originals: the
/// station name (VEH id 404), the seven resources with their raw ITM mass, the four trade recipes and their credit values
/// (RT-F045), the three repair rows with their raw amounts (RT-F047), and the upgrade/armor rows with name, raw buy price,
/// family, held category f[20] and raw modifier block f[42..44] (RT-F041/F048). Everything else is NEW/provisional and is
/// listed in <see cref="StationDefinition.NewValues"/>: placement, dock radius, starting wallet, hold capacity, repair prices,
/// collectible piles, credits per unit, and every upgrade effect.
/// </summary>
public static class OriginalStationBuilder
{
    public const string StationId = "trading_outpost";

    // ---- NEW/provisional values (no original source) -----------------------------------------------------------------
    private const float StationX = 90, StationY = 20, DockRadius = 150;
    /// <summary>CFG [Event] FirstTimeInvSetup runs setcash=0; RPG f[38]=3499 (upstream name CashAdjustment) is the unproven alternative.</summary>
    private const long StartingWallet = 0;
    /// <summary>Raw ITM mass units: 20 Wreckage (200,000 each).</summary>
    private const long CargoCapacity = 4_000_000;
    /// <summary>Repair rows have raw buy 0 (no dock cost); price = 10 credits per raw amount point.</summary>
    private const long RepairPricePerPoint = 10;
    /// <summary>Raw modifier f[42..44] read as thousandths of forward thrust / maximum speed / rotation speed.</summary>
    private const float ModifierScale = 1000f;
    /// <summary>Armor protection grades are undecoded; hull bonus = raw buy price / 200 hull points.</summary>
    private const float ArmorPricePerHullPoint = 200f;

    private static readonly string[] NewValueNotes =
    [
        "Position (90, 20), dock radius 150 world units.",
        "Starting wallet 0 (follows CFG FirstTimeInvSetup setcash=0; RPG f[38]=3499 unproven alternative).",
        "Hold capacity 4,000,000 raw ITM mass units.",
        "Trading Outpost pays the credit item's raw f[31] value per input unit; recipe quantities and f+4 undecoded.",
        "Repair raw f[33] amount read as hull points; price 10 credits per point (original buy is 0).",
        "Upgrade effects: raw f[42..44] as thousandths of forward thrust / maximum speed / rotation; maneuver thrusters +5/10/15% rotation, capacitors +25/50/75% maximum energy, chargers and Moltor core +25/50/75% regeneration by tier; armor adds buy/200 hull points; HyperDrive abilities have no effect yet. Only the highest-priced owned item per held category f[20] is active.",
        "Collectible piles: placement and quantities.",
        "Refinery: placed at LIO row 13 'RefineryA' (actor -400, map 10752, 624) drawn into the practice sector at the mineral-field scale (see data/minerals); dock radius 150 world units; each recipe turns one input unit into one output unit for free (RT-F044 quantities, costs and f+4 undecoded).",
    ];

    public const string RefineryId = "refinery";
    /// <summary>Actor id that LIO type 5 rows 13 and 14 place (VEH id 400 is the Refinery).</summary>
    public const int RefineryActor = -OriginalRefinery.VehicleId;
    private const float RefineryDockRadius = 150;

    public static StationDefinition Build(string assetsRoot)
    {
        var items = OriginalItemCatalog.Load(assetsRoot);
        var veh = RawRecordReader.Parse(AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh), PinnedAssets.Veh.RelativePath);
        var lio = RawRecordReader.Parse(AssetIntegrity.ReadVerified(assetsRoot, OriginalSafeZones.LioRogue), OriginalSafeZones.LioRogue.RelativePath);
        return Build(items, OriginalTradingPost.FromRecords(veh, items), StationName(veh), Refinery(veh, lio, items));
    }

    /// <summary>The refinery: name and recipes from VEH id 400, position from the first LIO type 5 row that places actor -400.</summary>
    private static RefineryDefinition Refinery(IReadOnlyList<RawRecord> veh, IReadOnlyList<RawRecord> lio, OriginalItemCatalog items)
    {
        var row = lio.First(r => r.FieldCount == OriginalMineralSpawns.FieldCount && r.Value(0) == "5" && r.Int(OriginalMineralSpawns.ItemField).Value == RefineryActor);
        var name = veh.Single(v => v.FieldCount > 3 && v.Value(2) == OriginalRefinery.VehicleId.ToString(CultureInfo.InvariantCulture)).Value(3);
        return new RefineryDefinition(RefineryId, string.IsNullOrWhiteSpace(name) ? "Refinery" : name,
            (row.Int(4).Value - OriginalMineralFieldBuilder.MapCentre) * OriginalMineralFieldBuilder.MapScale,
            (row.Int(5).Value - OriginalMineralFieldBuilder.MapCentre) * OriginalMineralFieldBuilder.MapScale, RefineryDockRadius)
        {
            Recipes = OriginalRefinery.FromRecords(veh, items)
                .Select(r => new StationRefineRecipe(Slug(r.Name), r.Name, Key(r.InputItemId.Value), Key(r.OutputItemId.Value), r.RawField4.Value)).ToList(),
        };
    }

    public static StationDefinition Build(OriginalItemCatalog items, IReadOnlyList<OriginalTradeRecipe> recipes, string stationName,
        RefineryDefinition? refinery = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(recipes);
        var goods = items.Resources.Select(i => new StationGood(Key(i.Id), i.Name, i.Mass.Value,
            i.Buy.Value > 0 ? i.Buy.Value : null, i.Sell.Value > 0 ? i.Sell.Value : null)).ToList();
        var trades = recipes.Select(r => new StationTrade(
            Slug(r.Name), r.Name, Key(r.InputItemId.Value), Key(r.CreditItemId.Value), r.CreditItem.Name,
            r.CreditItem.Record.Int(31).Value, r.RawField4.Value)).ToList();
        var repairs = OriginalEffectAmountRow.All(items).Select(r => new StationRepair(Key(r.Item.Id), r.Item.Name,
            r.Amount.Value * RepairPricePerPoint, r.Amount.Value, $"ITM id {r.Item.Id} f[33]={r.Amount.Value}")).ToList();
        var upgrades = OriginalUpgrade.All(items).Select(u => Upgrade(u.Item, u.Family))
            .Concat(OriginalArmor.All(items).Select(a => Upgrade(a.Item, OriginalArmor.ArmorCategory)))
            .ToList();
        return new StationDefinition
        {
            StationId = StationId,
            Name = stationName,
            EvidenceClassification = "ORIGINAL+NEW",
            Source = $"Derived by OriginalStationBuilder from pinned {PinnedAssets.Itm.RelativePath} and {PinnedAssets.Veh.RelativePath} (VEH id {OriginalTradingPost.VehicleId}; refinery VEH id {OriginalRefinery.VehicleId} at {OriginalSafeZones.LioRogue.RelativePath} actor {RefineryActor}); regenerate, never hand-edit.",
            NewValues = NewValueNotes,
            X = StationX,
            Y = StationY,
            DockRadius = DockRadius,
            StartingWallet = StartingWallet,
            CargoCapacity = CargoCapacity,
            Goods = goods,
            Trades = trades,
            Repairs = repairs,
            Upgrades = upgrades,
            Collectibles =
            [
                new CollectibleSpawn("pile_wreckage_launch", Key(2008), 9, 0, -70, 14),
                new CollectibleSpawn("pile_alloy_west", Key(2020), 12, -170, 20, 14),
                new CollectibleSpawn("pile_tellerium_south", Key(2004), 6, 40, 160, 14),
            ],
            Refinery = refinery,
        };
    }

    private static StationUpgrade Upgrade(OriginalItem item, string family)
    {
        var modifiers = item.RecordType == OriginalArmor.RecordType
            ? item.Record.Ints(OriginalArmor.ModifierA, 3).Select(m => m.Value).ToArray()
            : [];
        var (effect, basis) = EffectOf(item, family, modifiers);
        return new StationUpgrade(Key(item.Id), item.Name, item.Buy.Value)
        {
            Family = family,
            HeldCategory = item.Record.Int(20).Value,
            RawModifiers = modifiers,
            Effect = effect,
            EffectBasis = basis,
        };
    }

    private static (UpgradeEffect, string) EffectOf(OriginalItem item, string family, int[] raw)
    {
        var fromBlock = raw.Length == 3
            ? new UpgradeEffect { ForwardThrust = raw[0] / ModifierScale, MaximumSpeed = raw[1] / ModifierScale, RotationSpeed = raw[2] / ModifierScale }
            : UpgradeEffect.None;
        var tier = item.Name.StartsWith("Turbo", StringComparison.Ordinal) ? 1
            : item.Name.StartsWith("Super", StringComparison.Ordinal) ? 2
            : 3; // Hyper, Advanced, Bio
        const string blockBasis = "NEW: raw f[42..44] / 1000 as forward thrust / maximum speed / rotation fractions";
        switch (family)
        {
            case OriginalArmor.ArmorCategory:
                return (fromBlock with { MaximumHull = item.Buy.Value / ArmorPricePerHullPoint },
                    blockBasis + "; NEW: + buy/200 hull points (protection grades undecoded)");
            case "Thruster Mods" when raw.All(v => v == 0):
                return (fromBlock with { RotationSpeed = 0.05f * tier }, "NEW tier rule: +5/10/15% rotation (raw block is zero)");
            case "Power Mods":
                var regen = item.Name.Contains("charger", StringComparison.OrdinalIgnoreCase) || item.Name.Contains("Moltor", StringComparison.Ordinal);
                return regen
                    ? (fromBlock with { EnergyRegeneration = 0.25f * tier }, "NEW tier rule: +25/50/75% energy regeneration (raw block is zero)")
                    : (fromBlock with { MaximumEnergy = 0.25f * tier }, "NEW tier rule: +25/50/75% maximum energy (raw block is zero)");
            case "HyperDrive Systems":
                return (UpgradeEffect.None, "None yet: hyperdrive abilities are not implemented");
            default:
                return (fromBlock, blockBasis);
        }
    }

    private static string StationName(IReadOnlyList<RawRecord> veh)
    {
        var row = veh.Single(v => v.FieldCount > 3 && v.Value(2) == OriginalTradingPost.VehicleId.ToString(CultureInfo.InvariantCulture));
        var name = row.Value(3);
        return string.IsNullOrWhiteSpace(name) ? "Trading Outpost" : name;
    }

    private static string Key(int id) => id.ToString(CultureInfo.InvariantCulture);

    private static string Slug(string name) => string.Concat(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_'));
}
