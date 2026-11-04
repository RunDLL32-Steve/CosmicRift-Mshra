using System.Globalization;
using System.Text.Json;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>A pickup a player made that is not yet saved to that player's profile, kept with the sector so it survives a crash.</summary>
public sealed record PendingPickupGrant(string ProfileId, string WorldInstanceId, CargoPickup Pickup);

/// <summary>A kill reward a player earned that is not yet saved to that player's profile, kept with the sector.</summary>
public sealed record PendingKillGrant(string ProfileId, string SectorInstanceId, TargetKill Kill);

/// <summary>
/// Everything the server keeps about one sector between worlds (owner rule, November 2, 2026: world state lasts as long as the
/// server runs and is not reset by a client relaunch, a logout or a sector reset). <see cref="SectorInstanceId"/> names the
/// sector's lifetime: kill reward ids key on it, so it never changes while the sector is kept. <see cref="SavedAtUtc"/> lets
/// the target-group respawn timer count real time while nobody is in the sector. The pending grants are rewards already
/// taken out of the sector (a pile picked up, a target destroyed) but not yet saved to a profile: they are written with the
/// sector before any profile grant, so a crash between the two can neither refill the sector nor lose the reward.
/// </summary>
public sealed record SectorStateDocument(
    string SectorInstanceId,
    DateTimeOffset SavedAtUtc,
    SectorSnapshot Sector,
    IReadOnlyList<PendingPickupGrant> PendingPickups,
    IReadOnlyList<PendingKillGrant> PendingKills);

public enum SectorStateLoadStatus
{
    /// <summary>No saved sector: start it fresh.</summary>
    NotFound,
    Loaded,
    /// <summary>The primary file was unreadable; the previous generation was used.</summary>
    RecoveredFromBackup,
    /// <summary>No generation was readable; the files were moved to quarantine (never deleted).</summary>
    Corrupt,
    /// <summary>
    /// The sector had never been saved, and the save of the sector it replaces was carried over (<see cref="SectorStateStore.LoadOrMigrate"/>).
    /// </summary>
    Migrated,
}

public sealed record SectorStateLoadResult(SectorStateLoadStatus Status, SectorStateDocument? Document, string Message);

/// <summary>
/// File store for <see cref="SectorStateDocument"/>s in the local build, where the client hosts the authoritative world: the
/// save folder stands in for the running server, so the sector outlives the process. Open it only while holding the save
/// folder's single-owner lock (<see cref="RogueTrader.Persistence.FileProfileRepository"/>), so two clients never write one
/// sector. Writes are atomic (flushed temp file, then replace keeping one previous generation); unreadable files are
/// quarantined, never deleted. NEW/provisional: a dedicated server will keep this state in memory and its own storage.
/// </summary>
public sealed class SectorStateStore
{
    public const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private readonly string _directory;
    private readonly TimeProvider _time;

