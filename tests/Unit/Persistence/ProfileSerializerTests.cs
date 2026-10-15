using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RogueTrader.Persistence;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>Envelope, checksum, payload, migration hook and invariant checks. Fixture values are NEW provisional test data.</summary>
public sealed class ProfileSerializerTests
{
    private static readonly IReadOnlyList<IProfileMigration> NoMigrations = Array.Empty<IProfileMigration>();
    private static readonly PersistenceOptions Options = new();

    private static PlayerProfile Rich()
    {
        var applied = new AppliedTransaction("fixture-tx-1", TransactionKind.Buy, "abc123", -30,
            Env.Items((Fx.OreA, 3)), null, 70, 1, new DateTimeOffset(2030, 1, 2, 3, 4, 5, 678, TimeSpan.Zero).AddTicks(1234));
        var upgrade = new AppliedTransaction("fixture-tx-2", TransactionKind.BuyUpgrade, "def456", -500,
            ImmutableSortedDictionary.Create<string, int>(StringComparer.Ordinal), Fx.UpgradeX, 20, 2, new DateTimeOffset(2030, 1, 2, 3, 4, 6, TimeSpan.Zero));
        return new PlayerProfile
        {
            ProfileId = Fx.ProfileId,
            Revision = 2,
            Wallet = 20,
            Inventory = Env.Items((Fx.OreA, 3), (Fx.OddItem, 7)),
            Upgrades = ImmutableSortedSet.Create(StringComparer.Ordinal, Fx.UpgradeX, Fx.UpgradeY),
            ShipHullId = Fx.Hull,
            AppliedTransactions = ImmutableList.Create(applied, upgrade),
            Ledger = ImmutableList.Create(
                new WalletLedgerEntry(1, "fixture-tx-1", TransactionKind.Buy, -30, 70, applied.AppliedAtUtc),
                new WalletLedgerEntry(2, "fixture-tx-2", TransactionKind.BuyUpgrade, -500, 20, upgrade.AppliedAtUtc)),
            NextLedgerSequence = 3,
        };
    }

    private static byte[] WithPayload(string payloadJson, int version = 1, string format = ProfileSerializer.FormatName)
    {
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant();
        var envelope = new JsonObject { ["format"] = format, ["schemaVersion"] = version, ["sha256"] = sha, ["payload"] = payloadJson };
        return Encoding.UTF8.GetBytes(envelope.ToJsonString());
    }

    private static JsonObject PayloadOf(PlayerProfile profile)
    {
        var envelope = JsonNode.Parse(ProfileSerializer.Serialize(profile))!.AsObject();
        return JsonNode.Parse(envelope["payload"]!.GetValue<string>())!.AsObject();
    }

    // ---- Round trip ----------------------------------------------------------------------------------------------

    [Fact]
    public void SaveLoad_RoundTripPreservesWalletInventoryUpgradesHullAndWindow()
    {
        var original = Rich();

        var decoded = ProfileSerializer.Decode(ProfileSerializer.Serialize(original), NoMigrations);

        Assert.True(decoded.Succeeded);
        Assert.Null(decoded.Failure);
        ProfileAssert.Equivalent(original, decoded.Profile!);
        Assert.Same(StringComparer.Ordinal, decoded.Profile!.Inventory.KeyComparer);
        Assert.Same(StringComparer.Ordinal, decoded.Profile.Upgrades.KeyComparer);
        Assert.Same(StringComparer.Ordinal, decoded.Profile.AppliedTransactions[0].ItemDeltas.KeyComparer);
    }

