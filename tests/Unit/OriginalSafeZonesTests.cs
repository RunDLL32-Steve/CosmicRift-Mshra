using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F065 (data clause only): the safety/energy/prize/spec/quit keys of all 16 rogue.cfg terrain blocks, the arena-level spectator, team-switch,
/// door and safety keys, NWS lines 290/344/345/419/481/483 under their dated headings, and raw-text sightings of the LIO docking rows
/// (no LIO parser; queue G6). Oracle values were read from the pinned CFG/rogue.cfg, NWS/Rogue.nws and LIO/Rogue.lio (eb1514ea) and
/// reproduced by scripts/original-assets/audit_rt_f065.py. They document raw data and visible mismatches; they do not validate any
/// safe-zone, docking, door or forced-spec behaviour. Asset tests are Skipped without the assets.
/// </summary>
public sealed class OriginalSafeZonesTests
{
    private static readonly Lazy<OriginalSafeZones> Data = new(() => OriginalSafeZones.Load(OriginalAssets.Root!));

    [Fact]
    public void SyntheticBlocksKeepRawValuesEmptyValuesAndUnlistedKeys()
    {
        var keys = string.Join("\r\n", OriginalTerrainSafety.SelectedKeys.Select(k => k == "BountyAutoMax" ? k + "=" : k + "=1"));
        var cfg = "[Terrain0]\r\n" + keys + "\r\nStripShadows=9\r\n";
        var sections = string.Concat(Enumerable.Range(0, 16).Select(t => cfg.Replace("[Terrain0]", $"[Terrain{t}]") + "\r\n"));
        var all = OriginalTerrainSafety.All(OriginalCfg.Parse(sections, "t.cfg"));
        Assert.Equal(16, all.Count);
        Assert.Equal(new[] { 1, 32, 63 }, all.Take(3).Select(t => t.Line));
        var t0 = all[0];
        Assert.Equal("", t0.Raw("BountyAutoMax"));
        Assert.Throws<OriginalDataException>(() => t0.Int("BountyAutoMax"));
        Assert.Equal(new[] { "StripShadows" }, t0.OtherKeys);
        Assert.Equal(1, t0.Safety);
    }

    [RequiresOriginalAssetsFact]
    public void AllSixteenTerrainBlocksArePinnedWithLineNumbers()
    {
        var d = Data.Value;
        Assert.Equal(16, d.Terrains.Count);
        Assert.Equal(Enumerable.Range(0, 16).Select(t => 248 + 37 * t), d.Terrains.Select(t => t.Line));
        Assert.All(d.Terrains, t => Assert.Equal(35, t.EntryCount));
        Assert.All(d.Terrains, t => Assert.Equal(new[] { "StripShadows", "SoccerEnabled", "GoalFrequency", "FontColor", "Starfield", "GoalPoints", "FlagTimerSpeed" }, t.OtherKeys));
        Assert.Equal(28, OriginalTerrainSafety.SelectedKeys.Count);
        // header line + file position of the selected keys: Message is +1, Safety +3, EnergyRate +8, MaxTimeAllowed +19 in every block
        Assert.All(d.Terrains, t =>
        {
            Assert.Equal(t.Line + 1, t.Key("Message").Entry.LineNumber);
            Assert.Equal(t.Line + 3, t.Key("Safety").Entry.LineNumber);
            Assert.Equal(t.Line + 8, t.Key("EnergyRate").Entry.LineNumber);
            Assert.Equal(t.Line + 19, t.Key("MaxTimeAllowed").Entry.LineNumber);
        });
        Assert.Equal(new[] { "Message=", "Safety=0", "StoreEnabled=0" }, new[] { "Message", "Safety", "StoreEnabled" }.Select(k => d.Terrain(0).Key(k).Entry.RawLine));
        Assert.Equal("Safety=1", d.Terrain(15).Key("Safety").Entry.RawLine);
        Assert.Equal(806, d.Terrain(15).Key("Safety").Entry.LineNumber);
    }

