using System.Text;
using System.Text.Json.Nodes;
using RogueTrader.Persistence;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>Persistence and recovery behaviour of the isolated local save. Fixture values are NEW provisional test data.</summary>
public sealed class FileProfileRepositoryTests
{
    private static readonly PersistenceOptions Options = new();
    private static readonly IReadOnlyList<IProfileMigration> NoMigrations = Array.Empty<IProfileMigration>();

    private static FileProfileRepository Open(TempDirectory dir, TimeProvider? time = null) =>
        FileProfileRepository.Open(dir.Path, Options, NoMigrations, time);

    private static string Primary(TempDirectory d, string id = Fx.ProfileId) => d.File(id + ".profile.json");
    private static string Backup(TempDirectory d, string id = Fx.ProfileId) => d.File(id + ".profile.bak.json");
    private static string Temp(TempDirectory d, string id = Fx.ProfileId) => d.File(id + ".profile.json.tmp");
    private static string[] Quarantined(TempDirectory d) =>
        Directory.Exists(d.File("quarantine")) ? Directory.GetFiles(d.File("quarantine")).Select(p => Path.GetFileName(p)!).OrderBy(n => n, StringComparer.Ordinal).ToArray() : Array.Empty<string>();

    /// <summary>Creates the profile at revision 0 and saves it through the given number of further revisions (wallet = 100 + revision).</summary>
    private static async Task<PlayerProfile> CreateAndSave(FileProfileRepository repo, int revisions)
    {
        var profile = PlayerProfile.CreateNew(Fx.ProfileId, 100, Fx.Hull); // NEW fixture wallet
        Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(profile)).Status);
        for (var i = 1; i <= revisions; i++)
        {
            profile = profile with { Revision = i, Wallet = 100 + i };
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.SaveAsync(profile, i - 1)).Status);
        }
        return profile;
    }

    [Fact]
    public async Task Load_Missing_ReturnsNotFound()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);

        var result = await repo.LoadAsync(Fx.ProfileId);

        Assert.Equal(ProfileLoadStatus.NotFound, result.Status);
        Assert.Null(result.Profile);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Create_ThenLoad_RoundTrips()
    {
        using var dir = new TempDirectory();
        PlayerProfile created;
        using (var repo = Open(dir)) created = await CreateAndSave(repo, 0);

        using (var repo = Open(dir))
        {
            var loaded = await repo.LoadAsync(Fx.ProfileId);
            Assert.Equal(ProfileLoadStatus.Loaded, loaded.Status);
            ProfileAssert.Equivalent(created, loaded.Profile!);
        }
    }

    [Fact]
    public async Task Create_WhenAnyGenerationExists_ReturnsAlreadyExists()
    {
        foreach (var existing in new Func<TempDirectory, string>[] { d => Primary(d), d => Backup(d), d => Temp(d) })
        {
            using var dir = new TempDirectory();
            using var repo = Open(dir);
            await File.WriteAllBytesAsync(existing(dir), new byte[] { 1 });

            var result = await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, 1, null));

            Assert.Equal(ProfileSaveStatus.AlreadyExists, result.Status);
            Assert.Equal(new byte[] { 1 }, await File.ReadAllBytesAsync(existing(dir)));
        }
    }

    [Fact]
    public async Task Create_WithNonZeroRevisionOrBadInvariants_IsInvalid()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);

        Assert.Equal(ProfileSaveStatus.Invalid, (await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, 1, null) with { Revision = 1 })).Status);
        Assert.Equal(ProfileSaveStatus.Invalid, (await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, -1, null))).Status);
        Assert.False(File.Exists(Primary(dir)));
    }

    [Fact]
    public async Task Save_StaleRevision_ReturnsConflict()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        var rev1 = await CreateAndSave(repo, 1);

        var stale = await repo.SaveAsync(rev1 with { Wallet = 999 }, 0);                 // expected is behind
        var jump = await repo.SaveAsync(rev1 with { Revision = 3 }, 1);                 // not exactly +1
        var wrongNext = await repo.SaveAsync(rev1 with { Revision = 1, Wallet = 5 }, 1); // next.Revision != expected+1

        Assert.Equal(ProfileSaveStatus.RevisionConflict, stale.Status);
        Assert.Equal(ProfileSaveStatus.RevisionConflict, jump.Status);
        Assert.Equal(ProfileSaveStatus.RevisionConflict, wrongNext.Status);
        Assert.Equal(1, stale.StoredRevision);
        Assert.Equal(101, (await repo.LoadAsync(Fx.ProfileId)).Profile!.Wallet);
    }

    [Fact]
    public async Task Save_WithoutPriorLoad_ReturnsConflict()
    {
        using var dir = new TempDirectory();
        PlayerProfile profile;
        using (var repo = Open(dir)) profile = await CreateAndSave(repo, 0);

        using (var repo = Open(dir)) // new instance: it has never loaded or created this profile
        {
            var result = await repo.SaveAsync(profile with { Revision = 1, Wallet = 5 }, 0);

            Assert.Equal(ProfileSaveStatus.RevisionConflict, result.Status);
            Assert.Equal(-1, result.StoredRevision);
            Assert.Equal(100, (await repo.LoadAsync(Fx.ProfileId)).Profile!.Wallet);
        }
    }

    [Fact]
    public async Task Save_InvalidProfile_IsRejectedWithoutTouchingDisk()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        var rev0 = await CreateAndSave(repo, 0);
        var bytes = await File.ReadAllBytesAsync(Primary(dir));

        var negative = await repo.SaveAsync(rev0 with { Revision = 1, Wallet = -1 }, 0);

        Assert.Equal(ProfileSaveStatus.Invalid, negative.Status);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Primary(dir)));
        Assert.False(File.Exists(Backup(dir)));
    }

    [Fact]
    public async Task Save_KeepsPreviousGenerationAsBackup()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        await CreateAndSave(repo, 2);

        var primary = ProfileSerializer.Decode(await File.ReadAllBytesAsync(Primary(dir)), NoMigrations);
        var backup = ProfileSerializer.Decode(await File.ReadAllBytesAsync(Backup(dir)), NoMigrations);

        Assert.Equal(2, primary.Profile!.Revision);
        Assert.Equal(1, backup.Profile!.Revision);
        Assert.False(File.Exists(Temp(dir)));
    }

    [Fact]
    public async Task Save_IoFailure_ReturnsIoErrorAndKeepsPrimary()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        var rev0 = await CreateAndSave(repo, 0);
        Directory.CreateDirectory(Temp(dir)); // a directory squatting on the temp path makes the write fail
        var next = rev0 with { Revision = 1, Wallet = 5 };

        var failed = await repo.SaveAsync(next, 0);

        Assert.Equal(ProfileSaveStatus.IoError, failed.Status);
        Assert.Equal(0, failed.StoredRevision);
        Assert.Contains(failed.Diagnostics, d => d.Code == DiagnosticCode.IoFailure);

        Directory.Delete(Temp(dir));
        var retry = await repo.SaveAsync(next, 0); // tracked revision was left unchanged, so the retry is valid
        Assert.Equal(ProfileSaveStatus.Saved, retry.Status);
        Assert.Equal(5, (await repo.LoadAsync(Fx.ProfileId)).Profile!.Wallet);
    }

    // ---- Crash and corruption recovery ----------------------------------------------------------------------------

    [Fact]
    public async Task Load_OrphanTemp_UsesPrimaryAndQuarantinesTemp()
    {
        using var dir = new TempDirectory();
        using (var repo = Open(dir)) await CreateAndSave(repo, 1);
        await File.WriteAllBytesAsync(Temp(dir), Encoding.UTF8.GetBytes("{\"format\":\"rogue-trader.pla")); // partial write

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);

            Assert.Equal(ProfileLoadStatus.Loaded, result.Status);
            Assert.Equal(1, result.Profile!.Revision);
            Assert.False(File.Exists(Temp(dir)));
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.OrphanTempQuarantined);
            var moved = Assert.Single(Quarantined(dir));
            Assert.EndsWith("." + DiagnosticCode.OrphanTempQuarantined + ".json", moved);
            Assert.StartsWith(Fx.ProfileId + ".", moved);
        }
    }

    [Fact]
    public async Task Load_CrashBetweenTempAndReplace_KeepsLastAcknowledgedState()
    {
        using var dir = new TempDirectory();
        PlayerProfile acknowledged;
        using (var repo = Open(dir)) acknowledged = await CreateAndSave(repo, 1);
        // The process died after writing a complete temp for revision 2 but before File.Replace: never acknowledged.
        var unacknowledged = acknowledged with { Revision = 2, Wallet = 9999 };
        await File.WriteAllBytesAsync(Temp(dir), ProfileSerializer.Serialize(unacknowledged));

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);

            Assert.Equal(ProfileLoadStatus.Loaded, result.Status);
            ProfileAssert.Equivalent(acknowledged, result.Profile!);
            Assert.False(File.Exists(Temp(dir)));
            Assert.Single(Quarantined(dir));
            // The same transaction can now be applied exactly once on top of the acknowledged revision.
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.SaveAsync(unacknowledged, 1)).Status);
        }
    }

    [Fact]
    public async Task Load_FirstCreateCrashedBeforeMove_IsNotFoundAndLeavesNoLiveFiles()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        await File.WriteAllBytesAsync(Temp(dir), ProfileSerializer.Serialize(PlayerProfile.CreateNew(Fx.ProfileId, 1, null)));

        var result = await repo.LoadAsync(Fx.ProfileId);

        Assert.Equal(ProfileLoadStatus.NotFound, result.Status); // nothing was ever acknowledged
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.OrphanTempQuarantined);
        Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, 1, null))).Status);
    }

    [Fact]
    public async Task Load_PrimaryMissingWithBackup_RecoversFromBackup()
    {
        using var dir = new TempDirectory();
        PlayerProfile backupGeneration;
        using (var repo = Open(dir)) backupGeneration = (await CreateAndSave(repo, 2)) with { Revision = 1, Wallet = 101 };
        File.Delete(Primary(dir)); // crash inside File.Replace: primary gone, backup holds the previous generation
        await File.WriteAllBytesAsync(Temp(dir), new byte[] { 0 });

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);

            Assert.Equal(ProfileLoadStatus.RecoveredFromBackup, result.Status);
            Assert.True(result.Succeeded);
            Assert.Equal(1, result.Profile!.Revision);
            Assert.Equal(101, result.Profile.Wallet);
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.RecoveredFromBackup);
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.OrphanTempQuarantined);
            Assert.True(File.Exists(Primary(dir))); // restored through the atomic path
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.SaveAsync(result.Profile with { Revision = 2, Wallet = 102 }, 1)).Status);
        }

        using (var repo = Open(dir))
            Assert.Equal(ProfileLoadStatus.Loaded, (await repo.LoadAsync(Fx.ProfileId)).Status);
    }

    [Fact]
    public async Task Load_TruncatedPrimary_QuarantinesAndRecoversFromBackup()
    {
        using var dir = new TempDirectory();
        using (var repo = Open(dir)) await CreateAndSave(repo, 2);
        var bytes = await File.ReadAllBytesAsync(Primary(dir));
        await File.WriteAllBytesAsync(Primary(dir), bytes[..(bytes.Length / 2)]);

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);

            Assert.Equal(ProfileLoadStatus.RecoveredFromBackup, result.Status);
            Assert.Equal(1, result.Profile!.Revision); // at most the last generation is lost, and it is reported
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.Truncated);
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.PrimaryQuarantined);
            var moved = Assert.Single(Quarantined(dir));
            Assert.EndsWith("." + DiagnosticCode.Truncated + ".json", moved);
        }
    }

    [Fact]
    public async Task Load_BadChecksum_QuarantinesAndRecoversFromBackup()
    {
        using var dir = new TempDirectory();
        using (var repo = Open(dir)) await CreateAndSave(repo, 2);
        var envelope = JsonNode.Parse(await File.ReadAllBytesAsync(Primary(dir)))!.AsObject();
        envelope["sha256"] = new string('0', 64);
        await File.WriteAllTextAsync(Primary(dir), envelope.ToJsonString());

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);

            Assert.Equal(ProfileLoadStatus.RecoveredFromBackup, result.Status);
            Assert.Equal(1, result.Profile!.Revision);
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.ChecksumMismatch);
            Assert.EndsWith("." + DiagnosticCode.ChecksumMismatch + ".json", Assert.Single(Quarantined(dir)));
        }
    }

    [Fact]
    public async Task Load_EditedPayload_IsDetectedByChecksum()
    {
        using var dir = new TempDirectory();
        using (var repo = Open(dir)) await CreateAndSave(repo, 1);
        var envelope = JsonNode.Parse(await File.ReadAllBytesAsync(Primary(dir)))!.AsObject();
        var payload = envelope["payload"]!.GetValue<string>().Replace("\"wallet\":101", "\"wallet\":9999999");
        envelope["payload"] = payload; // checksum left as it was
        await File.WriteAllTextAsync(Primary(dir), envelope.ToJsonString());

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);
            Assert.Equal(ProfileLoadStatus.RecoveredFromBackup, result.Status);
            Assert.Equal(100, result.Profile!.Wallet);
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.ChecksumMismatch);
        }
    }

    [Fact]
    public async Task Load_InvariantViolation_TreatedAsCorrupt()
    {
        using var dir = new TempDirectory();
        PlayerProfile rev1;
        using (var repo = Open(dir)) rev1 = await CreateAndSave(repo, 1);
        // A well-formed, correctly checksummed file whose content breaks an invariant (negative wallet).
        await File.WriteAllBytesAsync(Primary(dir), ProfileSerializer.Serialize(rev1 with { Revision = 2, Wallet = -5 }));

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);

            Assert.Equal(ProfileLoadStatus.RecoveredFromBackup, result.Status);
            Assert.Equal(100, result.Profile!.Wallet);
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.InvariantViolation);
            Assert.EndsWith("." + DiagnosticCode.InvariantViolation + ".json", Assert.Single(Quarantined(dir)));
        }
    }

    [Fact]
    public async Task Load_PrimaryAndBackupCorrupt_ReturnsCorruptNeverEmptyProfile()
    {
        using var dir = new TempDirectory();
        using (var repo = Open(dir)) await CreateAndSave(repo, 2);
        await File.WriteAllBytesAsync(Primary(dir), Encoding.UTF8.GetBytes("{ not json"));
        var backupBytes = await File.ReadAllBytesAsync(Backup(dir));
        await File.WriteAllBytesAsync(Backup(dir), backupBytes[..10]);

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);

            Assert.Equal(ProfileLoadStatus.Corrupt, result.Status);
            Assert.Null(result.Profile);
            Assert.False(result.Succeeded);
            Assert.False(File.Exists(Primary(dir)));
            Assert.False(File.Exists(Backup(dir)));
            Assert.Equal(2, Quarantined(dir).Length); // moved, never deleted
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.PrimaryQuarantined);
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.BackupQuarantined);

            // A second load must not turn the loss into a clean slate, and no fresh profile may be created over it.
            Assert.Equal(ProfileLoadStatus.Corrupt, (await repo.LoadAsync(Fx.ProfileId)).Status);
            Assert.Equal(ProfileSaveStatus.AlreadyExists, (await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, 1, null))).Status);
        }
    }

    [Fact]
    public async Task Load_CorruptPrimaryWithoutBackup_ReturnsCorrupt()
    {
        using var dir = new TempDirectory();
        using (var repo = Open(dir)) await CreateAndSave(repo, 0);
        await File.WriteAllBytesAsync(Primary(dir), Array.Empty<byte>()); // zero-length after a crash

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);
            Assert.Equal(ProfileLoadStatus.Corrupt, result.Status);
            Assert.Null(result.Profile);
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.Truncated);
        }
    }

    [Fact]
    public async Task Load_NewerSchemaVersion_ReturnsUnsupportedAndLeavesFilesUntouched()
    {
        using var dir = new TempDirectory();
        using (var repo = Open(dir)) await CreateAndSave(repo, 2);
        var envelope = JsonNode.Parse(await File.ReadAllBytesAsync(Primary(dir)))!.AsObject();
        envelope["schemaVersion"] = PlayerProfile.CurrentSchemaVersion + 1;
        await File.WriteAllTextAsync(Primary(dir), envelope.ToJsonString());
        var primaryBytes = await File.ReadAllBytesAsync(Primary(dir));
        var backupBytes = await File.ReadAllBytesAsync(Backup(dir));

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);

            Assert.Equal(ProfileLoadStatus.UnsupportedNewerVersion, result.Status);
            Assert.Null(result.Profile); // no silent fall back to the older backup
            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.NewerVersion);
            Assert.Equal(primaryBytes, await File.ReadAllBytesAsync(Primary(dir)));
            Assert.Equal(backupBytes, await File.ReadAllBytesAsync(Backup(dir)));
            Assert.Empty(Quarantined(dir));
            // Not loaded, so saving is impossible.
            var save = await repo.SaveAsync(PlayerProfile.CreateNew(Fx.ProfileId, 1, null) with { Revision = 1 }, 0);
            Assert.Equal(ProfileSaveStatus.RevisionConflict, save.Status);
        }
    }

    [Fact]
    public async Task Load_OlderVersionWithoutMigration_ReturnsMigrationMissingAndLeavesFilesUntouched()
    {
        using var dir = new TempDirectory();
        using (var repo = Open(dir)) await CreateAndSave(repo, 1);
        var envelope = JsonNode.Parse(await File.ReadAllBytesAsync(Primary(dir)))!.AsObject();
        envelope["schemaVersion"] = 0;
        await File.WriteAllTextAsync(Primary(dir), envelope.ToJsonString());
        var primaryBytes = await File.ReadAllBytesAsync(Primary(dir));

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);

            Assert.Equal(ProfileLoadStatus.MigrationMissing, result.Status);
            Assert.Equal(primaryBytes, await File.ReadAllBytesAsync(Primary(dir)));
            Assert.Empty(Quarantined(dir));
        }
    }

    [Fact]
    public async Task Quarantine_FileNamesAreTimestampedAndNeverCollide()
    {
        using var dir = new TempDirectory();
        var time = new FixedTimeProvider(new DateTimeOffset(2031, 2, 3, 4, 5, 6, 789, TimeSpan.Zero)); // NEW fixture clock
        using (var repo = Open(dir, time)) await CreateAndSave(repo, 1);
        await File.WriteAllBytesAsync(Primary(dir), new byte[] { 1 });
        await File.WriteAllBytesAsync(Backup(dir), new byte[] { 2 });

        using (var repo = Open(dir, time))
            Assert.Equal(ProfileLoadStatus.Corrupt, (await repo.LoadAsync(Fx.ProfileId)).Status);

        var names = Quarantined(dir);
        Assert.Equal(2, names.Length);
        Assert.Contains(names, n => n == $"{Fx.ProfileId}.20310203T040506789Z.Truncated.json" || n == $"{Fx.ProfileId}.20310203T040506789Z.MalformedJson.json");
    }

    [Fact]
    public async Task Load_BackupOnlyCorrupt_PrimaryStillLoads()
    {
        using var dir = new TempDirectory();
        using (var repo = Open(dir)) await CreateAndSave(repo, 2);
        await File.WriteAllBytesAsync(Backup(dir), new byte[] { 9, 9 });

        using (var repo = Open(dir))
        {
            var result = await repo.LoadAsync(Fx.ProfileId);
            Assert.Equal(ProfileLoadStatus.Loaded, result.Status);
            Assert.Equal(2, result.Profile!.Revision);
        }
    }

    // ---- Identity, naming, ownership -------------------------------------------------------------------------------

    [Theory]
    [InlineData("../evil")]
    [InlineData("..\\evil")]
    [InlineData("a/b")]
    [InlineData("a.b")]
    [InlineData("")]
    [InlineData("name\n")]
    [InlineData("C:evil")]
    public async Task ProfileId_WithPathCharacters_IsRejected(string badId)
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.LoadAsync(badId));
        var created = await repo.CreateAsync(PlayerProfile.CreateNew(badId, 1, null));
        var saved = await repo.SaveAsync(PlayerProfile.CreateNew(badId, 1, null) with { Revision = 1 }, 0);

        Assert.Equal(ProfileSaveStatus.Invalid, created.Status);
        Assert.Equal(ProfileSaveStatus.Invalid, saved.Status);
        Assert.DoesNotContain(Directory.GetFiles(dir.Path), f => !f.EndsWith(".lock", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(dir.Path, "..", "evil")));
    }

    [Fact]
    public async Task Repository_SecondOpenOnSameRoot_IsRefused()
    {
        using var dir = new TempDirectory();
        using (var first = Open(dir))
        {
            Assert.Throws<ProfileRepositoryLockedException>(() => Open(dir));
            await CreateAndSave(first, 0); // the owner is unaffected
        }

        using var again = Open(dir); // released on Dispose
        Assert.Equal(ProfileLoadStatus.Loaded, (await again.LoadAsync(Fx.ProfileId)).Status);
    }

    [Fact]
    public async Task SaveLoad_PreservesOpaqueIdsVerbatim()
    {
        using var dir = new TempDirectory();
        var tricky = new[] { Fx.OddItem, "Fixture-Ore", "fixture-ore", " padded ", "tab\there", "quote\"back\\slash", "line\nbreak", "😀-emoji" };
        PlayerProfile saved;
        using (var repo = Open(dir))
        {
            var profile = PlayerProfile.CreateNew(Fx.ProfileId, 1, " hull/Ω 1 ");
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(profile)).Status);
            saved = profile with
            {
                Revision = 1,
                Inventory = Env.Items(tricky.Select((t, i) => (t, i + 1)).ToArray()),
                Upgrades = profile.Upgrades.Union(tricky),
            };
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.SaveAsync(saved, 0)).Status);
        }

        using (var repo = Open(dir))
        {
            var loaded = (await repo.LoadAsync(Fx.ProfileId)).Profile!;
            ProfileAssert.Equivalent(saved, loaded);
            Assert.Equal(tricky.Length, loaded.Inventory.Count); // case variants stay distinct
            Assert.Equal(" hull/Ω 1 ", loaded.ShipHullId);
            Assert.Same(StringComparer.Ordinal, loaded.Inventory.KeyComparer);
            Assert.Same(StringComparer.Ordinal, loaded.Upgrades.KeyComparer);
        }
    }

    [Fact]
    public async Task OpenProfile_ThroughProcessor_ReportsCorruptWithoutFabricatingProfile()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        await File.WriteAllBytesAsync(Primary(dir), new byte[] { 0, 0, 0 });
        var proc = new TransactionProcessor(repo, new FixturePriceSource(), new FixtureCargoPolicy(), Options);

        var opened = await proc.OpenProfileAsync(Fx.ProfileId);

        Assert.Equal(ProfileLoadStatus.Corrupt, opened.Status);
        Assert.Null(proc.GetSnapshot(Fx.ProfileId));
        var buy = await proc.BuyAsync(new TransactionContext(Fx.ProfileId, true), new BuyRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, Fx.OreA, 1));
        Assert.Equal(RejectionReason.ProfileNotOpen, buy.Reason);
    }
}
