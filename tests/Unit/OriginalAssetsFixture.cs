using System.Diagnostics;

namespace RogueTrader.UnitTests;

/// <summary>
/// Finds the canonical original assets (cosmic-rift-assets at the pinned commit). Lookup order:
/// 1. env var RT_ORIGINAL_ASSETS_ROOT; 2. walking up from the test base directory for
/// .tools/references/cosmic-rift-assets; 3. `git rev-parse --git-common-dir` (its parent is the main checkout, which
/// holds .tools when the tests run from a worktree).
/// </summary>
public static class OriginalAssets
{
    public const string EnvVar = "RT_ORIGINAL_ASSETS_ROOT";
    private static readonly string RelativeInRepo = Path.Combine(".tools", "references", "cosmic-rift-assets");
    private static readonly Lazy<string?> Located = new(Locate);

    /// <summary>Directory holding RPG/, VEH/, ITM/, or null when the assets are not available.</summary>
    public static string? Root => Located.Value;

    public static string SkipReason =>
        $"Original assets not found: set {EnvVar}, or provide {RelativeInRepo.Replace('\\', '/')} in this or the main checkout. " +
        "These tests were SKIPPED, not passed.";

    private static bool Looks(string? dir) => !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "RPG", "Rogue.rpg"));

    private static string? Locate()
    {
        var env = Environment.GetEnvironmentVariable(EnvVar);
        if (Looks(env)) return Path.GetFullPath(env!);

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, RelativeInRepo);
            if (Looks(candidate)) return candidate;
        }

        try
        {
            var psi = new ProcessStartInfo("git", "rev-parse --git-common-dir")
            {
                WorkingDirectory = AppContext.BaseDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.StandardError.ReadToEnd();
                if (!process.WaitForExit(10000)) { process.Kill(); }
                else if (process.ExitCode == 0 && output.Length > 0)
                {
                    var common = Path.GetFullPath(output, AppContext.BaseDirectory);
                    var parent = Directory.GetParent(common.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (parent != null)
                    {
                        var candidate = Path.Combine(parent.FullName, RelativeInRepo);
                        if (Looks(candidate)) return candidate;
                    }
                }
            }
        }
        catch (Exception)
        {
            // git unavailable: treated as "assets not found".
        }
        return null;
    }

    /// <summary>The repository root (folder holding ROGUE_TRADER_2_PLAN.md) found by walking up from the test base directory.</summary>
    public static string? RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "ROGUE_TRADER_2_PLAN.md"))) return dir.FullName;
        return null;
    }
}

/// <summary>[Fact] that reports as Skipped (with a reason) when the original assets cannot be located.</summary>
public sealed class RequiresOriginalAssetsFactAttribute : FactAttribute
{
    public RequiresOriginalAssetsFactAttribute()
    {
        if (OriginalAssets.Root == null) Skip = OriginalAssets.SkipReason;
    }
}

/// <summary>[Theory] that reports as Skipped (with a reason) when the original assets cannot be located.</summary>
public sealed class RequiresOriginalAssetsTheoryAttribute : TheoryAttribute
{
    public RequiresOriginalAssetsTheoryAttribute()
    {
        if (OriginalAssets.Root == null) Skip = OriginalAssets.SkipReason;
    }
}