    [RequiresOriginalAssetsFact]
    public void PerTerrainSafetyEnergyAndTimerValuesMatchTheFile()
    {
        var d = Data.Value;
        Assert.Equal(new[] { 9, 10, 15 }, d.Terrains.Where(t => t.Safety != 0).Select(t => t.Terrain));
        Assert.Equal(new[] { 9, 10, 15 }, d.Terrains.Where(t => t.EnergyRate != 0).Select(t => t.Terrain));
        Assert.Equal(new[] { -2000, -2000, -2000 }, d.Terrains.Where(t => t.EnergyRate != 0).Select(t => t.EnergyRate));
        Assert.Equal(new[] { 9, 15 }, d.Terrains.Where(t => t.MaxTimeAllowed != 0).Select(t => t.Terrain));
        Assert.Equal(new[] { 60, 60 }, d.Terrains.Where(t => t.MaxTimeAllowed != 0).Select(t => t.MaxTimeAllowed));
        Assert.Equal(
            new[] { (9, "Repair Dock"), (10, "SpaceDock. Store Zone."), (13, "Border Space"), (15, "Refinery Zone/Trade Zone") },
            d.Terrains.Where(t => t.Message.Length > 0).Select(t => (t.Terrain, t.Message)));
        // Store / Skill / TeamChange
        Assert.Equal(new[] { 0, 9, 15 }, d.Terrains.Where(t => t.Int("StoreEnabled") == 0).Select(t => t.Terrain));
        Assert.Equal(new[] { 0, 9, 10, 15 }, d.Terrains.Where(t => t.Int("SkillEnabled") == 0).Select(t => t.Terrain));
        Assert.Equal(new[] { 0, 9, 15 }, d.Terrains.Where(t => t.Int("TeamChangeEnabled") == 0).Select(t => t.Terrain));
        // Repel, live weapons, rewards
        Assert.Equal(new[] { 10, 15 }, d.Terrains.Where(t => t.Int("RepelVehicle") == 0).Select(t => t.Terrain));
        Assert.All(d.Terrains, t => Assert.Equal(1, t.Int("RepelWeapons")));
        Assert.Equal(new[] { 10, 15 }, d.Terrains.Where(t => t.Int("DeleteLiveWeapons") != 0).Select(t => t.Terrain));
        Assert.Equal(new[] { 10, 15 }, d.Terrains.Where(t => t.Int("PercentOfCashReward") == 0).Select(t => t.Terrain));
        Assert.Equal(new[] { 10, 15 }, d.Terrains.Where(t => t.Int("PercentOfExperienceReward") == 0).Select(t => t.Terrain));
        Assert.Equal(new[] { 10, 15 }, d.Terrains.Where(t => t.Int("PercentOfPointsReward") == 0).Select(t => t.Terrain));
        Assert.All(d.Terrains.Where(t => t.Terrain is not (10 or 15)), t => Assert.Equal(1000, t.Int("PercentOfCashReward")));
        // all-terrain constants
        Assert.All(d.Terrains, t => Assert.Equal((0, 0, 0, 3, 1, 1, 1, 0, 0), (t.Int("TrickleKill"), t.Int("HealthRate"), t.Int("RepairRate"), t.Int("PrizeEnableMode"), t.Int("AllowChangeArena"), t.Int("AllowGoToSpec"), t.Int("AllowQuitting"), t.Int("GoToSpecDelaySecs"), t.Int("ChangeArenaDelaySecs"))));
        Assert.All(d.Terrains, t => Assert.Equal("", t.Raw("EventString")));
        // prize expiry, quit delay and bounty keys
        Assert.Equal(720, d.Terrain(0).Int("PrizeExpire"));
        Assert.All(d.Terrains.Skip(1), t => Assert.Equal(120, t.Int("PrizeExpire")));
        Assert.Equal((3, 7), (d.Terrain(0).Int("PrizeBountyMultiplier"), d.Terrain(0).Int("QuitDelaySecs")));
        Assert.All(d.Terrains.Skip(1), t => Assert.Equal((0, 0), (t.Int("PrizeBountyMultiplier"), t.Int("QuitDelaySecs"))));
        Assert.Equal(new[] { "100", "100", "100", "100", "100", "100", "100", "100", "100", "", "0", "100", "100", "100", "100", "" }, d.Terrains.Select(t => t.Raw("BountyAutoMax")));
        Assert.Equal(new[] { 3, 200, 200, 200, 200, 200, 200, 200, 200 }, d.Terrains.Take(9).Select(t => t.Int("BountyAutoRate")));
        Assert.Equal(new[] { 9, 15 }, d.Terrains.Where(t => t.Key("BountyAutoMax").AsInt is null).Select(t => t.Terrain));
        Assert.Throws<OriginalDataException>(() => d.Terrain(9).Int("BountyAutoMax"));
    }

