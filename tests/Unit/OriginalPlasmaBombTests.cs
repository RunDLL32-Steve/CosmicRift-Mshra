using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for RT-F082 "Plasma Bomb" (ITM/Rogue_mp.itm row 7, id 1026), resolved with the reusable RT-F076
/// <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an independent read of the pinned file
/// (scripts/original-assets/audit_rt_f076.py with item id 1026, pinned eb1514ea). Data/reference clause only: nothing here fires, spawns
/// or damages. Field names that come from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence.
/// The BLO archives and members are not read (queue C1); the LIO is not read (queue G6). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalPlasmaBombTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Bomb => Resolver.Value.Resolve(1026);

    [RequiresOriginalAssetsFact]
    public void FullVersionedRecordAndRawPricing()
    {
        var m = Bomb;
        Assert.Equal("ITM/Rogue_mp.itm", m.SourceFile);
        Assert.Equal(7, m.RowNumber);
        Assert.Equal("v57", m.VersionToken);
        Assert.Equal(6, m.RecordType);
        Assert.Equal(227, m.FieldCount);
        Assert.Equal(227, m.Fields.Count);
        Assert.Equal(1026, m.Id);
        Assert.Equal("Plasma Bomb", m.Name);
        Assert.Equal("Energy Bomb Projectors", m.Category);
        Assert.Equal("Energy Bomb Launcher.", m.Description);
        Assert.Equal((7, 0), (m.Mass.FieldIndex, m.Mass.Value));
        Assert.Equal((8, 5000), (m.Buy.FieldIndex, m.Buy.Value));
        Assert.Equal((15, 3500), (m.Sell.FieldIndex, m.Sell.Value));
        Assert.False(m.Sell.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsFact]
    public void SkillLogicIsOnlyAPercentReferenceWithNoHullSkills()
    {
        var m = Bomb;
        Assert.Equal("!%1", m.LogicRaw);
        Assert.Empty(m.SkillReferences);
        Assert.Equal([1], m.PercentReferences);
        Assert.Null(m.ExchangeLogicCopy);
        Assert.Null(m.ExchangeTargetSkill);
        Assert.Null(m.ExchangeTargetHull);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "SkillLogic" or "SkillId");
    }

    [RequiresOriginalAssetsFact]
    public void HeldCategorySixIsAnUnprovenCandidateWithCfgRowSix()
    {
        var m = Bomb;
        var h = m.HeldCategory!;
        Assert.Equal((20, 6), (h.CategoryConstant.FieldIndex, h.CategoryConstant.Value));
        Assert.Equal((6, 1, -1), (h.Limit!.CategoryId, h.Limit.Limit, h.Limit.ExtendedLimit));
        Assert.Equal(2977, h.Limit.LimitLine);
        var u = Assert.Single(m.Unresolved, x => x.Kind == "HeldCategoryMapping");
        Assert.Equal(20, u.FieldIndex);
        Assert.Contains("not established", u.Reason);
    }

    [RequiresOriginalAssetsFact]
    public void AmmoAndExplodeAreZeroSoThereIsNoChain()
    {
        var m = Bomb;
        Assert.Equal((31, 0), (m.AmmoItemRaw!.Value.FieldIndex, m.AmmoItemRaw.Value.Value));
        Assert.Equal((142, 0), (m.ExplodeItemRaw!.Value.FieldIndex, m.ExplodeItemRaw.Value.Value));
        Assert.True(m.EventString!.IsEmpty);
        Assert.Equal("", m.Fields[147].Value);
        Assert.NotNull(m.Chain);
        Assert.Null(m.Chain!.Ammo);
        Assert.False(m.Chain.AmmoIsSelf);
        Assert.Empty(m.Chain.Children);
        Assert.Empty(m.Chain.ChildEntries);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "AmmoOrChild" or "MultiItemEntry");
    }

    [RequiresOriginalAssetsFact]
    public void AssetPairsAreRawTextWithNoBloRead()
    {
        var m = Bomb;
        (int first, string archive, string member, string ints)[] expected =
        [
            (23, "None", "None", "0, 0, 0, 0, 0, 0"),
            (71, "None", "None", "0, 0, 0, 0, 0, 0"),
            (79, "None", "None", "0, 0, 0, 0, 0, 1"),
            (175, "None", "None", "24, 1, 0, 0, 0, 0"),
            (183, "WeaponEffects", "EnergyBoltD", "0, 0, 0, 0, -5, 30"),
            (191, "None", "None", "0, 0, 0, 0, 0, 0"),
            (199, "Ebombs", "EBombTrail", "0, 0, 0, 20, -5, 50"),
            (207, "ExplosionEffects", "bombhit2_", "0, 0, 95, 0, 0, 80"),
            (215, "WeaponSounds", "bomb2", "4, 0"),
            (219, "WeaponSounds", "Explode1", "3, 0"),
            (223, "None", "None", "0, 0"),
        ];
        Assert.Equal(expected, m.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
        Assert.Equal(11, m.AssetPairs.Count);
        Assert.Equal(5, m.AssetPairs.Count(a => !a.IsNone));
        Assert.Equal(226, m.AssetPairs[^1].LastFieldIndex);
        Assert.Empty(m.OtherTextFields);
        var blo = m.Unresolved.Where(x => x.Kind == "BloMember").Select(x => x.Raw).ToList();
        Assert.Equal(["WeaponEffects,EnergyBoltD", "Ebombs,EBombTrail", "ExplosionEffects,bombhit2_", "WeaponSounds,bomb2", "WeaponSounds,Explode1"], blo);
        // the package stores BLO/weaponeffects.blo (lower case); the other three archives match exactly; files are Git LFS pointers, never read
        var names = Directory.GetFiles(Path.Combine(OriginalAssets.Root!, "BLO")).Select(Path.GetFileName).ToList();
        Assert.Contains("weaponeffects.blo", names);
        Assert.DoesNotContain("WeaponEffects.blo", names);
        Assert.Contains("Ebombs.blo", names);
        Assert.Contains("ExplosionEffects.blo", names);
        Assert.Contains("WeaponSounds.blo", names);
    }

    [RequiresOriginalAssetsFact]
    public void NothingReferencesTheBombAndFieldCoverageIsHonest()
    {
        var m = Bomb;
        Assert.Empty(m.ReverseReferences);
        Assert.Empty(m.VehicleReferences);
        Assert.Equal(227, m.NamedFields.Count + m.UnnamedFieldIndices.Count);
        Assert.Equal(Resolver.Value.Resolve(1004).NamedFields.Keys.Order(), m.NamedFields.Keys.Order());
    }
}
