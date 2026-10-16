namespace RogueTrader.OriginalData;

/// <summary>Kind of one piece of an original event string.</summary>
public enum OriginalEventTokenKind
{
    /// <summary>An action in the observed shape <c>verb</c> or <c>verb=argument</c>.</summary>
    Action,
    /// <summary>The observed action separator <c>,</c>.</summary>
    Separator,
    /// <summary>Text between separators that does not match the observed grammar; kept verbatim, never interpreted.</summary>
    Unknown,
}

/// <summary>
/// One piece of an event string. <see cref="Raw"/> is the exact source text and <see cref="Offset"/> its start within the
/// string (text is decoded as Latin-1, so a character offset equals the byte offset inside the field value). For an
/// <see cref="OriginalEventTokenKind.Action"/>, <c>Verb + (Operator ?? "") + (Argument ?? "")</c> equals <see cref="Raw"/>
/// and the original casing is kept. Unknown tokens carry only <see cref="Raw"/> and a <see cref="UnknownReason"/>.
/// </summary>
public sealed record OriginalEventToken(
    OriginalEventTokenKind Kind,
    int Offset,
    string Raw,
    string? Verb,
    string? Operator,
    string? Argument,
    string? UnknownReason)
{
    /// <summary>Offset of <see cref="Argument"/> within the event string, or null when there is no argument.</summary>
    public int? ArgumentOffset => Argument is null ? null : Offset + Verb!.Length + Operator!.Length;

    /// <summary>
    /// The verb lower-cased. Upstream-emulator interpretation, not original-executable evidence: InfantryOnline
    /// ZoneServer/Logic/General/Assets.Events.cs line 52 lower-cases the whole event string before dispatch.
    /// </summary>
    public string? UpstreamFoldedVerb => Verb?.ToLowerInvariant();

    /// <summary>
    /// For an argument-less verb that starts with <c>event</c> (any case), the remainder (for example <c>Hold1</c> from
    /// <c>eventHold1</c>). Upstream-emulator interpretation, not original-executable evidence: Assets.Events.cs lines 94-97
    /// run the named [Event] string for any action starting with "event" (lines 118-174 map the names).
    /// </summary>
    public string? UpstreamEventCallName =>
        Kind == OriginalEventTokenKind.Action && Operator is null && Verb!.Length > 5
        && Verb.StartsWith("event", StringComparison.OrdinalIgnoreCase)
            ? Verb[5..]
            : null;
}

/// <summary>
/// Raw-preserving tokenizer for the original event strings (RT-F067, data clause only). The grammar is limited to what
/// the pinned eb1514ea VEH/CFG strings actually contain: actions separated by a single <c>,</c> with no padding; an action
/// is a verb (<c>[A-Za-z][A-Za-z0-9]*</c>) optionally followed by the only observed operator <c>=</c> and a non-empty
/// argument of letters, digits, space and <c>-</c> that neither starts nor ends with a space. Anything else between two
/// separators (an empty action, a leading/trailing space, <c>:</c> quantities, quotes, <c>[ ] ;</c> pick lists, a second
/// <c>=</c>, any other operator character) becomes an <see cref="OriginalEventTokenKind.Unknown"/> token holding its raw
/// text; it is never dropped, repaired or guessed. Verb meaning, targets, ordering effects, condition operators and
/// transactions are NOT decided here. Concatenating every token's <see cref="OriginalEventToken.Raw"/> reproduces the input.
/// </summary>
public sealed class OriginalEventScript
{
    /// <summary>The only action separator observed in the pinned strings (also the upstream split, Assets.Events.cs line 55).</summary>
    public const char ObservedSeparator = ',';
    /// <summary>The only operator observed in the pinned strings (upstream splits verb/argument at the first '=', line 101).</summary>
    public const char ObservedOperator = '=';

    private OriginalEventScript(string raw, IReadOnlyList<OriginalEventToken> tokens)
    {
        Raw = raw;
        Tokens = tokens;
    }

