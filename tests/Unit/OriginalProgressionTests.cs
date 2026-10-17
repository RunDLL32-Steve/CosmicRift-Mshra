using System.Collections.Immutable;
using RogueTrader.OriginalData;
using RogueTrader.Persistence;
using RogueTrader.UnitTests.Persistence;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F068 (evidence and binding only). Oracle values were read from the pinned NWS/Rogue.nws, CFG/rogue.cfg, RPG/Rogue.rpg,
/// VEH/Rogue.veh and ITM/Rogue_mp.itm (eb1514ea). The package holds policy text and event/config strings but no save schema, so the
/// profile id encoding and every save/load here use the RECONSTRUCTED (NEW) persistence contract. The character-wipe and same-race
/// hull-exchange policies are pinned as evidence only; no wipe, exchange, cash or reset rule is implemented or asserted.
/// Asset tests are Skipped without the assets.
/// </summary>
public sealed class OriginalProgressionTests
{
    private static readonly Lazy<OriginalProgressionEvidence> Evidence = new(() => OriginalProgressionEvidence.Load(OriginalAssets.Root!));
    private static readonly Lazy<OriginalProgressionIds> Ids = new(() => OriginalProgressionIds.Load(OriginalAssets.Root!));
    private static readonly Lazy<(OriginalHullCatalog Hulls, OriginalItemCatalog Items)> Catalogs =
        new(() => (OriginalHullCatalog.Load(OriginalAssets.Root!), OriginalItemCatalog.Load(OriginalAssets.Root!)));

    private static ProfileContentValidator Validator(OriginalProgressionIds ids) => new(ids.IsHull, ids.IsItem, ids.IsUpgrade);

    [RequiresOriginalAssetsFact]
    public void CharacterWipeTextIsPinnedUnderTheUndatedOverviewHeading()
    {
        var w = Evidence.Value.CharacterWipe;
        Assert.Equal(new[] { 26, 27, 28, 29 }, w.Select(l => l.Line.LineNumber));
        Assert.Equal(new[]
        {
            "",
            "When a new players begins the game, the first thing he needs to do is select a ship. This ship is essentially",
            "the players character. Once selected a new ship cannot be selected without wiping ones character using ?wipecharacter",
            "and starting fresh.",
        }, w.Select(l => l.Line.Text));
        Assert.All(w, l => Assert.Equal((25, "********~BGame Overview~B*********", (string?)null), (l.Heading.LineNumber, l.Heading.Text, l.HeadingDate)));
    }

    [RequiresOriginalAssetsFact]
    public void SameRaceHullExchangeIsPinnedUnderItsDatedHeading()
    {
        var x = Evidence.Value.HullExchange;
        Assert.Equal(399, x.Line.LineNumber);
        Assert.Equal("Added the ability to purchase a new ship hull. The caveat is you can only change to a ship hull that is the same race as your existing ship hull.", x.Line.Text);
        Assert.Equal((391, "*******9/08/01", "9/08/01"), (x.Heading.LineNumber, x.Heading.Text, x.HeadingDate));
    }

    [RequiresOriginalAssetsFact]
    public void SpecModeLossesArePinnedUnderTheirDatedHeading()
    {
        var s = Evidence.Value.SpecModeLosses;
        Assert.Equal(new[]
        {
            (218, "When you go to spec mode you will lose any unrefined minerals and any wreckage you are carrying."),
            (220, "When you go to spec mode you will lose any unused nanobot packages (factories/sats)."),
        }, s.Select(l => (l.Line.LineNumber, l.Line.Text)));
        Assert.All(s, l => Assert.Equal((215, "3/12/03"), (l.Heading.LineNumber, l.HeadingDate)));
    }

