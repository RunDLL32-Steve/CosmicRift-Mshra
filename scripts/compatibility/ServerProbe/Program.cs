using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using InfServer;
using InfServer.Game;

// The upstream entry point records unhandled exceptions but still lets Windows report them.
// This bounded diagnostic host turns unexpected worker failures into a logged controlled exit.
AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    Console.Error.WriteLine($"COMPATIBILITY_WORKER_FAILURE: {eventArgs.ExceptionObject}");
    Environment.Exit(2);
};
try { return Run(args); }
catch (Exception exception)
{
    Console.Error.WriteLine($"COMPATIBILITY_SERVER_FAILURE: {exception}");
    return 2;
}

static int Run(string[] args)
{
    if (args.Length != 1) throw new ArgumentException("Expected absolute output-report path.");
    if (Environment.GetEnvironmentVariable("RT_COMPATIBILITY_OFFLINE") != "1")
        throw new InvalidOperationException("Mandatory offline isolation flag missing.");
    var cfg = new Xmlconfig("server.xml", false).Settings;
    if (cfg["database/connectionDelay"].intValue != 0 || cfg.Exists("database2") ||
        cfg["server/bindIP"].Value != "127.0.0.1" ||
        cfg["server/updateGlobalNws"].Value.Length != 0 || cfg["server/copyServerFrom"].Value.Length != 0 ||
        cfg["server/pathFindingEnabled"].boolValue || cfg["server/pathFindingEnabledNew"].boolValue)
        throw new InvalidDataException("Server configuration violates standalone loopback isolation.");
    var expectedPort = cfg["server/bindPort"].intValue;
    if (expectedPort is < 1024 or > 65534) throw new InvalidDataException("Invalid local port pair.");
    if (!Log.init()) throw new IOException("Upstream logger initialization failed.");
    Log.assume(Log.createClient("CompatibilityProbe"));
    InfServer.Logic.Registrar.register();
    var server = new ProbeZoneServer();
    if (!server.init()) throw new InvalidDataException("Upstream server initialization rejected working zone; inspect captured logs.");
    if (!server.IsStandalone) throw new InvalidOperationException("Upstream server did not enter standalone mode.");
    Exception? workerFailure = null;
    var worker = new Thread(() =>
    {
        try { server.begin(Stopwatch.StartNew()); }
        catch (Exception exception) { workerFailure = exception; }
    }) { IsBackground = true, Name = "CompatibilityServerHost" };
    worker.Start();
    if (!server.GameReady.Wait(5000)) throw new TimeoutException("Actual game socket did not bind within five seconds.");
    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
    var responder = typeof(ZoneServer).GetField("_pingResponder", flags)!.GetValue(server)!;
    var pingSocket = (Socket)responder.GetType().GetField("_socket", flags)!.GetValue(responder)!;
    if (!SpinWait.SpinUntil(() => pingSocket.LocalEndPoint is not null, 5000))
        throw new TimeoutException("Actual ping socket did not bind within five seconds.");
    var gameEndpoint = server.GameEndpoint!;
    var pingEndpoint = (IPEndPoint)pingSocket.LocalEndPoint!;
    if (!gameEndpoint.Address.Equals(IPAddress.Loopback) || !pingEndpoint.Address.Equals(IPAddress.Loopback) ||
        gameEndpoint.Port != expectedPort || pingEndpoint.Port != expectedPort + 1)
        throw new InvalidOperationException("Actual socket endpoint differs from required loopback pair.");
    using var pingClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
    pingClient.Client.ReceiveTimeout = 2000;
    const int token = 0x55667788;
    var request = BitConverter.GetBytes(token);
    pingClient.Send(request, request.Length, pingEndpoint);
    IPEndPoint responseSource = new(IPAddress.Loopback, 0);
    var response = pingClient.Receive(ref responseSource);
    if (!responseSource.Equals(pingEndpoint) || response.Length != 8 ||
        BitConverter.ToInt32(response, 0) != 0 || BitConverter.ToInt32(response, 4) != token)
        throw new InvalidDataException("Loopback native ping response failed player-count/token verification.");
    server.run = false;
    // Upstream end() aborts a pending receive then rearms the disposed socket on a worker.
    // All upstream network workers are background threads; this bounded process returns
    // normally and the OS releases its sockets without invoking that defective end path.
    responder.GetType().GetMethod("End")!.Invoke(responder, null);
    if (!worker.Join(2000)) throw new TimeoutException("Owned host thread failed to stop.");
    if (workerFailure is not null) throw new InvalidOperationException("Server host failed.", workerFailure);
    if (server._arenas.Count != 0 || server._clients.Count != 0)
        throw new InvalidOperationException("Probe unexpectedly created an arena/client; no gameplay is allowed in this check.");
    var report = new
    {
        Purpose = "Completed standalone core-data/network compatibility feature; no gameplay, arena creation or native client execution",
        ProcessId = Environment.ProcessId, Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        UpstreamCommit = "bb97579efeceff1c63d781d8c1bb8dd4a5b9b849", Standalone = server.IsStandalone,
        GameSocket = gameEndpoint.ToString(), PingSocket = pingEndpoint.ToString(), PingPlayerCount = 0,
        PingTokenVerified = true, ArenaCount = server._arenas.Count, ClientCount = server._clients.Count,
        Cleanup = "Normal bounded host exit releases sockets; upstream end/rearm shutdown race remains a separate open defect",
        LoadBlobs = server._assets.bUseBlobs, server._assets.Level.Width, server._assets.Level.Height,
        Limitation = "Core-data/network startup only. Missing graphics dependency closure, Rogue game script/economic event behavior, client protocol/version compatibility and SilentClientCrash remain unverified."
    };
    File.WriteAllText(Path.GetFullPath(args[0]), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"COMPATIBILITY_SERVER_PASS: {JsonSerializer.Serialize(report)}");
    Log.close();
    return 0;
}

sealed class ProbeZoneServer : ZoneServer
{
    public ManualResetEventSlim GameReady { get; } = new();
    public IPEndPoint? GameEndpoint { get; private set; }
    protected override void onListening(IPEndPoint listenPoint)
    {
        base.onListening(listenPoint);
        GameEndpoint = (IPEndPoint)_sock.LocalEndPoint!;
        GameReady.Set();
    }
}
