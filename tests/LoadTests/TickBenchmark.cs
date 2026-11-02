using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using RogueTrader.GameData;
using RogueTrader.Protocol;
using RogueTrader.Server;
using RogueTrader.Simulation;
using RogueTrader.Simulation.Replay;

namespace RogueTrader.LoadTests;

/// <summary>
/// Stopwatch tick benchmark (queue C4). Times single 1/60 s ticks of a scripted single-ship workload and reports the
/// distribution with the host, runtime, build, data and revision it ran on. It reports; it never passes or fails on time.
/// The 60 FPS goal of master §20.4 (16.667 ms per tick) is shown as a reference only. A synthetic one-ship workload says
/// nothing about multiplayer capacity or production load.
/// </summary>
public static class TickBenchmark
{
    public const string ShipFile = "ships/starter_interceptor.json";
    public const string WeaponFile = "weapons/plasma_cannon.json";
    public const string SectorFile = "sectors/first_contact.json";
    public const double GoalTickMilliseconds = 1000.0 / WorldSimulation.TickRate;

    public static readonly string[] Scenarios = ["world-tick", "session-tick"];

    /// <summary>NEW scripted input for tick <paramref name="tick"/> (1-based): thrust bursts with reversals, a slow weave, fire held.</summary>
    public static ShipInput ScriptedInput(long tick)
    {
        var thrust = tick % 240 < 180 ? 1f : -0.5f;
        var turn = (tick / 90 % 3) - 1f;
        return new ShipInput(thrust, turn, Fire: true);
    }

    public static BenchmarkReport Run(BenchmarkOptions options)
    {
        var files = new[] { ShipFile, WeaponFile, SectorFile }.ToDictionary(f => f, f => File.ReadAllBytes(Path.Combine(options.DataRoot, f)));
        var catalog = GameCatalog.FromJson(Text(files[ShipFile]), Text(files[WeaponFile]), Text(files[SectorFile]));
        // Priming: every scenario runs once and is discarded, so no timed run pays for first-call JIT. Tiered compilation is off
        // in the project file: with it on, background tier-up landed mid-measurement and run medians differed up to 7x.
        foreach (var name in options.Scenarios) RunScenario(name, catalog, options with { Runs = 1 });
        var scenarios = options.Scenarios.Select(name => RunScenario(name, catalog, options)).ToArray();
        return new BenchmarkReport(
            Tool: "RogueTrader.LoadTests TickBenchmark v1 (queue C4)",
            StartedUtc: options.StartedUtc,
            Revision: options.Revision,
            Host: HostInfo.Current(),
            Workload: new WorkloadInfo(
                Description: "One player ship, scripted input (thrust 180 ticks / reverse 60, turn steps every 90 ticks, fire held), First Contact sector, no cargo or mining",
                ShipId: catalog.Ship.Id, WeaponId: catalog.Weapon.Id, SectorId: catalog.Sector.Id,
                Asteroids: catalog.Sector.Asteroids.Count, Targets: catalog.Sector.Targets.Count,
                DataFiles: files.ToDictionary(pair => pair.Key, pair => Convert.ToHexString(SHA256.HashData(pair.Value)).ToLowerInvariant())),
            Procedure: new ProcedureInfo(options.WarmupTicks, options.SampleTicks, options.Runs,
                "Tiered compilation off. Every scenario first runs once untimed and discarded. Each reported run builds a fresh world, runs the warmup ticks untimed, then times every sample tick on its own with Stopwatch.GetTimestamp. Runs repeat in one process."),
            GoalTickMilliseconds: GoalTickMilliseconds,
            Scenarios: scenarios);
    }

    private static string Text(byte[] bytes) => System.Text.Encoding.UTF8.GetString(bytes);

    private static ScenarioResult RunScenario(string name, GameCatalog catalog, BenchmarkOptions options)
    {
        var runs = new List<RunResult>();
        for (var run = 1; run <= options.Runs; run++)
        {
            var world = new WorldSimulation(catalog);
            Action<long> tick = name switch
            {
                "world-tick" => t => world.Tick(ScriptedInput(t)),
                "session-tick" => SessionTick(new AuthoritativeSession(world)),
                _ => throw new ArgumentException($"Unknown scenario '{name}'."),
            };
            for (long t = 1; t <= options.WarmupTicks; t++) tick(t);
            var samples = new double[options.SampleTicks];
            var first = options.WarmupTicks + 1;
            for (var i = 0; i < samples.Length; i++)
            {
                var start = Stopwatch.GetTimestamp();
                tick(first + i);
                samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            }
            runs.Add(RunResult.From(run, samples, world));
        }
        var medians = runs.Select(r => r.MedianMs).Order().ToArray();
        var medianOfMedians = Percentile(medians, 0.5);
        return new ScenarioResult(name, runs,
            MedianOfRunMediansMs: medianOfMedians,
            RunMedianSpreadPercent: medianOfMedians > 0 ? (medians[^1] - medians[0]) / medianOfMedians * 100 : 0,
            MedianShareOfGoalPercent: medianOfMedians / GoalTickMilliseconds * 100,
            DigestsIdentical: runs.Select(r => r.FinalDigest).Distinct().Count() == 1);
    }

    // The input is submitted for the next tick and that tick is stepped, so the timing covers queueing and tick together.
    private static Action<long> SessionTick(AuthoritativeSession session) => t =>
    {
        var input = ScriptedInput(t);
        var flags = input.Fire ? InputFlags.Fire : InputFlags.None;
        if (session.Submit(new PlayerInput((uint)t, (uint)t, 0, input.Thrust, input.Turn, flags)) != InputVerdict.Accepted)
            throw new InvalidOperationException($"Scripted input for tick {t} was not accepted.");
        session.Step();
    };

