using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for the four MicroMissile child projectile rows (RT-F077 Left1 1045, RT-F078 Left2 1048, RT-F079 Right1 1046, RT-F080 Right2 1049;
/// ITM/Rogue_mp.itm rows 2..5), resolved with the reusable RT-F076 <see cref="OriginalItemReferenceResolver"/> (not modified). Values come from an
/// independent read of the pinned file (scripts/original-assets/audit_rt_f077_f080.py, pinned eb1514ea). Everything is raw: the left/right/1/2
/// symmetry is stated only as which raw fields differ; no angle, offset, speed or direction meaning is read from the names or the numbers.
/// Field names that come from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence. The BLO archives and
/// members are not read (queue C1); the LIO is not read (queue G6). Asset tests are Skipped without assets.
/// </summary>
public sealed class OriginalMicroMissileChildTests
{
    private static readonly Lazy<OriginalItemReferenceResolver> Resolver = new(() => OriginalItemReferenceResolver.Load(OriginalAssets.Root!));

    // featureId, itemId, row, name
    private static readonly (string Feature, int Id, int Row, string Name)[] Children =
    [
        ("RT-F077", 1045, 2, "MicroMissile-Child-Left1"),
        ("RT-F078", 1048, 3, "MicroMissile-Child-Left2"),
        ("RT-F079", 1046, 4, "MicroMissile-Child-Right1"),
        ("RT-F080", 1049, 5, "MicroMissile-Child-Right2"),
    ];

    private static OriginalItemReferences Of(int id) => Resolver.Value.Resolve(id);

    [RequiresOriginalAssetsFact]
    public void EachChildRecordAndRawValues()
    {
        foreach (var (feature, id, row, name) in Children)
        {
            var c = Of(id);
            var why = $"{feature} {name}";
            Assert.True(c.SourceFile == "ITM/Rogue_mp.itm", why);
            Assert.Equal(row, c.RowNumber);
            Assert.Equal("v57", c.VersionToken);
            Assert.Equal(6, c.RecordType);
            Assert.Equal(227, c.FieldCount);
            Assert.Equal(227, c.Fields.Count);
            Assert.Equal(id, c.Id);
            Assert.Equal(name, c.Name);
            Assert.Equal("ChildProjectiles", c.Category);
            Assert.Equal("Low microton fusion warhead.", c.Description);
            Assert.Equal((7, 1000), (c.Mass.FieldIndex, c.Mass.Value));
            Assert.Equal((8, 0), (c.Buy.FieldIndex, c.Buy.Value));
            // sell -1 is kept raw and flagged; its native meaning (not for sale / no resale) is NOT established here
            Assert.Equal((15, -1), (c.Sell.FieldIndex, c.Sell.Value));
            Assert.True(c.Sell.IsNegativeSentinel);
            Assert.Equal("-1", c.Fields[15].Value);
        }
    }

    [RequiresOriginalAssetsFact]
    public void NoSkillRestrictionNoExchangeAndHeldCategoryZeroHasNoCfgRow()
    {
        foreach (var (_, id, _, _) in Children)
        {
            var c = Of(id);
            Assert.Equal("", c.LogicRaw);
            Assert.IsType<SkillLogicEmpty>(c.Logic);
            Assert.Empty(c.SkillReferences);
            Assert.Empty(c.PercentReferences);
            Assert.Null(c.ExchangeLogicCopy);
            Assert.Null(c.ExchangeTargetSkill);
            Assert.Null(c.ExchangeTargetHull);
            var h = c.HeldCategory!;
            Assert.Equal((20, 0), (h.CategoryConstant.FieldIndex, h.CategoryConstant.Value));
            // no CFG [HeldCategory] row has id 0 (rows exist for the ids the weapons use); recorded as unresolved, nothing enforced
            Assert.Null(h.Limit);
            var u = Assert.Single(c.Unresolved, x => x.Kind == "HeldCategoryMapping");
            Assert.Equal(20, u.FieldIndex);
            Assert.Equal("0", u.Raw);
            Assert.Contains("no CFG [HeldCategory] row has id 0", u.Reason);
            Assert.Empty(c.VehicleReferences);
        }
    }

    [RequiresOriginalAssetsFact]
    public void ChildrenHaveNoAmmoNoExplodeChildNoMultiItemAndNoEventString()
    {
        foreach (var (_, id, _, _) in Children)
        {
            var c = Of(id);
            Assert.Equal((31, 0), (c.AmmoItemRaw!.Value.FieldIndex, c.AmmoItemRaw.Value.Value));
            Assert.Equal((142, 0), (c.ExplodeItemRaw!.Value.FieldIndex, c.ExplodeItemRaw.Value.Value));
            Assert.NotNull(c.Chain);
            Assert.Null(c.Chain!.Ammo);
            Assert.False(c.Chain.AmmoIsSelf);
            Assert.Empty(c.Chain.Children);
            Assert.Empty(c.Chain.ChildEntries);
            Assert.Empty(c.MultiItemEntries);
            Assert.True(c.EventString!.IsEmpty);
            Assert.Equal("", c.Fields[147].Value);
        }
    }

    [RequiresOriginalAssetsFact]
    public void AssetPairsAreRawTextAndIdenticalExceptTheFirstEmptyTuplesInts()
    {
        // (first field, archive, member, trailing ints); the first tuple (f[23], None/None) carries the only per-row difference
        (int first, string archive, string member, string ints)[] Expected(string firstInts) =>
        [
            (23, "None", "None", firstInts),
            (71, "None", "None", "0, 0, 0, 0, 0, 0"),
            (79, "None", "None", "0, 0, 0, 0, 0, 1"),
            (175, "None", "None", "24, 1, 0, 0, 0, 0"),
            (183, "TerranWeapons", "MissileF", "0, 0, 95, 0, -5, 0"),
            (191, "None", "None", "0, 0, 0, 0, 0, 0"),
            (199, "ExplosionEffects", "contrail3", "0, 0, 90, -5, -5, 50"),
            (207, "ExplosionEffects", "canonXplosionSm", "0, 0, 90, 0, 0, 80"),
            (215, "WeaponSounds", "missile1", "4, 0"),
            (219, "WeaponSounds", "Explode1", "3, 0"),
            (223, "None", "None", "0, 0"),
        ];
        var firstInts = new Dictionary<int, string>
        {
            [1045] = "0, 0, 0, 0, 0, 0",
            [1048] = "0, 0, 90, 0, 0, 0",
            [1046] = "0, 0, 0, 0, 0, 50",
            [1049] = "0, 0, 0, 0, 0, 0",
        };
        foreach (var (_, id, _, _) in Children)
        {
            var c = Of(id);
            Assert.Equal(Expected(firstInts[id]), c.AssetPairs.Select(a => (a.FirstFieldIndex, a.Archive, a.Member, string.Join(", ", a.TrailingInts.Select(i => i.Value)))));
            Assert.Equal(11, c.AssetPairs.Count);
            Assert.Equal(5, c.AssetPairs.Count(a => !a.IsNone));
            Assert.Equal(226, c.AssetPairs[^1].LastFieldIndex);
            Assert.True(c.AssetPairs[4].TrailingInts[4].IsNegativeSentinel); // f[189] = -5 stays a raw negative
            Assert.Empty(c.OtherTextFields);
        }
    }

    [RequiresOriginalAssetsFact]
    public void BloArchivesAreOnlyLocatedAndOneIsCaseMismatched()
    {
        var c = Of(1045);
        var archives = c.AssetPairs.Where(a => !a.IsNone).Select(a => a.BloRelativePath!).Distinct().ToList();
        Assert.Equal(["BLO/TerranWeapons.blo", "BLO/ExplosionEffects.blo", "BLO/WeaponSounds.blo"], archives);
        // The package stores BLO/terranweapons.blo (lower case) while the ITM names TerranWeapons; Windows resolves it, a case-sensitive file system
        // does not. Located case-insensitively here; the file is a Git LFS pointer and its content (members MissileF, contrail3, canonXplosionSm,
        // missile1, Explode1) is never read.
        var names = Directory.GetFiles(Path.Combine(OriginalAssets.Root!, "BLO")).Select(Path.GetFileName).ToList();
        foreach (var rel in archives)
            Assert.Single(names, n => string.Equals(n, rel[4..], StringComparison.OrdinalIgnoreCase));
        Assert.Contains("terranweapons.blo", names);
        Assert.DoesNotContain("TerranWeapons.blo", names);
        Assert.Contains("ExplosionEffects.blo", names);
        Assert.Contains("WeaponSounds.blo", names);
        // every child lists the same six unresolved members and no other BloMember
        foreach (var (_, id, _, _) in Children)
        {
            var blo = Of(id).Unresolved.Where(x => x.Kind == "BloMember").Select(x => x.Raw).ToList();
            Assert.Equal(["TerranWeapons,MissileF", "ExplosionEffects,contrail3", "ExplosionEffects,canonXplosionSm", "WeaponSounds,missile1", "WeaponSounds,Explode1"], blo);
        }
    }

    [RequiresOriginalAssetsFact]
    public void UnresolvedListIsExactlyTheHeldCategoryAndFiveBloMembers()
    {
        foreach (var (_, id, _, _) in Children)
        {
            var c = Of(id);
            Assert.Equal(6, c.Unresolved.Count);
            Assert.Equal(1, c.Unresolved.Count(x => x.Kind == "HeldCategoryMapping"));
            Assert.Equal(5, c.Unresolved.Count(x => x.Kind == "BloMember"));
        }
    }

    [RequiresOriginalAssetsFact]
    public void FieldCoverageIsHonestAndIdenticalAcrossTheFour()
    {
        foreach (var (_, id, _, _) in Children)
        {
            var c = Of(id);
            Assert.Equal(227, c.NamedFields.Count + c.UnnamedFieldIndices.Count);
            Assert.Equal(Of(1045).NamedFields.Keys.Order(), c.NamedFields.Keys.Order());
            Assert.Equal("ItmProjectileFields.MuzzleVelocity", c.NamedFields[101]);
            Assert.DoesNotContain(113, c.NamedFields.Keys);
            Assert.DoesNotContain(114, c.NamedFields.Keys);
            Assert.Contains(113, c.UnnamedFieldIndices);
            Assert.Contains(114, c.UnnamedFieldIndices);
        }
        Assert.Equal(99, Of(1045).NamedFields.Count);
        Assert.Equal(128, Of(1045).UnnamedFieldIndices.Count);
    }

    [RequiresOriginalAssetsFact]
    public void OnlySevenRawFieldsDifferBetweenTheFourRows()
    {
        var rows = Children.Select(c => Of(c.Id).Fields.Select(f => f.Value).ToArray()).ToList();
        var differing = Enumerable.Range(0, 227).Where(i => rows.Select(r => r[i]).Distinct().Count() > 1).ToList();
        Assert.Equal([2, 3, 27, 30, 101, 113, 114], differing);
        // raw values in id order 1045 (Left1), 1048 (Left2), 1046 (Right1), 1049 (Right2); no meaning is attached
        Assert.Equal(["0", "90", "0", "0"], rows.Select(r => r[27]));
        Assert.Equal(["0", "0", "50", "0"], rows.Select(r => r[30]));
        Assert.Equal(["1500", "1250", "1500", "1250"], rows.Select(r => r[101]));
        Assert.Equal(["-350", "-350", "350", "350"], rows.Select(r => r[113]));
        Assert.Equal(["75", "50", "-75", "-50"], rows.Select(r => r[114]));
        // purely a restatement of the raw numbers: f[113] and f[114] of the Right rows are the negation of the Left rows' values, per pair
        Assert.Equal(["-350", "-350"], new[] { rows[0][113], rows[1][113] });
        Assert.Equal(-int.Parse(rows[0][114]), int.Parse(rows[2][114]));
        Assert.Equal(-int.Parse(rows[1][114]), int.Parse(rows[3][114]));
        Assert.Equal(-int.Parse(rows[0][113]), int.Parse(rows[2][113]));
        // f[101] pairs by the trailing 1/2 of the name: 1 -> 1500, 2 -> 1250, whichever side
        Assert.Equal(rows[0][101], rows[2][101]);
        Assert.Equal(rows[1][101], rows[3][101]);
        // everything else (category, description, mass/buy/sell, logic, ammo/explode, event, all asset pairs outside f[27]/f[30]) is identical
        foreach (var i in Enumerable.Range(0, 227).Except(differing))
            Assert.Single(rows.Select(r => r[i]).Distinct());
    }

    [RequiresOriginalAssetsFact]
    public void ReverseReferencesAreOnlyTheMirvBlastEntryListWithRowAndFieldIndex()
    {
        // (id, [(entry field, raw offset f[+4] via the chain entries)])
        var expected = new Dictionary<int, int[]>
        {
            [1045] = [97, 109],
            [1048] = [103],
            [1046] = [115, 127],
            [1049] = [121],
        };
        foreach (var (feature, id, _, name) in Children)
        {
            var rev = Of(id).ReverseReferences;
            Assert.Equal(expected[id].Length, rev.Count);
            Assert.All(rev, r =>
            {
                Assert.Equal(OriginalItemReverseKinds.MultiItemEntryOf, r.Kind);
                Assert.Equal("ITM/Rogue_mp.itm", r.SourceFile);
                Assert.Equal(16, r.Row);
                Assert.Equal(3008, r.RecordId);
                Assert.Equal("MIRV Missile-Blast", r.RecordName);
            });
            Assert.Equal(expected[id], rev.Select(r => r.FieldIndex!.Value));
            Assert.Equal(expected[id].Select(f => $"f[{f}]={id}"), rev.Select(r => r.Detail));
            // no other kind: not ammo, not an explode child directly, not a hull drop, not named by an event string, not an item name in any addinv
            Assert.DoesNotContain(rev, r => r.Kind is OriginalItemReverseKinds.AmmoOf or OriginalItemReverseKinds.ExplodeChildOf or OriginalItemReverseKinds.HullDropItem or OriginalItemReverseKinds.EventItemName);
            _ = feature; _ = name;
        }
    }

    [RequiresOriginalAssetsFact]
    public void ParentChainIsMirvLauncherToAmmoToBlastEntriesAndBlastEntryOffsetsAreRaw()
    {
        var launcher = Of(1047);
        Assert.Equal((6, "MIRV Missile Launcher"), (launcher.RowNumber, launcher.Name));
        Assert.Equal(2002, launcher.AmmoItemRaw!.Value.Value);
        Assert.Equal(3008, launcher.ExplodeItemRaw!.Value.Value);
        Assert.Equal(3008, launcher.Chain!.Children.Single().Id);
        // the launcher reaches the children only through the blast's entry list (type 8): the four child rows have no f[142] of their own
        var entries = launcher.Chain.ChildEntries;
        Assert.Equal([1045, 1048, 1045, 1046, 1049, 1046], entries.Select(e => e.ItemId));
        Assert.Equal([-5, -10, -15, 5, 10, 15], entries.Select(e => e.RawOffset));
        Assert.Equal(Children.Select(c => c.Name).Concat(["MicroMissile-Child-Left1", "MicroMissile-Child-Right1"]).Order().ToArray(),
            entries.Select(e => e.Item!.Name).Order().ToArray());
        // the reverse-reference field indexes and the chain entry order agree: entry k sits at f[97 + 6k]
        var blast = Of(3008);
        Assert.Equal((16, 8), (blast.RowNumber, blast.RecordType));
        Assert.Equal(Enumerable.Range(0, 6).Select(k => 97 + 6 * k).Zip(entries, (f, e) => (f, e.ItemId)),
            new[] { 1045, 1048, 1045, 1046, 1049, 1046 }.Select((id, k) => (97 + 6 * k, id)));
        foreach (var (_, id, _, _) in Children)
            Assert.All(Of(id).ReverseReferences, r => Assert.Equal(entries[(r.FieldIndex!.Value - 97) / 6].ItemId, id));
        // the blast row is itself reached only from the launcher's f[142]
        Assert.Equal([(OriginalItemReverseKinds.ExplodeChildOf, 6, 1047)], blast.ReverseReferences.Select(r => (r.Kind, r.Row, r.RecordId!.Value)));
        // parent acceptance (RT-F081 launcher, RT-F040 chain) is not claimed by this test: only the references are pinned
    }

    [RequiresOriginalAssetsFact]
    public void ChildrenAreTheFirstFourChildProjectilesRowsAndOnlyTheBlastReferencesThem()
    {
        var items = OriginalItemCatalog.Load(OriginalAssets.Root!).Items;
        // the category holds 27 rows in the pinned file; the four MicroMissile children are its first four (rows 2..5)
        var cp = items.Where(i => i.Category == "ChildProjectiles").ToList();
        Assert.Equal(27, cp.Count);
        Assert.Equal([1045, 1048, 1046, 1049], cp.Take(4).Select(i => i.Id));
        Assert.Equal([2, 3, 4, 5], cp.Take(4).Select(i => i.RowNumber));
        // across every item the only rows with a reverse reference to a child are the single type-8 blast
        var all = Children.SelectMany(c => Of(c.Id).ReverseReferences).ToList();
        Assert.Equal(6, all.Count);
        Assert.Single(all.Select(r => r.RecordId).Distinct());
        // no hull of the VEH catalog names a child as drop item or in a start loadout
        Assert.DoesNotContain(OriginalStartingLoadout.All(OriginalHullCatalog.Load(OriginalAssets.Root!), OriginalItemCatalog.Load(OriginalAssets.Root!)), l => l.Item is { } it && Children.Any(c => c.Id == it.Id));
    }
}
