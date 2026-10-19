using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Assets;
using InfServer;
using InfServer.Game;

AppDomain.CurrentDomain.UnhandledException += (_, e) => { Console.Error.WriteLine($"ARENA_WORKER_FAILURE: {e.ExceptionObject}"); Environment.Exit(2); };
try { return Run(args); }
catch (Exception exception) { Console.Error.WriteLine($"ARENA_INITIALIZATION_FAILED: {exception}"); return 2; }

static int Run(string[] args)
{
    if (args.Length != 1 || Environment.GetEnvironmentVariable("RT_COMPATIBILITY_OFFLINE") != "1")
        throw new ArgumentException("Expected report path and mandatory offline flag.");
    var cfg = new Xmlconfig("server.xml", false).Settings;
    if (cfg["database/connectionDelay"].intValue != 0 || cfg.Exists("database2") ||
        cfg["server/bindIP"].Value != "127.0.0.1" || cfg["server/gameType"].Value.Length != 0 ||
        cfg["server/updateGlobalNws"].Value.Length != 0 || cfg["server/copyServerFrom"].Value.Length != 0 ||
        cfg["server/pathFindingEnabled"].boolValue || cfg["server/pathFindingEnabledNew"].boolValue ||
        cfg["server/loadBlobs"].boolValue)
        throw new InvalidDataException("Init-only offline probe configuration differs from required isolation.");
    var sourceHashes = Directory.GetFiles("assets").ToDictionary(path => Path.GetFileName(path)!, Hash);
    if (!Log.init()) throw new IOException("Logger initialization failed.");
    Log.assume(Log.createClient("OriginalArenaProbe"));
    var unexpectedErrors = new List<string>();
    Log.LogMessage errorSink = (type, _, message) => { if (type is TLog.Error or TLog.Exception) unexpectedErrors.Add(message); };
    Log.eError += errorSink;
    try
    {
    InfServer.Logic.Registrar.register();
    var server = new ZoneServer();
    if (!server.init() || !server.IsStandalone) throw new InvalidDataException("Original data initialization rejected; inspect logs.");
    // Actual initialized catalog inspection is deliberately before newArena.
    var catalog = CatalogMembershipChecks.Inspect(server);
    if (!catalog.Accepted)
    {
        File.WriteAllText(Path.GetFullPath(args[0]), JsonSerializer.Serialize(new { Accepted = false, CatalogMembership = catalog }, new JsonSerializerOptions { WriteIndented = true }));
        throw new InvalidDataException("Selected actual catalog membership/lookup check rejected before newArena.");
    }
    // Do not call begin/poll: no game/ping bind, players, gameplay updates or production clocks.
    var arena = server.newArena("Original Rogue Trader initializer", false);
    arena._bActive = false;
    var assets = server._assets;
    var level = assets.Level;
    if (level.Width != 1999 || level.Height != 1998 || level.Tiles.Length != 3994002 ||
        assets.getItems.Count != 200 || assets.getVehicleInfos.Count != 30 || assets.getSkillInfos.Count != 12)
        throw new InvalidDataException("Loaded original map/item/vehicle/RPG roster does not match canonical counts.");
    var hulls = assets.getSkillInfos.Select(skill => new { skill.Name, skill.SkillId, skill.DefaultVehicleId,
        Vehicle = assets.getVehicleByID(skill.DefaultVehicleId)?.Name }).ToArray();
    if (hulls.Any(hull => hull.Vehicle != hull.Name)) throw new InvalidDataException("RPG-to-VEH hull identity mismatch.");
    var vehicles = arena.Vehicles.Select(vehicle => new { Id = vehicle._id, TypeId = vehicle._type.Id,
        Name = vehicle._type.Name, Kind = vehicle._type.Type.ToString(), Team = vehicle._team?._id,
        X = vehicle._state.positionX, Y = vehicle._state.positionY }).ToArray();
    var teams = ((IDictionary)Field(arena, "_teams")).Values.Cast<Team>().Select(team => new { team._id, team._name }).ToArray();
    var itemDrops = ((IDictionary)Field(arena, "_items")).Values.Cast<Arena.ItemDrop>()
        .Select(drop => new { Item = drop.item.name, ItemId = drop.item.id, drop.quantity, drop.positionX, drop.positionY }).ToArray();
    if (server._clients.Count != 0 || arena.PlayerCount != 0 || server._arenas.Count != 1)
        throw new InvalidDataException("Initializer unexpectedly created clients/players/additional arenas.");
    foreach (var pair in sourceHashes)
        if (Hash(Path.Combine("assets", pair.Key!)) != pair.Value) throw new InvalidDataException($"Original copy changed: {pair.Key}");
    if (unexpectedErrors.Count != 0) throw new InvalidDataException("Unexpected initialization error/exception: " + string.Join("; ", unexpectedErrors));
    var report = new
    {
        Accepted = true, CatalogMembership = catalog, UnexpectedInitializationErrors = unexpectedErrors,
        UpstreamCommit = "bb97579efeceff1c63d781d8c1bb8dd4a5b9b849", CanonicalCommit = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35",
        ProcessId = Environment.ProcessId, Stage = "Actual completed bounded original arena initializer verification; no gameplay/native client parity",
        Standalone = server.IsStandalone, Arena = arena._name, ArenaCount = server._arenas.Count, ClientCount = server._clients.Count,
        PollCalls = 0, SocketBeginCalls = 0, PhysicsTicks = 0, ScriptBinding = "ScriptArena(null): original CFG events/default handlers, no dedicated RT script",
        Map = new { level.Width, level.Height, TileCount = level.Tiles.Length, BlockedTiles = level.Tiles.Count(tile => tile.Blocked) },
        Items = assets.getItems.Count, Vehicles = assets.getVehicleInfos.Count, Rpg = hulls,
        Teams = teams, InitialSpawnedVehicles = vehicles, InitialSpawnedItemDrops = itemDrops,
        HideCount = CountField(arena, "_hides"), DoorCount = CountField(arena, "_doors"), FlagCount = CountField(arena, "_flags"),
        InitialHidesSetting = server._zoneConfig.startGame.initialHides, OriginalCopiedHashes = sourceHashes,
        Integrity = "All copied source bytes unchanged after initialization", LoadBlobs = assets.bUseBlobs,
        Pending = "Actual native connect/asset transfer/hull choose/unspec; original graphics; runtime spawn behavior/economy/production/persistence/shutdown race"
    };
    File.WriteAllText(Path.GetFullPath(args[0]), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"ORIGINAL_ARENA_INIT_PASS: {JsonSerializer.Serialize(report)}");
    return 0;
    }
    finally { Log.eError -= errorSink; Log.close(); }
}
static object Field(object value, string name)
{
    for (var type = value.GetType(); type is not null; type = type.BaseType)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field is not null) return field.GetValue(value) ?? throw new InvalidOperationException($"Null field {name}");
    }
    throw new MissingFieldException(name);
}
static int CountField(object value, string name) => ((IEnumerable)Field(value, name)).Cast<object>().Count();
static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