    [RequiresOriginalAssetsFact]
    public void ArenaLevelKeysArePinnedWithSectionsAndLines()
    {
        var d = Data.Value;
        (string, string, int, string) Row(string section, string key) { var k = d.ArenaKey(section, key); return (k.Section, k.Entry.Key, k.Entry.LineNumber, k.Entry.Value); }
        Assert.Equal(("Arena", "SpectatorVehicleId", 33, "2030"), Row("Arena", "SpectatorVehicleId"));
        Assert.Equal(("Arena", "SpectatorStore", 37, "0"), Row("Arena", "SpectatorStore"));
        Assert.Equal(("Arena", "SpectatorSkills", 38, "1"), Row("Arena", "SpectatorSkills"));
        Assert.Equal(("Arena", "StartInSpectator", 39, "1"), Row("Arena", "StartInSpectator"));
        Assert.Equal(("Arena", "ExitSpectatorLogic", 40, "(1|2|3|4|5|6|8|9|10|11|15|16)"), Row("Arena", "ExitSpectatorLogic"));
        Assert.Equal(("Arena", "ExitSpectatorMessage", 41, "You must choose a ship before you can enter. Press f11."), Row("Arena", "ExitSpectatorMessage"));
        Assert.Equal(("Arena", "SpectatorEnergyAmount", 64, "-1"), Row("Arena", "SpectatorEnergyAmount"));
        Assert.Equal(("Arena", "ExitSpectatorToggleSkills", 73, "2"), Row("Arena", "ExitSpectatorToggleSkills"));
        Assert.Equal(("Arena", "SkillScreenEnergyNeeded", 74, "1500"), Row("Arena", "SkillScreenEnergyNeeded"));
        Assert.Equal(("Arena", "AllowManualTeamSwitch", 50, "2"), Row("Arena", "AllowManualTeamSwitch"));
        Assert.Equal(("Arena", "TeamSwitchMinEnergy", 83, "500000"), Row("Arena", "TeamSwitchMinEnergy"));
        Assert.Equal(("Arena", "InactivityTimeout", 48, "720"), Row("Arena", "InactivityTimeout"));
        Assert.Equal(("Arena", "SuicidesAllowed", 34, "0"), Row("Arena", "SuicidesAllowed"));
        Assert.Equal(("Door", "AttemptTime", 841, "500"), Row("Door", "AttemptTime"));
        Assert.Equal(("Door", "RandomControlDelay", 842, "1000"), Row("Door", "RandomControlDelay"));
        Assert.Equal(("Flag", "AllowSafety", 873, "0"), Row("Flag", "AllowSafety"));
        Assert.Equal(("Soul", "EnergyDefaultMax", 97, "1700000"), Row("Soul", "EnergyDefaultMax"));
        Assert.Equal(("Soul", "EnergyDefaultRate", 98, "250"), Row("Soul", "EnergyDefaultRate"));
        Assert.Equal(("Event", "ExitSpectatorMode", 2507, "EventJoinTeam"), Row("Event", "ExitSpectatorMode"));
        Assert.Equal(("Event", "JoinTeam", 2506, "eventHold1,eventHold2,setenergy=10000,warp=0"), Row("Event", "JoinTeam"));
        Assert.Equal(("Event", "ManualJoinTeam", 2511, "eventHold1,eventJoinTeam"), Row("Event", "ManualJoinTeam"));
        Assert.Equal(26, d.ArenaKeys.Count);
        // the Sound-section AttemptTime (line 938) must not be mistaken for the Door one
        Assert.Equal(841, d.ArenaKey("Door", "AttemptTime").Entry.LineNumber);
        Assert.Throws<KeyNotFoundException>(() => d.ArenaKey("Arena", "ForceSpectate"));
    }

