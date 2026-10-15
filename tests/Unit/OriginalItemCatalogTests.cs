using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: the values written in docs/original-research/FEATURE_CATALOG.md rows RT-F013..RT-F019, copied from that text.
/// Asset-dependent tests are reported Skipped (never silently passed) when the canonical assets are not found.
/// </summary>
public sealed class OriginalItemCatalogTests
{
    private sealed record Oracle(string Id, string Name, int Row, int ItemId, int Mass, int Buy, int Sell);

    private static readonly Oracle[] Rows =
    [
        new("RT-F013", "Tellerium", 68, 2003, 50000, 0, 0),
        new("RT-F014", "Refined Tellerium", 69, 2004, 25000, 0, -1),
        new("RT-F015", "Krandazite", 70, 2005, 100000, 0, 0),
        new("RT-F016", "Refined Krandazite", 71, 2006, 50000, 0, -1),
        new("RT-F017", "Wreckage", 87, 2008, 200000, 0, 0),
        new("RT-F018", "Iron Ore", 166, 2019, 100000, 0, 0),
        new("RT-F019", "Alloy", 167, 2020, 75000, 0, 0),
    ];

    public static IEnumerable<object[]> OracleIds => Rows.Select(r => new object[] { r.Id });

    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void AllTwoHundredRowsLoadAsV57()
    {
        Assert.Equal(200, Catalog.Value.Items.Count);
        Assert.All(Catalog.Value.Items, i => Assert.Equal("v57", i.Record.VersionToken));
    }

    [RequiresOriginalAssetsFact]
    public void ExactlySevenResourcesInFileOrder() =>
        Assert.Equal(Rows.Select(r => r.Name), Catalog.Value.Resources.Select(i => i.Name));

    [RequiresOriginalAssetsTheory]
    [MemberData(nameof(OracleIds))]
    public void ResourceMatchesCatalogRow(string id)
    {
        var o = Rows.Single(r => r.Id == id);
        var item = Catalog.Value.ById(o.ItemId);
        Assert.Equal(o.Name, item.Name);
        Assert.Equal(o.Row, item.RowNumber);
        Assert.Equal(4, item.RecordType);
        Assert.Equal("Resources", item.Category);
        Assert.Equal(o.Mass, item.Mass.Value);
        Assert.Equal(o.Buy, item.Buy.Value);
        Assert.Equal(o.Sell, item.Sell.Value);
        Assert.Equal(o.Sell < 0, item.Sell.IsNegativeSentinel);
        Assert.Equal("ITM/Rogue_mp.itm", item.Record.SourceFile);
    }

    private static string Row(int type, string version, int id, string cat = "Resources")
    {
        var fields = Enumerable.Repeat("0", 31).ToArray();
        fields[0] = type.ToString(); fields[1] = version; fields[2] = id.ToString(); fields[3] = "\"n\""; fields[4] = $"\"{cat}\"";
        return string.Join(",", fields);
    }

    [Fact]
    public void RejectsUnsupportedVersion() =>
        Assert.Throws<OriginalItemException>(() =>
            OriginalItemCatalog.FromRecords(RawRecordReader.Parse(Row(4, "v56", 1), "t.itm")));

    [Fact]
    public void RejectsDuplicateIds() =>
        Assert.Throws<OriginalItemException>(() =>
            OriginalItemCatalog.FromRecords(RawRecordReader.Parse(Row(4, "v57", 1) + "\r\n" + Row(6, "v57", 1), "t.itm")));

    [Fact]
    public void RejectsShortRecords() =>
        Assert.Throws<OriginalItemException>(() =>
            OriginalItemCatalog.FromRecords(RawRecordReader.Parse("4,v57,1,\"n\",\"Resources\"", "t.itm")));

    [Fact]
    public void ResourcesNeedTypeFourAndResourcesCategory()
    {
        var text = Row(4, "v57", 1) + "\r\n" + Row(4, "v57", 2, "Ammo-Projectile") + "\r\n" + Row(6, "v57", 3);
        var catalog = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(text, "t.itm"));
        Assert.Equal([1], catalog.Resources.Select(i => i.Id));
    }
}