    [RequiresOriginalAssetsFact]
    public void CfgProgressionEntriesArePinnedRaw()
    {
        var e = Evidence.Value;
        Assert.Equal(new[]
        {
            (2146, "PrizeReset", "0"), (2147, "VehicleReset", "0"), (2148, "InitialHides", "0"),
            (2149, "ResetInventory", "0"), (2150, "ResetCharacter", "0"), (2151, "ClearProjectiles", "0"),
        }, e.StartGame.Select(c => (c.LineNumber, c.Key, c.Value)));
        Assert.Equal(new[]
        {
            (2492, "Hold1", "WipeInv=Tellerium,Wipeinv=Iron Ore,Wipeinv=Krandazite,Wipeinv=wreckage,wipeinv=null wreckage"),
            (2493, "Hold2", "Wipeinv=Trading Post,Wipeinv=BeamSat,Wipeinv=GunSat,Wipeinv=Defense Manufactory,Wipeinv=Missile Manufactory,Wipeinv=Munitions Manufactory,Wipeinv=Micro Repair Dock,wipeinv=Micro Refinery"),
            (2497, "SysopWipe", "EventSelfWipe"),
            (2498, "SelfWipe", "wipescore,EventFirstTimeInvSetup,EventFirstTimeSkillSetup"),
            (2499, "FirstTimeInvSetup", "Wipeinv,setcash=0,vehicleEvent"),
            (2502, "KilledByTeam", "reset,warp=0"),
            (2503, "KilledByEnemy", "reset,warp=0"),
            (2504, "EnterSpawnNoScore", "wipescore,EventFirstTimeInvSetup"),
            (2505, "ChangeDefaultVehicle", ""),
            (2508, "FirstTimeSkillSetup", "wipeskill,setexp=1"),
        }, e.ProgressionEvents.Select(c => (c.LineNumber, c.Key, c.Value)));
        Assert.Equal(new[] { (2216, "Wipe", "0"), (2217, "Profile", "0") }, e.OwnerFlags.Select(c => (c.LineNumber, c.Key, c.Value)));
        Assert.Equal(new[] { (86, "DefaultVItemId", "2000") }, e.PublicProfile.Select(c => (c.LineNumber, c.Key, c.Value)));
        Assert.Equal(new[] { (2544, "TrackDaily", "1"), (2545, "TrackWeekly", "1"), (2546, "TrackMonthly", "1"), (2547, "TrackYearly", "1") },
            e.StatTracking.Select(c => (c.LineNumber, c.Key, c.Value)));
    }

    [RequiresOriginalAssetsFact]
    public void RpgField38IsPinnedForAllTwelveHulls()
    {
        var c = Evidence.Value.RpgCashAdjustment;
        Assert.Equal(12, c.Count);
        Assert.All(c, x => Assert.Equal((38, 3499), (x.Value.FieldIndex, x.Value.Value)));
        Assert.Equal("Asp", c[0].Hull.Name);
    }

    [RequiresOriginalAssetsFact]
    public void PackageHoldsNoSaveSchemaKeywordsOutsideCfgAndNews()
    {
        var h = Evidence.Value.KeywordHits;
        Assert.Equal(new[] { "CFG/rogue.cfg", "ITM/Rogue_mp.itm", "NWS/Rogue.nws", "RPG/Rogue.rpg", "VEH/Rogue.veh" }, h.Keys.OrderBy(k => k, StringComparer.Ordinal));
        Assert.Empty(h["RPG/Rogue.rpg"]);
        Assert.Empty(h["VEH/Rogue.veh"]);
        Assert.Empty(h["ITM/Rogue_mp.itm"]);
        Assert.Equal(new[] { 85, 858, 864, 2146, 2147, 2149, 2150, 2216, 2217, 2492, 2493, 2497, 2498, 2499, 2502, 2503, 2504, 2508, 2534 },
            h["CFG/rogue.cfg"].Select(x => x.LineNumber));
        // 2534 is [SoccerMvp] Saves=60: a scoring weight, not a save setting.
        Assert.Equal("Saves=60", h["CFG/rogue.cfg"].Single(x => x.LineNumber == 2534).Text);
        Assert.Equal(new[] { (28, "wipe,character"), (56, "character"), (129, "character"), (171, "character") },
            h["NWS/Rogue.nws"].Select(x => (x.LineNumber, string.Join(",", x.Keywords))));
    }