    [RequiresOriginalAssetsFact]
    public void NwsLinesArePinnedVerbatimUnderTheirDatedHeadings()
    {
        var d = Data.Value;
        Assert.Equal(
            new[]
            {
                (282, "********2/25/03", 290, "You can change teams from a safe zone if you have at least 500 units of energy."),
                (342, "********2/16/03", 344, "Made some map changes in anticipation of further overall changes to the zone. You will note that there are doors at "),
                (342, "********2/16/03", 345, "the entrances to the spacedocks. They are not currently functional but will be in the near future."),
                (402, "*******9/06/01", 419, "You now lose energy when you sit in a safe zone surrounding a refinery/trading post. Dont dawdle. :)"),
                (460, "********8/27/01", 481, "You can only change teams from the Space Dock area."),
                (460, "********8/27/01", 483, "You can only stay in the safe zones protecting Refineries or Trading Posts for 60 seconds.  After that you will be sent to spec."),
            },
            d.Nws.Select(n => (n.Heading.LineNumber, n.Heading.Text, n.Line.LineNumber, n.Line.Text)));
        Assert.Equal(345, d.NwsLine(345).Line.LineNumber);
        Assert.Throws<KeyNotFoundException>(() => d.NwsLine(218));
    }

    [RequiresOriginalAssetsFact]
    public void TheCatalogQuotedFortyEightThreeSixtyTextDoesNotOccurInTheNews()
    {
        var d = Data.Value;
        Assert.False(d.NwsTextContains48360);
        Assert.Equal("48360-second", OriginalSafeZones.CatalogQuotedText);
        Assert.DoesNotContain("60-second", d.NwsLine(483).Line.Text);
        Assert.Contains("60 seconds", d.NwsLine(483).Line.Text);
    }

    [RequiresOriginalAssetsFact]
    public void LioDockingRowsAreOnlyRawTextSightingsAtTheCatalogLines()
    {
        var d = Data.Value;
        Assert.Equal(81, d.LioLineCount);
        Assert.Equal(new[] { 12, 40, 41, 42, 43, 44, 45, 46, 47, 48, 60, 61, 62 }, d.DockingLioLines.Select(l => l.Line));
        Assert.All(d.DockingLioLines, l => Assert.EndsWith(",SFAmbient,docking_proceedure,0,0", l.RawText));
        Assert.Equal("7,v23,10,\"ColdHarborDock\",1719,1799,100,200,0,-3,    -1,1000,1,0,0,  0,500,-1,  None,None,0,0,0,0,0,0,SFAmbient,docking_proceedure,0,0", d.DockingLioLines[0].RawText);
        Assert.Equal("7,v23,87,\"MarianasDockC\",13422,9854,180,20,0,-3,    -1,1000,1,0,0,  0,500,-1,  None,None,0,0,0,0,0,0,SFAmbient,docking_proceedure,0,0", d.DockingLioLines[^1].RawText);
        Assert.Empty(d.LioDoorTextLines);
        Assert.Equal(9782, OriginalSafeZones.LioRogue.Length);
    }