    [Fact]
    public async Task SaveLoad_ThroughFileRepository_PreservesEverything()
    {
        using var dir = new TempDirectory();
        var original = Rich() with { Revision = 0 };
        original = original with { AppliedTransactions = ImmutableList<AppliedTransaction>.Empty, Ledger = ImmutableList<WalletLedgerEntry>.Empty, NextLedgerSequence = 1 };
        using (var repo = FileProfileRepository.Open(dir.Path, Options, NoMigrations))
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(original)).Status);
        using (var repo = FileProfileRepository.Open(dir.Path, Options, NoMigrations))
        {
            var loaded = await repo.LoadAsync(Fx.ProfileId);
            ProfileAssert.Equivalent(original, loaded.Profile!);
        }
    }

    [Fact]
    public void Serialize_IsDeterministicUtf8WithoutBomAndSelfDescribing()
    {
        var bytes = ProfileSerializer.Serialize(Rich());

        Assert.Equal(bytes, ProfileSerializer.Serialize(Rich()));
        Assert.NotEqual((byte)0xEF, bytes[0]);
        var envelope = JsonNode.Parse(bytes)!.AsObject();
        Assert.Equal(ProfileSerializer.FormatName, envelope["format"]!.GetValue<string>());
        Assert.Equal(PlayerProfile.CurrentSchemaVersion, envelope["schemaVersion"]!.GetValue<int>());
        var payload = envelope["payload"]!.GetValue<string>();
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        Assert.Equal(expected, envelope["sha256"]!.GetValue<string>());
        Assert.Equal(new[] { "format", "schemaVersion", "sha256", "payload" }, envelope.Select(p => p.Key));
    }

    // ---- Decode failure ordering ---------------------------------------------------------------------------------

    [Fact]
    public void Decode_EveryProperPrefix_IsReportedAsTruncatedNeverSuccess()
    {
        var bytes = ProfileSerializer.Serialize(Rich());

        for (var length = 0; length < bytes.Length; length++)
        {
            var result = ProfileSerializer.Decode(bytes.AsSpan(0, length), NoMigrations);
            Assert.False(result.Succeeded, $"prefix {length} decoded");
            Assert.Equal(DiagnosticCode.Truncated, result.Failure);
        }
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{\"format\": nope}")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"format\":\"rogue-trader.player-profile\",\"schemaVersion\":1}")]
    [InlineData("{\"format\":\"rogue-trader.player-profile\",\"schemaVersion\":\"one\",\"sha256\":\"\",\"payload\":\"\"}")]
    public void Decode_GarbageEnvelope_IsMalformed(string text)
    {
        var result = ProfileSerializer.Decode(Encoding.UTF8.GetBytes(text), NoMigrations);

        Assert.False(result.Succeeded);
        Assert.Equal(DiagnosticCode.MalformedJson, result.Failure);
    }

    [Fact]
    public void Decode_TrailingGarbageAfterEnvelope_IsMalformed()
    {
        var bytes = ProfileSerializer.Serialize(Rich()).Concat(Encoding.UTF8.GetBytes("xyz")).ToArray();

        Assert.Equal(DiagnosticCode.MalformedJson, ProfileSerializer.Decode(bytes, NoMigrations).Failure);
    }

    [Fact]
    public void Decode_WrongFormat_IsReported()
    {
        var bytes = WithPayload("{}", format: "something-else");

        Assert.Equal(DiagnosticCode.WrongFormat, ProfileSerializer.Decode(bytes, NoMigrations).Failure);
    }

    [Fact]
    public void Decode_NewerVersion_StopsBeforeChecksumOrPayload()
    {
        var envelope = JsonNode.Parse(ProfileSerializer.Serialize(Rich()))!.AsObject();
        envelope["schemaVersion"] = PlayerProfile.CurrentSchemaVersion + 5;
        envelope["sha256"] = "bad";            // would be a checksum failure ...
        envelope["payload"] = "{ not json";    // ... and a payload failure, but neither is inspected
        var result = ProfileSerializer.Decode(Encoding.UTF8.GetBytes(envelope.ToJsonString()), NoMigrations);

        Assert.Equal(DiagnosticCode.NewerVersion, result.Failure);
        Assert.Single(result.Diagnostics);
    }

    [Fact]
    public void Decode_ChecksumMismatch_IsReported()
    {
        var envelope = JsonNode.Parse(ProfileSerializer.Serialize(Rich()))!.AsObject();
        envelope["sha256"] = new string('f', 64);

        Assert.Equal(DiagnosticCode.ChecksumMismatch, ProfileSerializer.Decode(Encoding.UTF8.GetBytes(envelope.ToJsonString()), NoMigrations).Failure);
    }

    [Fact]
    public void Decode_UppercaseChecksum_IsNotAccepted()
    {
        var envelope = JsonNode.Parse(ProfileSerializer.Serialize(Rich()))!.AsObject();
        envelope["sha256"] = envelope["sha256"]!.GetValue<string>().ToUpperInvariant();

        Assert.Equal(DiagnosticCode.ChecksumMismatch, ProfileSerializer.Decode(Encoding.UTF8.GetBytes(envelope.ToJsonString()), NoMigrations).Failure);
    }

    [Fact]
    public void Payload_UnknownField_IsRejected()
    {
        var payload = PayloadOf(Rich());
        payload["bonusCredits"] = 1_000_000;

        var result = ProfileSerializer.Decode(WithPayload(payload.ToJsonString()), NoMigrations);

        Assert.False(result.Succeeded);
        Assert.Equal(DiagnosticCode.MalformedJson, result.Failure);
    }

    [Fact]
    public void Payload_MissingField_IsRejected()
    {
        var payload = PayloadOf(Rich());
        payload.Remove("wallet");

        Assert.Equal(DiagnosticCode.MalformedJson, ProfileSerializer.Decode(WithPayload(payload.ToJsonString()), NoMigrations).Failure);
    }

    [Fact]
    public void Payload_WrongTypes_AreRejected()
    {
        var payload = PayloadOf(Rich());
        payload["wallet"] = "lots";

        Assert.Equal(DiagnosticCode.MalformedJson, ProfileSerializer.Decode(WithPayload(payload.ToJsonString()), NoMigrations).Failure);
    }

    [Fact]
    public void Payload_QuantityBeyondInt32_IsRejected()
    {
        var payload = PayloadOf(Rich());
        payload["inventory"]![Fx.OreA] = 3_000_000_000L;

        Assert.False(ProfileSerializer.Decode(WithPayload(payload.ToJsonString()), NoMigrations).Succeeded);
    }

    [Fact]
    public void Payload_NegativeOrZeroInventoryAndWallet_AreInvariantViolations()
    {
        foreach (var mutate in new Action<JsonObject>[]
        {
            p => p["wallet"] = -1,
            p => p["inventory"]![Fx.OreA] = 0,
            p => p["inventory"]![Fx.OreA] = -4,
            p => p["revision"] = -1,
            p => p["profileId"] = "../escape",
            p => p["nextLedgerSequence"] = 1,                       // ledger sequences must be below it
            p => p["upgrades"] = new JsonArray(Fx.UpgradeX, Fx.UpgradeX), // duplicates
            p => p["appliedTransactions"]![1]!["transactionId"] = "fixture-tx-1", // duplicate id
            p => p["appliedTransactions"]![0]!["transactionId"] = "",
        })
        {
            var payload = PayloadOf(Rich());
            mutate(payload);

            var result = ProfileSerializer.Decode(WithPayload(payload.ToJsonString()), NoMigrations);

            Assert.False(result.Succeeded);
            Assert.Equal(DiagnosticCode.InvariantViolation, result.Failure);
        }
    }

    [Fact]
    public void Payload_UnknownTransactionKind_IsRejected()
    {
        var payload = PayloadOf(Rich());
        payload["ledger"]![0]!["kind"] = "Steal";

        Assert.Equal(DiagnosticCode.InvariantViolation, ProfileSerializer.Decode(WithPayload(payload.ToJsonString()), NoMigrations).Failure);
    }

    [Fact]
    public void Decode_OlderVersionWithoutMigration_IsMigrationMissing()
    {
        var result = ProfileSerializer.Decode(WithPayload(PayloadOf(Rich()).ToJsonString(), version: 0), NoMigrations);

        Assert.Equal(DiagnosticCode.MigrationMissing, result.Failure);
    }

    [Fact]
    public void Decode_OlderVersionWithMigration_AppliesItAndReports()
    {
        // Synthetic migration for version 0 (NEW test fixture): the old payload called the wallet "credits".
        var old = PayloadOf(Rich());
        old["credits"] = old["wallet"]!.DeepClone().GetValue<long>();
        old.Remove("wallet");

        var result = ProfileSerializer.Decode(WithPayload(old.ToJsonString(), version: 0), new IProfileMigration[] { new RenameCreditsMigration() });

        Assert.True(result.Succeeded);
        Assert.Equal(20, result.Profile!.Wallet);
        Assert.Equal(PlayerProfile.CurrentSchemaVersion, result.Profile.SchemaVersion);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.MigrationApplied);
    }

    private sealed class RenameCreditsMigration : IProfileMigration
    {
        public int FromVersion => 0;

        public JsonObject Migrate(JsonObject payload)
        {
            payload["wallet"] = payload["credits"]!.GetValue<long>();
            payload.Remove("credits");
            return payload;
        }
    }

    // ---- Migrator --------------------------------------------------------------------------------------------------

    private sealed class RecordingMigration(int from, List<int> log) : IProfileMigration
    {
        public int FromVersion => from;

        public JsonObject Migrate(JsonObject payload)
        {
            log.Add(from);
            payload["touchedBy" + from] = true;
            return payload;
        }
    }

    [Fact]
    public void Migrator_AppliesStepsInOrder()
    {
        var log = new List<int>();
        // Supplied out of order on purpose; synthetic versions 3 -> 6 (NEW test fixture).
        var steps = new IProfileMigration[] { new RecordingMigration(5, log), new RecordingMigration(3, log), new RecordingMigration(4, log) };

        var outcome = ProfileMigrator.Upgrade(new JsonObject(), 3, 6, steps);

        Assert.True(outcome.Succeeded);
        Assert.Equal(new[] { 3, 4, 5 }, log);
        Assert.Equal(6, outcome.ReachedVersion);
        Assert.Null(outcome.MissingFromVersion);
        Assert.True(outcome.Payload!["touchedBy3"]!.GetValue<bool>());
        Assert.True(outcome.Payload["touchedBy5"]!.GetValue<bool>());
    }

    [Fact]
    public void Migrator_MissingStep_ReportsMigrationMissing()
    {
        var log = new List<int>();
        var steps = new IProfileMigration[] { new RecordingMigration(3, log), new RecordingMigration(5, log) }; // no step for 4

        var outcome = ProfileMigrator.Upgrade(new JsonObject(), 3, 6, steps);

        Assert.False(outcome.Succeeded);
        Assert.Null(outcome.Payload);
        Assert.Equal(4, outcome.MissingFromVersion);
        Assert.Equal(4, outcome.ReachedVersion);
        Assert.Equal(new[] { 3 }, log);
    }

    [Fact]
    public void Migrator_AlreadyAtTarget_ReturnsPayloadUnchanged()
    {
        var payload = new JsonObject();

        var outcome = ProfileMigrator.Upgrade(payload, 6, 6, NoMigrations);

        Assert.True(outcome.Succeeded);
        Assert.Same(payload, outcome.Payload);
    }

    // ---- Invariants ------------------------------------------------------------------------------------------------

    [Fact]
    public void Invariants_ValidProfiles_HaveNoViolations()
    {
        Assert.Empty(ProfileInvariants.Validate(Rich(), Options));
        Assert.Empty(ProfileInvariants.Validate(PlayerProfile.CreateNew(Fx.ProfileId, 0, null), Options));
    }

    [Fact]
    public void Invariants_ReportEachBrokenRule()
    {
        var good = Rich();
        var cases = new (string Name, PlayerProfile Profile)[]
        {
            ("schema", good with { SchemaVersion = 2 }),
            ("id", good with { ProfileId = "bad id" }),
            ("revision", good with { Revision = -1 }),
            ("wallet", good with { Wallet = -1 }),
            ("zero qty", good with { Inventory = good.Inventory.SetItem(Fx.OreA, 0) }),
            ("negative qty", good with { Inventory = good.Inventory.SetItem(Fx.OreA, -1) }),
            ("empty item id", good with { Inventory = good.Inventory.Add("", 1) }),
            ("empty upgrade id", good with { Upgrades = good.Upgrades.Add("") }),
            ("empty hull", good with { ShipHullId = "" }),
            ("non-ordinal inventory", good with { Inventory = ImmutableSortedDictionary.Create<string, int>(StringComparer.OrdinalIgnoreCase) }),
            ("long tx id", good with { AppliedTransactions = good.AppliedTransactions.SetItem(0, good.AppliedTransactions[0] with { TransactionId = new string('x', 129) }) }),
            ("duplicate tx id", good with { AppliedTransactions = good.AppliedTransactions.SetItem(1, good.AppliedTransactions[1] with { TransactionId = "fixture-tx-1" }) }),
            ("ledger order", good with { Ledger = good.Ledger.Reverse() }),
            ("ledger >= next", good with { NextLedgerSequence = 2 }),
            ("next < 1", good with { NextLedgerSequence = 0, Ledger = ImmutableList<WalletLedgerEntry>.Empty }),
        };
        foreach (var (name, profile) in cases)
            Assert.True(ProfileInvariants.Validate(profile, Options).Count > 0, name);

        Assert.NotEmpty(ProfileInvariants.Validate(good, new PersistenceOptions { IdempotencyWindow = 1 }));
        Assert.NotEmpty(ProfileInvariants.Validate(good, new PersistenceOptions { LedgerRetention = 1 }));
    }
}
