using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for RT-F083 "Repulsor" (ITM/Rogue_mp.itm row 8, id 101), resolved with the reusable RT-F076
/// <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an independent read of the pinned file
/// (scripts/original-assets/audit_rt_f076.py with item id 101, pinned eb1514ea). Data/reference clause only: nothing here fires, spawns
/// or damages. Field names that come from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence.
/// The BLO archives and members are not read (queue C1); the LIO is not read (queue G6). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalRepulsorTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Repulsor => Resolver.Value.Resolve(101);

    [RequiresOriginalAssetsFact]
    public void FullVersionedRecordAndSentinelPricing()
    {
        var m = Repulsor;
        Assert.Equal("ITM/Rogue_mp.itm", m.SourceFile);
        Assert.Equal(8, m.RowNumber);
        Assert.Equal("v57", m.VersionToken);
        Assert.Equal(6, m.RecordType);
        Assert.Equal(227, m.FieldCount);
        Assert.Equal(227, m.Fields.Count);
        Assert.Equal(101, m.Id);
        Assert.Equal("Repulsor", m.Name);
        Assert.Equal("BaseLimitedUse", m.Category);
        Assert.Equal("Repulses incoming projectiles and enemies.", m.Description);
        Assert.Equal((7, 1000), (m.Mass.FieldIndex, m.Mass.Value));
        Assert.Equal((8, 0), (m.Buy.FieldIndex, m.Buy.Value));
        Assert.Equal((15, -1), (m.Sell.FieldIndex, m.Sell.Value));
        Assert.True(m.Sell.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsFact]
    public void SkillLogicIsEmptyWithNoReferences()
    {
        var m = Repulsor;
        Assert.Equal("", m.LogicRaw);
        Assert.Empty(m.SkillReferences);
        Assert.Empty(m.PercentReferences);
        Assert.Null(m.ExchangeLogicCopy);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "SkillLogic" or "SkillId");
    }

    [RequiresOriginalAssetsFact]
    public void HeldCategoryZeroHasNoCfgRow()
    {
        var m = Repulsor;
        var h = m.HeldCategory!;
        Assert.Equal((20, 0), (h.CategoryConstant.FieldIndex, h.CategoryConstant.Value));
        Assert.Null(h.Limit);
        var u = Assert.Single(m.Unresolved, x => x.Kind == "HeldCategoryMapping");
        Assert.Equal(20, u.FieldIndex);
        Assert.Contains("no CFG [HeldCategory] row has id 0", u.Reason);
    }

    [RequiresOriginalAssetsFact]
    public void AmmoIsItselfAndExplodeIsZero()
    {
        var m = Repulsor;
        Assert.Equal((31, 101), (m.AmmoItemRaw!.Value.FieldIndex, m.AmmoItemRaw.Value.Value));
        Assert.Equal((142, 0), (m.ExplodeItemRaw!.Value.FieldIndex, m.ExplodeItemRaw.Value.Value));
        Assert.True(m.EventString!.IsEmpty);
        Assert.Equal("", m.Fields[147].Value);
        Assert.NotNull(m.Chain);
        Assert.True(m.Chain!.AmmoIsSelf);
        Assert.Equal((8, 101), (m.Chain.Ammo!.RowNumber, m.Chain.Ammo.Id));
        Assert.Empty(m.Chain.Children);
        Assert.Empty(m.Chain.ChildEntries);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "AmmoOrChild" or "MultiItemEntry");
    }

    [RequiresOriginalAssetsFact]
    public void AssetPairsAreRawTextWithNoBloRead()
    {
        var m = Repulsor;
        (int first, string archive, string member, string ints)[] expected =
        [
            (23, "UtilityEffects", "SuperMultiPrize", "0, 0, 15, 5, 0, 150"),
            (71, "None", "None", "0, 0, 0, 0, 0, 0"),
            (79, "None", "None", "0, 0, 0, 0, 0, 1"),
            (175, "None", "None", "0, 0, 0, 0, 0, 0"),
            (183, "None", "None", "0, 0, 0, 0, 0, 0"),
            (191, "None", "None", "0, 0, 0, 0, 0, 0"),
            (199, "None", "None", "0, 0, 0, 0, 0, 0"),
            (207, "UtilityEffects", "RepelB", "48, 0, 0, 0, 0, 80"),
            (215, "None", "None", "3, 0"),
            (219, "UtilitySounds", "Repel", "2, 0"),
            (223, "None", "None", "0, 0"),
        ];
        Assert.Equal(expected, m.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
        Assert.Equal(11, m.AssetPairs.Count);
        Assert.Equal(3, m.AssetPairs.Count(a => !a.IsNone));
        Assert.Equal(226, m.AssetPairs[^1].LastFieldIndex);
        Assert.Empty(m.OtherTextFields);
        var blo = m.Unresolved.Where(x => x.Kind == "BloMember").Select(x => x.Raw).ToList();
        Assert.Equal(["UtilityEffects,SuperMultiPrize", "UtilityEffects,RepelB", "UtilitySounds,Repel"], blo);
        var names = Directory.GetFiles(Path.Combine(OriginalAssets.Root!, "BLO")).Select(Path.GetFileName).ToList();
        Assert.Contains("UtilityEffects.blo", names);
        Assert.Contains("UtilitySounds.blo", names);
    }

    [RequiresOriginalAssetsFact]
    public void OnlyItsOwnAmmoFieldReferencesTheRepulsor()
    {
        var m = Repulsor;
        var r = Assert.Single(m.ReverseReferences);
        Assert.Equal((OriginalItemReverseKinds.AmmoOf, 8, 31, 101), (r.Kind, r.Row, r.FieldIndex, r.RecordId));
        Assert.Equal("self", r.Detail);
        Assert.Empty(m.VehicleReferences);
        Assert.Equal(227, m.NamedFields.Count + m.UnnamedFieldIndices.Count);
        Assert.Equal(Resolver.Value.Resolve(1004).NamedFields.Keys.Order(), m.NamedFields.Keys.Order());
    }
}