    [RequiresOriginalAssetsFact]
    public void VisibleMismatchesAreRecordedAndNothingIsCorrected()
    {
        var d = Data.Value;
        Assert.Equal(
            new[]
            {
                "The catalog row quotes '48360-second', which does not occur anywhere in NWS/Rogue.nws; NWS line 483 (********8/27/01) reads '60 seconds ... sent to spec', so the catalog text is not a quotation; it appears to run 'NWS 483' together with a '60-second' rule (an inference, not evidence).",
                "NWS 483 (********8/27/01): 60 seconds in the safe zones protecting Refineries or Trading Posts, then sent to spec. CFG MaxTimeAllowed is non-zero (60, 60) only on Terrain9 'Repair Dock' and Terrain15 'Refinery Zone/Trade Zone'; Terrain9,10,15 are Safety=1 but Terrain10 'SpaceDock. Store Zone.' has MaxTimeAllowed=0. No terrain message names a Trading Post, and no CFG key names a forced move to spec (AllowGoToSpec=1 and GoToSpecDelaySecs=0 are the same on all 16 terrains), so the link between MaxTimeAllowed=60 and 'sent to spec' is only a numeric match until the executable is read.",
                "NWS 419 (*******9/06/01): lose energy in a safe zone surrounding a refinery/trading post. CFG EnergyRate is non-zero (-2000) on Terrain9,10,15 (Terrain9 'Repair Dock'; Terrain10 'SpaceDock. Store Zone.'; Terrain15 'Refinery Zone/Trade Zone'), the same terrains that have Safety=1 (Terrain9,10,15), so the loss is on the Repair Dock and SpaceDock zones as well, not only the refinery/trade zone. The sign fits 'lose' but the unit of -2000 is undecoded (context: [Soul] EnergyDefaultMax=1700000, EnergyDefaultRate=250).",
                "NWS 290 (********2/25/03): change teams from a safe zone with at least 500 units of energy. CFG [Arena] TeamSwitchMinEnergy=500000 (line 83) is 1000 times the text, and TeamChangeEnabled=0 on Terrain0,9,15, which includes the Safety=1 terrains 9,15; only Terrain10 'SpaceDock. Store Zone.' is both a safe zone and team-change enabled. The scale (500 vs 500000) is not proven.",
                "NWS 481 (********8/27/01): teams can only be changed from the Space Dock area. CFG TeamChangeEnabled=1 on 13 terrains (Terrain1,2,3,4,5,6,7,8,10,11,12,13,14), only one of which is the message-labelled 'SpaceDock. Store Zone.'; whether the other terrains are Space Dock area is a map (LVL) question, and LVL is not available as text.",
                "NWS 345 (********2/16/03, with 344) says the doors at the spacedock entrances 'are not currently functional'; the catalog row words it 'not yet functional'. CFG has a [Door] block (AttemptTime=500, RandomControlDelay=1000) but LIO/Rogue.lio contains 0 lines with 'door' in any case, so the spacedock doors are not in the LIO text; any door placement would be in the LVL map (Git LFS, not available). The 'current door state' the acceptance asks for is therefore not decodable here.",
                "The catalog names LIO docking rows 12, 40-48, 60-62 (13 rows); raw-text sightings of ',SFAmbient,docking_proceedure,' are on lines 12,40,41,42,43,44,45,46,47,48,60,61,62, which matches exactly. Those rows are not read: no coordinates, sizes, ids or boundaries are decoded, so no docking boundary exists in this view.",
                "No CFG, NWS or LIO text defines a safe-zone entry/exit trigger geometry; terrain permissions are per-terrain-id flags and the terrain id of each map cell comes from LVL/BLO data that is not available. Zone triggers/timing beyond MaxTimeAllowed/EnergyRate and original forced-spec behaviour remain undecoded.",
            }, d.VisibleMismatches);
        // raw values stay untouched by the notes
        Assert.Equal(60, d.Terrain(15).MaxTimeAllowed);
    }
}
