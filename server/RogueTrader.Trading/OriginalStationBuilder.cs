using System.Globalization;
using System.Numerics;
using RogueTrader.GameData;
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
    /// <summary>
    /// The station stands at the centre of LIO row 11 'ColdHarbor' (1984, 1744), the Cold Harbor SpaceDock whose [Terrain10]
    /// is the store zone, moved by the LVL offset like every LIO position. The dock radius is NEW.
    /// </summary>
    private const int ColdHarborLioX = 1984, ColdHarborLioY = 1744;
    private static readonly float StationX = OriginalMapBuilder.World(ColdHarborLioX), StationY = OriginalMapBuilder.World(ColdHarborLioY);
    private const float DockRadius = 150;
    /// <summary>CFG [Event] FirstTimeInvSetup runs setcash=0; RPG f[38]=3499 (upstream name CashAdjustment) is the unproven alternative.</summary>
    private const long StartingWallet = 0;
    /// <summary>Repair rows have raw buy 0 (no dock cost); price = 10 credits per raw amount point.</summary>
    private const long RepairPricePerPoint = 10;
    /// <summary>The original repair that fills the hull (ITM id 43, raw amount 50 = every original hull's points).</summary>
    private const string FullRepairName = "Full Repair";
    /// <summary>Raw modifier f[42..44] read as thousandths of forward thrust / maximum speed / rotation speed.</summary>
    private const float ModifierScale = 1000f;

    private static readonly string[] NewValueNotes =
    [
        "Position: the main station is the Cold Harbor SpaceDock (LIO row 11, map 1936, 1696), whose terrain 10 is the original store zone; it sells upgrades and hulls and holds the hangar. Every other team's home store stands at the open store-zone (terrain 10) point nearest the centre of its LIO station-area row. Dock radius 150 map pixels everywhere.",
        "Services (Steve, November 4, 2026): goods and the Trading Outpost recipes trade only at the Trading Posts (LIO type 5 rows placing actor -404, VEH 404 Trading Outpost, terrain 15), and repairs are sold only at the Repair Docks (actor -407, VEH 407 Repair Dock, terrain 9). Upgrades and hulls sell at every team's home store (NEW: whether the original restricted a store to its team is unknown).",
        "Starting wallet 0 (follows CFG FirstTimeInvSetup setcash=0; RPG f[38]=3499 unproven alternative).",
        "Trading Outpost pays the credit item's raw f[31] value per input unit; recipe quantities and f+4 undecoded.",
        "Repair raw f[33] amount read as hull points; price 10 credits per point (original buy is 0). Full Repair (500 credits, Steve's price) restores the whole hull.",
        "Upgrade effects: raw f[42..44] as thousandths of forward thrust / maximum speed / rotation; maneuver thrusters +5/10/15% rotation, capacitors +25/50/75% maximum energy, chargers and Moltor core +25/50/75% regeneration by tier; armor protects by damage type from ITM f[52..57] (Ignore in thousandths of a hull point, Percent read as per mille) and its f[42..44] penalty applies like the other blocks; HyperDrive abilities have no effect yet. Only the highest-priced owned item per held category f[20] is active.",
        "Collectible piles: placement (offsets from the station) and quantities.",
        "Refinery: placed at LIO row 13 'RefineryA' (actor -400, LIO 10752, 624; map 10704, 576 after the LVL offset); the second refinery (row 14) is not placed yet; dock radius 150 map pixels; each recipe turns one input unit into one output unit for free (RT-F044 quantities, costs and f+4 undecoded).",
    ];

    public const string RefineryId = "refinery";
    /// <summary>Actor id that LIO type 5 rows 13 and 14 place (VEH id 400 is the Refinery).</summary>
    public const int RefineryActor = -OriginalRefinery.VehicleId;
    private const float RefineryDockRadius = 150;

    public const int TradingPostActor = -OriginalTradingPost.VehicleId;
    /// <summary>VEH id 407 is the Repair Dock; LIO type 5 rows place it as actor -407.</summary>
    public const int RepairDockVehicleId = 407;
    public const int RepairDockActor = -RepairDockVehicleId;
    private const int AreaType = 8;

    /// <summary>
    /// The station id of a team's home store: the main station for team 0 (kept as <see cref="StationId"/> so saved ledgers stay
    /// valid), <c>store_&lt;name&gt;</c> for the others.
    /// </summary>
    public static string StoreIdOf(int team, string teamName) => team == OriginalMapBuilder.PilotTeam ? StationId : $"store_{Slug(teamName)}";

    /// <param name="map">The derived map (<c>data/sectors/rogue_map.json</c>); stores move to its nearest open point.</param>
    /// <param name="teams">The sector's teams (<c>data/sectors/rogue.json</c>); each one gets its home store.</param>
    public static StationDefinition Build(string assetsRoot, SectorMap map, IReadOnlyList<TeamDefinition> teams)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(teams);
        var items = OriginalItemCatalog.Load(assetsRoot);
        var veh = RawRecordReader.Parse(AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh), PinnedAssets.Veh.RelativePath);
        var lio = RawRecordReader.Parse(AssetIntegrity.ReadVerified(assetsRoot, OriginalSafeZones.LioRogue), OriginalSafeZones.LioRogue.RelativePath);
        var home = teams.FirstOrDefault(t => t.Index == OriginalMapBuilder.PilotTeam)?.Name;
        return Build(items, OriginalTradingPost.FromRecords(veh, items), home ?? StationName(veh), Refinery(veh, lio, items),
            OriginalShipBuilder.StationShips(OriginalHullCatalog.Load(assetsRoot), items), Outposts(veh, lio, map, teams));
    }

    /// <summary>
    /// The Trading Posts and Repair Docks in LIO order (named after their VEH row and the letter ending the LIO name), then the
    /// home store of every team after the first, at the open point nearest the centre of the LIO station-area row (type 8) whose
    /// name, without spaces, starts with the team's name.
    /// </summary>
    private static IReadOnlyList<ServiceSite> Outposts(IReadOnlyList<RawRecord> veh, IReadOnlyList<RawRecord> lio, SectorMap map, IReadOnlyList<TeamDefinition> teams)
    {
        var sites = new List<ServiceSite>();
        foreach (var (actor, service, vehicle, prefix) in new[]
                 {
                     (TradingPostActor, SiteServices.Trade, OriginalTradingPost.VehicleId, "trading_post"),
                     (RepairDockActor, SiteServices.Repair, RepairDockVehicleId, "repair_dock"),
                 })
        {
            var name = VehName(veh, vehicle);
            foreach (var row in lio.Where(r => r.FieldCount == OriginalMineralSpawns.FieldCount && r.Value(0) == "5" && r.Int(OriginalMineralSpawns.ItemField).Value == actor))
            {
                var letter = row.Value(3).Trim('"')[^1..].ToUpperInvariant();
                var at = new Vector2(OriginalMapBuilder.World(row.Int(4).Value), OriginalMapBuilder.World(row.Int(5).Value));
                if (map.IsBlocked(at)) throw new OriginalDataException($"LIO row {row.RowNumber} {name} stands on a blocked tile.");
                sites.Add(new ServiceSite($"{prefix}_{letter.ToLowerInvariant()}", $"{name} {letter}", service, at.X, at.Y, DockRadius) { LioRow = row.RowNumber });
            }
        }
        static string Squash(string text) => string.Concat(text.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        foreach (var team in teams.Where(t => t.Index != OriginalMapBuilder.PilotTeam))
        {
            var area = lio.FirstOrDefault(r => r.FieldCount > 7 && r.Value(0) == AreaType.ToString(CultureInfo.InvariantCulture)
                && Squash(r.Value(3).Trim('"')).StartsWith(Squash(team.Name), StringComparison.Ordinal))
                ?? throw new OriginalDataException($"No LIO station area is named after team {team.Index} '{team.Name}'.");
            var centre = new Vector2(OriginalMapBuilder.World(area.Int(4).Value), OriginalMapBuilder.World(area.Int(5).Value));
            var at = map.NearestOpen(centre, area.Int(6).Value / 2f, area.Int(7).Value / 2f, OriginalMapBuilder.StoreZoneTerrain)
                ?? throw new OriginalDataException($"LIO row {area.RowNumber} '{team.Name}' has no open tile.");
            sites.Add(new ServiceSite(StoreIdOf(team.Index, team.Name), team.Name, SiteServices.Store, at.X, at.Y, DockRadius) { LioRow = area.RowNumber });
        }
        return sites;
    }

    private static string VehName(IReadOnlyList<RawRecord> veh, int vehicleId)
    {
        var row = veh.Single(v => v.FieldCount > 3 && v.Value(2) == vehicleId.ToString(CultureInfo.InvariantCulture));
        return row.Value(3).Trim('"');
    }

    /// <summary>The refinery: name and recipes from VEH id 400, position from the first LIO type 5 row that places actor -400.</summary>
    private static RefineryDefinition Refinery(IReadOnlyList<RawRecord> veh, IReadOnlyList<RawRecord> lio, OriginalItemCatalog items)
    {
        var row = lio.First(r => r.FieldCount == OriginalMineralSpawns.FieldCount && r.Value(0) == "5" && r.Int(OriginalMineralSpawns.ItemField).Value == RefineryActor);
        var name = veh.Single(v => v.FieldCount > 3 && v.Value(2) == OriginalRefinery.VehicleId.ToString(CultureInfo.InvariantCulture)).Value(3);
        return new RefineryDefinition(RefineryId, string.IsNullOrWhiteSpace(name) ? "Refinery" : name,
            OriginalMapBuilder.World(row.Int(4).Value), OriginalMapBuilder.World(row.Int(5).Value), RefineryDockRadius)
        {
            Recipes = OriginalRefinery.FromRecords(veh, items)
                .Select(r => new StationRefineRecipe(Slug(r.Name), r.Name, Key(r.InputItemId.Value), Key(r.OutputItemId.Value), r.RawField4.Value)).ToList(),
        };
    }

    public static StationDefinition Build(OriginalItemCatalog items, IReadOnlyList<OriginalTradeRecipe> recipes, string stationName,
        RefineryDefinition? refinery = null, IReadOnlyList<StationShip>? ships = null, IReadOnlyList<ServiceSite>? outposts = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(recipes);
        var goods = items.Resources.Select(i => new StationGood(Key(i.Id), i.Name, i.Mass.Value,
            i.Buy.Value > 0 ? i.Buy.Value : null, i.Sell.Value > 0 ? i.Sell.Value : null)).ToList();
        var trades = recipes.Select(r => new StationTrade(
            Slug(r.Name), r.Name, Key(r.InputItemId.Value), Key(r.CreditItemId.Value), r.CreditItem.Name,
            r.CreditItem.Record.Int(31).Value, r.RawField4.Value)).ToList();
        var repairs = OriginalEffectAmountRow.All(items).Select(r => new StationRepair(Key(r.Item.Id), r.Item.Name,
            r.Amount.Value * RepairPricePerPoint, r.Amount.Value, $"ITM id {r.Item.Id} f[33]={r.Amount.Value}")
        {
            // Steve (Nov 4 2026): the repair costs 500 credits and fills the hull, including a legacy hull above the raw 50.
            RestoresFullHull = r.Item.Name == FullRepairName,
        }).ToList();
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
            Goods = goods,
            Trades = trades,
            Repairs = repairs,
            Upgrades = upgrades,
            Collectibles =
            [
                // 100 pixels straight ahead of the Cold Harbor spawn (1888, 1728), inside the bay and the dock radius.
                new CollectibleSpawn("pile_wreckage_launch", Key(2008), 9, StationX - 48, StationY - 68, 14),
                new CollectibleSpawn("pile_alloy_west", Key(2020), 12, StationX - 170, StationY + 140, 14),
                new CollectibleSpawn("pile_tellerium_south", Key(2004), 6, StationX + 90, StationY + 200, 14),
            ],
            Refinery = refinery,
            Ships = ships,
            Outposts = outposts ?? [],
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
            case OriginalArmor.ArmorCategory when OriginalArmor.TryOf(item, out var armor):
                return (fromBlock with { Armor = ArmorOf(armor!) },
                    blockBasis + "; ORIGINAL: protection from ITM f[52..57] (Kinetic, Explosive, Electronic = Energy Ignore/Percent pairs), "
                    + "Ignore / 1000 hull points, NEW: Percent read as per mille");
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

    /// <summary>The protection an original armour gives (RT-F041).</summary>
    public static ShipArmor ArmorOf(OriginalArmor armor)
    {
        ArgumentNullException.ThrowIfNull(armor);
        static ArmorProtection P(OriginalArmorPair p) => new() { Ignore = p.IgnoreHullPoints, Reduction = p.ReductionFraction };
        return new ShipArmor { Kinetic = P(armor.Kinetic), Explosive = P(armor.Explosive), Energy = P(armor.Electronic) };
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
