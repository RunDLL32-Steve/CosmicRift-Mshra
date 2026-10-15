using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RogueTrader.OriginalData;

/// <summary>
/// One comma-separated field exactly as it appears in the source. <see cref="RawText"/> is every character between
/// the delimiters (padding spaces and quotes included); <see cref="Value"/> is the same text with padding removed and
/// surrounding quotes stripped. Nothing is scaled, defaulted or reinterpreted.
/// </summary>
public readonly record struct RawField(int Index, string RawText, string Value, bool WasQuoted);

/// <summary>An integer field read as-is, with the index it came from. No units or scaling are implied.</summary>
/// <param name="FieldIndex">Zero-based index of the source field in its record.</param>
/// <param name="Value">The raw integer from the file.</param>
/// <param name="IsNegativeSentinel">
/// True for any negative raw value (for example the -1 backward-thrust values). The flag only marks that the value is
/// a candidate sentinel whose native meaning is still unvalidated; the value itself is never replaced.
/// </param>
public readonly record struct OriginalInt(int FieldIndex, int Value, bool IsNegativeSentinel)
{
    public override string ToString() => $"f[{FieldIndex}]={Value}" + (IsNegativeSentinel ? " (negative sentinel candidate)" : "");
}

/// <summary>One physical, non-blank line of a CSV-style original file with all fields kept by index.</summary>
public sealed class RawRecord
{
    private static readonly Regex VersionPattern = new(@"^v\d+$", RegexOptions.CultureInvariant);

    internal RawRecord(string sourceFile, int rowNumber, string rawLine, RawField[] fields)
    {
        SourceFile = sourceFile;
        RowNumber = rowNumber;
        RawLine = rawLine;
        Fields = fields;
        VersionToken = FindVersionToken(fields);
    }

    /// <summary>Name of the source (for example <c>VEH/Rogue.veh</c>) as given to the reader.</summary>
    public string SourceFile { get; }

    /// <summary>1-based physical line number in the source (blank lines count, so this equals the catalog "row").</summary>
    public int RowNumber { get; }

    /// <summary>The line text without its terminator.</summary>
    public string RawLine { get; }

    /// <summary>All fields in source order. A trailing comma yields a final empty field.</summary>
    public IReadOnlyList<RawField> Fields { get; }

    /// <summary>
    /// The record's <c>v&lt;number&gt;</c> version token (first of field 0 or 1 matching that shape; RPG keeps it at
    /// f[0], VEH at f[1]), or null when neither does. Kept as text, for example <c>v53</c>.
    /// </summary>
    public string? VersionToken { get; }

    public int FieldCount => Fields.Count;

    /// <summary>The trimmed, unquoted value of a field.</summary>
    public string Value(int index) => Field(index).Value;

    public RawField Field(int index)
    {
        if ((uint)index >= (uint)Fields.Count)
            throw new OriginalDataFormatException(SourceFile, RowNumber, index, $"record has only {Fields.Count} fields.");
        return Fields[index];
    }

    /// <summary>Reads a field as an integer, unscaled. Throws <see cref="OriginalDataFormatException"/> if it is not one.</summary>
    public OriginalInt Int(int index)
    {
        var field = Field(index);
        if (!int.TryParse(field.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            throw new OriginalDataFormatException(SourceFile, RowNumber, index, $"'{field.Value}' is not a 32-bit integer.");
        return new OriginalInt(index, value, value < 0);
    }

    public IReadOnlyList<OriginalInt> Ints(int firstIndex, int count)
    {
        var result = new OriginalInt[count];
        for (var i = 0; i < count; i++) result[i] = Int(firstIndex + i);
        return result;
    }

    private static string? FindVersionToken(RawField[] fields)
    {
        for (var i = 0; i < Math.Min(2, fields.Length); i++)
            if (VersionPattern.IsMatch(fields[i].Value)) return fields[i].Value;
        return null;
    }
}

/// <summary>
/// Reader for the comma-separated original files (.rpg, .veh, .itm). Rules, derived from the file contents:
/// lines end in CRLF (LF or CR alone are accepted); blank lines are skipped but still counted for row numbers;
/// fields are separated by commas with optional space/tab padding; a field whose first non-space character is a
/// double quote runs to the next double quote (so it may contain commas) and may be followed only by padding before the
/// next comma; there is no escape syntax, so a stray quote after the closing quote is an error; a trailing comma
/// produces a final empty field; text is decoded as Latin-1 so every byte round-trips.
/// </summary>
public static class RawRecordReader
{
    public static IReadOnlyList<RawRecord> Parse(ReadOnlySpan<byte> bytes, string sourceFile) =>
        Parse(Encoding.Latin1.GetString(bytes), sourceFile);

    public static IReadOnlyList<RawRecord> Parse(string text, string sourceFile)
    {
        var records = new List<RawRecord>();
        var row = 0;
        var start = 0;
        while (start < text.Length)
        {
            var end = start;
            while (end < text.Length && text[end] != '\r' && text[end] != '\n') end++;
            row++;
            var line = text.Substring(start, end - start);
            if (line.Trim(' ', '\t').Length > 0)
                records.Add(new RawRecord(sourceFile, row, line, ParseLine(line, sourceFile, row)));
            if (end >= text.Length) break;
            start = end + 1;
            if (text[end] == '\r' && start < text.Length && text[start] == '\n') start++;
        }
        return records;
    }

    private static RawField[] ParseLine(string line, string sourceFile, int row)
    {
        var fields = new List<RawField>();
        var pos = 0;
        while (true)
        {
            var start = pos;
            var p = start;
            while (p < line.Length && (line[p] == ' ' || line[p] == '\t')) p++;
            string value;
            bool quoted;
            int end;
            if (p < line.Length && line[p] == '"')
            {
                var close = line.IndexOf('"', p + 1);
                if (close < 0)
                    throw new OriginalDataFormatException(sourceFile, row, fields.Count, "unterminated quoted string.");
                value = line.Substring(p + 1, close - p - 1);
                quoted = true;
                end = close + 1;
                while (end < line.Length && (line[end] == ' ' || line[end] == '\t')) end++;
                if (end < line.Length && line[end] != ',')
                    throw new OriginalDataFormatException(sourceFile, row, fields.Count, "unexpected text after closing quote.");
            }
            else
            {
                end = line.IndexOf(',', p);
                if (end < 0) end = line.Length;
                value = line.Substring(start, end - start).Trim(' ', '\t');
                quoted = false;
            }
            fields.Add(new RawField(fields.Count, line.Substring(start, end - start), value, quoted));
            if (end >= line.Length) break;
            pos = end + 1;
        }
        return fields.ToArray();
    }
}
