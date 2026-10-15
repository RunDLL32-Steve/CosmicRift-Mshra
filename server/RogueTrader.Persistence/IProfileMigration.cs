using System.Text.Json.Nodes;

namespace RogueTrader.Persistence;

/// <summary>Upgrades a payload from <see cref="FromVersion"/> to <c>FromVersion + 1</c>.</summary>
public interface IProfileMigration
{
    int FromVersion { get; }
    JsonObject Migrate(JsonObject payload);
}