    /// <summary>The event string exactly as given.</summary>
    public string Raw { get; }

    /// <summary>Every piece in source order, separators included. Empty for the empty string.</summary>
    public IReadOnlyList<OriginalEventToken> Tokens { get; }

    /// <summary>The non-separator tokens (actions and unknowns) in source order.</summary>
    public IEnumerable<OriginalEventToken> Steps => Tokens.Where(t => t.Kind != OriginalEventTokenKind.Separator);

    /// <summary>The well-formed actions in source order.</summary>
    public IEnumerable<OriginalEventToken> Actions => Tokens.Where(t => t.Kind == OriginalEventTokenKind.Action);

    /// <summary>The tokens outside the observed grammar, in source order.</summary>
    public IEnumerable<OriginalEventToken> Unknowns => Tokens.Where(t => t.Kind == OriginalEventTokenKind.Unknown);

    public bool IsEmpty => Raw.Length == 0;

    /// <summary>Concatenation of every token's raw text; always equal to <see cref="Raw"/>.</summary>
    public string Reconstruct() => string.Concat(Tokens.Select(t => t.Raw));

    public static OriginalEventScript Parse(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var tokens = new List<OriginalEventToken>();
        if (raw.Length == 0) return new OriginalEventScript(raw, tokens);
        var start = 0;
        while (true)
        {
            var sep = raw.IndexOf(ObservedSeparator, start);
            var end = sep < 0 ? raw.Length : sep;
            tokens.Add(Segment(raw.Substring(start, end - start), start));
            if (sep < 0) break;
            tokens.Add(new OriginalEventToken(OriginalEventTokenKind.Separator, sep, ObservedSeparator.ToString(), null, null, null, null));
            start = sep + 1;
        }
        return new OriginalEventScript(raw, tokens);
    }

    private static OriginalEventToken Segment(string s, int offset)
    {
        OriginalEventToken Unknown(string why) => new(OriginalEventTokenKind.Unknown, offset, s, null, null, null, why);

        if (s.Length == 0) return Unknown("empty action between separators (not observed)");
        if (!IsLetter(s[0]))
            return Unknown($"'{s[0]}' at offset {offset}: a verb must start with a letter (observed shape)");
        var v = 1;
        while (v < s.Length && (IsLetter(s[v]) || IsDigit(s[v]))) v++;
        var verb = s[..v];
        if (v == s.Length) return new(OriginalEventTokenKind.Action, offset, s, verb, null, null, null);
        if (s[v] != ObservedOperator)
            return Unknown($"'{s[v]}' at offset {offset + v} after verb '{verb}' is not the observed operator '='");
        var arg = s[(v + 1)..];
        if (arg.Length == 0) return Unknown("empty argument after '=' (not observed)");
        for (var i = 0; i < arg.Length; i++)
        {
            var c = arg[i];
            if (!(IsLetter(c) || IsDigit(c) || c == ' ' || c == '-'))
                return Unknown($"'{c}' at offset {offset + v + 1 + i} in the argument is outside the observed argument characters (letters, digits, space, '-')");
        }
        if (arg[0] == ' ' || arg[^1] == ' ') return Unknown("argument with leading or trailing space (not observed)");
        return new(OriginalEventTokenKind.Action, offset, s, verb, ObservedOperator.ToString(), arg, null);
    }

    private static bool IsLetter(char c) => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    private static bool IsDigit(char c) => c is >= '0' and <= '9';
}

/// <summary>Which original slot an event string was read from.</summary>
public enum OriginalEventSlotKind
{
    /// <summary>VEH f[129..131] on every row (upstream names EventString1..3, VehInfo.cs lines 247-249).</summary>
    VehEventString,
    /// <summary>A key of the CFG [Event] section.</summary>
    CfgEventSection,
    /// <summary>CFG [TerrainN] EventString.</summary>
    CfgTerrainEventString,
    /// <summary>CFG [TeamInfoN] EventString.</summary>
    CfgTeamEventString,
    /// <summary>CFG [TeamInfoN] TeamEventStartGame.</summary>
    CfgTeamEventStartGame,
    /// <summary>ITM type-6 f[147] (upstream damageEventString, ItmInfo.Projectile.cs line 253).</summary>
    ItmProjectileDamageEventString,
}