    /// <param name="saveRoot">The save folder (the profile repository root); sectors live in its <c>world</c> subfolder.</param>
    public SectorStateStore(string saveRoot, TimeProvider? time = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveRoot);
        _directory = Path.Combine(Path.GetFullPath(saveRoot), "world");
        _time = time ?? TimeProvider.System;
    }

    public DateTimeOffset UtcNow => _time.GetUtcNow();

    private string Primary(string sectorId) => Path.Combine(_directory, FileName(sectorId) + ".sector.json");
    private string Backup(string sectorId) => Path.Combine(_directory, FileName(sectorId) + ".sector.bak.json");
    private string Temp(string sectorId) => Path.Combine(_directory, FileName(sectorId) + ".sector.json.tmp");

    private static string FileName(string sectorId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sectorId);
        return sectorId.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
            ? sectorId
            : Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(sectorId)).ToLowerInvariant();
    }

    public SectorStateLoadResult Load(string sectorId)
    {
        var primary = Primary(sectorId);
        var backup = Backup(sectorId);
        if (File.Exists(Temp(sectorId))) Quarantine(Temp(sectorId), sectorId, "orphan-temp");
        var hadFiles = false;
        if (File.Exists(primary))
        {
            hadFiles = true;
            if (TryRead(primary, sectorId, out var document, out var why))
                return new SectorStateLoadResult(SectorStateLoadStatus.Loaded, document, "Sector state loaded.");
            Quarantine(primary, sectorId, "primary");
            if (File.Exists(backup) && TryRead(backup, sectorId, out document, out _))
            {
                Write(sectorId, document!); // the recovered generation becomes the primary again
                return new SectorStateLoadResult(SectorStateLoadStatus.RecoveredFromBackup, document,
                    $"Sector state was unreadable ({why}); recovered the previous save.");
            }
        }
        else if (File.Exists(backup) && TryRead(backup, sectorId, out var older, out _))
        {
            Write(sectorId, older!);
            return new SectorStateLoadResult(SectorStateLoadStatus.RecoveredFromBackup, older, "Sector state recovered from the previous save.");
        }
        if (File.Exists(backup))
        {
            hadFiles = true;
            Quarantine(backup, sectorId, "backup");
        }
        return hadFiles
            ? new SectorStateLoadResult(SectorStateLoadStatus.Corrupt, null, "Sector state was unreadable and was quarantined; the sector starts afresh.")
            : new SectorStateLoadResult(SectorStateLoadStatus.NotFound, null, "No saved sector state; the sector starts afresh.");
    }

    /// <summary>
    /// Loads <paramref name="sectorId"/>, or, only when that sector has never been saved in this folder, the save of the sector
    /// it replaces (<paramref name="legacySectorId"/>), renamed to <paramref name="sectorId"/> (review R1-1). "Never saved" means
    /// no primary, backup or temp file and nothing in quarantine for it: a corrupt save is quarantined, never deleted, so it keeps
    /// counting as saved on every later load, and a corrupt sector starts afresh instead of rewinding to the older save. The
    /// legacy save is retired (moved to <c>world/retired</c>, never deleted) when this sector is first saved, and at once when
    /// this sector turns out corrupt; until then a crash simply carries it over again, so the move is idempotent.
    /// </summary>
    public SectorStateLoadResult LoadOrMigrate(string sectorId, string legacySectorId)
    {
        ArgumentException.ThrowIfNullOrEmpty(legacySectorId);
        if (string.Equals(sectorId, legacySectorId, StringComparison.Ordinal)) return Load(sectorId);
        var everSaved = EverSaved(sectorId);
        var result = Load(sectorId);
        if (result.Status == SectorStateLoadStatus.Corrupt || (everSaved && result.Status == SectorStateLoadStatus.NotFound))
        {
            RetireLegacy(legacySectorId);
            return result;
        }
        if (result.Status != SectorStateLoadStatus.NotFound) return result;
        var legacy = Load(legacySectorId);
        if (legacy.Document is not { } old) return result;
        _retireOnSave[sectorId] = legacySectorId;
        return new SectorStateLoadResult(SectorStateLoadStatus.Migrated, old with { Sector = old.Sector with { SectorId = sectorId } },
            $"The '{legacySectorId}' sector save was carried over to '{sectorId}'.");
    }

    /// <summary>True when the folder holds, or ever quarantined, a file of this sector.</summary>
    public bool EverSaved(string sectorId)
    {
        if (File.Exists(Primary(sectorId)) || File.Exists(Backup(sectorId)) || File.Exists(Temp(sectorId))) return true;
        var quarantine = Path.Combine(_directory, "quarantine");
        return Directory.Exists(quarantine) && Directory.EnumerateFiles(quarantine, FileName(sectorId) + ".*").Any();
    }

    private readonly Dictionary<string, string> _retireOnSave = new(StringComparer.Ordinal);

    /// <summary>Moves every file of the legacy sector to <c>world/retired</c>, so it can never be loaded again.</summary>
    private void RetireLegacy(string legacySectorId)
    {
        foreach (var path in new[] { Primary(legacySectorId), Backup(legacySectorId), Temp(legacySectorId) })
        {
            if (!File.Exists(path)) continue;
            var folder = Path.Combine(_directory, "retired");
            Directory.CreateDirectory(folder);
            var stamp = _time.GetUtcNow().UtcDateTime.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
            var destination = Path.Combine(folder, $"{stamp}.{Path.GetFileName(path)}");
            for (var n = 2; File.Exists(destination); n++) destination = Path.Combine(folder, $"{stamp}.{n}.{Path.GetFileName(path)}");
            File.Move(path, destination);
        }
    }

    /// <summary>Writes the sector atomically. IO failures throw; the caller must not grant rewards that this save did not cover.</summary>
    public void Save(SectorStateDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Sector);
        ArgumentException.ThrowIfNullOrEmpty(document.SectorInstanceId);
        Write(document.Sector.SectorId, document);
        // The carried-over sector is now saved under its own id: the legacy save can never be loaded again.
        if (_retireOnSave.Remove(document.Sector.SectorId, out var legacy)) RetireLegacy(legacy);
    }

    private void Write(string sectorId, SectorStateDocument document)
    {
        Directory.CreateDirectory(_directory);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new Envelope(SchemaVersion, document), Json);
        var temp = Temp(sectorId);
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(flushToDisk: true);
        }
        var primary = Primary(sectorId);
        if (File.Exists(primary)) File.Replace(temp, primary, Backup(sectorId), ignoreMetadataErrors: true);
        else File.Move(temp, primary);
    }

    private static bool TryRead(string path, string sectorId, out SectorStateDocument? document, out string why)
    {
        document = null;
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllBytes(path), Json);
            if (envelope is null || envelope.Version != SchemaVersion) why = $"schema {envelope?.Version.ToString(CultureInfo.InvariantCulture) ?? "missing"}";
            else if (envelope.State is not { Sector: not null } state || string.IsNullOrEmpty(state.SectorInstanceId)) why = "incomplete";
            else if (state.Sector.SectorId != sectorId) why = $"holds sector '{state.Sector.SectorId}'";
            else
            {
                document = state with { PendingPickups = state.PendingPickups ?? [], PendingKills = state.PendingKills ?? [] };
                why = "";
                return true;
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            why = exception.Message;
        }
        return false;
    }

    private void Quarantine(string source, string sectorId, string kind)
    {
        var folder = Path.Combine(_directory, "quarantine");
        Directory.CreateDirectory(folder);
        var stamp = _time.GetUtcNow().UtcDateTime.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
        var destination = Path.Combine(folder, $"{FileName(sectorId)}.{stamp}.{kind}.json");
        for (var n = 2; File.Exists(destination); n++) destination = Path.Combine(folder, $"{FileName(sectorId)}.{stamp}.{kind}.{n}.json");
        File.Move(source, destination);
    }

    private sealed record Envelope(int Version, SectorStateDocument? State);
}
