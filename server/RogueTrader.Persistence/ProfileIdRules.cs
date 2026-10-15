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
        return !IsReservedDeviceName(id);
    }

    /// <summary>
    /// Windows device names (CON, PRN, AUX, NUL, COM0-9, LPT0-9, any case) redirect "&lt;name&gt;.profile.json" to a
    /// device, so saves would never reach a file. They are refused on every platform to keep ids portable.
    /// </summary>
    private static bool IsReservedDeviceName(string id)
    {
        if (id.Length == 3)
            return id.Equals("CON", StringComparison.OrdinalIgnoreCase) || id.Equals("PRN", StringComparison.OrdinalIgnoreCase)
                || id.Equals("AUX", StringComparison.OrdinalIgnoreCase) || id.Equals("NUL", StringComparison.OrdinalIgnoreCase);
        return id.Length == 4 && char.IsAsciiDigit(id[3])
            && (id.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || id.StartsWith("LPT", StringComparison.OrdinalIgnoreCase));
    }

    public static void ThrowIfInvalid(string? id, string paramName)
    {
        if (!IsValid(id)) throw new ArgumentException("Profile id must match ^[A-Za-z0-9_-]{1,64}$ and not be a reserved device name.", paramName);
    }
}
