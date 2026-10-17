using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for RT-F081 "MIRV Missile Launcher" (ITM/Rogue_mp.itm row 6, id 1047), resolved with the reusable RT-F076
/// <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an independent read of the pinned file
/// (scripts/original-assets/audit_rt_f076.py with item id 1047, pinned eb1514ea). Data/reference clause only: nothing here fires, spawns
/// or damages. Field names that come from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence.
/// The BLO archives and members are not read (queue C1); the LIO is not read (queue G6). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalMirvLauncherTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Launcher => Resolver.Value.Resolve(1047);

    [RequiresOriginalAssetsFact]
    public void FullVersionedRecordAndRawPricing()
    {
        var m = Launcher;
        Assert.Equal("ITM/Rogue_mp.itm", m.SourceFile);
        Assert.Equal(6, m.RowNumber);
        Assert.Equal("v57", m.VersionToken);
        Assert.Equal(6, m.RecordType);
        Assert.Equal(227, m.FieldCount);
        Assert.Equal(227, m.Fields.Count);
        Assert.Equal(1047, m.Id);
        Assert.Equal("MIRV Missile Launcher", m.Name);
        Assert.Equal("Rocket/Missile Systems", m.Category);
        Assert.Equal("Launch System firing missiles packed with mini missiles.", m.Description);
        Assert.Equal((7, 0), (m.Mass.FieldIndex, m.Mass.Value));
        Assert.Equal((8, 14000), (m.Buy.FieldIndex, m.Buy.Value));
        Assert.Equal((15, 5000), (m.Sell.FieldIndex, m.Sell.Value));
        Assert.False(m.Sell.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsFact]
    public void SkillLogicIsTwoHullSkillsAndNoPercentOrExchange()
    {
        var m = Launcher;
        Assert.Equal("16|15", m.LogicRaw);
        Assert.IsType<SkillLogicChain>(m.Logic);
        Assert.Equal([16, 15], m.SkillReferences.Select(s => s.SkillId));
        Assert.Equal(["Ravager", "Excelsior"], m.SkillReferences.Select(s => s.Hull!.Name));
        Assert.Empty(m.PercentReferences);
        Assert.Null(m.ExchangeLogicCopy);
        Assert.Null(m.ExchangeTargetSkill);
        Assert.Null(m.ExchangeTargetHull);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind == "SkillLogic");
    }

    [RequiresOriginalAssetsFact]
    public void HeldCategoryNineIsAnUnprovenCandidateWithCfgRowNine()
    {
        var m = Launcher;
        var h = m.HeldCategory!;
        Assert.Equal((20, 9), (h.CategoryConstant.FieldIndex, h.CategoryConstant.Value));
        Assert.Equal((9, 1, -1), (h.Limit!.CategoryId, h.Limit.Limit, h.Limit.ExtendedLimit));
        Assert.Equal(2975 + 8, h.Limit.LimitLine);
        var u = Assert.Single(m.Unresolved, x => x.Kind == "HeldCategoryMapping");
        Assert.Equal(20, u.FieldIndex);
        Assert.Contains("not established", u.Reason);
    }

    [RequiresOriginalAssetsFact]
    public void AmmoAndExplodeChainAreRawReferencesToRows82And16()
    {
        var m = Launcher;
        Assert.Equal((31, 2002), (m.AmmoItemRaw!.Value.FieldIndex, m.AmmoItemRaw.Value.Value));
        Assert.Equal((142, 3008), (m.ExplodeItemRaw!.Value.FieldIndex, m.ExplodeItemRaw.Value.Value));
        Assert.True(m.EventString!.IsEmpty);
        Assert.Equal("", m.Fields[147].Value);
        Assert.NotNull(m.Chain);
        var ammo = m.Chain!.Ammo!;
        Assert.Equal((82, 4, "MIRV Missiles", "Ammo-Rocket/Missile"), (ammo.RowNumber, ammo.RecordType, ammo.Name, ammo.Category));
        Assert.False(m.Chain.AmmoIsSelf);
        var blast = Assert.Single(m.Chain.Children);
        Assert.Equal((16, 8, 3008, "MIRV Missile-Blast"), (blast.RowNumber, blast.RecordType, blast.Id, blast.Name));
        Assert.Equal([1045, 1048, 1045, 1046, 1049, 1046], m.Chain.ChildEntries.Select(e => e.ItemId));
        Assert.Equal([-5, -10, -15, 5, 10, 15], m.Chain.ChildEntries.Select(e => e.RawOffset));
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "AmmoOrChild" or "MultiItemEntry");
    }

    [RequiresOriginalAssetsFact]
    public void AssetPairsAreRawTextWithNoBloRead()
    {
        var m = Launcher;
        (int first, string archive, string member, string ints)[] expected =
        [
            (23, "None", "None", "0, 0, 85, 0, 0, 300"),
            (71, "None", "None", "0, 0, 0, 0, 0, 0"),
            (79, "None", "None", "0, 0, 0, 0, 0, 1"),
            (175, "None", "None", "24, 1, 0, 0, 0, 0"),
            (183, "TerranWeapons", "MirvMissile", "0, 0, 95, 0, 0, 0"),
            (191, "None", "None", "0, 0, 0, 0, 0, 0"),
            (199, "ExplosionEffects", "contrail2", "0, 0, 95, -5, -5, 75"),
            (207, "ExplosionEffects", "canonXplosionSm", "0, 0, 0, 0, 0, 80"),
            (215, "WeaponSounds", "missile2", "4, 0"),
            (219, "WeaponSounds", "Explode1", "3, 0"),
            (223, "None", "None", "0, 0"),
        ];
        Assert.Equal(expected, m.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
        Assert.Equal(11, m.AssetPairs.Count);
        Assert.Equal(5, m.AssetPairs.Count(a => !a.IsNone));
        Assert.Equal(226, m.AssetPairs[^1].LastFieldIndex);
        Assert.Empty(m.OtherTextFields);
        var blo = m.Unresolved.Where(x => x.Kind == "BloMember").Select(x => x.Raw).ToList();
        Assert.Equal(["TerranWeapons,MirvMissile", "ExplosionEffects,contrail2", "ExplosionEffects,canonXplosionSm", "WeaponSounds,missile2", "WeaponSounds,Explode1"], blo);
        // the package stores BLO/terranweapons.blo (lower case); the other two archives match exactly; files are Git LFS pointers, never read
        var names = Directory.GetFiles(Path.Combine(OriginalAssets.Root!, "BLO")).Select(Path.GetFileName).ToList();
        Assert.Contains("terranweapons.blo", names);
        Assert.DoesNotContain("TerranWeapons.blo", names);
        Assert.Contains("ExplosionEffects.blo", names);
        Assert.Contains("WeaponSounds.blo", names);
    }

    [RequiresOriginalAssetsFact]
    public void NothingReferencesTheLauncherItselfAndFieldCoverageIsHonest()
    {
        var m = Launcher;
        // no ITM ammo/explode/entry link, no hull drop item and no event-string name names 1047
        Assert.Empty(m.ReverseReferences);
        Assert.Empty(m.VehicleReferences);
        Assert.Equal(227, m.NamedFields.Count + m.UnnamedFieldIndices.Count);
        // the same 99/128 split the four child rows show: the named set depends only on the type-6/227 layout
        Assert.Equal(Resolver.Value.Resolve(1004).NamedFields.Keys.Order(), m.NamedFields.Keys.Order());
    }
}
