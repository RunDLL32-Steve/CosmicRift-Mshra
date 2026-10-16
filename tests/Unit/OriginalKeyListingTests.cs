using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>[Fact] that reports as Skipped (with a reason) when RT_ROGUE_KEYS_TXT is unset or the file does not exist.</summary>
public sealed class RequiresRogueKeysTxtFactAttribute : FactAttribute
{
    public const string EnvVar = "RT_ROGUE_KEYS_TXT";

    public RequiresRogueKeysTxtFactAttribute()
    {
        if (Path == null)
            Skip = $"Archived keys.txt not provided: run scripts/original-assets/fetch_rogue_keys.py <folder outside the repo> and set {EnvVar}. " +
                   "These tests were SKIPPED, not passed.";
    }

    public static string? Path
    {
        get
        {
            var env = Environment.GetEnvironmentVariable(EnvVar);
            return !string.IsNullOrEmpty(env) && File.Exists(env) ? env : null;
        }
    }
}

/// <summary>
/// RT-F026 (data only). Synthetic parser/provenance tests always run. Oracle tests read the archived keys.txt
/// (InfantryOnline/Zone-Assets 401633b9, git blob e8dc82bb, 1942 bytes; NOT committed) from RT_ROGUE_KEYS_TXT and the pinned
/// CFG/rogue.cfg + NWS/Rogue.nws; values cross-checked with scripts/original-assets/audit_rt_f026.py.
/// Substitution tokens are placeholders, not key codes: nothing here maps a token to a key.
/// </summary>
public sealed class OriginalKeyListingTests
{
    private static readonly Lazy<OriginalKeyListing> Keys = new(() => OriginalKeyListing.Load(RequiresRogueKeysTxtFactAttribute.Path!));

    private const string Synthetic =
        "~B~4Ship~2~B\n\t~5Rotate Left/Right =~2 ~~0key / ~~1key\n\t~5Mine =~2 ~~13key\n\n" +
        "~B~4UI~B (extra)~2\n\t~5Toggle Store =~2 ~~51key  ~1(not used in all zones)~2\n";

    [Fact]
    public void ParsesSectionsActionsTokensSeparatorsAndNotes()
    {
        var k = OriginalKeyListing.Parse(Synthetic, "t.txt");
        Assert.Equal(6, k.LineCount);
        Assert.Equal(new[] { "Ship", "UI (extra)" }, k.Sections.Select(s => s.Title));
        Assert.Equal(new[] { 1, 5 }, k.Sections.Select(s => s.LineNumber));
        Assert.Equal("~B~4Ship~2~B", k.Sections[0].RawLine);
        Assert.Equal(new[] { "Rotate Left/Right", "Mine", "Toggle Store" }, k.Actions.Select(a => a.Label));
        Assert.Equal(new[] { 2, 3, 6 }, k.Actions.Select(a => a.LineNumber));
        Assert.Equal(new[] { 0, 0, 1 }, k.Actions.Select(a => a.SectionIndex));
        var rot = k.Action("Rotate Left/Right");
        Assert.Equal("~5Rotate Left/Right ", rot.RawLabel);
        Assert.Equal(new[] { 0, 1 }, rot.Tokens);
        Assert.Equal(new[] { " / " }, rot.Separators);
        Assert.Null(rot.RawNote);
        Assert.Equal(new[] { 13 }, k.Action("Mine").Tokens);
        Assert.Empty(k.Action("Mine").Separators);
        var store = k.Action("Toggle Store");
        Assert.Equal("~1(not used in all zones)~2", store.RawNote);
        Assert.Equal("(not used in all zones)", store.Note);
    }

    [Fact]
    public void AcceptsCrlfAndNoFinalTerminator()
    {
        var k = OriginalKeyListing.Parse("~B~4A~2~B\r\n\t~5X =~2 ~~7key", "t.txt");
        Assert.Equal(2, k.LineCount);
        Assert.Equal(new[] { 7 }, k.Action("X").Tokens);
    }

    [Fact]
    public void RejectsLinesItCannotClassify()
    {
        Assert.Throws<OriginalDataException>(() => OriginalKeyListing.Parse("\t~5X =~2 ~~1key\n", "t.txt"));            // action before a section
        Assert.Throws<OriginalDataException>(() => OriginalKeyListing.Parse("~B~4A~2~B\njunk\n", "t.txt"));
        Assert.Throws<OriginalDataException>(() => OriginalKeyListing.Parse("~B~4A~2~B\n\t~5X ~2 ~~1key\n", "t.txt"));   // no '='
        Assert.Throws<OriginalDataException>(() => OriginalKeyListing.Parse("~B~4A~2~B\n\t~5X =~2 none\n", "t.txt"));    // no token
        Assert.Throws<OriginalDataException>(() => OriginalKeyListing.Parse("~B~4A~2~B\n\t~5X =~2 ~~1key ~~2key ~~3key\n", "t.txt"));
        Assert.Throws<OriginalDataException>(() => OriginalKeyListing.Parse("~B~4A~2~B\n\t~5X =~2 ~~1key ~\n", "t.txt"));  // dangling '~'
        var k = OriginalKeyListing.Parse(Synthetic + "\t~5Mine =~2 ~~13key\n", "t.txt");
        Assert.Throws<OriginalDataException>(() => k.Action("Mine"));
        Assert.Throws<OriginalDataException>(() => k.Action("Nope"));
    }

