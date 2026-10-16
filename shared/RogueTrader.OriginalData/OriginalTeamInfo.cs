using System.Globalization;

namespace RogueTrader.OriginalData;

/// <summary>
/// One <c>[TeamInfoN]</c> block of CFG/rogue.cfg (RT-F052 public militia teams, RT-F053 Free Traders team), data only.
/// Every field is the raw file value; <see cref="Entries"/> keeps all lines in file order (the order of <c>Colors</c> varies
/// between blocks). Established from the archived file: the block layout and values. NOT established here: which engine
/// consumer reads each key, what <c>hAdjust/sAdjust/vAdjust</c> adjust, whether <c>MaxPlayers</c> is a hard limit, and how
/// the spawn rows of Rogue.lio (record type 4, claimed by the queue-G6 LIO reader) map onto teams. No behaviour is implied.
/// </summary>
public sealed record OriginalTeamInfo(
    int Index,
    int HeaderLine,
    int NameLine,
    string Name,
    int DefaultVehicle,
    string ColorsRaw,
    IReadOnlyList<int> ColorValues,
    int RelativeVehicle,
    string RadarColorRaw,
    int RadarColor,
    int NameFontColor,
    int MaxPlayers,
    string EventString,
    int HAdjust,
    int SAdjust,
    int VAdjust,
    int DisallowWinFlagGame,
    IReadOnlyList<int> RtsStates,
    string TeamEventStartGame,
    IReadOnlyList<CfgEntry> Entries)
{
    /// <summary>[TeamInfo0]..[TeamInfo49] are present in rogue.cfg.</summary>
    public const int TeamCount = 50;

    /// <summary>Number of <c>RtsStateN</c> keys (N = 0..7).</summary>
    public const int RtsStateCount = 8;

    /// <summary>
    /// True when the name is literally <c>Public&lt;Index&gt;</c>. This is a pure text-pattern fact about the file; it is
    /// not asserted to mean "unused team" (in the archived file all such blocks also have MaxPlayers=0).
    /// </summary>
    public bool NameIsPublicIndexPattern => string.Equals(Name, $"Public{Index}", StringComparison.Ordinal);

    public static OriginalTeamInfo Read(OriginalCfg cfg, int index)
    {
        var s = cfg.Section($"TeamInfo{index}");
        string Raw(string key) => (s.Find(key) ?? throw new OriginalDataException($"CFG [{s.Name}] has no key '{key}'.")).Value;
        var rts = new int[RtsStateCount];
        for (var i = 0; i < RtsStateCount; i++) rts[i] = s.Int($"RtsState{i}");
        var colors = Raw("Colors");
        return new OriginalTeamInfo(
            index, s.LineNumber, s.Find("Name")!.LineNumber, Raw("Name"),
            s.Int("DefaultVehicle"),
            colors, colors.Length == 0 ? [] : colors.Split(',').Select(ParseColor).ToArray(),
            s.Int("RelativeVehicle"),
            Raw("RadarColor"), ParseColor(Raw("RadarColor")),
            s.Int("NameFontColor"), s.Int("MaxPlayers"), Raw("EventString"),
            s.Int("hAdjust"), s.Int("sAdjust"), s.Int("vAdjust"), s.Int("DisallowWinFlagGame"),
            rts, Raw("TeamEventStartGame"), s.Entries);
    }

    public static IReadOnlyList<OriginalTeamInfo> All(OriginalCfg cfg) =>
        Enumerable.Range(0, TeamCount).Select(i => Read(cfg, i)).ToArray();

    /// <summary>Parses a CFG color token: <c>0x</c>-prefixed hexadecimal or a plain (possibly negative) integer; throws otherwise.</summary>
    public static int ParseColor(string token)
    {
        var t = token.Trim();
        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (uint.TryParse(t.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var h)) return unchecked((int)h);
        }
        else if (int.TryParse(t, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var d)) return d;
        throw new OriginalDataException($"CFG color token '{token}' is neither 0x-hex nor a plain integer.");
    }
}

/// <summary>
/// The raw frequency settings of CFG <c>[Arena]</c> (RT-F053 data only). <see cref="AllowPrivateFrequencies"/> is the value on
/// <see cref="AllowPrivateFrequenciesLine"/>. The neighbouring FrequencyMax / MaxPerFrequency / DesiredFrequencies keys are kept
/// as raw integers under their file names only; their meaning (and how a private frequency is created) is not established.
/// </summary>
public sealed record OriginalArenaFrequencies(
    int AllowPrivateFrequencies, int AllowPrivateFrequenciesLine,
    int FrequencyMax, int MaxPerFrequency, int DesiredFrequencies)
{
    public static OriginalArenaFrequencies Read(OriginalCfg cfg)
    {
        var a = cfg.Section("Arena");
        var allow = a.Find("AllowPrivateFrequencies") ?? throw new OriginalDataException("CFG [Arena] has no key 'AllowPrivateFrequencies'.");
        return new OriginalArenaFrequencies(a.Int("AllowPrivateFrequencies"), allow.LineNumber,
            a.Int("FrequencyMax"), a.Int("MaxPerFrequency"), a.Int("DesiredFrequencies"));
    }
}
