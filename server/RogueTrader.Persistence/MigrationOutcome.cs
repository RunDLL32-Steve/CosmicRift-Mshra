using System.Text.Json.Nodes;

namespace RogueTrader.Persistence;

/// <param name="Succeeded">True when the payload reached the target version.</param>
/// <param name="Payload">The migrated payload (the input when no step was needed); null on failure.</param>
/// <param name="ReachedVersion">Highest version reached.</param>
/// <param name="MissingFromVersion">On failure, the FromVersion for which no step was supplied.</param>
public sealed record MigrationOutcome(bool Succeeded, JsonObject? Payload, int ReachedVersion, int? MissingFromVersion);
