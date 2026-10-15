using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: independent Python audit of the pinned VEH/Rogue.veh f[129] and ITM/Rogue_mp.itm type-16 rows 181-192 (eb1514ea).
/// Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalLoadoutTests
{
    private static readonly Lazy<OriginalItemCatalog> Items = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));
    private static readonly Lazy<OriginalHullCatalog> Hulls = new(() => OriginalHullCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void TwelveStartingLoadoutsAreOneAddInvEachAndResolveToACannon()
    {
        var l = OriginalStartingLoadout.All(Hulls.Value, Items.Value);
        Assert.Equal(12, l.Count);
        Assert.All(l, x => { Assert.Equal("addinv", x.Verb); Assert.NotNull(x.Item); Assert.Equal("Cannon", x.Item!.Category); });
        var byName = l.ToDictionary(x => x.Hull.Name, x => x.ItemName);
        Assert.Equal("Plasma Cannon", byName["Asp"]);
        Assert.Equal("Plasma Cannon", byName["Hornet"]);
        Assert.Equal("Plasma Cannon", byName["Excelsior"]);
        Assert.Equal("Plasma Cannon", byName["Ravager"]);
        Assert.Equal("Bio Cannon", byName["Skithist"]);
        Assert.Equal("Bio Cannon", byName["Tarthist"]);
        Assert.Equal("D-Cannon", byName["Rak'Kir"]);
        Assert.Equal("D-Cannon", byName["Zag'Gir"]);
        Assert.Equal(new[] { 1004, 1036, 1041 }, l.Select(x => x.Item!.Id).Distinct().OrderBy(x => x));
    }

    [RequiresOriginalAssetsFact]
    public void TwelveHullExchangeRowsPriceFifteenThousandNoResaleAndTargetTheirHullSkill()
    {
        var x = OriginalHullExchange.All(Items.Value);
        Assert.Equal(Enumerable.Range(181, 12), x.Select(e => e.Item.RowNumber));
        Assert.All(x, e => { Assert.Equal(15000, e.Item.Buy.Value); Assert.Equal(0, e.Item.Sell.Value); });
        foreach (var e in x)
            Assert.Equal(Hulls.Value.ByName(e.Item.Name).SkillId, e.TargetSkill.Value);
        // Logic lists the skills of the other same-race hulls (3 terms), never the target's own skill.
        foreach (var e in x)
        {
            var terms = e.Logic.Split('|').Select(int.Parse).ToList();
            Assert.Equal(3, terms.Count);
            Assert.DoesNotContain(e.TargetSkill.Value, terms);
            var race = Hulls.Value.ByName(e.Item.Name).Category;
            Assert.All(terms, t => Assert.Equal(race, Hulls.Value.BySkillId(t).Category));
        }
    }

    [RequiresOriginalAssetsFact]
    public void ZagGirLogicCopyDiffersOnlyInTermOrder()
    {
        var x = OriginalHullExchange.All(Items.Value);
        var odd = x.Where(e => e.Logic != e.LogicCopy).ToList();
        var z = Assert.Single(odd);
        Assert.Equal("Zag'Gir", z.Item.Name);
        Assert.Equal("9|6|5", z.Logic);
        Assert.Equal("5|6|9", z.LogicCopy);
    }
}
