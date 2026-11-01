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

    public static IReadOnlyList<IProfileMigration> BuiltIn { get; } = [V1ToV2];

    private sealed class Step(int from, Func<JsonObject, JsonObject> migrate) : IProfileMigration
    {
        public int FromVersion => from;
        public JsonObject Migrate(JsonObject payload) => migrate(payload);
    }
}
