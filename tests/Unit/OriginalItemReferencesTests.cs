using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for the reusable ITM reference resolver (RT-F076 Plasma Cannon, ITM/Rogue_mp.itm row 1 id 1004). Values come from an
/// independent read of the pinned file (scripts/original-assets/audit_rt_f076.py, pinned eb1514ea). Later single-item entries add
/// one oracle each against the same resolver. BLO members, held-category id mapping and every effect/timing/presentation clause
/// stay unresolved on purpose. Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalItemReferencesTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    private static OriginalItemReferences Of(int id) => Resolver.Value.Resolve(id);

    [RequiresOriginalAssetsFact]
    public void PlasmaCannonRecordAndRawValues()
    {
        var p = Of(1004);
        Assert.Equal("ITM/Rogue_mp.itm", p.SourceFile);
        Assert.Equal(1, p.RowNumber);
        Assert.Equal("v57", p.VersionToken);
        Assert.Equal(6, p.RecordType);
        Assert.Equal(227, p.FieldCount);
        Assert.Equal(227, p.Fields.Count);
        Assert.Equal("Plasma Cannon", p.Name);
        Assert.Equal("Cannon", p.Category);
        Assert.Equal("Cannon firing bursts of high energy plasma.", p.Description);
        Assert.Equal(0, p.Mass.Value);
        Assert.Equal(2000, p.Buy.Value);
        Assert.Equal(1500, p.Sell.Value);
        Assert.False(p.Sell.IsNegativeSentinel);
        Assert.Equal(7, p.Mass.FieldIndex);
        Assert.Equal(8, p.Buy.FieldIndex);
        Assert.Equal(15, p.Sell.FieldIndex);
    }

    [RequiresOriginalAssetsFact]
    public void PlasmaCannonHasNoSkillRestrictionAndNoAmmoOrChild()
    {
        var p = Of(1004);
        Assert.Equal("", p.LogicRaw);
        Assert.IsType<SkillLogicEmpty>(p.Logic);
        Assert.Empty(p.SkillReferences);
        Assert.Empty(p.PercentReferences);
        Assert.Null(p.ExchangeTargetSkill);
        Assert.Equal(0, p.AmmoItemRaw!.Value.Value);
        Assert.Equal(0, p.ExplodeItemRaw!.Value.Value);
        Assert.Equal(31, p.AmmoItemRaw.Value.FieldIndex);
        Assert.Equal(142, p.ExplodeItemRaw.Value.FieldIndex);
        Assert.NotNull(p.Chain);
        Assert.Null(p.Chain!.Ammo);
        Assert.Empty(p.Chain.Children);
        Assert.Empty(p.MultiItemEntries);
        Assert.NotNull(p.EventString);
        Assert.True(p.EventString!.IsEmpty);
        Assert.Empty(p.VehicleReferences);
    }

    [RequiresOriginalAssetsFact]
    public void PlasmaCannonHeldCategoryIsAnUnprovenCandidate()
    {
        var p = Of(1004);
        var h = p.HeldCategory!;
        Assert.Equal(20, h.CategoryConstant.FieldIndex);
        Assert.Equal(5, h.CategoryConstant.Value);
        Assert.Equal((5, 1, -1), (h.Limit!.CategoryId, h.Limit.Limit, h.Limit.ExtendedLimit));
        Assert.Equal(2967 + 8, h.Limit.LimitLine);
        var u = Assert.Single(p.Unresolved, x => x.Kind == "HeldCategoryMapping");
        Assert.Equal(20, u.FieldIndex);
        Assert.Contains("not established", u.Reason);
        Assert.Equal(10, Resolver.Value.UtilitySlots.Default);
    }

    [RequiresOriginalAssetsFact]
    public void PlasmaCannonAssetPairsAreRawTextWithNoBloRead()
    {
        var p = Of(1004);
        (int first, string archive, string member, string ints)[] expected =
        [
            (23, "None", "None", "0, 0, 0, 0, 0, 0"),
            (71, "None", "None", "0, 0, 0, 0, 0, 0"),
            (79, "None", "None", "0, 0, 0, 0, 0, 1"),
            (175, "None", "None", "24, 1, 0, 0, 0, 0"),
            (183, "Ebombs", "EbulletA", "0, 0, 0, 0, 0, 0"),
            (191, "None", "None", "0, 0, 0, 0, 0, 0"),
            (199, "ExplosionEffects", "bullettrail2", "0, 0, 0, 0, -20, 0"),
            (207, "Ebombs", "EBulletHitA", "0, 0, 0, 0, 0, 0"),
            (215, "WeaponSounds", "gun1", "8, 0"),
            (219, "WeaponSounds", "Explode0", "3, 0"),
            (223, "None", "None", "0, 0"),
        ];
        Assert.Equal(expected, p.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
        Assert.Equal(11, p.AssetPairs.Count);
        Assert.Equal(5, p.AssetPairs.Count(a => !a.IsNone));
        Assert.Equal(226, p.AssetPairs[^1].LastFieldIndex);
        Assert.Equal("BLO/Ebombs.blo", p.AssetPairs[4].BloRelativePath);
        Assert.Null(p.AssetPairs[0].BloRelativePath);
        Assert.True(p.AssetPairs[6].TrailingInts[4].IsNegativeSentinel);
        // the three non-None archive files named exist in the package (Git LFS pointers; their content is never read here)
        foreach (var path in p.AssetPairs.Where(a => !a.IsNone).Select(a => a.BloRelativePath!).Distinct())
            Assert.True(File.Exists(Path.Combine(OriginalAssets.Root!, path.Replace('/', Path.DirectorySeparatorChar))), path);
        Assert.Equal(["BLO/Ebombs.blo", "BLO/ExplosionEffects.blo", "BLO/WeaponSounds.blo"], p.AssetPairs.Where(a => !a.IsNone).Select(a => a.BloRelativePath!).Distinct());
        var blo = p.Unresolved.Where(x => x.Kind == "BloMember").ToList();
        Assert.Equal(5, blo.Count);
        Assert.Equal(["Ebombs,EbulletA", "ExplosionEffects,bullettrail2", "Ebombs,EBulletHitA", "WeaponSounds,gun1", "WeaponSounds,Explode0"], blo.Select(x => x.Raw));
        Assert.Empty(p.OtherTextFields);
    }

    [RequiresOriginalAssetsFact]
    public void PlasmaCannonUnresolvedListIsExactlyTheDocumentedSix()
    {
        var p = Of(1004);
        Assert.Equal(6, p.Unresolved.Count);
        Assert.Equal(1, p.Unresolved.Count(x => x.Kind == "HeldCategoryMapping"));
        Assert.Equal(5, p.Unresolved.Count(x => x.Kind == "BloMember"));
    }

    [RequiresOriginalAssetsFact]
    public void PlasmaCannonFieldCoverageIsHonest()
    {
        var p = Of(1004);
        Assert.Equal(99, p.NamedFields.Count);
        Assert.Equal(128, p.UnnamedFieldIndices.Count);
        Assert.Equal(227, p.NamedFields.Count + p.UnnamedFieldIndices.Count);
        Assert.Equal("ItmProjectileFields.ExplodeItem", p.NamedFields[142]);
        Assert.Equal("WeaponChainFields.AmmoItem", p.NamedFields[31]);
        Assert.DoesNotContain(9, p.NamedFields.Keys);
        Assert.Contains(9, p.UnnamedFieldIndices);
        // the unnamed fields keep their raw values in the record
        Assert.Equal("44800", p.Fields[17].Value);
        Assert.Equal("60000", p.Fields[36].Value);
        Assert.Equal("2200", p.Fields[101].Value);
    }

    [RequiresOriginalAssetsFact]
    public void PlasmaCannonIsReferencedOnlyByFourHullsStartingLoadouts()
    {
        var rev = Of(1004).ReverseReferences;
        Assert.Equal(4, rev.Count);
        Assert.All(rev, r =>
        {
            Assert.Equal(OriginalItemReverseKinds.EventItemName, r.Kind);
            Assert.Equal("VEH/Rogue.veh", r.SourceFile);
            Assert.Equal(129, r.FieldIndex);
            Assert.Equal("addinv=Plasma Cannon (exact name; EventString1)", r.Detail);
        });
        Assert.Equal([(18, 2000, "Hornet"), (20, 2002, "Asp"), (28, 2011, "Excelsior"), (29, 2012, "Ravager")], rev.Select(r => (r.Row, r.RecordId!.Value, r.RecordName)));
        // cross-check against the independent RT-F035 starting-loadout view
        var hulls = OriginalHullCatalog.Load(OriginalAssets.Root!);
        var items = OriginalItemCatalog.Load(OriginalAssets.Root!);
        Assert.Equal(rev.Select(r => r.RecordId!.Value).Order(), OriginalStartingLoadout.All(hulls, items).Where(l => l.Item?.Id == 1004).Select(l => l.Hull.VehicleId).Order());
        // no item names it as ammo, child or sub-projectile; no hull drops it
        Assert.DoesNotContain(rev, r => r.Kind is OriginalItemReverseKinds.AmmoOf or OriginalItemReverseKinds.ExplodeChildOf or OriginalItemReverseKinds.MultiItemEntryOf or OriginalItemReverseKinds.HullDropItem);
    }

    // ---- generality: the same resolver on other row shapes (used by later single-item entries) ----

    [RequiresOriginalAssetsFact]
    public void MirvLauncherResolvesSkillsAmmoChildAndReverseReferences()
    {
        var m = Of(1047);
        Assert.Equal("16|15", m.LogicRaw);
        Assert.Equal([16, 15], m.SkillReferences.Select(s => s.SkillId));
        Assert.Equal(["Ravager", "Excelsior"], m.SkillReferences.Select(s => s.Hull!.Name));
        Assert.Equal(2002, m.Chain!.Ammo!.Id);
        Assert.Equal(3008, m.Chain.Children.Single().Id);
        Assert.Equal(9, m.HeldCategory!.CategoryConstant.Value);
        Assert.Equal(14000, m.Buy.Value);
        Assert.Equal(5000, m.Sell.Value);

        var ammo = Of(2002);
        Assert.Equal(4, ammo.RecordType);
        Assert.Null(ammo.AmmoItemRaw);
        Assert.Null(ammo.Chain);
        Assert.Equal([(OriginalItemReverseKinds.AmmoOf, 6, 1047)], ammo.ReverseReferences.Select(r => (r.Kind, r.Row, r.RecordId!.Value)));

        var blast = Of(3008);
        Assert.Equal(8, blast.RecordType);
        Assert.Equal([1045, 1048, 1045, 1046, 1049, 1046], blast.MultiItemEntries.Select(e => e.ItemId));
        Assert.Equal([(OriginalItemReverseKinds.ExplodeChildOf, 6, 1047)], blast.ReverseReferences.Select(r => (r.Kind, r.Row, r.RecordId!.Value)));

        var child = Of(1045);
        Assert.Equal([OriginalItemReverseKinds.MultiItemEntryOf, OriginalItemReverseKinds.MultiItemEntryOf], child.ReverseReferences.Select(r => r.Kind));
        Assert.Equal([97, 109], child.ReverseReferences.Select(r => r.FieldIndex!.Value));
        Assert.All(child.ReverseReferences, r => Assert.Equal(3008, r.RecordId));
    }

    [RequiresOriginalAssetsFact]
    public void MassDriverLogicNamesFourHullsAndAPercentReference()
    {
        var d = Of(1024);
        Assert.Equal("(2|8|15|16)!%1", d.LogicRaw);
        Assert.Equal(["Asp", "Hornet", "Excelsior", "Ravager"], d.SkillReferences.Select(s => s.Hull!.Name));
        Assert.Equal([1], d.PercentReferences);
        Assert.DoesNotContain(d.Unresolved, u => u.Kind == "SkillId");
    }

    [RequiresOriginalAssetsFact]
    public void HullExchangeRowResolvesItsTargetHull()
    {
        var x = Resolver.Value.Resolve(OriginalItemCatalog.Load(OriginalAssets.Root!).Items.Single(i => i.RowNumber == 181));
        Assert.Equal(16, x.RecordType);
        Assert.Equal("Hornet", x.Name);
        Assert.Equal("2|15|16", x.LogicRaw);
        Assert.Equal("2|15|16", x.ExchangeLogicCopy);
        Assert.Equal(8, x.ExchangeTargetSkill!.Value.Value);
        Assert.Equal("Hornet", x.ExchangeTargetHull!.Name);
        Assert.Equal(15000, x.Buy.Value);
        Assert.Equal(0, x.Sell.Value);
    }

    [RequiresOriginalAssetsFact]
    public void EveryItemResolvesAndEveryReverseReferenceIsConsistent()
    {
        var items = OriginalItemCatalog.Load(OriginalAssets.Root!).Items;
        Assert.Equal(200, items.Count);
        var all = items.Select(i => Resolver.Value.Resolve(i)).ToList();
        Assert.All(all, r => Assert.Equal(r.FieldCount, r.NamedFields.Count + r.UnnamedFieldIndices.Count));
        // all 92 type-6/227 rows share the same 11 asset pairs at the same positions
        var projectile = all.Where(r => r.RecordType == 6 && r.FieldCount == 227).ToList();
        Assert.Equal(92, projectile.Count);
        Assert.All(projectile, r => Assert.Equal([23, 71, 79, 175, 183, 191, 199, 207, 215, 219, 223], r.AssetPairs.Select(a => a.FirstFieldIndex)));
        Assert.All(projectile, r => Assert.NotNull(r.Chain));
        // every ammo/child reference of the pinned file resolves (no AmmoOrChild entry anywhere)
        Assert.DoesNotContain(all.SelectMany(r => r.Unresolved), u => u.Kind is "AmmoOrChild" or "MultiItemEntry" or "SkillLogic" or "ExchangeTargetSkill");
        // reverse references: every direct f[31]/f[142] reference is found from the target
        var ammoRefs = all.Sum(r => r.ReverseReferences.Count(x => x.Kind == OriginalItemReverseKinds.AmmoOf));
        Assert.Equal(20, ammoRefs);
        var hullDrops = all.Sum(r => r.ReverseReferences.Count(x => x.Kind == OriginalItemReverseKinds.HullDropItem));
        Assert.Equal(12, hullDrops);
        Assert.Equal(12, all.Single(r => r.Id == 2008).ReverseReferences.Count(x => x.Kind == OriginalItemReverseKinds.HullDropItem));
        // all 12 hull starting loadouts (addinv=...) resolve to some item
        var addinv = all.Sum(r => r.ReverseReferences.Count(x => x.Kind == OriginalItemReverseKinds.EventItemName && x.Detail.StartsWith("addinv=")));
        Assert.Equal(12, addinv);
    }
}
