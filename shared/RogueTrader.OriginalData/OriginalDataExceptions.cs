namespace RogueTrader.OriginalData;

/// <summary>Base type for every failure raised while reading the canonical original data.</summary>
public class OriginalDataException : Exception
{
    public OriginalDataException(string message) : base(message) { }
    public OriginalDataException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>A source file is missing, has the wrong byte length, or has the wrong SHA-256.</summary>
public sealed class AssetIntegrityException : OriginalDataException
{
    public AssetIntegrityException(string relativePath, AssetIntegrityFailure failure, string expected, string actual)
        : base($"Original asset '{relativePath}' failed its integrity check ({failure}): expected {expected}, actual {actual}.")
    {
        RelativePath = relativePath;
        Failure = failure;
        Expected = expected;
        Actual = actual;
    }

    public string RelativePath { get; }
    public AssetIntegrityFailure Failure { get; }
    public string Expected { get; }
    public string Actual { get; }
}

public enum AssetIntegrityFailure
{
    Missing,
    LengthMismatch,
    Sha256Mismatch,
    /// <summary>The git blob SHA-1 (sha1("blob &lt;length&gt;\0" + bytes)) differs from the pinned value (used for files pinned by a git blob id, e.g. the archived keys.txt).</summary>
    GitBlobSha1Mismatch,
}

/// <summary>Malformed CSV text or a field that cannot be read as the requested type.</summary>
public sealed class OriginalDataFormatException : OriginalDataException
{
    public OriginalDataFormatException(string sourceFile, int row, int? fieldIndex, string message)
        : base($"{sourceFile} row {row}" + (fieldIndex is { } i ? $" field [{i}]" : "") + ": " + message)
    {
        SourceFile = sourceFile;
        Row = row;
        FieldIndex = fieldIndex;
    }

    public string SourceFile { get; }
    public int Row { get; }
    public int? FieldIndex { get; }
}

/// <summary>An RPG hull row could not be linked to exactly one matching VEH Car row.</summary>
public sealed class OriginalHullLinkException : OriginalDataException
{
    public OriginalHullLinkException(string message) : base(message) { }
}
