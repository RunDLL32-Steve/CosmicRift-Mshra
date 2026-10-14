using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Assets;

try
{
    return Run(args);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"COMPATIBILITY_RUNNER_FAILURE: {exception}");
    return 2;
}

static int Run(string[] args)
{
if (args.Length != 3) throw new ArgumentException("Expected assets-directory, report-directory, upstream-source-directory.");
var assets = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var sourceRoot = Path.GetFullPath(args[2]);
Directory.CreateDirectory(output);
var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
var results = new List<object>();
var failures = 0;
var originalHashes = Directory.GetFiles(assets).ToDictionary(path => Path.GetFileName(path)!, Hash);

Check("cfg", "rogue.cfg", path =>
{
    var cfg = CfgInfo.Load(path);
    Save("pristine-config.json", cfg);
    var sourceCases = Regex.Matches(File.ReadAllText(Path.Combine(sourceRoot,
        "dotnetcore/ZoneServer/Logic/General/Assets.Events.cs")), "case \\\"([^\\\"]+)\\\"")
        .Select(match => match.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var eventCommands = typeof(CfgInfo.Event).GetFields().Where(field => field.FieldType == typeof(string))
        .SelectMany(field => ((string?)field.GetValue(cfg.EventInfo) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(command => new { Field = field.Name, Command = command.Split('=')[0].Trim() }))
        .Where(command => command.Command.Length > 0).ToArray();
    Save("cfg-event-command-audit.json", new
    {
        Evaluation = "Static CFG EventInfo command-name presence in upstream switch cases; not runtime execution proof",
        Commands = eventCommands,
        UnrecognizedBySwitch = eventCommands.Where(command => !sourceCases.Contains(command.Command)).ToArray()
    });
    return new { Overhead = !cfg.level.isometric, cfg.level.lvlFile, cfg.level.itmFile, cfg.level.vehFile };
});
Check("itm", "Rogue_mp.itm", path =>
{
    var items = ItemInfo.Load(path);
    var sourceRows = File.ReadAllLines(path).Count(line => !string.IsNullOrWhiteSpace(line));
    Save("pristine-items.json", items.Select(item => new { Kind = item.GetType().Name, Record = RuntimeRecord(item) }).ToArray());
    var types = items.GroupBy(item => item.itemType.ToString()).ToDictionary(group => group.Key, group => group.Count());
    if (items.Count != sourceRows) throw new InvalidDataException($"Loader skipped records: source={sourceRows}, loaded={items.Count}.");
    return new { Records = items.Count, SourceRows = sourceRows, Versions = items.Select(item => item.version).Distinct().ToArray(), Types = types };
});
Check("veh", "Rogue.veh", path =>
{
    var vehicles = VehInfo.Load(path);
    Save("pristine-vehicles.json", vehicles.Select(vehicle => new { Kind = vehicle.GetType().Name, Record = RuntimeRecord(vehicle) }).ToArray());
    return new { Records = vehicles.Count, Versions = vehicles.Select(vehicle => vehicle.Version).Distinct().ToArray() };
});
Check("lvl", "Rogue.lvl", path =>
{
    var level = LvlInfo.Load(path);
    if (level.Tiles.Length != checked(level.Width * level.Height)) throw new InvalidDataException("Incomplete row-major tile grid.");
    Save("rogue-level-metadata.json", new
    {
        SourceFile = "LVL/Rogue.lvl", SourceSha256 = Hash(path), ServerCommit = "bb97579efeceff1c63d781d8c1bb8dd4a5b9b849",
        level.Width, level.Height, level.OffsetX, level.OffsetY,
        NativeTileStride = 16,
        TileStrideEvidence = "Upstream Arena.getTerrain and LvlInfo.getTileAt divide native coordinates by 16; no conversion to prototype units",
        Ordering = "row-major y*Width+x", level.ObjectBlobs, level.FloorBlobs, level.TerrainLookup, level.PhysicsLow, level.PhysicsHigh,
        level.LightColorWhite, level.LightColorRed, level.LightColorGreen, level.LightColorBlue,
        RawUnknown0Base64 = Convert.ToBase64String(level.Tiles.Select(tile => tile.Unknown0).ToArray()),
        RawUnknown1Base64 = Convert.ToBase64String(level.Tiles.Select(tile => tile.Unknown1).ToArray()),
        RawPhysicsVisionBase64 = Convert.ToBase64String(level.Tiles.Select(tile => tile.PhysicsVision).ToArray()),
        PhysicsIdsBase64 = Convert.ToBase64String(level.Tiles.Select(tile => (byte)tile.Physics).ToArray()),
        VisionIdsBase64 = Convert.ToBase64String(level.Tiles.Select(tile => (byte)tile.Vision).ToArray()),
        TerrainLookupIndicesBase64 = Convert.ToBase64String(level.Tiles.Select(tile => (byte)tile.TerrainLookup).ToArray()),
        BlockedTileCount = level.Tiles.Count(tile => tile.Blocked),
        Limitation = "Upstream LvlInfo reads three RLE tile layers and does not decode trailing graphical entity/object data. Blocked is a raw physics mask; vehicle height/physics modes still affect traversal. No BMP-derived collision."
    });
    return new { level.Width, level.Height, Tiles = level.Tiles.Length, BlockedTiles = level.Tiles.Count(tile => tile.Blocked) };
});
Check("lio", "Rogue.lio", path =>
{
    var lios = LioInfo.Load(path);
    Save("rogue-lio-records.json", new
    {
        SourceFile = "LIO/Rogue.lio", SourceSha256 = Hash(path),
        Interpretation = "Upstream parsed canonical records; offsets, dimensions, relative IDs and graphics fields retained without inventing placement semantics",
        Records = lios.Select(lio => new { Kind = lio.GetType().Name, Record = RuntimeRecord(lio) }).ToArray()
    });
    return new { Records = lios.Count, Types = lios.GroupBy(lio => lio.GeneralData.Type.ToString()).ToDictionary(group => group.Key, group => group.Count()) };
});
Check("rpg", "Rogue.rpg", path =>
{
    var skills = SkillInfo.Load(path);
    Save("pristine-skills.json", skills);
    return new { Records = skills.Count, Versions = skills.Select(skill => skill.Version).Distinct().ToArray(), Hulls = skills.Select(skill => new { skill.SkillId, skill.Name, skill.DefaultVehicleId }).ToArray() };
});
var integrity = originalHashes.All(pair => Hash(Path.Combine(assets, pair.Key!)) == pair.Value);
if (!integrity) failures++;
Save("pristine-load-report.json", new
{
    Purpose = "Completed data-loading compatibility feature validation; no gameplay simulation or original-unit conversion",
    Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    UpstreamCommit = "bb97579efeceff1c63d781d8c1bb8dd4a5b9b849", AssetsUnchanged = integrity,
    Inputs = originalHashes, Checks = results, FailureCount = failures
});
Console.WriteLine($"COMPATIBILITY_DATA_LOAD: failures={failures}; unchanged={integrity}; report={Path.Combine(output, "pristine-load-report.json")}");
return failures == 0 ? 0 : 1;

void Check(string kind, string filename, Func<string, object> load)
{
    var path = Path.Combine(assets, filename);
    try
    {
        var result = load(path);
        results.Add(new { Kind = kind, File = filename, Success = true, Result = result });
        Console.WriteLine($"PARSE_PASS {filename}: {JsonSerializer.Serialize(result)}");
    }
    catch (Exception exception)
    {
        failures++;
        var rowFailures = new List<object>();
        if (kind is "itm" or "veh" or "lio" or "rpg")
        {
            var rows = File.ReadAllLines(path);

            for (var i = 0; i < rows.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(rows[i])) continue;
                var shard = Path.Combine(output, $"diagnostic-record-{i + 1:D4}.{kind}");
                File.WriteAllText(shard, rows[i] + Environment.NewLine);
                try
                {
                    switch (kind)
                    {
                        case "itm": ItemInfo.Load(shard); break;
                        case "veh": VehInfo.Load(shard); break;
                        case "lio": LioInfo.Load(shard); break;
                        case "rpg": SkillInfo.Load(shard); break;
                    }
                }
                catch (Exception rowException)
                {
                    var fields = CSVReader.Parse(rows[i]);
                    rowFailures.Add(new { Line = i + 1, Identity = fields.Take(4).ToArray(), Error = rowException.Message });
                }
            }
        }
        results.Add(new { Kind = kind, File = filename, Success = false, Error = exception.ToString(), RecordFailures = rowFailures });
        Console.WriteLine($"PARSE_FAIL {filename}: {exception.GetType().Name}: {exception.Message}; badRecords={rowFailures.Count}");
    }
}
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
void Save(string filename, object value) => File.WriteAllText(Path.Combine(output, filename), JsonSerializer.Serialize(StructuralValue(value), options));
JsonElement RuntimeRecord(object value) => JsonSerializer.SerializeToElement(StructuralValue(value), options);

// Export public data fields only. Some upstream CFG properties return by-ref dictionaries,
// which are valid runtime APIs but unsupported JSON metadata; do not alter their parser semantics.
object? StructuralValue(object? value)
{
    if (value is null || value is string || value is JsonElement) return value;
    var type = value.GetType();
    if (type.IsEnum) return Convert.ToInt64(value);
    if (type.IsPrimitive || value is decimal) return value;
    if (value is System.Collections.IDictionary dictionary)
    {
        var result = new Dictionary<string, object?>();
        foreach (System.Collections.DictionaryEntry entry in dictionary)
            result[entry.Key.ToString()!] = StructuralValue(entry.Value);
        return result;
    }
    if (value is System.Collections.IEnumerable sequence)
        return sequence.Cast<object?>().Select(StructuralValue).ToArray();
    if (type.Name.Contains("AnonymousType", StringComparison.Ordinal))
        return type.GetProperties().ToDictionary(property => property.Name, property => StructuralValue(property.GetValue(value)));
    return type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
        .ToDictionary(field => field.Name, field => StructuralValue(field.GetValue(value)));
}
}
