using System.Globalization;
using System.Text.Json;

namespace RogueTrader.GameData;

/// <summary>
/// The original radar colours and frame art from data/ui/radar.json: CFG/rogue.cfg [Radar] ColorSelf/ColorFriendly/ColorEnemy and
/// [Uiart] RadarBorder (RT-F064 radar rendering step). Colours are 0xRRGGBB as written; the channel order is a reading, see the JSON.
/// </summary>
public sealed record RadarLook
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string ColorSelf { get; init; } = "";
    public string ColorFriendly { get; init; } = "";
    public string ColorEnemy { get; init; } = "";
    public string BorderArchive { get; init; } = "";
    public string BorderMember { get; init; } = "";

    public int SelfRgb => Parse(ColorSelf);
    public int FriendlyRgb => Parse(ColorFriendly);
    public int EnemyRgb => Parse(ColorEnemy);

    public static RadarLook FromJson(string json)
    {
        var look = JsonSerializer.Deserialize<RadarLook>(json, JsonOptions) ?? throw new JsonException("Empty radar definition.");
        _ = (look.SelfRgb, look.FriendlyRgb, look.EnemyRgb);
        if (look.BorderArchive.Length == 0 || look.BorderMember.Length == 0) throw new JsonException("The radar definition names no border art.");
        return look;
    }

    /// <summary>Parses a <c>0x</c>-prefixed 24-bit colour; anything else throws instead of defaulting.</summary>
    public static int Parse(string token)
    {
        if (!token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(token.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value) || value is < 0 or > 0xFFFFFF)
            throw new JsonException($"Invalid radar colour '{token}'.");
        return value;
    }
}