/// <summary>
/// Where one event string came from. <see cref="Line"/> is the 1-based physical line/row in <see cref="SourceFile"/>;
/// <see cref="FieldIndex"/> is the zero-based CSV field for VEH/ITM (null for CFG); <see cref="Section"/> and
/// <see cref="Key"/> are the CFG section and key (for VEH/ITM, Key is the upstream slot label, e.g. EventString1);
/// <see cref="RecordId"/>/<see cref="RecordName"/> identify the VEH/ITM row (CFG: null id, the section name).
/// </summary>
public sealed record OriginalEventSource(
    OriginalEventSlotKind Kind,
    string SourceFile,
    int Line,
    int? FieldIndex,
    string? Section,
    string Key,
    int? RecordId,
    string RecordName)
{
    public override string ToString() => FieldIndex is { } f
        ? $"{SourceFile} row {Line} f[{f}] ({Key}) id {RecordId} '{RecordName}'"
        : $"{SourceFile} line {Line} [{Section}] {Key}";
}

/// <summary>One scanned event slot with its raw text (possibly empty) and the tokenized script.</summary>
public sealed record OriginalEventEntry(OriginalEventSource Source, OriginalEventScript Script);

/// <summary>Occurrences of one verb spelling (or one folded verb) across the inventory.</summary>
public sealed record OriginalEventVerbCount(string Verb, int Count, IReadOnlyList<string> Spellings, IReadOnlyList<OriginalEventSource> Sources);

/// <summary>
/// An item-name argument of an <c>addinv</c>/<c>wipeinv</c> action (verb compared case-insensitively). Reading these
/// arguments as item names is an upstream-emulator interpretation (Assets.Events.cs lines 196-223 and 346-387), not
/// original-executable evidence. Exact (ordinal) and case-insensitive matches against the ITM names are reported
/// separately; an argument with neither stays visible as unresolved.
/// </summary>
public sealed record OriginalEventItemReference(
    OriginalEventEntry Entry,
    OriginalEventToken Action,
    OriginalItem? ExactMatch,
    OriginalItem? CaseInsensitiveMatch)
{
    public string Name => Action.Argument!;
    public bool IsResolved => ExactMatch is not null || CaseInsensitiveMatch is not null;
}

/// <summary>
/// An argument-less <c>event&lt;Name&gt;</c> action resolved against the CFG [Event] keys (exact and case-insensitive
/// separately). The call reading is an upstream-emulator interpretation (Assets.Events.cs lines 94-97, 118-174).
/// </summary>
public sealed record OriginalEventCallReference(
    OriginalEventEntry Entry,
    OriginalEventToken Action,
    CfgEntry? ExactKey,
    CfgEntry? CaseInsensitiveKey)
{
    public string Name => Action.UpstreamEventCallName!;
    public bool IsResolved => ExactKey is not null || CaseInsensitiveKey is not null;
}

/// <summary>
/// Inventory of every original event-string slot in the pinned (hash-verified) VEH/Rogue.veh, CFG/rogue.cfg and
/// ITM/Rogue_mp.itm (RT-F067 data clause). VEH: f[129], f[130], f[131] of all 30 rows (upstream VehInfo.cs v&gt;=33/38
/// EventString1..3; the position follows from EnergyMax f[94] and the upstream field order, and every row is required to
/// have those three fields quoted, otherwise <see cref="OriginalDataFormatException"/>). CFG: every [Event] key plus
/// every EventString / TeamEventStartGame key in any section. ITM: f[147] of every type-6 row (required quoted).
/// LIO (warp/trap records) is NOT read here: that reader is queue G6. Nothing is executed or interpreted.
/// </summary>
public sealed class OriginalEventInventory
{
    public const int VehFirstEventStringField = 129;
    public const int VehEventStringCount = 3;
    public const int ItmProjectileRecordType = 6;
    public const int ItmDamageEventStringField = 147;
    public const string CfgEventSectionName = "Event";

