using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Assets;
using InfServer;
using InfServer.Game;

AppDomain.CurrentDomain.UnhandledException += (_, e) => { Console.Error.WriteLine($"NATIVE_SESSION_WORKER_FAILED: {e.ExceptionObject.GetType().Name}"); Environment.Exit(2); };
try { return Run(args); }
catch (Exception e)
{
    Console.Error.WriteLine($"NATIVE_SESSION_FAILED: {e.GetType().Name}: {e.Message}");
    if (args.Length >= 1)
    {
        try { Directory.CreateDirectory(args[0]); File.WriteAllText(Path.Combine(args[0], "exit.json"),
            JsonSerializer.Serialize(new { ControlledExitCode = 2, FailureType = e.GetType().Name,
                Cleanup = "Controlled helper process exit releases owned handles; startup/readiness did not complete" })); }
        catch (IOException) { /* stderr retains the controlled failure if a report cannot be written */ }
    }
    return 2;
}

static int Run(string[] args)
{
    if (args.Length != 3 || !int.TryParse(args[2], out var duration) || duration is < 10 or > 120 ||
        Environment.GetEnvironmentVariable("RT_COMPATIBILITY_OFFLINE") != "1")
        throw new ArgumentException("Expected report-directory, stop-marker and bounded seconds (10..120); mandatory offline flag required.");
    var reportRoot = Path.GetFullPath(args[0]); Directory.CreateDirectory(reportRoot);
    var stopPath = Path.GetFullPath(args[1]);
    var cfg = new Xmlconfig("server.xml", false).Settings;
    if (cfg["database/connectionDelay"].intValue != 0 || cfg.Exists("database2") ||
        cfg["server/bindIP"].Value != "127.0.0.1" || cfg["server/bindPort"].intValue != 21337 ||
        cfg["server/maxPlayers"].intValue != 1 || cfg["server/gameType"].Value.Length != 0 ||
        cfg["server/updateGlobalNws"].Value.Length != 0 || cfg["server/copyServerFrom"].Value.Length != 0 ||
        cfg["server/pathFindingEnabled"].boolValue || cfg["server/pathFindingEnabledNew"].boolValue ||
        !cfg["server/loadBlobs"].boolValue)
        throw new InvalidDataException("Bounded native session isolation configuration mismatch.");
    var hashes = Directory.GetFiles("assets").ToDictionary(path => Path.GetFileName(path)!, Hash);
    if (hashes.Keys.Count(name => name.EndsWith(".blo", StringComparison.OrdinalIgnoreCase)) != 90 ||
        !hashes.TryGetValue("soccer.blo", out var sharedSoccerHash) ||
        sharedSoccerHash != "93ec29ba6ea7618b6f298253fa75ef397ec934911231370d961a960f7d050784")
        throw new InvalidDataException("All 89 canonical BLO archives and authentic shared soccer.blo required.");
    if (!Log.init()) throw new IOException("Logger initialization failed.");
    Log.assume(Log.createClient("BoundedNativeSession")); InfServer.Logic.Registrar.register();
    var server = new SessionZoneServer();
    if (!server.init() || !server.IsStandalone || !server._assets.bUseBlobs)
        throw new InvalidDataException("Original full-asset standalone initialization failed.");
    if (server._assets.getItems.Count != 200 || server._assets.getVehicleInfos.Count != 30 || server._assets.getSkillInfos.Count != 12 ||
        server._assets.Level.Width != 1999 || server._assets.Level.Height != 1998)
        throw new InvalidDataException("Canonical Rogue Trader roster/map mismatch.");
    // NEW host routing metadata only. Original CFG arena rows and file bytes remain unchanged.
    // allocatePlayer uses _namedArenas, not server.xml namedArena children; bind its real supported route.
    var tree = new Dictionary<string, Dictionary<string, string>>
    {
        ["NamedArena0"] = new() { ["Name"] = "Rogue Trader", ["DisplayLogic"] = "", ["PlayLogic"] = "", ["StartLogic"] = "" }
    };
    server._namedArenas = new List<CfgInfo.NamedArena> { new(ref tree, 0) };
    var arena = server.newArena("Rogue Trader", true); arena._bActive = true;
    var assetList = server._assets.getAssetList().Select(asset => new { Name = Path.GetFileName(asset.filepath), asset.checksum,
        Bytes = new FileInfo(asset.filepath).Length, Sha256 = Hash(asset.filepath) }).ToArray();
    Exception? workerError = null;
    var worker = new Thread(() => { try { server.begin(Stopwatch.StartNew()); } catch (Exception e) { workerError = e; } })
        { IsBackground = true, Name = "BoundedNativeSession" };
    worker.Start();
    if (!server.Ready.Wait(5000)) throw new TimeoutException("Game socket did not bind.");
    var responder = typeof(ZoneServer).GetField("_pingResponder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(server)!;
    var ping = (Socket)responder.GetType().GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(responder)!;
    if (!SpinWait.SpinUntil(() => ping.LocalEndPoint is not null, 5000)) throw new TimeoutException("Ping socket did not bind.");
    var gameEndpoint = server.Endpoint!; var pingEndpoint = (IPEndPoint)ping.LocalEndPoint!;
    if (!gameEndpoint.Equals(new IPEndPoint(IPAddress.Loopback, 21337)) || !pingEndpoint.Equals(new IPEndPoint(IPAddress.Loopback, 21338)))
        throw new InvalidDataException("Actual socket binding is not the required loopback pair.");
    Save("ready.json", new { Ready = true, ProcessId = Environment.ProcessId, Game = gameEndpoint.ToString(), Ping = pingEndpoint.ToString(),
        Arena = arena._name, Standalone = true, MaxClients = 1, DurationLimitSeconds = duration, Assets = assetList,
        MapWidth = server._assets.Level.Width, MapHeight = server._assets.Level.Height, Items = server._assets.getItems.Count,
        Vehicles = server._assets.getVehicleInfos.Count, Hulls = server._assets.getSkillInfos.Count,
        InitialVehicles = arena.Vehicles.Count(), InitialItems = CountField(arena, "_items"), NamedArenaOverride = "NEW host routing metadata; original CFG unchanged",
        Limits = "No durable persistence; generic script fallback; original native playback/unspec/economy remain unverified" });
    Console.WriteLine($"NATIVE_SESSION_READY: pid={Environment.ProcessId}; game={gameEndpoint}; ping={pingEndpoint}; arena=Rogue Trader; limit={duration}s");
    var clock = Stopwatch.StartNew(); var maxClients = 0; var maxArenaPlayers = 0;
    while (clock.Elapsed.TotalSeconds < duration && !File.Exists(stopPath))
    {
        if (workerError is not null || !worker.IsAlive) throw new InvalidOperationException("Owned server worker ended unexpectedly.");
        maxClients = Math.Max(maxClients, server._clients.Count);
        maxArenaPlayers = Math.Max(maxArenaPlayers, arena.TotalPlayerCount);
        // Aggregate observations only; do not record aliases, credentials, UID, chat or real user state.
        Thread.Sleep(200);
    }
    server.run = false;
    responder.GetType().GetMethod("End")!.Invoke(responder, null);
    if (!worker.Join(2000)) throw new TimeoutException("Owned server thread failed to stop.");
    foreach (var pair in hashes) if (Hash(Path.Combine("assets", pair.Key)) != pair.Value) throw new InvalidDataException($"Source bytes changed: {pair.Key}");
    Save("exit.json", new { ProcessId = Environment.ProcessId, ControlledExitCode = 0, BoundedSeconds = clock.Elapsed.TotalSeconds,
        StopMarkerObserved = File.Exists(stopPath), MaxClientCount = maxClients, MaxArenaPlayerCount = maxArenaPlayers,
        FinalClientCount = server._clients.Count, ArenaCount = server._arenas.Count, OriginalHashesUnchanged = true,
        SyntheticPlayerInputs = 0, Cleanup = "Normal bounded process exit releases both owned sockets; upstream end/rearm path deliberately not invoked",
        NativeBehaviorProof = "Client presence count alone is not proof of original gameplay" });
    Console.WriteLine("NATIVE_SESSION_EXIT: controlled=0; originals unchanged; owned process releases loopback sockets.");
    Log.close(); return 0;
    void Save(string name, object value) => File.WriteAllText(Path.Combine(reportRoot, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
}
static int CountField(object value, string name)
{
    for (var type = value.GetType(); type is not null; type = type.BaseType)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field?.GetValue(value) is System.Collections.IEnumerable collection) return collection.Cast<object>().Count();
    }
    throw new MissingFieldException(name);
}
static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
sealed class SessionZoneServer : ZoneServer
{
    public ManualResetEventSlim Ready { get; } = new();
    public IPEndPoint? Endpoint { get; private set; }
    protected override void onListening(IPEndPoint endpoint) { base.onListening(endpoint); Endpoint = (IPEndPoint)_sock.LocalEndPoint!; Ready.Set(); }
}