    internal static double Percentile(double[] sorted, double fraction)
    {
        if (sorted.Length == 0) return 0;
        var rank = fraction * (sorted.Length - 1);
        var low = (int)Math.Floor(rank);
        var high = (int)Math.Ceiling(rank);
        return sorted[low] + (sorted[high] - sorted[low]) * (rank - low);
    }

    public static string ToJson(BenchmarkReport report) =>
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    public static string ToMarkdown(BenchmarkReport report)
    {
        var w = new System.Text.StringBuilder();
        w.AppendLine("# Simulation tick benchmark (queue C4)");
        w.AppendLine();
        w.AppendLine($"-   Started (UTC): {report.StartedUtc:yyyy-MM-dd HH:mm:ss}; revision: {report.Revision}");
        var h = report.Host;
        w.AppendLine($"-   Host: {h.OsDescription}, {h.ProcessArchitecture}, {h.ProcessorCount} logical processors");
        w.AppendLine($"-   Runtime: {h.FrameworkDescription}; build {h.BuildConfiguration}; server GC {h.ServerGc}; tiered compilation {h.TieredCompilation}; Stopwatch {h.StopwatchFrequency} Hz (high resolution {h.StopwatchHighResolution})");
        var wl = report.Workload;
        w.AppendLine($"-   Workload: {wl.Description}; ship `{wl.ShipId}`, weapon `{wl.WeaponId}`, sector `{wl.SectorId}` ({wl.Asteroids} asteroids, {wl.Targets} targets)");
        foreach (var (file, sha) in wl.DataFiles) w.AppendLine($"-   Data `data/{file}` SHA-256 `{sha}`");
        var p = report.Procedure;
        w.AppendLine($"-   Procedure: {p.Runs} runs x ({p.WarmupTicks} warmup + {p.SampleTicks} timed ticks). {p.Description}");
        w.AppendLine($"-   Reference goal (master §20.4, not asserted): {report.GoalTickMilliseconds:F3} ms per tick at 60 Hz");
        foreach (var s in report.Scenarios)
        {
            w.AppendLine();
            w.AppendLine($"## {s.Name}");
            w.AppendLine();
            w.AppendLine("| Run | Mean ms | Median ms | p95 ms | p99 ms | Max ms | Total ms | Shots | Kills | Final digest |");
            w.AppendLine("| --- | ------- | --------- | ------ | ------ | ------ | -------- | ----- | ----- | ------------ |");
            foreach (var r in s.Runs)
                w.AppendLine($"| {r.Run} | {r.MeanMs:F5} | {r.MedianMs:F5} | {r.P95Ms:F5} | {r.P99Ms:F5} | {r.MaxMs:F5} | {r.TotalMs:F2} | {r.ShotsFired} | {r.DestroyedTargets} | `{r.FinalDigest}` |");
            w.AppendLine();
            w.AppendLine($"Median of run medians {s.MedianOfRunMediansMs:F5} ms ({s.MedianShareOfGoalPercent:F3}% of the 60 Hz goal); run-median spread {s.RunMedianSpreadPercent:F1}%; final digests identical across runs: {s.DigestsIdentical}.");
        }
        return w.ToString();
    }
}

public sealed record BenchmarkOptions(string DataRoot, int WarmupTicks, int SampleTicks, int Runs, string[] Scenarios, string Revision, DateTime StartedUtc);

public sealed record BenchmarkReport(string Tool, DateTime StartedUtc, string Revision, HostInfo Host, WorkloadInfo Workload,
    ProcedureInfo Procedure, double GoalTickMilliseconds, ScenarioResult[] Scenarios);

public sealed record HostInfo(string OsDescription, string ProcessArchitecture, int ProcessorCount, string FrameworkDescription,
    string BuildConfiguration, bool ServerGc, string GcLatencyMode, string TieredCompilation, long StopwatchFrequency,
    bool StopwatchHighResolution)
{
    public static HostInfo Current() => new(RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture.ToString(),
        Environment.ProcessorCount, RuntimeInformation.FrameworkDescription,
#if DEBUG
        "Debug",
#else
        "Release",
#endif
        GCSettings.IsServerGC, GCSettings.LatencyMode.ToString(),
        Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") is { } env ? $"env {env}"
            : AppContext.GetData("System.Runtime.TieredCompilation")?.ToString() ?? "default",
        Stopwatch.Frequency, Stopwatch.IsHighResolution);
}

public sealed record WorkloadInfo(string Description, string ShipId, string WeaponId, string SectorId, int Asteroids, int Targets,
    Dictionary<string, string> DataFiles);

public sealed record ProcedureInfo(int WarmupTicks, int SampleTicks, int Runs, string Description);

public sealed record ScenarioResult(string Name, IReadOnlyList<RunResult> Runs, double MedianOfRunMediansMs, double RunMedianSpreadPercent,
    double MedianShareOfGoalPercent, bool DigestsIdentical);

public sealed record RunResult(int Run, double MeanMs, double MedianMs, double P95Ms, double P99Ms, double MinMs, double MaxMs, double TotalMs,
    long FinalTick, long ShotsFired, int DestroyedTargets, string FinalDigest)
{
    public static RunResult From(int run, double[] samples, WorldSimulation world)
    {
        var sorted = samples.Order().ToArray();
        return new RunResult(run, samples.Average(), TickBenchmark.Percentile(sorted, 0.5), TickBenchmark.Percentile(sorted, 0.95),
            TickBenchmark.Percentile(sorted, 0.99), sorted[0], sorted[^1], samples.Sum(), world.TickNumber, world.ShotsFired,
            world.DestroyedTargets, WorldStateHasher.Hash(world).ToString("x16"));
    }
}
