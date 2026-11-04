using System.Text.Json.Nodes;

namespace RogueTrader.Persistence;

/// <summary>Migrations the persistence core ships, applied automatically when a file is older than <see cref="PlayerProfile.CurrentSchemaVersion"/>.</summary>
public static class ProfileSchemaMigrations
{
    /// <summary>
    /// Schema 2 adds the optional <c>repairId</c> to applied transactions (queue M5). A schema 1 payload is already valid schema 2
    /// content (the field is simply absent), so the step only advances the version. The envelope version is what lets an older
    /// build refuse a schema 2 file as newer instead of quarantining it as corrupt.
    /// </summary>
    public static IProfileMigration V1ToV2 { get; } = new Step(1, static p => (JsonObject)p.DeepClone());

    /// <summary>
    /// Schema 3 adds the optional <c>ship</c> condition (hull and position, queue M8). Older payloads simply lack it, so the step
    /// only advances the version; an older build then refuses a schema 3 file as newer instead of quarantining it.
    /// </summary>
    public static IProfileMigration V2ToV3 { get; } = new Step(2, static p => (JsonObject)p.DeepClone());

    /// <summary>
    /// Schema 4 adds the <c>Refine</c> transaction kind (queue M7 mining slice). Older payloads never contain it, so the step only
    /// advances the version; an older build then refuses a schema 4 file as newer instead of quarantining an unknown kind as corrupt.
    /// </summary>
    public static IProfileMigration V3ToV4 { get; } = new Step(3, static p => (JsonObject)p.DeepClone());

    /// <summary>
    /// Schema 5 adds the optional <c>bounty</c>, <c>reputation</c> and per-transaction <c>conduct</c> fields and the
    /// <c>RecordConduct</c> kind (queue C9). Older payloads lack them (no bounty, neutral standing), so the step only advances the
    /// version; an older build then refuses a schema 5 file as newer.
    /// </summary>
    public static IProfileMigration V4ToV5 { get; } = new Step(4, static p => (JsonObject)p.DeepClone());

    /// <summary>
    /// Schema 6 adds <c>ownedShips</c> (one of each hull, Steve, Nov 3 2026) and the <c>BuyShip</c> and <c>ShipDestroyed</c> kinds.
    /// Every existing field is carried over unchanged: the hull a schema 5 profile flies becomes its only owned ship, still active,
    /// and its saved condition stays in <c>ship</c>. A profile with no hull owns no ship. The file on disk is not touched here;
    /// the repository keeps a copy of the pre-migration file before the first save at the new version.
    /// </summary>
    public static IProfileMigration V5ToV6 { get; } = new Step(5, static p =>
    {
        var next = (JsonObject)p.DeepClone();
        var owned = new JsonObject();
        if (next["shipHullId"] is JsonValue hull && hull.TryGetValue<string>(out var hullId) && hullId.Length > 0)
            owned[hullId] = null;
        next["ownedShips"] = owned;
        return next;
    });

    /// <summary>
    /// Schema 7 adds the optional per-ship <c>coordinateVersion</c> (review R1-2 on #118). Every condition saved before it was
    /// saved in the practice sector's coordinates, which is what an absent field reads as, so the step only advances the
    /// version; an older build then refuses a schema 7 file as newer instead of reading map positions as practice positions.
    /// </summary>
    public static IProfileMigration V6ToV7 { get; } = new Step(6, static p => (JsonObject)p.DeepClone());

    public static IReadOnlyList<IProfileMigration> BuiltIn { get; } = [V1ToV2, V2ToV3, V3ToV4, V4ToV5, V5ToV6, V6ToV7];

    private sealed class Step(int from, Func<JsonObject, JsonObject> migrate) : IProfileMigration
    {
        public int FromVersion => from;
        public JsonObject Migrate(JsonObject payload) => migrate(payload);
    }
}
