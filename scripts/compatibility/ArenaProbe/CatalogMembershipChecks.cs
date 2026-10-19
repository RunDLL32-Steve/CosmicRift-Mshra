using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Assets;
using InfServer;
using InfServer.Game;

// Checks the actual initialized AssetManager; never loads ItemInfo again or
// substitutes/injects its maps. This is only the selected standalone catalog.
public static class CatalogMembershipChecks
{
    public static CatalogMembershipResult Inspect(ZoneServer server)
    {
        string source = SourcePath();
        string repo = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "../../.."));
        string zone = OwnedRegular(Path.Combine(repo, ".tools/compatibility/arena-working/rogue"), repo);
        Require(string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.CurrentDirectory)), Path.TrimEndingDirectorySeparator(zone), StringComparison.OrdinalIgnoreCase), "Unexpected probe working directory.");
        string assetsRoot = OwnedRegular(Path.Combine(zone, "assets"), zone);
        string[] searchPaths = AssetFileFactory.AssetFolderPaths.Select(path => OwnedRegular(path, zone)).ToArray();
        Require(searchPaths.Length == 1 && string.Equals(Path.TrimEndingDirectorySeparator(searchPaths[0]), Path.TrimEndingDirectorySeparator(assetsRoot), StringComparison.OrdinalIgnoreCase), "Expected only the issue-owned staged assets search path.");
        string cfgName = server._config["server/zoneConfig"].Value;
        string itemName = server._zoneConfig.level.itmFile;
        string cfgPath = Resolve(cfgName, searchPaths, assetsRoot);
        string itemPath = Resolve(itemName, searchPaths, assetsRoot);
        var inputs = new[]
        {
            Input("server.xml", OwnedRegular(Path.Combine(zone, "server.xml"), zone)),
            Input(cfgName, cfgPath), Input(itemName, itemPath),
            Input("scripts.xml", OwnedRegular(Path.Combine(zone, "scripts.xml"), zone)),
            Input(server._config["server/ipAllowlistFile"].Value,
                OwnedRegular(Path.Combine(zone, server._config["server/ipAllowlistFile"].Value), zone))
        };
        // Authority: docs/original-research/ASSET_MANIFEST.json assets entry originalPath CFG/rogue.cfg, originalSizeBytes/originalSha256.
        Require(inputs[1].SizeBytes == 48399 && inputs[1].Sha256 == "82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90", "Selected CFG is not the pinned canonical bytes.");
        // Authority: same manifest assets entry originalPath ITM/Rogue_mp.itm, originalSizeBytes/originalSha256.
        Require(inputs[2].SizeBytes == 111614 && inputs[2].Sha256 == "dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7", "Selected ITM is not the pinned canonical bytes.");

        // Existing token reader only: no second ItemInfo.Load and no static
        // BlobsToLoad/FilesToLoad effects. Hash guard fixes the selected schema.
        // ID column2/type18 come from inspected pinned upstream loader source;
        // they are not independently confirmed original/native semantics.
        var expected = new List<int>();
        foreach (string line in File.ReadLines(itemPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = CSVReader.Parse(line);
            Require(fields.Count >= 3 && CSVReader.GetInt(fields[0]) != 18, "Selected canonical main file unexpectedly contains a nested/non-record row.");
            expected.Add(CSVReader.GetInt(fields[2]));
        }
        int[] expectedIds = expected.Order().ToArray();
        var actualItems = server._assets.getItems.ToArray();
        int[] actualIds = actualItems.Select(item => item.id).Order().ToArray();
        int[] duplicateExpected = Duplicates(expectedIds), duplicateActual = Duplicates(actualIds);
        var lookupMismatches = new List<int>();
        var warnings = new List<string>();
        var errors = new List<string>();
        Log.LogMessage warningSink = (type, _, message) => { if (type == TLog.Warning) warnings.Add(message); };
        Log.LogMessage errorSink = (type, _, message) => { if (type is TLog.Error or TLog.Exception) errors.Add(message); };
        bool missing66;
        Log.eWarning += warningSink; Log.eError += errorSink;
        try
        {
            foreach (var item in actualItems)
            {
                var lookup = server._assets.getItemByID(item.id);
                if (!ReferenceEquals(item, lookup) || lookup?.id != item.id) lookupMismatches.Add(item.id);
            }
            // Exactly one positive66 lookup. No alias/sentinel/synthetic item.
            missing66 = server._assets.getItemByID(66) is null;
        }
        finally { Log.eWarning -= warningSink; Log.eError -= errorSink; }
        bool expectedWarning = warnings.Count == 1 && warnings[0] == "Unable to get item by ID: 66";
        bool accepted = expectedIds.Length == 200 && actualIds.Length == 200 && duplicateExpected.Length == 0 && duplicateActual.Length == 0 &&
            expectedIds.SequenceEqual(actualIds) && lookupMismatches.Count == 0 && missing66 && expectedWarning && errors.Count == 0;
        return new(accepted, expectedIds, actualIds, duplicateExpected, duplicateActual, lookupMismatches.ToArray(),
            Digest(expectedIds), Digest(actualIds), missing66, warnings.ToArray(), errors.ToArray(), inputs, searchPaths,
            new[] { Loaded("Runner", typeof(CatalogMembershipChecks).Assembly), Loaded("Server", typeof(ZoneServer).Assembly), Loaded("Assets", typeof(ItemInfo).Assembly) },
            new[] { Input("Program.cs", OwnedRegular(Path.Combine(Path.GetDirectoryName(source)!, "Program.cs"), repo)), Input("CatalogMembershipChecks.cs", OwnedRegular(source, repo)) });
    }

    static string Resolve(string name, string[] paths, string assetsRoot)
    {
        foreach (string path in paths)
        {
            string? found = AssetFileFactory.findAssetFile(name, path);
            if (!string.IsNullOrWhiteSpace(found)) return OwnedRegular(found, assetsRoot);
        }
        throw new FileNotFoundException("Actual configured asset search did not resolve selected input.", name);
    }
    // Reject output/input redirection through junctions as well as lexical escapes.
    static string OwnedRegular(string path, string owner)
    {
        string full = Path.GetFullPath(path), root = Path.GetFullPath(owner);
        string relative = Path.GetRelativePath(root, full);
        Require(!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar), "Path escaped issue-owned root: " + full);
        for (string? current = full; current is not null && current.Length >= root.Length; current = Path.GetDirectoryName(current))
            Require((File.GetAttributes(current) & FileAttributes.ReparsePoint) == 0, "Issue-owned path is redirected: " + current);
        return full;
    }
    static int[] Duplicates(int[] ids) => ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
    static string Digest(int[] ids) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", ids.Select(id => id.ToString(CultureInfo.InvariantCulture))) + "\n"))).ToLowerInvariant();
    static InputIdentity Input(string name, string path) => new(name, Path.GetFullPath(path), new FileInfo(path).Length, Hash(path));
    static AssemblyIdentity Loaded(string role, Assembly assembly) => new(role, assembly.Location, Hash(assembly.Location));
    static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    static string SourcePath([CallerFilePath] string path = "") => path;
    static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}

public sealed record InputIdentity(string ConfiguredName, string ResolvedPath, long SizeBytes, string Sha256);
public sealed record AssemblyIdentity(string Role, string Location, string Sha256);
public sealed record CatalogMembershipResult(bool Accepted, int[] ExpectedIds, int[] ActualIds, int[] DuplicateExpectedIds,
    int[] DuplicateActualIds, int[] LookupMismatchIds, string ExpectedIdsSha256, string ActualIdsSha256, bool Positive66LookupNull,
    string[] ObservedWarnings, string[] UnexpectedErrors, InputIdentity[] SelectedInputs, string[] AssetSearchPaths,
    AssemblyIdentity[] LoadedAssemblies, InputIdentity[] SourceFiles)
{
    public int ExpectedCount => ExpectedIds.Length;
    public int ActualCount => ActualIds.Length;
    public bool LookupReferenceConsistency => LookupMismatchIds.Length == 0;
    public string IdDigestAlgorithm => "SHA256 lowercase hex over UTF8 without BOM of invariant decimal sorted IDs joined LF with a trailing LF";
    public string Scope => "Actual selected standalone AssetManager after server.init and before newArena; no injected maps, second ItemInfo.Load, arbitrary/live/custom/native catalog closure";
}
