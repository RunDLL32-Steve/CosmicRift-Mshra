using RogueTrader.LoadTests;

// Usage: dotnet run -c Release --project tests/LoadTests -- [--warmup N] [--ticks N] [--runs N] [--scenario world-tick|session-tick]
//        [--data <dir>] [--revision <sha>] [--json <path>] [--markdown <path>]
// Reports only: the exit code is 0 unless the arguments are wrong or a scenario's runs end in different world digests.
var warmup = 600;
var ticks = 3600;
var runs = 5;
var scenarios = new List<string>();
var dataRoot = Path.Combine(AppContext.BaseDirectory, "data");
string? revision = null, jsonPath = null, markdownPath = null;
try
{
    for (var i = 0; i < args.Length; i++)
    {
        string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");
        int Positive() => int.TryParse(Next(), out var n) && n > 0 ? n : throw new ArgumentException($"{args[i - 1]} needs a positive whole number.");
        switch (args[i])
        {
            case "--warmup": warmup = int.TryParse(Next(), out var w) && w >= 0 ? w : throw new ArgumentException("--warmup needs a whole number >= 0."); break;
            case "--ticks": ticks = Positive(); break;
            case "--runs": runs = Positive(); break;
            case "--scenario":
                var name = Next();
                if (!TickBenchmark.Scenarios.Contains(name)) throw new ArgumentException($"Unknown scenario '{name}'.");
                scenarios.Add(name);
                break;
            case "--data": dataRoot = Next(); break;
            case "--revision": revision = Next(); break;
            case "--json": jsonPath = Next(); break;
            case "--markdown": markdownPath = Next(); break;
            default: throw new ArgumentException($"Unknown argument '{args[i]}'.");
        }
    }
}
catch (ArgumentException error)
{
    Console.Error.WriteLine(error.Message);
    return 2;
}

var report = TickBenchmark.Run(new BenchmarkOptions(dataRoot, warmup, ticks, runs,
    scenarios.Count > 0 ? scenarios.Distinct().ToArray() : TickBenchmark.Scenarios, revision ?? GitRevision(), DateTime.UtcNow));
var markdown = TickBenchmark.ToMarkdown(report);
Console.Write(markdown);
if (jsonPath is not null) File.WriteAllText(jsonPath, TickBenchmark.ToJson(report));
if (markdownPath is not null) File.WriteAllText(markdownPath, markdown);
if (report.Scenarios.Any(s => !s.DigestsIdentical))
{
    Console.Error.WriteLine("BENCHMARK_WORKLOAD_NOT_REPRODUCIBLE: runs of one scenario ended in different world digests.");
    return 1;
}
Console.Error.WriteLine("BENCHMARK_REPORTED");
return 0;

static string GitRevision()
{
    try
    {
        using var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", "rev-parse HEAD")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
        });
        if (git is null) return "unknown";
        var output = git.StandardOutput.ReadToEnd().Trim();
        git.WaitForExit();
        return git.ExitCode == 0 && output.Length > 0 ? output : "unknown";
    }
    catch (System.ComponentModel.Win32Exception)
    {
        return "unknown";
    }
}