    [RequiresOriginalAssetsFact]
    public void VisibleMismatchesAreListed()
    {
        var m = Evidence.Value.VisibleMismatches;
        Assert.Equal(7, m.Count);
        Assert.Contains(m, n => n.Contains("which statement is the later policy is unproven", StringComparison.Ordinal));
        Assert.Contains(m, n => n.StartsWith("No CFG line contains 'wipecharacter'", StringComparison.Ordinal));
        Assert.Contains(m, n => n.Contains("ChangeDefaultVehicle='' is empty", StringComparison.Ordinal));
        Assert.Contains(m, n => n.Contains("is 3499 on all 12 hull rows", StringComparison.Ordinal));
        Assert.Contains(m, n => n.Contains("ITM id 2000 is 'Light Mass Driver Pellets' and hull VEH id 2000 is 'Hornet'", StringComparison.Ordinal));
    }

    [RequiresOriginalAssetsFact]
    public void OriginalIdSetsComeFromTheCatalogs()
    {
        var ids = Ids.Value;
        Assert.Equal(new[] { "2000", "2001", "2002", "2003", "2004", "2005", "2006", "2008", "2009", "2010", "2011", "2012" }, ids.HullIds.OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(200, ids.ItemIds.Count);
        Assert.Equal(26, ids.UpgradeIds.Count);
        Assert.Equal(new[] { "1", "10", "11", "12", "14", "15", "2", "20", "2011", "21", "22", "25", "29", "3", "30", "31", "33", "34", "35", "36", "61", "62", "63", "7", "8", "9" },
            ids.UpgradeIds.OrderBy(x => x, StringComparer.Ordinal));
        Assert.True(ids.UpgradeIds.IsSubsetOf(ids.ItemIds));
        // 2011 is both the Excelsior hull (VEH) and the HyperCore item (ITM); the namespaces stay separate.
        Assert.True(ids.IsHull("2011") && ids.IsItem("2011") && ids.IsUpgrade("2011"));
        Assert.Equal("HyperCore", Catalogs.Value.Items.ById(2011).Name);
        Assert.Equal("Excelsior", Catalogs.Value.Hulls.Hulls.Single(h => h.VehicleId == 2011).Name);
    }

    [RequiresOriginalAssetsFact]
    public void OnlyTheCanonicalDecimalFormOfAnOriginalIdIsKnown()
    {
        var v = Validator(Ids.Value);
        foreach (var bad in new[] { "Asp", "asp", "02002", "+2002", " 2002", "2002 ", "2", "2007", "9999" })
            Assert.Equal(new[] { new ProfileContentViolation(ProfileContentField.Hull, bad) }, v.Validate(PlayerProfile.CreateNew("f068-bad", 0, bad)));
        var p = PlayerProfile.CreateNew("f068-bad", 0, "2002") with
        {
            Inventory = Env.Items(("Plasma Cannon", 1), ("01004", 1), ("99999", 1), ("1004", 1)), // NEW fixture quantities
            Upgrades = ImmutableSortedSet.Create(StringComparer.Ordinal, "1004", "2", "TurboBoosters"),
        };
        Assert.Equal(new[]
        {
            new ProfileContentViolation(ProfileContentField.Inventory, "01004"),
            new ProfileContentViolation(ProfileContentField.Inventory, "99999"),
            new ProfileContentViolation(ProfileContentField.Inventory, "Plasma Cannon"),
            new ProfileContentViolation(ProfileContentField.Upgrade, "1004"), // Plasma Cannon is an ITM item, not an RT-F048 upgrade row
            new ProfileContentViolation(ProfileContentField.Upgrade, "TurboBoosters"),
        }, v.Validate(p));
    }

    /// <summary>
    /// Asp (VEH 2002) with its original starting loadout item (VEH f[129] addinv=Plasma Cannon, ITM 1004), an original resource
    /// (ITM 2003 Tellerium) and two original upgrade rows (ITM 2 TurboBoosters, ITM 2011 HyperCore). Quantities and the wallet are
    /// NEW fixtures: f[129] names no count and the package states no starting cash rule.
    /// </summary>
    private static PlayerProfile AspProfile()
    {
        var (hulls, items) = Catalogs.Value;
        var asp = hulls.ByName("Asp");
        var loadout = OriginalStartingLoadout.All(hulls, items).Single(l => l.Hull == asp);
        Assert.Equal(("addinv", 1004, "Plasma Cannon"), (loadout.Verb, loadout.Item!.Id, loadout.Item.Name));
        var upgrades = OriginalUpgrade.All(items);
        var turbo = upgrades.Single(u => u.Item.Name == "TurboBoosters");
        var core = upgrades.Single(u => u.Item.Name == "HyperCore");
        Assert.Equal((2, "Engine Mods", 2011, "HyperDrive Systems"), (turbo.Item.Id, turbo.Family, core.Item.Id, core.Family));
        var tellerium = items.ById(2003);
        Assert.Equal(("Tellerium", "Resources"), (tellerium.Name, tellerium.Category));

        return PlayerProfile.CreateNew("f068-asp", 12345, OriginalProgressionIds.HullId(asp)) with // NEW fixture wallet, not original data
        {
            Inventory = Env.Items(
                (OriginalProgressionIds.ItemId(loadout.Item), 1),   // NEW fixture quantity (f[129] has no count)
                (OriginalProgressionIds.ItemId(tellerium), 7)),     // NEW fixture quantity
            Upgrades = ImmutableSortedSet.Create(StringComparer.Ordinal, OriginalProgressionIds.ItemId(turbo.Item)),
        };
    }

    [RequiresOriginalAssetsFact]
    public async Task OriginalIdProfileSurvivesFileSaveAndLoadExactly()
    {
        var ids = Ids.Value;
        var validator = Validator(ids);
        var options = new PersistenceOptions();
        var created = AspProfile();
        Assert.Equal(("2002", 12345L), (created.ShipHullId, created.Wallet));
        Assert.Equal(new[] { new KeyValuePair<string, int>("1004", 1), new KeyValuePair<string, int>("2003", 7) }, created.Inventory);
        Assert.Empty(validator.Validate(created));

        // Revision 1: an original upgrade row is added and cash changes (NEW fixture amount); hull and inventory unchanged.
        var next = created with
        {
            Revision = 1,
            Wallet = created.Wallet - 5000, // NEW fixture delta, not the ITM price
            Upgrades = created.Upgrades.Add(OriginalProgressionIds.Encode(2011)),
        };
        Assert.Empty(validator.Validate(next));

        using var dir = new TempDirectory();
        using (var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>()))
        {
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(created)).Status);
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.SaveAsync(next, 0)).Status);
        }

        using (var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>()))
        {
            var loaded = await repo.LoadAsync("f068-asp");
            Assert.Equal(ProfileLoadStatus.Loaded, loaded.Status);
            var p = loaded.Profile!;
            ProfileAssert.Equivalent(next, p);
            Assert.Equal("2002", p.ShipHullId);
            Assert.Equal(7345L, p.Wallet);
            Assert.Equal(new[] { new KeyValuePair<string, int>("1004", 1), new KeyValuePair<string, int>("2003", 7) }, p.Inventory);
            Assert.Equal(new[] { "2", "2011" }, p.Upgrades);
            Assert.Empty(validator.Validate(p));
            Assert.Equal("Asp", Catalogs.Value.Hulls.Hulls.Single(h => OriginalProgressionIds.HullId(h) == p.ShipHullId).Name);
            Assert.Equal(new[] { "Plasma Cannon", "Tellerium" }, p.Inventory.Keys.Select(k => Catalogs.Value.Items.ById(int.Parse(k)).Name));
            Assert.Equal(new[] { "TurboBoosters", "HyperCore" }, p.Upgrades.Select(k => Catalogs.Value.Items.ById(int.Parse(k)).Name));
        }
    }

    [RequiresOriginalAssetsFact]
    public void HullChangeAndWipePoliciesAreNotEnforced()
    {
        // NWS 28 (wipe) and 399 (same-race exchange) are evidence only: the binding accepts any original hull, including a
        // different-race hull, and an empty profile; it neither forbids nor performs a change, wipe or cash reset.
        var v = Validator(Ids.Value);
        var asp = AspProfile();
        var skithist = asp with { ShipHullId = OriginalProgressionIds.HullId(Catalogs.Value.Hulls.ByName("Skithist")) };
        Assert.Equal("Ship Hull - Skithzar", Catalogs.Value.Hulls.ByName("Skithist").Category);
        Assert.Empty(v.Validate(skithist));
        Assert.Empty(v.Validate(PlayerProfile.CreateNew("f068-empty", 0, null)));
        Assert.Equal(asp.Wallet, skithist.Wallet);
        Assert.Equal(asp.Inventory, skithist.Inventory);
    }
}
