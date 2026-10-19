using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for RT-F093 "SuperBoosters" (ITM/Rogue_mp.itm row 18, id 1, type 13, category Engine Mods), resolved with the reusable
/// RT-F076 <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an independent read of the pinned file
/// (scripts/original-assets/audit_rt_f076.py with item id 1, pinned eb1514ea). Data/reference clause only: nothing here applies the
/// engine modification. Field names from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence.
/// The BLO archives are not read (queue C1). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalSuperBoostersTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Boosters => Resolver.Value.Resolve(1);

    [RequiresOriginalAssetsFact]
    public void FullVersionedRecordAndRawPricing()
    {
        var m = Boosters;
        Assert.Equal("ITM/Rogue_mp.itm", m.SourceFile);
        Assert.Equal(18, m.RowNumber);
        Assert.Equal("v57", m.VersionToken);
        Assert.Equal(13, m.RecordType);
        Assert.Equal(137, m.FieldCount);
        Assert.Equal(137, m.Fields.Count);
        Assert.Equal(1, m.Id);
        Assert.Equal("SuperBoosters", m.Name);
        Assert.Equal("Engine Mods", m.Category);
        Assert.Equal("This engine modification boosts Top Speed and HyperThrust output.", m.Description);
        Assert.Equal((7, 0), (m.Mass.FieldIndex, m.Mass.Value));
        Assert.Equal((8, 10000), (m.Buy.FieldIndex, m.Buy.Value));
        Assert.Equal((15, 7500), (m.Sell.FieldIndex, m.Sell.Value));
        Assert.False(m.Sell.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsFact]
    public void SkillLogicNamesFiveHullsWithNoPercentReference()
    {
        var m = Boosters;
        Assert.Equal("10|1|3|15|16", m.LogicRaw);
        Assert.Equal([10, 1, 3, 15, 16], m.SkillReferences.Select(r => r.SkillId));
        Assert.Equal(["Zag'Gir", "Skithist", "Kragath", "Excelsior", "Ravager"], m.SkillReferences.Select(r => r.Hull!.Name));
        Assert.Empty(m.PercentReferences);
        Assert.Null(m.ExchangeLogicCopy);
        Assert.Null(m.ExchangeTargetSkill);
        Assert.Null(m.ExchangeTargetHull);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "SkillLogic" or "SkillId");
    }

    [RequiresOriginalAssetsFact]
    public void HeldCategoryFourteenIsAnUnprovenCandidateWithCfgRowFourteen()
    {
        var m = Boosters;
        var h = m.HeldCategory!;
        Assert.Equal((20, 14), (h.CategoryConstant.FieldIndex, h.CategoryConstant.Value));
        Assert.Equal((14, 1, -1), (h.Limit!.CategoryId, h.Limit.Limit, h.Limit.ExtendedLimit));
        Assert.Equal(2993, h.Limit.LimitLine);
        var u = Assert.Single(m.Unresolved, x => x.Kind == "HeldCategoryMapping");
        Assert.Equal(20, u.FieldIndex);
        Assert.Contains("not established", u.Reason);
    }

    [RequiresOriginalAssetsFact]
    public void BothAssetPairsAreNoneAndNoBloMemberIsNamed()
    {
        var m = Boosters;
        (int first, string archive, string member, string ints)[] expected =
        [
            (23, "None", "None", "0, 0, 0, 0, 0, 0"),
            (48, "None", "None", "0, 0, 0, 0, 0, 0"),
        ];
        Assert.Equal(expected, m.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
        Assert.All(m.AssetPairs, a => Assert.True(a.IsNone));
        Assert.Empty(m.OtherTextFields);
        Assert.DoesNotContain(m.Unresolved, x => x.Kind == "BloMember");
    }

    [RequiresOriginalAssetsFact]
    public void NothingReferencesTheItemAndFieldCoverageIsHonest()
    {
        var m = Boosters;
        Assert.Empty(m.ReverseReferences);
        Assert.Empty(m.VehicleReferences);
        Assert.Equal(137, m.NamedFields.Count + m.UnnamedFieldIndices.Count);
    }
}