    /// <summary>Verbs whose argument upstream looks up as an item name (case-insensitive comparison of the verb).</summary>
    public static readonly IReadOnlyList<string> UpstreamItemArgumentVerbs = ["addinv", "wipeinv"];

    private OriginalEventInventory(
        IReadOnlyList<OriginalEventEntry> slots,
        IReadOnlyList<OriginalEventItemReference> items,
        IReadOnlyList<OriginalEventCallReference> calls)
    {
        Slots = slots;
        ItemReferences = items;
        EventCalls = calls;
    }

    /// <summary>Every scanned slot, empty ones included, in order VEH rows, CFG lines, ITM rows.</summary>
    public IReadOnlyList<OriginalEventEntry> Slots { get; }

    /// <summary>The slots holding a non-empty event string.</summary>
    public IEnumerable<OriginalEventEntry> Entries => Slots.Where(s => !s.Script.IsEmpty);

    /// <summary>The scanned slots that are empty.</summary>
    public IEnumerable<OriginalEventEntry> EmptySlots => Slots.Where(s => s.Script.IsEmpty);

    /// <summary>Every unknown token of every entry (empty in the pinned files).</summary>
    public IEnumerable<(OriginalEventEntry Entry, OriginalEventToken Token)> Unknowns =>
        Entries.SelectMany(e => e.Script.Unknowns.Select(t => (e, t)));

    public IReadOnlyList<OriginalEventItemReference> ItemReferences { get; }
    public IReadOnlyList<OriginalEventCallReference> EventCalls { get; }

    /// <summary>Verb spellings exactly as written (ordinal), in order of first appearance.</summary>
    public IReadOnlyList<OriginalEventVerbCount> VerbsExact => Count(StringComparer.Ordinal, a => a.Verb!);

    /// <summary>Verbs folded to lower case (upstream-emulator interpretation of case), with the raw spellings seen.</summary>
    public IReadOnlyList<OriginalEventVerbCount> VerbsFolded => Count(StringComparer.Ordinal, a => a.UpstreamFoldedVerb!);

    private IReadOnlyList<OriginalEventVerbCount> Count(StringComparer cmp, Func<OriginalEventToken, string> key)
    {
        var order = new List<string>();
        var map = new Dictionary<string, (List<string> Spell, List<OriginalEventSource> Src)>(cmp);
        foreach (var e in Entries)
            foreach (var a in e.Script.Actions)
            {
                var k = key(a);
                if (!map.TryGetValue(k, out var v)) { v = ([], []); map[k] = v; order.Add(k); }
                if (!v.Spell.Contains(a.Verb!, StringComparer.Ordinal)) v.Spell.Add(a.Verb!);
                v.Src.Add(e.Source);
            }
        return order.Select(k => new OriginalEventVerbCount(k, map[k].Src.Count, map[k].Spell, map[k].Src)).ToList();
    }

    /// <summary>Loads VEH, CFG and ITM through their hash-verified loaders and builds the inventory.</summary>
    public static OriginalEventInventory Load(string assetsRoot) =>
        From(OriginalVehicleCatalog.Load(assetsRoot), OriginalCfg.LoadRogue(assetsRoot), OriginalItemCatalog.Load(assetsRoot));

