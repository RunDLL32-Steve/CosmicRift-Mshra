namespace RogueTrader.Persistence;

/// <summary>
/// Profile ids are used in file names and must match ^[A-Za-z0-9_-]{1,64}$. Checked manually because a
/// regex '$' would accept a trailing newline.
/// </summary>
internal static class ProfileIdRules
{
    public const int MaxLength = 64;

    public static bool IsValid(string? id)
    {
        if (id is null || id.Length == 0 || id.Length > MaxLength) return false;
        foreach (var c in id)
        {
            var ok = c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-';
            if (!ok) return false;
        }
        return true;
    }

    public static void ThrowIfInvalid(string? id, string paramName)
    {
        if (!IsValid(id)) throw new ArgumentException("Profile id must match ^[A-Za-z0-9_-]{1,64}$.", paramName);
    }
}
