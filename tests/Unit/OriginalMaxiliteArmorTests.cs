using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for F098 "Maxilite Armor" (ITM/Rogue_mp.itm row 23, id 6, type 13, category Hull Armor), resolved with the reusable
/// RT-F076 <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an independent read of the pinned file
/// (scripts/original-assets/audit_rt_f076.py with item id 6, pinned eb1514ea). Data/reference clause only: nothing here applies the
/// engine modification. Field names from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence.
/// The BLO archives are not read (queue C1). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalMaxiliteArmorTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Item => Resolver.Value.Resolve(6);

    [RequiresOriginalAssetsFact]
    public void FullVersionedRecordAndRawPricing()
    {
        var m = Item;
        Assert.Equal("ITM/Rogue_mp.itm", m.SourceFile);
        Assert.Equal(23, m.RowNumber);
        Assert.Equal("v57", m.VersionToken);
        Assert.Equal(13, m.RecordType);
        Assert.Equal(137, m.FieldCount);
        Assert.Equal(137, m.Fields.Count);
        Assert.Equal(6, m.Id);
        Assert.Equal("Maxilite Armor", m.Name);
        Assert.Equal("Hull Armor", m.Category);
        Assert.Equal("Armor Plating. light protection versus Kinetic damage, very light protection versus Explosive damage, very light protection versus Energy damage.", m.Description);
        Assert.Equal((7, 0), (m.Mass.FieldIndex, m.Mass.Value));
        Assert.Equal((8, 2000), (m.Buy.FieldIndex, m.Buy.Value));
        Assert.Equal((15, 0), (m.Sell.FieldIndex, m.Sell.Value));
        Assert.False(m.Sell.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsFact]
    public void SkillLogicAndNoPercentReference()
    {
        var m = Item;
        Assert.Equal("", m.LogicRaw);
        Assert.Empty(m.SkillReferences);
        Assert.Empty(m.PercentReferences);
        Assert.Null(m.ExchangeLogicCopy);
        Assert.Null(m.ExchangeTargetSkill);
        Assert.Null(m.ExchangeTargetHull);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "SkillLogic" or "SkillId");
    }

    [RequiresOriginalAssetsFact]
    public void HeldCategory11IsAnUnprovenCandidateWithCfgRow11()
    {
        var m = Item;
        var h = m.HeldCategory!;
        Assert.Equal((20, 11), (h.CategoryConstant.FieldIndex, h.CategoryConstant.Value));
        Assert.Equal((11, 1, -1), (h.Limit!.CategoryId, h.Limit.Limit, h.Limit.ExtendedLimit));
        Assert.Equal(2987, h.Limit.LimitLine);
        var u = Assert.Single(m.Unresolved, x => x.Kind == "HeldCategoryMapping");
        Assert.Equal(20, u.FieldIndex);
        Assert.Contains("not established", u.Reason);
    }

    [RequiresOriginalAssetsFact]
    public void BothAssetPairsAreNoneAndNoBloMemberIsNamedButSecondPairCarriesNonZeroInts()
    {
        var m = Item;
        (int first, string archive, string member, string ints)[] expected =
        [
            (23, "None", "None", "0, 0, 0, 0, 0, 0"),
            (48, "None", "None", "0, 0, 1000, 40, 0, 20"),
        ];
        Assert.Equal(expected, m.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
        Assert.All(m.AssetPairs, a => Assert.True(a.IsNone));
        Assert.Empty(m.OtherTextFields);
        Assert.DoesNotContain(m.Unresolved, x => x.Kind == "BloMember");
    }

    [RequiresOriginalAssetsFact]
    public void FieldCoverageIsHonest()
    {
        var m = Item;
        Assert.Empty(m.VehicleReferences);
        Assert.Equal(137, m.NamedFields.Count + m.UnnamedFieldIndices.Count);
    }
}