    public static OriginalEventInventory From(OriginalVehicleCatalog veh, OriginalCfg cfg, OriginalItemCatalog itm)
    {
        var slots = new List<OriginalEventEntry>();
        foreach (var row in veh.All)
        {
            if (row.FieldCount < VehFirstEventStringField + VehEventStringCount)
                throw new OriginalDataFormatException(row.Record.SourceFile, row.RowNumber, null,
                    $"only {row.FieldCount} fields; event strings expected at f[{VehFirstEventStringField}..{VehFirstEventStringField + VehEventStringCount - 1}].");
            for (var k = 0; k < VehEventStringCount; k++)
            {
                var f = row.Field(VehFirstEventStringField + k);
                if (!f.WasQuoted)
                    throw new OriginalDataFormatException(row.Record.SourceFile, row.RowNumber, f.Index,
                        "expected a quoted event string here (alignment check).");
                slots.Add(new(new(OriginalEventSlotKind.VehEventString, row.Record.SourceFile, row.RowNumber, f.Index, null,
                    $"EventString{k + 1}", row.Id, row.Name), OriginalEventScript.Parse(f.Value)));
            }
        }

        var cfgFile = OriginalCfg.Rogue.RelativePath;
        foreach (var section in cfg.Sections)
            foreach (var e in section.Entries)
            {
                OriginalEventSlotKind? kind =
                    string.Equals(section.Name, CfgEventSectionName, StringComparison.Ordinal) ? OriginalEventSlotKind.CfgEventSection
                    : e.Key == "EventString" && section.Name.StartsWith("Terrain", StringComparison.Ordinal) ? OriginalEventSlotKind.CfgTerrainEventString
                    : e.Key == "EventString" && section.Name.StartsWith("TeamInfo", StringComparison.Ordinal) ? OriginalEventSlotKind.CfgTeamEventString
                    : e.Key == "TeamEventStartGame" ? OriginalEventSlotKind.CfgTeamEventStartGame
                    : e.Key is "EventString" ? throw new OriginalDataException($"{cfgFile} line {e.LineNumber}: EventString in unexpected section [{section.Name}].")
                    : null;
                if (kind is { } k)
                    slots.Add(new(new(k, cfgFile, e.LineNumber, null, section.Name, e.Key, null, section.Name), OriginalEventScript.Parse(e.Value)));
            }

        foreach (var item in itm.Items.Where(i => i.RecordType == ItmProjectileRecordType))
        {
            var f = item.Record.Field(ItmDamageEventStringField);
            if (!f.WasQuoted)
                throw new OriginalDataFormatException(item.Record.SourceFile, item.RowNumber, f.Index,
                    "expected a quoted damage event string here (alignment check).");
            slots.Add(new(new(OriginalEventSlotKind.ItmProjectileDamageEventString, item.Record.SourceFile, item.RowNumber, f.Index, null,
                "damageEventString", item.Id, item.Name), OriginalEventScript.Parse(f.Value)));
        }

        var items = new List<OriginalEventItemReference>();
        var calls = new List<OriginalEventCallReference>();
        var eventSection = cfg.Sections.Where(s => s.Name == CfgEventSectionName).ToList();
        if (eventSection.Count != 1)
            throw new OriginalDataException($"{cfgFile}: {eventSection.Count} [{CfgEventSectionName}] sections, expected exactly one.");
        foreach (var entry in slots.Where(s => !s.Script.IsEmpty))
            foreach (var a in entry.Script.Actions)
            {
                if (a.Argument is { } name && UpstreamItemArgumentVerbs.Contains(a.UpstreamFoldedVerb!))
                    items.Add(new(entry, a,
                        One(itm.Items, i => string.Equals(i.Name, name, StringComparison.Ordinal), name),
                        One(itm.Items, i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase), name)));
                if (a.UpstreamEventCallName is { } target)
                    calls.Add(new(entry, a,
                        One(eventSection[0].Entries, c => string.Equals(c.Key, target, StringComparison.Ordinal), target),
                        One(eventSection[0].Entries, c => string.Equals(c.Key, target, StringComparison.OrdinalIgnoreCase), target)));
            }
        return new OriginalEventInventory(slots, items, calls);
    }

    /// <summary>The single match, null for none; an ambiguous name is reported rather than picked.</summary>
    private static T? One<T>(IEnumerable<T> source, Func<T, bool> match, string name) where T : class
    {
        var m = source.Where(match).Take(2).ToList();
        if (m.Count > 1) throw new OriginalDataException($"event argument '{name}' matches more than one original record.");
        return m.Count == 1 ? m[0] : null;
    }
}
