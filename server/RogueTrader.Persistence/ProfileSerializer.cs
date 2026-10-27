using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RogueTrader.Persistence;

public static class ProfileSerializer
{
    public const string FormatName = "rogue-trader.player-profile";

    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false,
    };

    /// <summary>Serializes at the current schema version. Does not validate invariants (repositories do that before saving).</summary>
    public static byte[] Serialize(PlayerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var payload = JsonSerializer.Serialize(ProfileDocumentV1.From(profile), PayloadOptions);
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteString("format", FormatName);
            w.WriteNumber("schemaVersion", PlayerProfile.CurrentSchemaVersion);
            w.WriteString("sha256", Sha256Hex(payload));
            w.WriteString("payload", payload);
            w.WriteEndObject();
        }
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Decode order: envelope, format, newer version, checksum, payload parse, migration, mapping, invariants.</summary>
    public static ProfileDecodeResult Decode(ReadOnlySpan<byte> bytes, IReadOnlyList<IProfileMigration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        var data = bytes.ToArray();

        JsonDocument doc;
        try { doc = JsonDocument.Parse(data); }
        catch (JsonException ex)
        {
            return IsTruncated(data)
                ? Fail(DiagnosticCode.Truncated, "The file ends before the JSON envelope is complete.")
                : Fail(DiagnosticCode.MalformedJson, "The file is not valid JSON: " + ex.Message);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Fail(DiagnosticCode.MalformedJson, "The JSON root is not an object.");
            if (!root.TryGetProperty("format", out var formatEl) || formatEl.ValueKind != JsonValueKind.String)
                return Fail(DiagnosticCode.MalformedJson, "The envelope has no 'format' string.");
            if (!root.TryGetProperty("schemaVersion", out var versionEl) || versionEl.ValueKind != JsonValueKind.Number
                || !versionEl.TryGetInt32(out var version))
                return Fail(DiagnosticCode.MalformedJson, "The envelope has no integer 'schemaVersion'.");
            if (!string.Equals(formatEl.GetString(), FormatName, StringComparison.Ordinal))
                return Fail(DiagnosticCode.WrongFormat, $"Unexpected format '{formatEl.GetString()}'.");
            if (version > PlayerProfile.CurrentSchemaVersion)
                return Fail(DiagnosticCode.NewerVersion, $"File schema version {version} is newer than supported version {PlayerProfile.CurrentSchemaVersion}.");
            if (!root.TryGetProperty("sha256", out var shaEl) || shaEl.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("payload", out var payloadEl) || payloadEl.ValueKind != JsonValueKind.String)
                return Fail(DiagnosticCode.MalformedJson, "The envelope has no 'sha256'/'payload' strings.");

            var payloadText = payloadEl.GetString()!;
            if (!string.Equals(Sha256Hex(payloadText), shaEl.GetString(), StringComparison.Ordinal))
                return Fail(DiagnosticCode.ChecksumMismatch, "The payload checksum does not match.");

            JsonObject? payload;
            try { payload = JsonNode.Parse(payloadText) as JsonObject; }
            catch (Exception ex) when (ex is JsonException or ArgumentException)
            { return Fail(DiagnosticCode.MalformedJson, "The payload is not valid JSON: " + ex.Message); }
            if (payload is null) return Fail(DiagnosticCode.MalformedJson, "The payload is not a JSON object.");

            var diagnostics = new List<PersistenceDiagnostic>();
            if (version < PlayerProfile.CurrentSchemaVersion)
            {
                // Caller-supplied steps win; the built-in steps keep files from earlier builds loadable without registration.
                var steps = migrations.Concat(ProfileSchemaMigrations.BuiltIn).ToArray();
                var outcome = ProfileMigrator.Upgrade(payload, version, PlayerProfile.CurrentSchemaVersion, steps);
                if (!outcome.Succeeded)
                    return Fail(DiagnosticCode.MigrationMissing, $"No migration from schema version {outcome.MissingFromVersion} is available.");
                payload = outcome.Payload!;
                diagnostics.Add(new PersistenceDiagnostic(DiagnosticCode.MigrationApplied,
                    $"Migrated schema version {version} to {PlayerProfile.CurrentSchemaVersion}; it is written at the current version on the next save.", null));
            }

            ProfileDocumentV1? document;
            try { document = payload.Deserialize<ProfileDocumentV1>(PayloadOptions); }
            catch (JsonException ex) { return Fail(DiagnosticCode.MalformedJson, "The payload does not match the profile document: " + ex.Message); }
            if (document is null) return Fail(DiagnosticCode.MalformedJson, "The payload is empty.");

            var profile = document.ToProfile(out var mapError);
            if (profile is null) return Fail(DiagnosticCode.InvariantViolation, mapError ?? "The payload cannot be mapped.");

            var violations = ProfileInvariants.Validate(profile, DecodeLimits);
            if (violations.Count > 0)
                return Fail(DiagnosticCode.InvariantViolation, "Invariant violations: " + string.Join("; ", violations));
            return new ProfileDecodeResult(profile, null, diagnostics);
        }
    }

    /// <summary>Decode checks invariants with unbounded window/retention: the caller's configured limits are applied again by the repository.</summary>
    private static readonly PersistenceOptions DecodeLimits = new() { IdempotencyWindow = int.MaxValue, LedgerRetention = int.MaxValue };

    internal static string Sha256Hex(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static bool IsTruncated(byte[] data)
    {
        try
        {
            var reader = new Utf8JsonReader(data, isFinalBlock: false, state: default);
            while (reader.Read()) { }
            return true; // a non-final scan that never faults means the input is a valid prefix
        }
        catch (JsonException) { return false; }
    }

    private static ProfileDecodeResult Fail(DiagnosticCode code, string message) =>
        new(null, code, new[] { new PersistenceDiagnostic(code, message, null) });
}