    [Fact]
    public void GitBlobHashMatchesGitsDefinition()
    {
        // `printf 'hello' | git hash-object --stdin` = b6fc4c620b67d95f953a5c1c1230aaab5db5a1b0; empty blob = e69de29b...
        Assert.Equal("b6fc4c620b67d95f953a5c1c1230aaab5db5a1b0", OriginalKeyListing.GitBlobSha1Hex(Encoding.ASCII.GetBytes("hello")));
        Assert.Equal("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", OriginalKeyListing.GitBlobSha1Hex([]));
    }

    [Fact]
    public void VerifyRejectsWrongBytesAndMissingFile()
    {
        var wrongLength = Assert.Throws<AssetIntegrityException>(() => OriginalKeyListing.Verify(new byte[10]));
        Assert.Equal(AssetIntegrityFailure.LengthMismatch, wrongLength.Failure);
        var sameLength = Assert.Throws<AssetIntegrityException>(() => OriginalKeyListing.Verify(new byte[(int)OriginalKeyListing.Length]));
        Assert.Equal(AssetIntegrityFailure.GitBlobSha1Mismatch, sameLength.Failure);
        var path = Path.Combine(Path.GetTempPath(), "rt-f026-missing-" + Guid.NewGuid().ToString("N") + ".txt");
        Assert.Equal(AssetIntegrityFailure.Missing, Assert.Throws<AssetIntegrityException>(() => OriginalKeyListing.Load(path)).Failure);
    }

