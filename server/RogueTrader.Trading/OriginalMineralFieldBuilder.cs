using System.Globalization;
using System.Text.Json;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>Raw evidence for one derived mineral spawn: the LIO row and every integer field it was read from.</summary>
public sealed record MineralSpawnSource(int LioRow, string Name, IReadOnlyDictionary<string, int> Raw);

/// <summary>A derived mineral spawn: the simulation values plus where they came from.</summary>
public sealed record DerivedMineralSpawn(string Id, string ItemId, string ItemName, int Quantity, float X, float Y, float Width, float Height,
    int AttemptTicks, int SucceedTicks, float PileRadius, int InitialCount, int MaxInArea, int MaxInLevel, float ClumpRadius, int ClumpQuantity,
    int MinPlayers, int MaxPlayers, float MinPlayerDistance, float MaxPlayerDistance, long ExpireTicks, int Probability, MineralSpawnSource Source);

/// <summary>A mineral row that is in the originals but is not spawned, and why.</summary>
public sealed record SkippedMineralSpawn(int LioRow, string Name, string Reason);

/// <summary>The document behind <c>data/minerals/mineral_spawns.json</c>; <see cref="MineralFieldCatalog.FromJson"/> reads it.</summary>
public sealed record MineralFieldDocument(string Id, string EvidenceClassification, string Source, IReadOnlyList<string> NewRules,
    ulong Seed, IReadOnlyList<DerivedMineralSpawn> Spawns, IReadOnlyList<SkippedMineralSpawn> Skipped)
{
    /// <summary>LF line endings on every OS, so the derived file and its oracle test match on Windows too (review R3-2).</summary>
    public string ToJson() => JsonSerializer.Serialize(this, StationDefinition.Json).ReplaceLineEndings("\n") + "\n";
}

/// <summary>
/// Derives the testbed mineral field from the pinned originals (queue M7 mining slice). Taken from the originals: which rows are
/// mineral spawns, their item ids, positions, areas and every raw field (LIO/Rogue.lio type 5 v23), the pickup distance
/// (CFG/rogue.cfg [Arena] ItemPickupDistance) and the drop life (CFG [Terrain0] PrizeExpire). The field meanings follow the
/// InfantryOnline emulator's names and spawn loop (<c>LioInfo.Hide.cs</c>, <c>ArenaLio.hideSpawn</c>/<c>hideItem</c> at bb97579,
/// docs/original-research/ZONE_ASSETS_COMPARISON.md section 4), adopted on Steve's call (November 3, 2026). The emulator is a
/// reconstruction, not the native server, so the readings stay RECONSTRUCTED; the NEW parts are listed in <see cref="NewRules"/>.
/// </summary>
public static class OriginalMineralFieldBuilder
{
    public const string FieldId = "original_mineral_spawns";

    // ---- NEW/provisional readings ------------------------------------------------------------------------------------
    /// <summary>Assumed centre of a 32,000-unit square original map (LVL 1999x1998 tiles at 16 units; RT-F021 unverified).</summary>
    public const float MapCentre = 16000f;
    /// <summary>Practice-sector units per original unit: the 32,000-unit map is drawn into a 1,400-unit square.</summary>
    public const float MapScale = 1400f / 32000f;
    /// <summary>Raw delay fields in tenths of a second (the emulator multiplies them by 100 ms), as 60 Hz ticks.</summary>
    public const float TicksPerRawDelayUnit = WorldSimulation.TickRate / 10f;
    /// <summary>The emulator's "no limit" for f[17] MaxPlayerDistance (it checks the distance only below this).</summary>
    public const int NoMaxPlayerDistance = 99999;
    /// <summary>NEW placement seed; the client derives a fresh one per sector instance.</summary>
    public const ulong Seed = 0x52545F4D494E4552UL; // "RT_MINER"

    // Raw field positions (emulator names in LioInfo.Hide.cs).
    private const int FX = 4, FY = 5, FWidth = 6, FHeight = 7, FInitialCount = 10, FAttemptDelay = 11, FSucceedDelay = 12,
        FProbability = 13, FMinPlayers = 14, FMaxPlayers = 15, FMinPlayerDistance = 16, FMaxPlayerDistance = 17, FMaxTypeInArea = 18,
        FMaxTypeInLevel = 19, FHideQuantity = 21, FClumpRadius = 26, FClumpQuantity = 27;

