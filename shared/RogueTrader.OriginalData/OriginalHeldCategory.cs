namespace RogueTrader.OriginalData;

/// <summary>
/// One raw <c>LimitN</c>/<c>ExtendedLimitN</c> pair of the CFG <c>[HeldCategory]</c> section (RT-F033, data only).
/// Values are as written: -1 appears in the file for "no limit" and is kept as -1 (flagged by <see cref="IsUnlimited"/>,
/// not translated; that -1 means unlimited is an inference, not decoded behaviour). Which ITM category each
/// <see cref="CategoryId"/> denotes is NOT established here.
/// </summary>
public sealed record OriginalHeldCategoryLimit(int CategoryId, int LimitLine, int Limit, int ExtendedLimitLine, int ExtendedLimit)
{
    public const int CategoryCount = 30;

    public bool IsUnlimited => Limit == -1;
    public bool ExtendedIsUnlimited => ExtendedLimit == -1;

    public static IReadOnlyList<OriginalHeldCategoryLimit> All(OriginalCfg cfg)
    {
        var s = cfg.Section("HeldCategory");
        if (s.Entries.Count != CategoryCount * 2)
            throw new OriginalDataException($"CFG [HeldCategory] has {s.Entries.Count} entries, expected {CategoryCount * 2}.");
        var list = new List<OriginalHeldCategoryLimit>();
        for (var i = 1; i <= CategoryCount; i++)
        {
            var l = s.Find($"Limit{i}") ?? throw new OriginalDataException($"CFG [HeldCategory] has no Limit{i}.");
            var e = s.Find($"ExtendedLimit{i}") ?? throw new OriginalDataException($"CFG [HeldCategory] has no ExtendedLimit{i}.");
            list.Add(new OriginalHeldCategoryLimit(i, l.LineNumber, s.Int($"Limit{i}"), e.LineNumber, s.Int($"ExtendedLimit{i}")));
        }
        return list;
    }
}

/// <summary>Raw <c>[Soul]</c> UtilitySlots keys of the Rogue arena: the plain key and the five 100-id range keys, as written.</summary>
public sealed record OriginalUtilitySlots(int Default, IReadOnlyList<(string Key, int Line, int Slots)> Ranges)
{
    public static OriginalUtilitySlots From(OriginalCfg cfg)
    {
        var s = cfg.Section("Soul");
        var ranges = s.Entries
            .Where(e => e.Key.StartsWith("UtilitySlots", StringComparison.OrdinalIgnoreCase) && e.Key.Length > "UtilitySlots".Length)
            .Select(e => (e.Key, e.LineNumber, s.Int(e.Key)))
            .ToList();
        return new OriginalUtilitySlots(s.Int("UtilitySlots"), ranges);
    }
}
