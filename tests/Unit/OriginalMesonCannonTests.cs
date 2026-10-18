using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for RT-F087 "Meson Cannon" (ITM/Rogue_mp.itm row 12, id 1084, type 6), resolved with the reusable RT-F076
/// <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an independent read of the pinned file
/// (scripts/original-assets/audit_rt_f076.py with item id 1084, pinned eb1514ea). Data/reference clause only: nothing here fires, spawns
/// or damages. Field names that come from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence.
/// The BLO archives and members are not read (queue C1); the LIO is not read (queue G6). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalMesonCannonTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Cannon => Resolver.Value.Resolve(1084);

    [RequiresOriginalAssetsFact]
    public void FullVersionedRecordAndRawPricing()
    {
        var m = Cannon;
        Assert.Equal("ITM/Rogue_mp.itm", m.SourceFile);
        Assert.Equal(12, m.RowNumber);
        Assert.Equal("v57", m.VersionToken);
        Assert.Equal(6, m.RecordType);
        Assert.Equal(227, m.FieldCount);
        Assert.Equal(227, m.Fields.Count);
        Assert.Equal(1084, m.Id);
        Assert.Equal("Meson Cannon", m.Name);
        Assert.Equal("Cannon", m.Category);
        Assert.Equal("Meson Energy Bomb", m.Description);
        Assert.Equal((7, 0), (m.Mass.FieldIndex, m.Mass.Value));
        Assert.Equal((8, 9000), (m.Buy.FieldIndex, m.Buy.Value));
        Assert.Equal((15, 7500), (m.Sell.FieldIndex, m.Sell.Value));
        Assert.False(m.Sell.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsFact]
    public void SkillLogicIsOnlyAPercentReferenceWithNoHullSkills()
    {
        var m = Cannon;
        Assert.Equal("!%1", m.LogicRaw);
        Assert.Empty(m.SkillReferences);
        Assert.Equal([1], m.PercentReferences);
        Assert.Null(m.ExchangeLogicCopy);
        Assert.Null(m.ExchangeTargetSkill);
        Assert.Null(m.ExchangeTargetHull);
        Assert.DoesNotContain(m.Unresolved, u => u.Kind is "SkillLogic" or "SkillId");
    }

    [RequiresOriginalAssetsFact]
    public void HeldCategoryFiveIsAnUnprovenCandidateWithCfgRowFive()
    {
        var m = Cannon;
        var h = m.HeldCategory!;
        Assert.Equal((20, 5), (h.CategoryConstant.FieldIndex, h.CategoryConstant.Value));
        Assert.Equal((5, 1, -1), (h.Limit!.CategoryId, h.Limit.Limit, h.Limit.ExtendedLimit));
        Assert.Equal(2975, h.Limit.LimitLine);
        var u = Assert.Single(m.Unresolved, x => x.Kind == "HeldCategoryMapping");
        Assert.Equal(20, u.FieldIndex);
        Assert.Contains("not established", u.Reason);
    }

    [RequiresOriginalAssetsFact]
    public void AmmoAndExplodeAreZeroSoThereIsNoChain()
    {
        var m = Cannon;
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
        var m = Cannon;
        (int first, string archive, string member, string ints)[] expected =
        [
            (23, "None", "None", "0, 0, 0, 0, 0, 0"),
            (71, "None", "None", "0, 0, 0, 0, 0, 0"),
            (79, "None", "None", "0, 0, 0, 0, 0, 1"),
            (175, "None", "None", "24, 1, 0, 0, 0, 0"),
            (183, "Ebombs", "EnergyBombI2", "0, 0, 45, 0, -5, 0"),
            (191, "None", "None", "0, 0, 0, 0, 0, 0"),
            (199, "Ebombs", "EBombTrail2", "0, 0, 45, 0, -5, 50"),
            (207, "ExplosionEffects", "bombHit1b", "0, 0, 40, 0, 0, 0"),
            (215, "WeaponSounds", "bomb1", "4, 0"),
            (219, "WeaponSounds", "explode0", "3, 0"),
            (223, "None", "None", "0, 0"),
        ];
        Assert.Equal(expected, m.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
        Assert.Equal(11, m.AssetPairs.Count);
        Assert.Equal(5, m.AssetPairs.Count(a => !a.IsNone));
        Assert.Equal(226, m.AssetPairs[^1].LastFieldIndex);
        Assert.Empty(m.OtherTextFields);
        var blo = m.Unresolved.Where(x => x.Kind == "BloMember").Select(x => x.Raw).ToList();
        Assert.Equal(["Ebombs,EnergyBombI2", "Ebombs,EBombTrail2", "ExplosionEffects,bombHit1b", "WeaponSounds,bomb1", "WeaponSounds,explode0"], blo);
        // all three archives are stored under exactly the case the item names; files are Git LFS pointers, never read
        var names = Directory.GetFiles(Path.Combine(OriginalAssets.Root!, "BLO")).Select(Path.GetFileName).ToList();
        Assert.Contains("Ebombs.blo", names);
        Assert.Contains("ExplosionEffects.blo", names);
        Assert.Contains("WeaponSounds.blo", names);
    }

    [RequiresOriginalAssetsFact]
    public void NothingReferencesTheCannonAndFieldCoverageIsHonest()
    {
        var m = Cannon;
        Assert.Empty(m.ReverseReferences);
        Assert.Empty(m.VehicleReferences);
        Assert.Equal(227, m.NamedFields.Count + m.UnnamedFieldIndices.Count);
        Assert.Equal(Resolver.Value.Resolve(1004).NamedFields.Keys.Order(), m.NamedFields.Keys.Order());
    }

    [RequiresOriginalAssetsFact]
    public void ReleaseNotesNameTheCannonOnlyAsAThreeRoundBurstSwitch()
    {
        // NWS/Rogue.nws line 318 is the only mention of "Meson" (raw text, not parsed by the C# reader); the audit's cannon-category
        // lines do not include it. The note says nothing about numbers, so no burst parameter is derived from it here.
        var lines = File.ReadAllLines(Path.Combine(OriginalAssets.Root!, "NWS", "Rogue.nws"));
        var hits = lines.Select((t, i) => (t, line: i + 1)).Where(x => x.t.Contains("Meson", StringComparison.OrdinalIgnoreCase)).ToList();
        var hit = Assert.Single(hits);
        Assert.Equal(318, hit.line);
        Assert.Equal("Meson Cannon was switched to a 3 round burst format.", hit.t.Trim());
    }
}