    public static readonly IReadOnlyList<string> NewRules =
    [
        "Field meanings follow the InfantryOnline emulator (LioInfo.Hide.cs, ArenaLio.hideSpawn/hideItem at bb97579), adopted on Steve's call (2026-11-03): f[10] InitialCount, f[11] AttemptDelay, f[12] SucceedDelay, f[13] Probability, f[14] MinPlayers, f[15] MaxPlayers, f[16] MinPlayerDistance, f[17] MaxPlayerDistance, f[18] MaxTypeInArea, f[19] MaxTypeInLevel, f[21] HideQuantity, f[26] ClumpRadius, f[27] ClumpQuantity. The emulator is a reconstruction of the server, not the native one (RECONSTRUCTED).",
        "Each drop is f[21] units (1). An event drops f[27] drops (15 Tellerium and Iron Ore, 10 Krandazite) in an f[26] box (350) around a random anchor in the area; an event fails while the area already holds f[18] units (1) of the item, so an area refills only when it is empty; an attempt fails while the whole sector holds f[19] units of the item (100 Tellerium, 100 Iron Ore, 50 Krandazite; -1 = no limit, as on Tellerium SpawnG) or while the sector has fewer than f[14] or more than f[15] players.",
        "Delays are tenths of a second as the emulator reads them (f[11]=3000 is 300 s). After a success the next attempt comes after the shorter of f[11] and f[12], as the emulator's two timers make it.",
        "Every drop vanishes CFG [Terrain0] PrizeExpire seconds (720) after it lands. The emulator takes the PrizeExpire of the terrain under the drop (120 in docks and trade zones); the practice sector has no terrain map yet, so open space is assumed everywhere (NEW).",
        "Positions f[4]/f[5] are read as the area centre and f[6]/f[7] as its full width/height in original map units; they, f[16], f[17] and f[26] are drawn into the 1,400-unit practice sector around an assumed 32,000-unit map centre (16,000, 16,000) at 1,400/32,000 scale. f[17]=99999 means no check.",
        "A drop that would overlap a (NEW) practice-sector asteroid is retried, up to the emulator's 10 tries per event with 5 back per drop placed (the emulator's blocked-tile test). The map-edge clamp is not applied.",
        "Pickup radius is CFG [Arena] ItemPickupDistance=80 drawn at the same scale (3.5 world units, added to the ship radius).",
        "f[22], f[24] and f[25] are kept raw and not applied. Rows whose f[21] is not positive are not spawned.",
    ];

    public static MineralFieldDocument Build(string assetsRoot)
    {
        var items = OriginalItemCatalog.Load(assetsRoot);
        var rows = OriginalMineralSpawns.Load(assetsRoot, items);
        var cfg = OriginalCfg.LoadRogue(assetsRoot);
        var arena = cfg.Section("Arena");
        var pickup = arena.Find("ItemPickupDistance") ?? throw new OriginalDataException("CFG [Arena] has no ItemPickupDistance.");
        var open = cfg.Section("Terrain0");
        var expire = open.Find("PrizeExpire") ?? throw new OriginalDataException("CFG [Terrain0] has no PrizeExpire.");
        return Build(rows, arena.Int("ItemPickupDistance"), pickup.LineNumber, open.Int("PrizeExpire"), expire.LineNumber);
    }

    public static MineralFieldDocument Build(IReadOnlyList<OriginalMineralSpawn> rows, int itemPickupDistance, int pickupLine,
        int prizeExpireSeconds, int prizeExpireLine)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var spawns = new List<DerivedMineralSpawn>();
        var skipped = new List<SkippedMineralSpawn>();
        foreach (var row in rows)
        {
            if (row.F(FHideQuantity) <= 0)
            {
                skipped.Add(new SkippedMineralSpawn(row.Row, row.Name, $"f[21] HideQuantity={row.F(FHideQuantity)}: no units to drop."));
                continue;
            }
            var maxDistance = row.F(FMaxPlayerDistance);
            spawns.Add(new DerivedMineralSpawn(
                row.Id.ToString(CultureInfo.InvariantCulture), Key(row.Item.Id), row.Item.Name, row.F(FHideQuantity),
                (row.F(FX) - MapCentre) * MapScale, (row.F(FY) - MapCentre) * MapScale, row.F(FWidth) * MapScale, row.F(FHeight) * MapScale,
                Ticks(row.F(FAttemptDelay)), Ticks(row.F(FSucceedDelay)), itemPickupDistance * MapScale,
                row.F(FInitialCount), row.F(FMaxTypeInArea), row.F(FMaxTypeInLevel), row.F(FClumpRadius) * MapScale, row.F(FClumpQuantity),
                row.F(FMinPlayers), row.F(FMaxPlayers), row.F(FMinPlayerDistance) * MapScale,
                maxDistance >= NoMaxPlayerDistance ? 0 : maxDistance * MapScale,
                Math.Max(0, prizeExpireSeconds) * (long)WorldSimulation.TickRate, row.F(FProbability),
                new MineralSpawnSource(row.Row, row.Name, row.Raw.ToDictionary(r => $"f{r.FieldIndex}", r => r.Value))));
        }
        return new MineralFieldDocument(FieldId, "ORIGINAL+RECONSTRUCTED+NEW",
            $"Derived by OriginalMineralFieldBuilder from pinned {OriginalSafeZones.LioRogue.RelativePath} (type 5 v23 rows with a Resources item in f[20]), {PinnedAssets.Itm.RelativePath}, CFG/rogue.cfg [Arena] ItemPickupDistance={itemPickupDistance} (line {pickupLine}) and [Terrain0] PrizeExpire={prizeExpireSeconds} (line {prizeExpireLine}); field meanings from the InfantryOnline emulator at bb97579; regenerate, never hand-edit.",
            NewRules, Seed, spawns, skipped);
    }

    private static int Ticks(int raw) => Math.Max(1, (int)MathF.Round(raw * TicksPerRawDelayUnit));

    private static string Key(int id) => id.ToString(CultureInfo.InvariantCulture);
}
