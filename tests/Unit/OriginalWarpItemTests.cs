using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: FEATURE_CATALOG.md RT-F054 (ITM HyperCore2011 row 111; Hyper Jump25 row 110, Beacon29 row 116, Controlled30
/// row 117, Recall31 row 118, Gate33 row 119) plus rows 112-114 (Portal 26, Warp-Start 27, Warp-Team 28), checked against
/// an independent Python audit (scripts/original-assets/audit_rt_f054.py) of the pinned Rogue_mp.itm (eb1514ea).
/// Asset-dependent tests are Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalWarpItemTests
{
    private sealed record Oracle(int Row, int Id, string Name, string Category, string Logic, int[] Ints, string[] Texts);

    // Ints are in OriginalWarpItem.DistinguishingIntFields order (36 values); Texts are f[23], f[24], f[101], f[102], f[109], f[110].
    private static readonly Oracle[] Abilities =
    [
        new(110, 25, "Hyper Jump", "HyperDrive Systems", "", [2500,5,-1,0,1000,18,0,0,0,0,2011,1,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,1000,1000,1000,0,9,250,0,0], ["None", "None", "cruiart", "warpPorter", "crwav", "warpin"]),
        new(112, 26, "Portal", "BaseLimitedUse", "!%1", [0,10,-2,3,0,0,35,300,5,1,0,0,0,-1,-1,-1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,5,0,0,3000,2], ["UtilityEffects", "PrizeCrateA", "None", "None", "UtilitySounds", "portal"]),
        new(113, 27, "Warp-Start", "BaseWarp", "", [0,11,0,3,0,0,0,0,0,0,0,0,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,0,0,0,6,0,0,0,0], ["None", "None", "None", "None", "UtilitySounds", "teleport"]),
        new(114, 28, "Warp-Team", "BaseWarp", "!%1", [0,5,-1,3,0,0,0,0,0,0,0,0,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,2000,0,0,1,0,100,0,0], ["None", "None", "None", "None", "UtilitySounds", "teleport"]),
        new(116, 29, "Hyper Beacon System", "HyperDrive Systems", "!1&!2&!3&!4&!5&!6&!8&!9&!10&!11&!15&!16", [3500,5,-1,0,2000,18,0,0,0,0,2011,1,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,1000,1000,1000,5,0,250,6000,0], ["None", "None", "cruiart", "warpPorter", "crwav", "warpin"]),
        new(117, 30, "Controlled Hyper Jump", "HyperDrive Systems", "", [5000,5,-1,0,3000,18,0,0,0,1,2011,1,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,1000,1000,1000,1,0,250,0,0], ["None", "None", "cruiart", "warpPorter", "crwav", "warpin"]),
        new(118, 31, "Hyper Recall", "HyperDrive Systems", "", [2500,5,-1,0,1000,18,0,0,0,0,2011,1,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,1000,1000,1000,6,10,250,0,0], ["None", "None", "cruiart", "warpPorter", "crwav", "warpin"]),
        new(119, 33, "Hyper Gate", "HyperDrive Systems", "", [3500,5,-1,0,2000,18,0,0,0,0,2011,1,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,1000,1000,1000,3,0,250,0,0], ["None", "None", "cruiart", "warpPorter", "crwav", "warpin"]),
    ];

    private static readonly int[] TextFields = [23, 24, 101, 102, 109, 110];

    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    private static string AbilityRow(int id, string cat, int f34)
    {
        var fields = Enumerable.Repeat("0", OriginalWarpItem.AbilityFieldCount).ToArray();
        fields[0] = "17"; fields[1] = "v57"; fields[2] = id.ToString(); fields[3] = "\"n\""; fields[4] = $"\"{cat}\"";
        fields[5] = "\"\""; fields[6] = "\"d\""; fields[13] = "-2"; fields[34] = f34.ToString(); fields[23] = "None"; fields[24] = "None";
        return string.Join(",", fields);
    }

    [Fact]
    public void SyntheticRowKeepsRawSentinelsAndSelectsOnlyTheWarpIds()
    {
        var text = AbilityRow(25, "HyperDrive Systems", 2011) + "\r\n" + AbilityRow(26, "BaseWarp", 0) + "\r\n" + AbilityRow(999, "Other", 0);
        var catalog = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(text, "t.itm"));
        var rows = OriginalWarpItems.Resolve(catalog);
        Assert.Equal([25, 26], rows.Select(r => r.Item.Id));
        Assert.Equal([true, false], rows.Select(r => r.IsHyperDriveSystem));
        Assert.Equal(2011, rows[0].HyperCoreReferenceCandidate!.Value.Value);
        Assert.Equal(-2, rows[0].Int(13).Value);
        Assert.True(rows[0].Int(13).IsNegativeSentinel);
        Assert.Equal(OriginalWarpItem.DistinguishingIntFields.Count, rows[0].DistinguishingInts.Count);
        Assert.Equal(3, OriginalWarpItems.AllAbilityRows(catalog).Count);
    }

    [Fact]
    public void DistinguishingFieldListsAreSortedAndUnique()
    {
        Assert.Equal(36, OriginalWarpItem.DistinguishingIntFields.Count);
        Assert.Equal(OriginalWarpItem.DistinguishingIntFields.OrderBy(i => i), OriginalWarpItem.DistinguishingIntFields);
        Assert.Equal(OriginalWarpItem.DistinguishingIntFields.Distinct().Count(), OriginalWarpItem.DistinguishingIntFields.Count);
        Assert.All(Abilities, o => Assert.Equal(36, o.Ints.Length));
    }

    [RequiresOriginalAssetsFact]
    public void CatalogRowClaimsMatchTheFile()
    {
        var cat = Catalog.Value;
        // Rows named by the catalog.
        var named = new (int Row, int Id, string Name)[]
        {
            (110, 25, "Hyper Jump"), (111, 2011, "HyperCore"), (116, 29, "Hyper Beacon System"),
            (117, 30, "Controlled Hyper Jump"), (118, 31, "Hyper Recall"), (119, 33, "Hyper Gate"),
        };
        foreach (var (row, id, name) in named)
        {
            var item = cat.ById(id);
            Assert.Equal(row, item.RowNumber);
            Assert.Equal(name, item.Name);
        }
        Assert.Equal(OriginalWarpItems.CatalogRows, named.Select(n => n.Row));
        // Rows 112-114 are warp rows of other categories (not listed by the catalog); row 115 is not a warp row.
        Assert.Equal(new[] { 26, 27, 28 }, OriginalWarpItems.UncatalogedWarpRows.Select(r => cat.Items.Single(i => i.RowNumber == r).Id));
        var r115 = cat.Items.Single(i => i.RowNumber == 115);
        Assert.Equal((3009, "Skithzar ClusterBurst-Child", "ChildProjectiles", 8), (r115.Id, r115.Name, r115.Category, r115.RecordType));
        Assert.DoesNotContain(r115.Id, OriginalWarpItems.ItemIds);
    }

    [RequiresOriginalAssetsFact]
    public void ResolveReturnsTheNineRowsInFileOrder()
    {
        var all = OriginalWarpItems.Resolve(Catalog.Value);
        Assert.Equal(new[] { 110, 111, 112, 113, 114, 116, 117, 118, 119 }, all.Select(a => a.Item.RowNumber));
        Assert.Equal(OriginalWarpItems.ItemIds, all.Select(a => a.Item.Id));
        // Every type-17 row in the file is in the set and carries 113 fields.
        var t17 = OriginalWarpItems.AllAbilityRows(Catalog.Value);
        Assert.Equal(8, t17.Count);
        Assert.Equal(new[] { 25, 26, 27, 28, 29, 30, 31, 33 }, t17.Select(a => a.Item.Id));
        Assert.All(t17, a => Assert.Equal(113, a.Item.Record.FieldCount));
        Assert.Equal(new[] { 25, 29, 30, 31, 33 }, all.Where(a => a.IsHyperDriveSystem).Select(a => a.Item.Id));
        // The six "HyperDrive Systems" category rows are these five plus HyperCore (RT-F048's family).
        Assert.Equal(6, Catalog.Value.Items.Count(i => i.Category == "HyperDrive Systems"));
    }

    [RequiresOriginalAssetsFact]
    public void HyperCoreIsTheSingleType4RowWith31Fields()
    {
        var core = OriginalWarpItems.Resolve(Catalog.Value).Single(a => a.IsHyperCore);
        Assert.Equal((111, 2011, "HyperCore", "HyperDrive Systems", 4, 31), (core.Item.RowNumber, core.Item.Id, core.Item.Name, core.Item.Category, core.Item.RecordType, core.Item.Record.FieldCount));
        Assert.Equal((0, 5000, 1500), (core.Item.Mass.Value, core.Item.Buy.Value, core.Item.Sell.Value));
        Assert.Equal(-1, core.Int(13).Value);
        Assert.True(core.Int(13).IsNegativeSentinel);
        Assert.Null(core.HyperCoreReferenceCandidate);
        Assert.Empty(core.SixteenValueBlock);
        Assert.Equal(8, core.DistinguishingInts.Count);   // indexes below 31: 8,11,13,14,15,20,27,30
        Assert.Equal("", core.Logic);
        Assert.Contains("necessary to have a HyperCore", core.Description);
    }

    [RequiresOriginalAssetsFact]
    public void AbilityRowsPinEveryDistinguishingRawField()
    {
        var byId = OriginalWarpItems.Resolve(Catalog.Value).Where(a => a.IsAbility).ToDictionary(a => a.Item.Id);
        Assert.Equal(Abilities.Length, byId.Count);
        foreach (var o in Abilities)
        {
            var a = byId[o.Id];
            Assert.Equal(o.Row, a.Item.RowNumber);
            Assert.Equal(o.Name, a.Item.Name);
            Assert.Equal(o.Category, a.Item.Category);
            Assert.Equal(17, a.Item.RecordType);
            Assert.Equal(o.Logic, a.Logic);
            Assert.Equal(o.Ints, a.DistinguishingInts.Select(i => i.Value));
            Assert.Equal(OriginalWarpItem.DistinguishingIntFields, a.DistinguishingInts.Select(i => i.FieldIndex));
            Assert.Equal(o.Texts, TextFields.Select(a.Text));
            Assert.Equal(o.Ints[0], a.Item.Buy.Value);       // f[8]
            Assert.Equal(o.Ints[4], a.Item.Sell.Value);      // f[15]
            Assert.Equal(0, a.Item.Mass.Value);
        }
    }

    [RequiresOriginalAssetsFact]
    public void SentinelsAreKeptAndFlagged()
    {
        var all = OriginalWarpItems.Resolve(Catalog.Value).Where(a => a.IsAbility).ToDictionary(a => a.Item.Id);
        // f[13] (ids 25,26,27,28,29,30,31,33): -1 for six rows, -2 for Portal, 0 for Warp-Start (unexplained, unchanged).
        Assert.Equal(new[] { -1, -2, 0, -1, -1, -1, -1, -1 }, all.Values.Select(a => a.Int(13).Value));
        Assert.Equal(new[] { true, true, false, true, true, true, true, true }, all.Values.Select(a => a.Int(13).IsNegativeSentinel));
        // f[36..51] is all -2 in seven rows; Portal holds {0,-1,-1,-1,0 x12}.
        foreach (var id in new[] { 25, 27, 28, 29, 30, 31, 33 })
            Assert.All(all[id].SixteenValueBlock, i => { Assert.Equal(-2, i.Value); Assert.True(i.IsNegativeSentinel); });
        Assert.Equal(new[] { 0, -1, -1, -1 }, all[26].SixteenValueBlock.Take(4).Select(i => i.Value));
        Assert.All(all[26].SixteenValueBlock.Skip(4), i => Assert.Equal(0, i.Value));
        Assert.Equal(16, all[26].SixteenValueBlock.Count);
    }

    [RequiresOriginalAssetsFact]
    public void HyperCoreReferenceIs2011OnlyInTheFiveHyperDriveSystems()
    {
        var all = OriginalWarpItems.Resolve(Catalog.Value).Where(a => a.IsAbility).ToList();
        Assert.Equal(new[] { 25, 29, 30, 31, 33 }, all.Where(a => a.HyperCoreReferenceCandidate!.Value.Value == OriginalWarpItem.HyperCoreId).Select(a => a.Item.Id));
        Assert.All(all.Where(a => !a.IsHyperDriveSystem), a => Assert.Equal(0, a.HyperCoreReferenceCandidate!.Value.Value));
        Assert.All(all.Where(a => a.IsHyperDriveSystem), a => Assert.Equal(1, a.Int(35).Value));
        // Outside f[2] the value 2011 occurs only at f[34] of those five rows in the whole item file.
        var elsewhere = Catalog.Value.Items.SelectMany(i => i.Record.Fields.Where(f => f.Index != 2 && f.Value == "2011").Select(f => (i.RowNumber, f.Index)));
        Assert.Equal(new[] { (110, 34), (116, 34), (117, 34), (118, 34), (119, 34) }, elsewhere);
        // The number 2011 is also a VEH hull id (Excelsior); the reference is inferred, not proven to be an item lookup.
        Assert.Equal(OriginalWarpItem.HyperCoreReferenceField, 34);
    }
}
