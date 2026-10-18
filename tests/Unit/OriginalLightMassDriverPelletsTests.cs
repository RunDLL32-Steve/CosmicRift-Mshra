using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for RT-F084 "Light Mass Driver Pellets" (ITM/Rogue_mp.itm row 9, id 2000, type 4 ammo), resolved with the reusable
/// RT-F076 <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an independent read of the pinned file
/// (scripts/original-assets/audit_rt_f076.py with item id 2000, pinned eb1514ea). Data/reference clause only: nothing here fires,
/// consumes or prices ammo at runtime. The BLO archives are not read (queue C1). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalLightMassDriverPelletsTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Pellets => Resolver.Value.Resolve(2000);

    [RequiresOriginalAssetsFact]
    public void FullVersionedRecordAndRawPricing()
    {
        var p = Pellets;
        Assert.Equal("ITM/Rogue_mp.itm", p.SourceFile);
        Assert.Equal(9, p.RowNumber);
        Assert.Equal("v57", p.VersionToken);
        Assert.Equal(4, p.RecordType);
        Assert.Equal(31, p.FieldCount);
        Assert.Equal(31, p.Fields.Count);
        Assert.Equal(2000, p.Id);
        Assert.Equal("Light Mass Driver Pellets", p.Name);
        Assert.Equal("Ammo-Projectile", p.Category);
        Assert.Equal("Zero G forged and face hardened pellets for use in a mass driver.", p.Description);
        Assert.Equal((7, 0), (p.Mass.FieldIndex, p.Mass.Value));
        Assert.Equal((8, 1), (p.Buy.FieldIndex, p.Buy.Value));
        Assert.Equal((15, 1), (p.Sell.FieldIndex, p.Sell.Value));
        Assert.False(p.Sell.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsFact]
    public void LogicIsEmptyAndNoChainIsDeclared()
    {
        var p = Pellets;
        Assert.Equal("", p.LogicRaw);
        Assert.Empty(p.SkillReferences);
        Assert.Empty(p.PercentReferences);
        Assert.Null(p.ExchangeLogicCopy);
        Assert.Null(p.ExchangeTargetSkill);
        Assert.Null(p.ExchangeTargetHull);
        Assert.Null(p.AmmoItemRaw);
        Assert.Null(p.ExplodeItemRaw);
        Assert.Null(p.Chain);
        Assert.Empty(p.MultiItemEntries);
    }

    [RequiresOriginalAssetsFact]
    public void OnlyAssetPairIsNoneNone()
    {
        var p = Pellets;
        var pair = Assert.Single(p.AssetPairs);
        Assert.Equal((23, "None", "None"), (pair.FirstFieldIndex, pair.Archive, pair.Member));
        Assert.True(pair.IsNone);
        Assert.Equal(new[] { 0, 0, 0, 0, 0, 0 }, pair.TrailingInts.Select(i => i.Value));
        Assert.Equal(30, pair.LastFieldIndex);
        Assert.Empty(p.OtherTextFields);
        Assert.DoesNotContain(p.Unresolved, u => u.Kind == "BloMember");
    }

    [RequiresOriginalAssetsFact]
    public void OnlyTheLightMassDriverReferencesThePellets()
    {
        var p = Pellets;
        var r = Assert.Single(p.ReverseReferences);
        Assert.Equal(OriginalItemReverseKinds.AmmoOf, r.Kind);
        Assert.Equal(86, r.Row);
        Assert.Equal(31, r.FieldIndex);
        Assert.Equal(1028, r.RecordId);
        Assert.Equal("Light Mass Driver", r.RecordName);
        Assert.Empty(p.VehicleReferences);
        Assert.Equal(31, p.NamedFields.Count + p.UnnamedFieldIndices.Count);
    }
}
