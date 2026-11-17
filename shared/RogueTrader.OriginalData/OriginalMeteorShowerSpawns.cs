namespace RogueTrader.OriginalData;

/// <summary>
/// One meteor shower row of LIO/Rogue.lio (RT-F061, data only): a type 5 v23 "hide" record whose f[20] is -402, the negative id of
/// VEH 402 "MeteorShower" (the emulator reads a negative f[20] as a vehicle to place, <c>ArenaLio.hideSpawn</c> at bb97579). Every
/// integer field is kept raw by index; <see cref="Announcement"/> is f[23] as written.
/// </summary>
public sealed record OriginalMeteorShowerSpawn(int Row, int Id, string Name, string Announcement, IReadOnlyList<OriginalInt> Raw)
{
    /// <summary>Raw integer field <paramref name="index"/> (f[4..22] and f[24..27]).</summary>
    public int F(int index) => Raw.Single(r => r.FieldIndex == index).Value;
}

public static class OriginalMeteorShowerSpawns
{
    public const int ActorField = 20;
    public const int AnnouncementField = 23;
    /// <summary>f[20] of a meteor shower row: minus the VEH id of <see cref="OriginalMeteorShower.VehicleId"/>.</summary>
    public const int ActorValue = -OriginalMeteorShower.VehicleId;

    public static IReadOnlyList<OriginalMeteorShowerSpawn> Load(string assetsRoot)
    {
        var lio = AssetIntegrity.ReadVerified(assetsRoot, OriginalSafeZones.LioRogue);
        return FromRecords(RawRecordReader.Parse(lio, OriginalSafeZones.LioRogue.RelativePath));
    }

    public static IReadOnlyList<OriginalMeteorShowerSpawn> FromRecords(IReadOnlyList<RawRecord> lio)
    {
        ArgumentNullException.ThrowIfNull(lio);
        var list = new List<OriginalMeteorShowerSpawn>();
        foreach (var r in lio)
        {
            if (r.FieldCount == 0 || r.Value(0) != "5") continue;
            if (r.FieldCount != OriginalMineralSpawns.FieldCount || r.VersionToken != "v23")
                throw new OriginalDataException($"LIO row {r.RowNumber}: type 5 record has {r.FieldCount} fields ({r.VersionToken}), expected {OriginalMineralSpawns.FieldCount} (v23).");
            if (r.Int(ActorField).Value != ActorValue) continue;
            list.Add(new OriginalMeteorShowerSpawn(r.RowNumber, r.Int(2).Value, r.Value(3), r.Value(AnnouncementField).Trim(),
                OriginalMineralSpawns.RawFieldIndexes.Select(r.Int).ToArray()));
        }
        return list;
    }
}