    [Fact]
    public void LoadRefusesAFileWithTheWrongContent()
    {
        var path = Path.Combine(Path.GetTempPath(), "rt-f026-bad-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(path, Synthetic);
        try { Assert.Equal(AssetIntegrityFailure.LengthMismatch, Assert.Throws<AssetIntegrityException>(() => OriginalKeyListing.Load(path)).Failure); }
        finally { File.Delete(path); }
    }

    [RequiresRogueKeysTxtFact]
    public void ArchivedKeysTxtHasThePinnedProvenanceAndShape()
    {
        var bytes = File.ReadAllBytes(RequiresRogueKeysTxtFactAttribute.Path!);
        Assert.Equal(1942, bytes.Length);
        Assert.Equal("e8dc82bb2e4d11a6aa28d0b9ee68d5df3613d7ba", OriginalKeyListing.GitBlobSha1Hex(bytes));
        Assert.Equal("d004b97b69a97a2f3b4917dcf911d3a93f06d406e886a29dc5dc486a07e014f3", AssetIntegrity.Sha256Hex(bytes));
        var k = Keys.Value;
        Assert.Equal(71, k.LineCount);
        Assert.Equal(5, k.Sections.Count);
        Assert.Equal(57, k.Actions.Count);
        Assert.Equal(new[] { 1, 7, 27, 45, 59 }, k.Sections.Select(s => s.LineNumber));
        Assert.Equal(new[] { "Ship Controls", "Weapon Controls", "User Interface Controls", "Message Macros",
            "Extended Message Macros (must have shift distinguishing turned on)" }, k.Sections.Select(s => s.Title));
        Assert.Equal("~B~4Extended Message Macros~B (must have shift distinguishing turned on)~2", k.Sections[4].RawLine);
        Assert.Equal(new[] { 4, 16, 13, 12, 12 }, Enumerable.Range(0, 5).Select(i => k.ActionsIn(i).Count));
    }

    [RequiresRogueKeysTxtFact]
    public void ShipControlTokensArePinned()
    {
        var k = Keys.Value;
        Assert.Equal(new[] { 0, 1 }, k.Action("Rotate Left/Right").Tokens);
        Assert.Equal(new[] { 2, 3 }, k.Action("Thrust Forward/Backwards").Tokens);
        Assert.Equal(new[] { 8, 9 }, k.Action("Hyper Thrust Forward/Backwards").Tokens);
        Assert.Equal(new[] { 6, 7 }, k.Action("Strafe Left/Right").Tokens);
        Assert.All(k.ActionsIn(0), a => Assert.Equal(new[] { " / " }, a.Separators));
        Assert.Equal(new[] { 2, 3, 4, 5 }, k.ActionsIn(0).Select(a => a.LineNumber));
        Assert.Equal("~5Rotate Left/Right =~2 ~~0key / ~~1key", k.Action("Rotate Left/Right").RawLine.TrimStart('\t'));
    }

    [RequiresRogueKeysTxtFact]
    public void WeaponControlTokensArePinned()
    {
        var weapons = Keys.Value.ActionsIn(1);
        Assert.Equal(new[]
        {
            "Light Weapon", "Heavy Weapon", "Mine", "Repulsor", "Team-Warp", "ECM Device", "Utility Device",
            "Special Weapon #1", "Special Weapon #2", "Special Weapon #3",
            "Extra Weapon #1", "Extra Weapon #2", "Extra Weapon #3", "Extra Weapon #4", "Extra Weapon #5", "Extra Weapon #6",
        }, weapons.Select(a => a.Label));
        Assert.Equal(Enumerable.Range(11, 16).ToArray(), weapons.Select(a => Assert.Single(a.Tokens)).ToArray());
        Assert.All(weapons, a => { Assert.Empty(a.Separators); Assert.Null(a.Note); });
    }

    [RequiresRogueKeysTxtFact]
    public void UserInterfaceTokensAndNotesArePinned()
    {
        var k = Keys.Value;
        var ui = k.ActionsIn(2);
        Assert.Equal(new[]
        {
            "Help", "Toggle Main Menu", "Toggle Ship Selection", "Toggle Spectator Mode", "Toggle Map", "Toggle Store",
            "Enter/Exit Alt. Ship", "Extend View In/Out", "Message Scroll Up/Down", "Message Toggle",
            "Player List Up/Down", "Player List Select", "Player List Toggle",
        }, ui.Select(a => a.Label));
        Assert.Equal(new[] { new[] { 66 }, new[] { 65 }, new[] { 50 }, new[] { 49 }, new[] { 27 }, new[] { 51 }, new[] { 32, 33 }, new[] { 5, 4 },
            new[] { 30, 31 }, new[] { 48 }, new[] { 28, 29 }, new[] { 52 }, new[] { 34 } }, ui.Select(a => a.Tokens.ToArray()));
        Assert.Equal(new[] { 28, 29, 30, 31, 32, 33, 35, 36, 38, 39, 41, 42, 43 }, ui.Select(a => a.LineNumber));
        Assert.Equal(new[] { "Toggle Store", "Enter/Exit Alt. Ship" }, ui.Where(a => a.Note != null).Select(a => a.Label));
        Assert.All(ui.Where(a => a.Note != null), a => { Assert.Equal("(not used in all zones)", a.Note); Assert.Equal("~1(not used in all zones)~2", a.RawNote); });
        // Extend View In/Out lists token 5 (In) before 4 (Out); keep the file order rather than sorting.
        Assert.Equal(new[] { 5, 4 }, k.Action("Extend View In/Out").Tokens);
    }

    [RequiresRogueKeysTxtFact]
    public void MacroTokensArePinned()
    {
        var k = Keys.Value;
        var basic = k.ActionsIn(3);
        var extended = k.ActionsIn(4);
        Assert.Equal(Enumerable.Range(1, 12).Select(n => $"Macro #{n}"), basic.Select(a => a.Label));
        Assert.Equal(Enumerable.Range(35, 12).ToArray(), basic.Select(a => Assert.Single(a.Tokens)).ToArray());
        Assert.Equal(Enumerable.Range(13, 12).Select(n => $"Macro #{n}"), extended.Select(a => a.Label));
        Assert.Equal(Enumerable.Range(53, 12).ToArray(), extended.Select(a => Assert.Single(a.Tokens)).ToArray());
        // Every token number appears at most once across the file; the used set is 0..66 except 10 and 47 (65 tokens on 57 actions).
        var all = k.Actions.SelectMany(a => a.Tokens).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
        Assert.Equal(Enumerable.Range(0, 67).Except(new[] { 10, 47 }), all.Order());
    }

    [RequiresOriginalAssetsFact]
    public void CfgAndNwsControlReferencesArePinned()
    {
        var r = OriginalControlReferences.Load(OriginalAssets.Root!);
        Assert.Equal(41, r.CfgLine);
        Assert.Equal("Arena", r.CfgSection);
        Assert.Equal("You must choose a ship before you can enter. Press f11.", r.CfgText);
        Assert.Equal(108, r.NwsLine);
        Assert.Equal("You take the trading post to an area of space you wish to place it and activate it (Press Home).", r.NwsText);
    }
}
