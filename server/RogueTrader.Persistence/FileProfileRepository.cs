using System.Globalization;

namespace RogueTrader.Persistence;

/// <summary>
/// Isolated local save: one directory, one owner. Atomic temp-write plus replace, one previous generation as backup,
/// SHA-256 checksum, quarantine (never delete) of unreadable files, schema version with a migration hook.
/// </summary>
public sealed class FileProfileRepository : IProfileRepository, IDisposable
{
    private static readonly IReadOnlyList<PersistenceDiagnostic> NoDiagnostics = Array.Empty<PersistenceDiagnostic>();

    private readonly string _root;
    private readonly string _quarantineDir;
    private readonly PersistenceOptions _options;
    private readonly IReadOnlyList<IProfileMigration> _migrations;
    private readonly TimeProvider _time;
    private readonly ProfileCoordinator _coordinator = new();
    private readonly FileStream _lockStream;
    private bool _disposed;

    private FileProfileRepository(string root, PersistenceOptions options, IReadOnlyList<IProfileMigration> migrations, TimeProvider time, FileStream lockStream)
    {
        _root = root;
        _quarantineDir = Path.Combine(root, "quarantine");
        _options = options;
        _migrations = migrations;
        _time = time;
        _lockStream = lockStream;
    }

    /// <summary>Opens (creating if needed) the repository root and takes the single-owner lock until <see cref="Dispose"/>.</summary>
    public static FileProfileRepository Open(string rootDirectory, PersistenceOptions options, IReadOnlyList<IProfileMigration> migrations, TimeProvider? time = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootDirectory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(migrations);
        var root = Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(root);
        FileStream lockStream;
        try
        {
            lockStream = new FileStream(Path.Combine(root, ".repository.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException ex)
        {
            throw new ProfileRepositoryLockedException(root, ex);
        }
        return new FileProfileRepository(root, options, migrations.ToArray(), time ?? TimeProvider.System, lockStream);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lockStream.Dispose();
        _coordinator.Dispose();
    }

    private string Primary(string id) => Path.Combine(_root, id + ".profile.json");
    private string Backup(string id) => Path.Combine(_root, id + ".profile.bak.json");
    private string Temp(string id) => Path.Combine(_root, id + ".profile.json.tmp");

    // ---- Load ---------------------------------------------------------------------------------------------

    public async Task<ProfileLoadResult> LoadAsync(string profileId, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ProfileIdRules.ThrowIfInvalid(profileId, nameof(profileId));
        using (await _coordinator.LockAsync(profileId, ct).ConfigureAwait(false))
        {
            var diagnostics = new List<PersistenceDiagnostic>();
            try { return LoadCore(profileId, diagnostics); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                diagnostics.Add(new PersistenceDiagnostic(DiagnosticCode.IoFailure, ex.Message, null));
                return new ProfileLoadResult(ProfileLoadStatus.IoError, null, diagnostics);
            }
        }
    }

    private ProfileLoadResult LoadCore(string id, List<PersistenceDiagnostic> diagnostics)
    {
        var primary = Primary(id);
        var backup = Backup(id);
        var temp = Temp(id);

        // (1) An existing temp file was never acknowledged: quarantine it.
        if (File.Exists(temp))
            diagnostics.Add(Quarantine(temp, id, DiagnosticCode.OrphanTempQuarantined, DiagnosticCode.OrphanTempQuarantined,
                "Unacknowledged temporary write file moved to quarantine."));

        var quarantinedGeneration = false;

        // (2)-(4) Primary.
        if (File.Exists(primary))
        {
            var decoded = ReadAndDecode(primary);
            if (decoded.Succeeded)
            {
                diagnostics.AddRange(WithPath(decoded.Diagnostics, primary));
                _coordinator.SetTracked(id, decoded.Profile!.Revision);
                return new ProfileLoadResult(ProfileLoadStatus.Loaded, decoded.Profile, diagnostics);
            }
            diagnostics.AddRange(WithPath(decoded.Diagnostics, primary));
            if (decoded.Failure == DiagnosticCode.NewerVersion)
                return new ProfileLoadResult(ProfileLoadStatus.UnsupportedNewerVersion, null, diagnostics);
            if (decoded.Failure == DiagnosticCode.MigrationMissing)
                return new ProfileLoadResult(ProfileLoadStatus.MigrationMissing, null, diagnostics);
            diagnostics.Add(Quarantine(primary, id, decoded.Failure!.Value, DiagnosticCode.PrimaryQuarantined,
                "Unreadable primary profile moved to quarantine."));
            quarantinedGeneration = true;
        }

        // (5) Backup.
        if (File.Exists(backup))
        {
            var decoded = ReadAndDecode(backup);
            diagnostics.AddRange(WithPath(decoded.Diagnostics, backup));
            if (decoded.Succeeded)
            {
                var profile = decoded.Profile!;
                try { WriteAtomic(id, ProfileSerializer.Serialize(profile)); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    diagnostics.Add(new PersistenceDiagnostic(DiagnosticCode.IoFailure,
                        "Recovered from backup but could not restore the primary file: " + ex.Message, primary));
                }
                diagnostics.Add(new PersistenceDiagnostic(DiagnosticCode.RecoveredFromBackup,
                    $"Recovered revision {profile.Revision} from the backup generation.", backup));
                _coordinator.SetTracked(id, profile.Revision);
                return new ProfileLoadResult(ProfileLoadStatus.RecoveredFromBackup, profile, diagnostics);
            }
            if (decoded.Failure == DiagnosticCode.NewerVersion)
                return new ProfileLoadResult(ProfileLoadStatus.UnsupportedNewerVersion, null, diagnostics);
            if (decoded.Failure == DiagnosticCode.MigrationMissing)
                return new ProfileLoadResult(ProfileLoadStatus.MigrationMissing, null, diagnostics);
            diagnostics.Add(Quarantine(backup, id, decoded.Failure!.Value, DiagnosticCode.BackupQuarantined,
                "Unreadable backup profile moved to quarantine."));
            return new ProfileLoadResult(ProfileLoadStatus.Corrupt, null, diagnostics);
        }

        // (6) Never fabricate a profile: only a clean absence is NotFound. A generation quarantined by an earlier
        // call (which already returned Corrupt) must keep the profile Corrupt, otherwise a second Load would say
        // NotFound and invite the caller to start a fresh profile over the lost progress.
        if (!quarantinedGeneration && HasQuarantinedGeneration(id))
        {
            diagnostics.Add(new PersistenceDiagnostic(DiagnosticCode.PrimaryQuarantined,
                "No live profile files exist, but earlier unreadable generations are in quarantine; an operator must resolve them.", _quarantineDir));
            quarantinedGeneration = true;
        }
        return new ProfileLoadResult(quarantinedGeneration ? ProfileLoadStatus.Corrupt : ProfileLoadStatus.NotFound, null, diagnostics);
    }

    /// <summary>True if the quarantine folder holds a primary/backup generation of this profile (orphan temp files do not count).</summary>
    private bool HasQuarantinedGeneration(string id)
    {
        if (!Directory.Exists(_quarantineDir)) return false;
        var orphanMarker = "." + DiagnosticCode.OrphanTempQuarantined + ".";
        foreach (var path in Directory.EnumerateFiles(_quarantineDir, id + ".*.json"))
        {
            var name = Path.GetFileName(path);
            if (name.StartsWith(id + ".", StringComparison.Ordinal) && !name.Contains(orphanMarker, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private ProfileDecodeResult ReadAndDecode(string path) => ProfileSerializer.Decode(File.ReadAllBytes(path), _migrations);

    private static IEnumerable<PersistenceDiagnostic> WithPath(IEnumerable<PersistenceDiagnostic> items, string path) =>
        items.Select(d => d with { FilePath = path });

    private PersistenceDiagnostic Quarantine(string source, string id, DiagnosticCode nameCode, DiagnosticCode reportCode, string message)
    {
        Directory.CreateDirectory(_quarantineDir);
        var stamp = _time.GetUtcNow().UtcDateTime.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
        var destination = Path.Combine(_quarantineDir, $"{id}.{stamp}.{nameCode}.json");
        for (var n = 2; File.Exists(destination); n++)
            destination = Path.Combine(_quarantineDir, $"{id}.{stamp}.{nameCode}.{n}.json");
        File.Move(source, destination);
        return new PersistenceDiagnostic(reportCode, $"{message} Moved to '{destination}'.", source);
    }

    // ---- Create / Save ------------------------------------------------------------------------------------

    public async Task<ProfileSaveResult> CreateAsync(PlayerProfile initial, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(initial);
        if (!ProfileIdRules.IsValid(initial.ProfileId)) return Invalid("ProfileId is invalid.");
        using (await _coordinator.LockAsync(initial.ProfileId, ct).ConfigureAwait(false))
        {
            if (initial.Revision != 0) return Invalid("A new profile must have Revision 0.");
            var violations = ProfileInvariants.Validate(initial, _options);
            if (violations.Count > 0) return Invalid(string.Join("; ", violations));
            try
            {
                var id = initial.ProfileId;
                if (File.Exists(Primary(id)) || File.Exists(Backup(id)) || File.Exists(Temp(id)) || HasQuarantinedGeneration(id))
                    return new ProfileSaveResult(ProfileSaveStatus.AlreadyExists, _coordinator.TrackedOrMinusOne(id), NoDiagnostics);
                WriteAtomic(id, ProfileSerializer.Serialize(initial));
                _coordinator.SetTracked(id, 0);
                return new ProfileSaveResult(ProfileSaveStatus.Saved, 0, NoDiagnostics);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return IoError(initial.ProfileId, ex);
            }
        }
    }

    public async Task<ProfileSaveResult> SaveAsync(PlayerProfile next, long expectedRevision, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(next);
        if (!ProfileIdRules.IsValid(next.ProfileId)) return Invalid("ProfileId is invalid.");
        using (await _coordinator.LockAsync(next.ProfileId, ct).ConfigureAwait(false))
        {
            if (!_coordinator.CasHolds(next.ProfileId, expectedRevision, next.Revision))
                return new ProfileSaveResult(ProfileSaveStatus.RevisionConflict, _coordinator.TrackedOrMinusOne(next.ProfileId), NoDiagnostics);
            var violations = ProfileInvariants.Validate(next, _options);
            if (violations.Count > 0) return Invalid(string.Join("; ", violations));
            try
            {
                WriteAtomic(next.ProfileId, ProfileSerializer.Serialize(next));
                _coordinator.SetTracked(next.ProfileId, next.Revision);
                return new ProfileSaveResult(ProfileSaveStatus.Saved, next.Revision, NoDiagnostics);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return IoError(next.ProfileId, ex);
            }
        }
    }

    /// <summary>Temp write (write-through, flushed to disk) then Replace (keeping the previous primary as backup) or Move for the first generation.</summary>
    private void WriteAtomic(string id, byte[] bytes)
    {
        var primary = Primary(id);
        var temp = Temp(id);
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(flushToDisk: true);
        }
        if (File.Exists(primary)) File.Replace(temp, primary, Backup(id), ignoreMetadataErrors: true);
        else File.Move(temp, primary);
    }

    private static ProfileSaveResult Invalid(string message) =>
        new(ProfileSaveStatus.Invalid, -1, new[] { new PersistenceDiagnostic(DiagnosticCode.InvariantViolation, message, null) });

    private ProfileSaveResult IoError(string id, Exception ex) =>
        new(ProfileSaveStatus.IoError, _coordinator.TrackedOrMinusOne(id), new[] { new PersistenceDiagnostic(DiagnosticCode.IoFailure, ex.Message, null) });
}
