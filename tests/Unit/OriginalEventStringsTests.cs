using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F067 data clause: raw-preserving event-string tokenizer (always runs) and the inventory of the pinned eb1514ea
/// VEH/CFG/ITM event slots (oracle: scripts/original-assets/audit_rt_f067.py). Verb semantics, targets, transactions and
/// native behaviour are NOT tested here; asset tests are Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalEventStringsTests
{
    private static readonly Lazy<OriginalEventInventory> Inv = new(() => OriginalEventInventory.Load(OriginalAssets.Root!));

    private static string Shape(OriginalEventScript s) =>
        string.Join("|", s.Tokens.Select(t => t.Kind switch
        {
            OriginalEventTokenKind.Separator => ",",
            OriginalEventTokenKind.Action => $"{t.Verb}{t.Operator}{(t.Argument is null ? "" : "<" + t.Argument + ">")}@{t.Offset}",
            _ => $"?{t.Raw}@{t.Offset}",
        }));

    [Theory]
    [InlineData("")]
    [InlineData("addinv=Plasma Cannon")]
    [InlineData("WipeInv=Tellerium,Wipeinv=Iron Ore,wipeinv=null wreckage")]
    [InlineData("Wipeinv,setcash=0,vehicleEvent")]
    [InlineData("addinv=Plasma Cannon:5")]
    [InlineData("pick\"a;b\"")]
    [InlineData(",,a=b=c, x ,setcash+=5,")]
    [InlineData("a=é")]
    public void ConcatenatingTokensReproducesTheInputExactly(string raw)
    {
        var s = OriginalEventScript.Parse(raw);
        Assert.Equal(raw, s.Reconstruct());
        Assert.Equal(raw, s.Raw);
        var pos = 0;
        foreach (var t in s.Tokens)
        {
            Assert.Equal(pos, t.Offset);
            Assert.Equal(t.Raw, raw.Substring(t.Offset, t.Raw.Length));
            if (t.Kind == OriginalEventTokenKind.Action)
            {
                Assert.Equal(t.Raw, t.Verb + t.Operator + t.Argument);
                if (t.ArgumentOffset is { } ao) Assert.Equal(t.Argument, raw.Substring(ao, t.Argument!.Length));
            }
            pos += t.Raw.Length;
        }
        Assert.Equal(raw.Length, pos);
    }

    [Fact]
    public void EmptyStringHasNoTokens()
    {
        var s = OriginalEventScript.Parse("");
        Assert.True(s.IsEmpty);
        Assert.Empty(s.Tokens);
        Assert.Empty(s.Steps);
    }

    [Fact]
    public void MultipleActionsKeepOrderOffsetsCaseAndSpaces()
    {
        var s = OriginalEventScript.Parse("WipeInv=Tellerium,Wipeinv=Iron Ore,wipeinv=null wreckage,eventHold1,setenergy=10000");
        Assert.Equal("WipeInv=<Tellerium>@0|,|Wipeinv=<Iron Ore>@18|,|wipeinv=<null wreckage>@35|,|eventHold1@57|,|setenergy=<10000>@68", Shape(s));
        Assert.Empty(s.Unknowns);
        Assert.Equal(new[] { "wipeinv", "wipeinv", "wipeinv", "eventhold1", "setenergy" }, s.Actions.Select(a => a.UpstreamFoldedVerb));
        Assert.Equal(new string?[] { null, null, null, "Hold1", null }, s.Actions.Select(a => a.UpstreamEventCallName));
        Assert.Equal(26, s.Actions.ElementAt(1).ArgumentOffset);
    }

    [Theory]
    [InlineData("addinv=Plasma Cannon:5", "':' at offset 20")]
    [InlineData("setcash+=5", "'+' at offset 7 after verb 'setcash'")]
    [InlineData("setcash<5", "'<' at offset 7")]
    [InlineData("a=b=c", "'=' at offset 3")]
    [InlineData("pick\"a;b\"", "'\"' at offset 4")]
    [InlineData("pick[a;b]", "'[' at offset 4")]
    [InlineData(" warp=0", "a verb must start with a letter")]
    [InlineData("warp=0 ", "leading or trailing space")]
    [InlineData("warp=", "empty argument")]
    [InlineData("1warp", "a verb must start with a letter")]
    public void UnknownOperatorsAndShapesArePreservedNotGuessed(string raw, string reasonPart)
    {
        var s = OriginalEventScript.Parse(raw);
        var t = Assert.Single(s.Tokens);
        Assert.Equal(OriginalEventTokenKind.Unknown, t.Kind);
        Assert.Equal(raw, t.Raw);
        Assert.Null(t.Verb);
        Assert.Null(t.Argument);
        Assert.Contains(reasonPart, t.UnknownReason);
    }

    [Fact]
    public void EmptyActionsBetweenSeparatorsStayVisible()
    {
        var s = OriginalEventScript.Parse(",reset,");
        Assert.Equal("?@0|,|reset@1|,|?@7", Shape(s));
        Assert.Equal(2, s.Unknowns.Count());
        Assert.Equal(new[] { "?", "reset", "?" }, s.Steps.Select(t => t.Verb ?? "?"));
    }

    // ---- pinned assets (eb1514ea) ----

    [RequiresOriginalAssetsTheory]
    [InlineData(18, 2000, "Hornet", "addinv=Plasma Cannon")]
    [InlineData(19, 2001, "Skithist", "addinv=Bio Cannon")]
    [InlineData(20, 2002, "Asp", "addinv=Plasma Cannon")]
    [InlineData(21, 2003, "Kragath", "addinv=Bio Cannon")]
    [InlineData(22, 2004, "Mastakith", "addinv=Bio Cannon")]
    [InlineData(23, 2005, "Rak'Kir", "addinv=D-Cannon")]
    [InlineData(24, 2006, "Rip'Pir", "addinv=D-Cannon")]
    [InlineData(25, 2008, "Zap'Pir", "addinv=D-Cannon")]
    [InlineData(26, 2009, "Zag'Gir", "addinv=D-Cannon")]
    [InlineData(27, 2010, "Tarthist", "addinv=Bio Cannon")]
    [InlineData(28, 2011, "Excelsior", "addinv=Plasma Cannon")]
    [InlineData(29, 2012, "Ravager", "addinv=Plasma Cannon")]
    public void HullRowsCarryOneAddinvInEventString1Only(int row, int id, string name, string raw)
    {
        var veh = Inv.Value.Slots.Where(s => s.Source.Kind == OriginalEventSlotKind.VehEventString && s.Source.Line == row).ToList();
        Assert.Equal(new int?[] { 129, 130, 131 }, veh.Select(s => s.Source.FieldIndex));
        Assert.All(veh, s => Assert.Equal((id, name), (s.Source.RecordId!.Value, s.Source.RecordName)));
        Assert.Equal(new[] { raw, "", "" }, veh.Select(s => s.Script.Raw));
        Assert.Equal("EventString1", veh[0].Source.Key);
        var a = Assert.Single(veh[0].Script.Tokens);
        Assert.Equal(("addinv", "=", raw[7..], 7), (a.Verb, a.Operator, a.Argument, a.ArgumentOffset!.Value));
    }

    [RequiresOriginalAssetsFact]
    public void SlotCountsAndOnlyHullsAndEventSectionAreNonEmpty()
    {
        var inv = Inv.Value;
        Assert.Equal(318, inv.Slots.Count);
        var byKind = inv.Slots.GroupBy(s => s.Source.Kind).ToDictionary(g => g.Key, g => (All: g.Count(), NonEmpty: g.Count(s => !s.Script.IsEmpty)));
        Assert.Equal((90, 12), byKind[OriginalEventSlotKind.VehEventString]);
        Assert.Equal((20, 13), byKind[OriginalEventSlotKind.CfgEventSection]);
        Assert.Equal((16, 0), byKind[OriginalEventSlotKind.CfgTerrainEventString]);
        Assert.Equal((50, 0), byKind[OriginalEventSlotKind.CfgTeamEventString]);
        Assert.Equal((50, 0), byKind[OriginalEventSlotKind.CfgTeamEventStartGame]);
        Assert.Equal((92, 0), byKind[OriginalEventSlotKind.ItmProjectileDamageEventString]);
        Assert.Equal(25, inv.Entries.Count());
        Assert.Equal(293, inv.EmptySlots.Count());
        Assert.Empty(inv.Unknowns);
        Assert.All(inv.Entries, e => Assert.Equal(e.Script.Raw, e.Script.Reconstruct()));
        // Computer rows 1-17 and Spectator Mode row 30 have all three VEH event strings empty.
        Assert.All(inv.Entries.Where(e => e.Source.Kind == OriginalEventSlotKind.VehEventString),
            e => Assert.True(e.Source.Line is >= 18 and <= 29 && e.Source.FieldIndex == 129));
    }

    [RequiresOriginalAssetsFact]
    public void CfgEventSectionStringsExactlyAsPinned()
    {
        var cfg = Inv.Value.Slots.Where(s => s.Source.Kind == OriginalEventSlotKind.CfgEventSection)
            .Select(s => (s.Source.Line, s.Source.Key, s.Script.Raw)).ToArray();
        Assert.Equal(new (int, string, string)[]
        {
            (2492, "Hold1", "WipeInv=Tellerium,Wipeinv=Iron Ore,Wipeinv=Krandazite,Wipeinv=wreckage,wipeinv=null wreckage"),
            (2493, "Hold2", "Wipeinv=Trading Post,Wipeinv=BeamSat,Wipeinv=GunSat,Wipeinv=Defense Manufactory,Wipeinv=Missile Manufactory,Wipeinv=Munitions Manufactory,Wipeinv=Micro Repair Dock,wipeinv=Micro Refinery"),
            (2494, "Hold3", ""),
            (2495, "Hold4", ""),
            (2496, "StartGame", "setenergy=10000"),
            (2497, "SysopWipe", "EventSelfWipe"),
            (2498, "SelfWipe", "wipescore,EventFirstTimeInvSetup,EventFirstTimeSkillSetup"),
            (2499, "FirstTimeInvSetup", "Wipeinv,setcash=0,vehicleEvent"),
            (2500, "KilledTeam", ""),
            (2501, "KilledEnemy", ""),
            (2502, "KilledByTeam", "reset,warp=0"),
            (2503, "KilledByEnemy", "reset,warp=0"),
            (2504, "EnterSpawnNoScore", "wipescore,EventFirstTimeInvSetup"),
            (2505, "ChangeDefaultVehicle", ""),
            (2506, "JoinTeam", "eventHold1,eventHold2,setenergy=10000,warp=0"),
            (2507, "ExitSpectatorMode", "EventJoinTeam"),
            (2508, "FirstTimeSkillSetup", "wipeskill,setexp=1"),
            (2509, "EndGame", ""),
            (2510, "SoonGame", ""),
            (2511, "ManualJoinTeam", "eventHold1,eventJoinTeam"),
        }, cfg);
    }

    [RequiresOriginalAssetsFact]
    public void VerbInventoryCounts()
    {
        var inv = Inv.Value;
        Assert.Equal(48, inv.Entries.Sum(e => e.Script.Actions.Count()));
        Assert.Equal(new (string, int)[]
        {
            ("addinv", 12), ("WipeInv", 1), ("Wipeinv", 11), ("wipeinv", 2), ("setenergy", 2), ("EventSelfWipe", 1),
            ("wipescore", 2), ("EventFirstTimeInvSetup", 2), ("EventFirstTimeSkillSetup", 1), ("setcash", 1),
            ("vehicleEvent", 1), ("reset", 2), ("warp", 3), ("eventHold1", 2), ("eventHold2", 1), ("EventJoinTeam", 1),
            ("wipeskill", 1), ("setexp", 1), ("eventJoinTeam", 1),
        }, inv.VerbsExact.Select(v => (v.Verb, v.Count)));
        var folded = inv.VerbsFolded.ToDictionary(v => v.Verb);
        Assert.Equal(16, folded.Count);
        Assert.Equal(14, folded["wipeinv"].Count);
        Assert.Equal(new[] { "WipeInv", "Wipeinv", "wipeinv" }, folded["wipeinv"].Spellings);
        Assert.Equal(new[] { "EventJoinTeam", "eventJoinTeam" }, folded["eventjointeam"].Spellings);
        Assert.All(folded["addinv"].Sources, s => Assert.Equal(OriginalEventSlotKind.VehEventString, s.Kind));
        Assert.Equal(new[] { 2502, 2503, 2506 }, folded["warp"].Sources.Select(s => s.Line));
        // Only '=' is observed as an operator; every argument-bearing action uses it.
        Assert.All(inv.Entries.SelectMany(e => e.Script.Actions), a => Assert.True(a.Operator is null or "="));
    }

    [RequiresOriginalAssetsFact]
    public void ItemArgumentsResolveExceptWreckageCaseAndNullWreckage()
    {
        var refs = Inv.Value.ItemReferences;
        Assert.Equal(25, refs.Count); // 12 hull addinv + 13 [Event] Hold1/Hold2 wipeinv with an argument
        var byName = refs.GroupBy(r => r.Name).ToDictionary(g => g.Key, g => g.First());
        Assert.Equal(16, byName.Count);
        foreach (var (name, id) in new[] { ("Plasma Cannon", 1004), ("Bio Cannon", 1036), ("D-Cannon", 1041), ("Tellerium", 2003),
                     ("Iron Ore", 2019), ("Krandazite", 2005), ("Trading Post", 45), ("BeamSat", 48), ("GunSat", 47),
                     ("Defense Manufactory", 85), ("Missile Manufactory", 83), ("Munitions Manufactory", 84),
                     ("Micro Repair Dock", 49), ("Micro Refinery", 46) })
        {
            Assert.Equal(id, byName[name].ExactMatch!.Id);
            Assert.Same(byName[name].ExactMatch, byName[name].CaseInsensitiveMatch);
        }
        // "wreckage" matches ITM id 2008 "Wreckage" only case-insensitively; "null wreckage" matches no ITM name.
        Assert.Null(byName["wreckage"].ExactMatch);
        Assert.Equal((2008, "Wreckage"), (byName["wreckage"].CaseInsensitiveMatch!.Id, byName["wreckage"].CaseInsensitiveMatch!.Name));
        Assert.False(byName["null wreckage"].IsResolved);
        Assert.Equal(new[] { "null wreckage" }, refs.Where(r => !r.IsResolved).Select(r => r.Name));
        Assert.Equal(12, refs.Count(r => r.Entry.Source.Kind == OriginalEventSlotKind.VehEventString));
    }

    [RequiresOriginalAssetsFact]
    public void EventCallsResolveExactlyToEventSectionKeys()
    {
        var calls = Inv.Value.EventCalls;
        Assert.Equal(new[] { "SelfWipe", "FirstTimeInvSetup", "FirstTimeSkillSetup", "FirstTimeInvSetup", "Hold1", "Hold2", "JoinTeam", "Hold1", "JoinTeam" },
            calls.Select(c => c.Name));
        Assert.All(calls, c => Assert.Equal(c.Name, c.ExactKey!.Key));
        Assert.All(calls, c => Assert.Same(c.ExactKey, c.CaseInsensitiveKey));
    }
}
