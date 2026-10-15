using System.Text.Json.Nodes;

namespace RogueTrader.Persistence;

public static class ProfileMigrator
{
    /// <summary>Applies steps in order from <paramref name="fromVersion"/> to <paramref name="targetVersion"/>; a gap yields a failed outcome.</summary>
    public static MigrationOutcome Upgrade(JsonObject payload, int fromVersion, int targetVersion, IReadOnlyList<IProfileMigration> steps)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(steps);
        var current = payload;
        var version = fromVersion;
        while (version < targetVersion)
        {
            IProfileMigration? step = null;
            foreach (var s in steps)
                if (s.FromVersion == version) { step = s; break; }
            if (step is null) return new MigrationOutcome(false, null, version, version);
            current = step.Migrate(current);
            version++;
        }
        return new MigrationOutcome(true, current, version, null);
    }
}
