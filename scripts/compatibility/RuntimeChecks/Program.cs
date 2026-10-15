using System.Security.Cryptography;
using System.Text.Json;
using InfServer;

try
{
    if (args.Length != 4 || Environment.GetEnvironmentVariable("RT_COMPATIBILITY_OFFLINE") != "1")
        throw new ArgumentException("Expected repository root, report path, baseline/fixed mode and generated routing config; mandatory offline flag.");
    string repo = Path.GetFullPath(args[0]);
    string reportPath = Path.GetFullPath(args[1]);
    string relative = Path.GetRelativePath(Path.Combine(repo, "artifacts", "rogue-runtime-checks"), reportPath);
    if (Path.IsPathRooted(relative) || relative.StartsWith("..") || args[2] is not ("baseline" or "fixed"))
        throw new ArgumentException("Invalid report destination/mode.");
    if (!Log.init()) throw new IOException("Diagnostic logger initialization failed.");
    Log.assume(Log.createClient("RuntimeChecks"));
    string itemPath = Path.Combine(repo, ".tools", "references", "cosmic-rift-assets", "ITM", "Rogue_mp.itm");
    string originalHash = Hash(itemPath);
    var checks = ShopChecks.Run(itemPath).Concat(CapacityChecks.Run()).Concat(RoutingChecks.Run(args[3])).ToArray();
    if (Hash(itemPath) != originalHash) throw new InvalidDataException("Original item bytes changed.");
    var failures = checks.Where(check => !check.Passed).Select(check => check.Name).Order().ToArray();
    string[] expectedBaselineFailures = [
        "Shop.Oversell", "Shop.ZeroSkipsHook", "Shop.MinValue", "Shop.BalanceOverflow", "Shop.PriceOverflow",
        "Shop.HookRemovesInventory", "Shop.HookFillsBalance", "Shop.InvalidSkipsHook",
        "Buy.PriceOverflow", "Buy.NegativePrice", "Buy.StackOverflow", "Buy.RejectedDeliveryNotCharged",
        "Buy.HookDrainsCash", "Buy.HookFillsLimit",
        "Store.SellOutsideStore", "Store.BuyOutsideStore", "Store.OutsideSkipsHook", "Store.TerrainBoundary", "Store.OutOfRangePosition",
        "Store.SpectatorStoreDisabled", "Store.SpectatorFlagIgnoredForPlayers", "Store.MissingState",
        "Capacity.NullScript.AtCapacity", "Capacity.NullScript.OverCapacity", "Capacity.NullScript.ZeroCapacity",
        "routing.generated-config", "routing.near-update", "routing.radar-update", "routing.far-update", "routing.radar-skipped-update"
    ];
    bool accepted = args[2] == "baseline" ? failures.SequenceEqual(expectedBaselineFailures.Order()) : failures.Length == 0;
    string source = Path.Combine(repo, ".tools", "compatibility", "server-working", "dotnetcore", "ZoneServer", "Game", "Arena", "ScriptArena.cs");
    var report = new
    {
        Mode = args[2], Accepted = accepted, CheckCount = checks.Length, FailureCount = failures.Length,
        Checks = checks, ExpectedBaselineFailures = args[2] == "baseline" ? expectedBaselineFailures : [],
        CanonicalCommit = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35",
        UpstreamCommit = "bb97579efeceff1c63d781d8c1bb8dd4a5b9b849",
        CanonicalItemSha256 = originalHash, DerivedArenaSha256 = Hash(source),
        RoutingConfigSha256 = Hash(args[3]),
        AssemblyHashes = new
        {
            Runner = Hash(typeof(CheckResult).Assembly.Location), Server = Hash(typeof(InfServer.Game.ScriptArena).Assembly.Location),
            Network = Hash(typeof(InfServer.Protocol.Client).Assembly.Location), Assets = Hash(typeof(Assets.ItemInfo).Assembly.Location)
        },
        SocketBeginCalls = 0, PollCalls = 0, PhysicsTicks = 0, NativeClients = 0,
        Scope = "Actual shop sell/buy, join and routing handlers with NEW in-memory boundary fixtures; no durable transactions, native entry, gameplay parity or persistence acceptance."
    };
    Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
    File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"RUNTIME_CHECKS_{(accepted ? "PASS" : "FAIL")}: mode={args[2]}, checks={checks.Length}, observedFailures={failures.Length}");
    foreach (var check in checks.Where(check => !check.Passed)) Console.WriteLine($"{check.Name}: {check.Detail}");
    Log.close();
    return accepted ? 0 : 2;
}
catch (Exception exception) { Console.Error.WriteLine($"RUNTIME_CHECKS_ERROR: {exception}"); return 2; }

static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
public sealed record CheckResult(string Name, bool Passed, string Detail);
