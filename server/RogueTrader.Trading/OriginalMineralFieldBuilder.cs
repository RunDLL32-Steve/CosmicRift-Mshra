using System.Globalization;
using System.Text.Json;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>Raw evidence for one derived mineral spawn: the LIO row and every integer field it was read from.</summary>
public sealed record MineralSpawnSource(int LioRow, string Name, IReadOnlyDictionary<string, int> Raw);

/// <summary>A derived mineral spawn: the simulation values plus where they came from.</summary>
public sealed record DerivedMineralSpawn(string Id, string ItemId, string ItemName, int Quantity, int MaxPiles, float X, float Y,
    float Width, float Height, int AttemptTicks, int SucceedTicks, float ClearRadius, float PileRadius, int StartingPiles, MineralSpawnSource Source);

/// <summary>A mineral row that is in the originals but is not spawned, and why.</summary>
public sealed record SkippedMineralSpawn(int LioRow, string Name, string Reason);

/// <summary>The document behind <c>data/minerals/mineral_spawns.json</c>; <see cref="MineralFieldCatalog.FromJson"/> reads it.</summary>
public sealed record MineralFieldDocument(string Id, string EvidenceClassification, string Source, IReadOnlyList<string> NewRules,
    ulong Seed, IReadOnlyList<DerivedMineralSpawn> Spawns, IReadOnlyList<SkippedMineralSpawn> Skipped)
{
    public string ToJson() => JsonSerializer.Serialize(this, StationDefinition.Json) + "\n";
}

/// <summary>
/// Derives the testbed mineral field from the pinned originals (queue M7 mining slice, RT-F042 stays open). Taken from the
/// originals: which rows are mineral spawns, their item ids, positions, areas and every raw field (LIO/Rogue.lio type 5 v23),
/// and the pickup distance (CFG/rogue.cfg [Arena] ItemPickupDistance). How those numbers are read is NEW and listed in
/// <see cref="NewRules"/>: the LIO type 5 field meanings are undecoded, so each reading below is a guess that a later G6/RT-F042
/// decode may overturn.
/// </summary>
public static class OriginalMineralFieldBuilder
{
    public const string FieldId = "original_mineral_spawns";

    // ---- NEW/provisional readings ------------------------------------------------------------------------------------
    /// <summary>Assumed centre of a 32,000-unit square original map (LVL 1999x1998 tiles at 16 units; RT-F021 unverified).</summary>
    public const float MapCentre = 16000f;
    /// <summary>Practice-sector units per original unit: the 32,000-unit map is drawn into a 1,400-unit square.</summary>
    public const float MapScale = 1400f / 32000f;
    /// <summary>Raw delay fields read as hundredths of a second, converted to 60 Hz ticks.</summary>
    public const float TicksPerRawDelayUnit = WorldSimulation.TickRate / 100f;
    /// <summary>NEW placement seed; the client derives a fresh one per sector instance.</summary>
    public const ulong Seed = 0x52545F4D494E4552UL; // "RT_MINER"

    // Raw field positions used by the readings below (names are ours, not the original's).
    private const int FX = 4, FY = 5, FWidth = 6, FHeight = 7, FAttempt = 11, FSucceed = 12, FClear = 14, FQuantity = 18, FMaxPiles = 19;

    public static readonly IReadOnlyList<string> NewRules =
    [
        "Positions f[4]/f[5] are read as the area centre and f[6]/f[7] as its full width/height in original map units, then drawn into the 1,400-unit practice sector around an assumed 32,000-unit map centre (16,000, 16,000) at 1,400/32,000 scale.",
        "f[11] is read as the retry delay after a failed attempt and f[12] as the delay after a successful spawn, both in hundredths of a second (3000 = 30 s).",
        "f[14] (0/40/60/80) is read as a player clear radius in map units (NWS row 470: 'a mineral spawn anchor has a detection radius to ensure no players are in the area'); an attempt with the player that close fails.",
        "f[18] (1 in every spawned row) is read as the units in one pile and f[19] (100 Tellerium and Iron Ore, 50 Krandazite) as the most piles of that spawn in space at once. The swapped reading (100-unit piles) was dropped on Steve's call (2026-11-02): one such pile of Tellerium weighs more than a whole hold.",
        "A fresh sector places every spawn's full f[19] piles on its first tick, so each spawn area starts with about 100 of its mineral (Steve, 2026-11-02; the original start rule is undecoded). Afterwards a spawn adds one pile per f[12] delay while it is below f[19].",
        "A pile lands at a uniformly random point of the area that does not overlap a (NEW) practice-sector asteroid; up to 8 points are tried per attempt.",
        "Pickup radius is CFG [Arena] ItemPickupDistance=80 drawn at the same scale (3.5 world units, added to the ship radius).",
        "f[13], f[15..17], f[21], f[22], f[24..27] (tail 350/15 or 350/10) are kept raw and not applied. Rows whose f[18]/f[19] are -1 are not spawned.",
    ];

    public static MineralFieldDocument Build(string assetsRoot)
    {
        var items = OriginalItemCatalog.Load(assetsRoot);
        var rows = OriginalMineralSpawns.Load(assetsRoot, items);
        var arena = OriginalCfg.LoadRogue(assetsRoot).Section("Arena");
        var pickup = arena.Find("ItemPickupDistance") ?? throw new OriginalDataException("CFG [Arena] has no ItemPickupDistance.");
        return Build(rows, arena.Int("ItemPickupDistance"), pickup.LineNumber);
    }

    public static MineralFieldDocument Build(IReadOnlyList<OriginalMineralSpawn> rows, int itemPickupDistance, int pickupLine)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var spawns = new List<DerivedMineralSpawn>();
        var skipped = new List<SkippedMineralSpawn>();
        foreach (var row in rows)
        {
            if (row.F(FMaxPiles) <= 0 || row.F(FQuantity) <= 0)
            {
                skipped.Add(new SkippedMineralSpawn(row.Row, row.Name, $"f[18]={row.F(FQuantity)}, f[19]={row.F(FMaxPiles)}: raw values are undecoded, so the row is not spawned."));
                continue;
            }
            spawns.Add(new DerivedMineralSpawn(
                row.Id.ToString(CultureInfo.InvariantCulture), Key(row.Item.Id), row.Item.Name, row.F(FQuantity), row.F(FMaxPiles),
                (row.F(FX) - MapCentre) * MapScale, (row.F(FY) - MapCentre) * MapScale, row.F(FWidth) * MapScale, row.F(FHeight) * MapScale,
                Ticks(row.F(FAttempt)), Ticks(row.F(FSucceed)), row.F(FClear) * MapScale, itemPickupDistance * MapScale, row.F(FMaxPiles),
                new MineralSpawnSource(row.Row, row.Name, row.Raw.ToDictionary(r => $"f{r.FieldIndex}", r => r.Value))));
        }
        return new MineralFieldDocument(FieldId, "ORIGINAL+NEW",
            $"Derived by OriginalMineralFieldBuilder from pinned {OriginalSafeZones.LioRogue.RelativePath} (type 5 v23 rows with a Resources item in f[20]), {PinnedAssets.Itm.RelativePath} and CFG/rogue.cfg [Arena] ItemPickupDistance={itemPickupDistance} (line {pickupLine}); regenerate, never hand-edit.",
            NewRules, Seed, spawns, skipped);
    }

    private static int Ticks(int raw) => Math.Max(1, (int)MathF.Round(raw * TicksPerRawDelayUnit));

    private static string Key(int id) => id.ToString(CultureInfo.InvariantCulture);
}
