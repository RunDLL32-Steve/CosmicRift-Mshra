using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for RT-F090 "Moltor Mine" (ITM/Rogue_mp.itm row 15, id 1044, type 6, category Space Mines), resolved with the reusable
/// RT-F076 <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an independent read of the pinned file
/// (scripts/original-assets/audit_rt_f076.py with item id 1044, pinned eb1514ea). Data/reference clause only: nothing here lays,
/// triggers or damages. Field names that come from the InfantryOnline emulator parser are upstream-parser names, not original-executable
/// evidence. The BLO archives and members are not read (queue C1); the LIO is not read (queue G6). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalMoltorMineTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Mine => Resolver.Value.Resolve(1044);

    [RequiresOriginalAssetsFact]
    public void FullVersionedRecordAndRawPricing()
    {
        var m = Mine;
        Assert.Equal("ITM/Rogue_mp.itm", m.SourceFile);
        Assert.Equal(15, m.RowNumber);
        Assert.Equal("v57", m.VersionToken);
        Assert.Equal(6, m.RecordType);
        Assert.Equal(227, m.FieldCount);
        Assert.Equal(227, m.Fields.Count);
        Assert.Equal(1044, m.Id);
        Assert.Equal("Moltor Mine", m.Name);
        Assert.Equal("Space Mines", m.Category);
        Assert.Equal("A special mine type used by the Grazi race.  Moltor Core energy is used to charge a crystal lattice capacitor that detonates upon contact.", m.Description);
        Assert.Equal((7, 0), (m.Mass.FieldIndex, m.Mass.Value));
        Assert.Equal((8, 25), (m.Buy.FieldIndex, m.Buy.Value));
        Assert.Equal((15, 25), (m.Sell.FieldIndex, m.Sell.Value));
        Assert.False(m.Sell.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsFact]
    public void SkillLogicNamesFourGraziHullsAndPercentReferenceOne()
    {
        var m = Mine;
        Assert.Equal("(5|6|9|10)&!%1", m.LogicRaw);
        Assert.Equal([5, 6, 9, 10], m.SkillReferences.Select(r => r.SkillId));
        Assert.Equal(["Rak'Kir", "Rip'Pir", "Zap'Pir", "Zag'Gir"], m.SkillReferences.Select(r => r.Hull!.Name));
        Assert.Equal([1], m.PercentReferences);
        Assert.Null(m.ExchangeLogicCopy);
        Assert.Null(m.ExchangeTargetSkill);
        Assert.Null(m.ExchangeTargetHull);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "SkillLogic" or "SkillId");
    }

    [RequiresOriginalAssetsFact]
    public void HeldCategorySevenIsAnUnprovenCandidateWithCfgRowSeven()
    {
        var m = Mine;
        var h = m.HeldCategory!;
        Assert.Equal((20, 7), (h.CategoryConstant.FieldIndex, h.CategoryConstant.Value));
        Assert.Equal((7, 1, -1), (h.Limit!.CategoryId, h.Limit.Limit, h.Limit.ExtendedLimit));
        Assert.Equal(2979, h.Limit.LimitLine);
        var u = Assert.Single(m.Unresolved, x => x.Kind == "HeldCategoryMapping");
        Assert.Equal(20, u.FieldIndex);
        Assert.Contains("not established", u.Reason);
    }

    [RequiresOriginalAssetsFact]
    public void AmmoIsTheMineItselfAndExplodeIsZeroSoThereAreNoChildren()
    {
        var m = Mine;
        Assert.Equal((31, 1044), (m.AmmoItemRaw!.Value.FieldIndex, m.AmmoItemRaw.Value.Value));
        Assert.Equal((142, 0), (m.ExplodeItemRaw!.Value.FieldIndex, m.ExplodeItemRaw.Value.Value));
        Assert.True(m.EventString!.IsEmpty);
        Assert.Equal("", m.Fields[147].Value);
        Assert.NotNull(m.Chain);
        Assert.True(m.Chain!.AmmoIsSelf);
        var ammo = m.Chain.Ammo!;
        Assert.Equal((15, 6, 1044, "Moltor Mine"), (ammo.RowNumber, ammo.RecordType, ammo.Id, ammo.Name));
        Assert.Empty(m.Chain.Children);
        Assert.Empty(m.Chain.ChildEntries);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "AmmoOrChild" or "MultiItemEntry");
    }

    [RequiresOriginalAssetsFact]
    public void AssetPairsAreRawTextWithNoBloRead()
    {
        var m = Mine;
        (int first, string archive, string member, string ints)[] expected =
        [
            (23, "None", "None", "0, 0, 0, 0, 0, 0"),
            (71, "None", "None", "0, 0, 0, 0, 0, 0"),
            (79, "None", "None", "0, 0, 0, 0, 0, 1"),
            (175, "None", "None", "0, 0, 0, 0, 0, 0"),
            (183, "GraziWeapons", "GraziSuperMine", "0, 0, 0, 0, 0, 100"),
            (191, "None", "None", "24, 0, 0, 0, 0, 0"),
            (199, "None", "None", "0, 0, 0, 0, 0, 0"),
            (207, "ExplosionEffects", "bombHit0_", "0, 0, 90, 0, 0, 0"),
            (215, "WeaponSounds", "mine1", "2, 0"),
            (219, "WeaponSounds", "explode2", "3, 0"),
            (223, "None", "None", "2, 0"),
        ];
        Assert.Equal(expected, m.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
        Assert.Equal(11, m.AssetPairs.Count);
        Assert.Equal(4, m.AssetPairs.Count(a => !a.IsNone));
        Assert.Equal(226, m.AssetPairs[^1].LastFieldIndex);
        Assert.Empty(m.OtherTextFields);
        var blo = m.Unresolved.Where(x => x.Kind == "BloMember").Select(x => x.Raw).ToList();
        Assert.Equal(["GraziWeapons,GraziSuperMine", "ExplosionEffects,bombHit0_", "WeaponSounds,mine1", "WeaponSounds,explode2"], blo);
        // the audit reports BLO/GraziWeapons.blo, BLO/ExplosionEffects.blo and BLO/WeaponSounds.blo present; files are Git LFS pointers, never read
        var names = Directory.GetFiles(Path.Combine(OriginalAssets.Root!, "BLO")).Select(Path.GetFileName).ToList();
        Assert.Contains("GraziWeapons.blo", names);
        Assert.Contains("ExplosionEffects.blo", names);
        Assert.Contains("WeaponSounds.blo", names);
    }

    [RequiresOriginalAssetsFact]
    public void OnlyTheMineItselfReferencesTheMineAndFieldCoverageIsHonest()
    {
        var m = Mine;
        // the sole referrer is the mine's own f[31] ammo link; no hull drop item and no event-string name names 1044
        var r = Assert.Single(m.ReverseReferences);
        Assert.Equal(OriginalItemReverseKinds.AmmoOf, r.Kind);
        Assert.Equal(15, r.Row);
        Assert.Equal(31, r.FieldIndex);
        Assert.Equal(1044, r.RecordId);
        Assert.Equal("Moltor Mine", r.RecordName);
        Assert.Empty(m.VehicleReferences);
        Assert.Equal(227, m.NamedFields.Count + m.UnnamedFieldIndices.Count);
        Assert.Equal(Resolver.Value.Resolve(1004).NamedFields.Keys.Order(), m.NamedFields.Keys.Order());
    }
}
