using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for RT-F085 "Mass Driver" (ITM/Rogue_mp.itm row 10, id 1024, type 6 cannon), resolved with the reusable RT-F076
/// <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an independent read of the pinned file
/// (scripts/original-assets/audit_rt_f076.py with item id 1024, pinned eb1514ea) and the resolver output. Data/reference clause
/// only: nothing here fires, consumes ammo or damages. Field names that come from the InfantryOnline emulator parser are
/// upstream-parser names, not original-executable evidence. The BLO archives and members are not read (queue C1); the LIO is not
/// read (queue G6). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalMassDriverTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Driver => Resolver.Value.Resolve(1024);

    [RequiresOriginalAssetsFact]
    public void FullVersionedRecordAndRawPricing()
    {
        var m = Driver;
        Assert.Equal("ITM/Rogue_mp.itm", m.SourceFile);
        Assert.Equal(10, m.RowNumber);
        Assert.Equal("v57", m.VersionToken);
        Assert.Equal(6, m.RecordType);
        Assert.Equal(227, m.FieldCount);
        Assert.Equal(227, m.Fields.Count);
        Assert.Equal(1024, m.Id);
        Assert.Equal("Mass Driver", m.Name);
        Assert.Equal("Cannon", m.Category);
        Assert.Equal("75mm DPU Mass Driver.", m.Description);
        Assert.Equal((7, 0), (m.Mass.FieldIndex, m.Mass.Value));
        Assert.Equal((8, 5000), (m.Buy.FieldIndex, m.Buy.Value));
        Assert.Equal((15, 3500), (m.Sell.FieldIndex, m.Sell.Value));
        Assert.False(m.Sell.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsFact]
    public void SkillLogicIsFourHullSkillsOnePercentRefAndNoExchange()
    {
        var m = Driver;
        Assert.Equal("(2|8|15|16)!%1", m.LogicRaw);
        Assert.IsType<SkillLogicChain>(m.Logic);
        Assert.Equal([2, 8, 15, 16], m.SkillReferences.Select(s => s.SkillId));
        Assert.Equal(["Asp", "Hornet", "Excelsior", "Ravager"], m.SkillReferences.Select(s => s.Hull!.Name));
        Assert.Equal([1], m.PercentReferences);
        Assert.Null(m.ExchangeLogicCopy);
        Assert.Null(m.ExchangeTargetSkill);
        Assert.Null(m.ExchangeTargetHull);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind == "SkillLogic");
    }

    [RequiresOriginalAssetsFact]
    public void HeldCategoryFiveIsAnUnprovenCandidateWithCfgRowFive()
    {
        var m = Driver;
        var h = m.HeldCategory!;
        Assert.Equal((20, 5), (h.CategoryConstant.FieldIndex, h.CategoryConstant.Value));
        Assert.Equal((5, 1, -1), (h.Limit!.CategoryId, h.Limit.Limit, h.Limit.ExtendedLimit));
        Assert.Equal(2975, h.Limit.LimitLine);
        var u = Assert.Single(m.Unresolved, x => x.Kind == "HeldCategoryMapping");
        Assert.Equal(20, u.FieldIndex);
        Assert.Contains("not established", u.Reason);
    }

    [RequiresOriginalAssetsFact]
    public void AmmoIsRawReferenceToRow134AndThereIsNoExplodeChild()
    {
        var m = Driver;
        Assert.Equal((31, 2014), (m.AmmoItemRaw!.Value.FieldIndex, m.AmmoItemRaw.Value.Value));
        Assert.Equal((142, 0), (m.ExplodeItemRaw!.Value.FieldIndex, m.ExplodeItemRaw.Value.Value));
        Assert.True(m.EventString!.IsEmpty);
        Assert.Equal("", m.Fields[147].Value);
        Assert.Empty(m.MultiItemEntries);
        Assert.NotNull(m.Chain);
        var ammo = m.Chain!.Ammo!;
        Assert.Equal((134, 4, 2014, "Mass Driver Pellets", "Ammo-Projectile"), (ammo.RowNumber, ammo.RecordType, ammo.Id, ammo.Name, ammo.Category));
        Assert.False(m.Chain.AmmoIsSelf);
        Assert.Empty(m.Chain.Children);
        Assert.Empty(m.Chain.ChildEntries);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "AmmoOrChild" or "MultiItemEntry");
    }

    [RequiresOriginalAssetsFact]
    public void AssetPairsAreRawTextWithNoBloRead()
    {
        var m = Driver;
        (int first, string archive, string member, string ints)[] expected =
        [
            (23, "None", "None", "0, 0, 0, 0, 0, 0"),
            (71, "None", "None", "0, 0, 0, 0, 0, 0"),
            (79, "None", "None", "0, 0, 0, 0, 0, 1"),
            (175, "None", "None", "24, 1, 0, 0, 0, 0"),
            (183, "TerranWeapons", "MassDriver", "0, 0, 0, 0, 0, 0"),
            (191, "None", "None", "0, 0, 0, 0, 0, 0"),
            (199, "ExplosionEffects", "bullettrail2", "0, 0, 0, 0, -20, 0"),
            (207, "Ebombs", "EBulletHitA", "0, 0, 5, 0, 0, 0"),
            (215, "WeaponSounds", "gun5", "8, 0"),
            (219, "WeaponSounds", "PBombC", "3, 0"),
            (223, "None", "None", "0, 0"),
        ];
        Assert.Equal(expected, m.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
        Assert.Equal(11, m.AssetPairs.Count);
        Assert.Equal(5, m.AssetPairs.Count(a => !a.IsNone));
        Assert.Equal(226, m.AssetPairs[^1].LastFieldIndex);
        Assert.Empty(m.OtherTextFields);
        var blo = m.Unresolved.Where(x => x.Kind == "BloMember").Select(x => x.Raw).ToList();
        Assert.Equal(["TerranWeapons,MassDriver", "ExplosionEffects,bullettrail2", "Ebombs,EBulletHitA", "WeaponSounds,gun5", "WeaponSounds,PBombC"], blo);
        // the package stores BLO/terranweapons.blo (lower case); the other three archives match exactly; files are Git LFS pointers, never read
        var names = Directory.GetFiles(Path.Combine(OriginalAssets.Root!, "BLO")).Select(Path.GetFileName).ToList();
        Assert.Contains("terranweapons.blo", names);
        Assert.DoesNotContain("TerranWeapons.blo", names);
        Assert.Contains("ExplosionEffects.blo", names);
        Assert.Contains("Ebombs.blo", names);
        Assert.Contains("WeaponSounds.blo", names);
    }

    [RequiresOriginalAssetsFact]
    public void NothingReferencesTheDriverItselfAndFieldCoverageIsHonest()
    {
        var m = Driver;
        // no ITM ammo/explode/entry link, no hull drop item and no event-string name names 1024
        Assert.Empty(m.ReverseReferences);
        Assert.Empty(m.VehicleReferences);
        Assert.Equal(227, m.NamedFields.Count + m.UnnamedFieldIndices.Count);
        Assert.Equal(99, m.NamedFields.Count);
        Assert.Equal(128, m.UnnamedFieldIndices.Count);
        // the named set depends only on the type-6/227 layout
        Assert.Equal(Resolver.Value.Resolve(1004).NamedFields.Keys.Order(), m.NamedFields.Keys.Order());
    }
}
