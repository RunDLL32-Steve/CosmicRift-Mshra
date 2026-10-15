using System.Buffers.Binary;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InfServer;
using InfServer.Game;
using InfServer.Network;
using InfServer.Protocol;

// Diagnostic local serialization only. No server initialization, authentication,
// account operations, socket creation, packet polling or transport sending.
try
{
    if (args.Length != 3 || args[2] is not ("baseline" or "fixed") || Environment.GetEnvironmentVariable("RT_COMPATIBILITY_OFFLINE") != "1")
        throw new ArgumentException("Expected repository root, report path and baseline/fixed mode; RT_COMPATIBILITY_OFFLINE=1 is mandatory.");
    string repo = Path.GetFullPath(args[0]);
    string reportPath = Path.GetFullPath(args[1]);
    RequireWithin(Path.Combine(repo, "artifacts", "native-delivery-checks"), reportPath);
    string canonicalRoot = Path.Combine(repo, ".tools", "references", "cosmic-rift-assets");
    string inventoryPath = Path.Combine(repo, "artifacts", "rogue-package-audit", "index.json");
    using var inventory = JsonDocument.Parse(File.ReadAllBytes(inventoryPath));
    Require(inventory.RootElement.GetProperty("canonicalCommit").GetString() == "eb1514eaae7f9b9425307d9cc043c74ecdd15e35", "Unexpected canonical inventory commit.");
    var files = inventory.RootElement.GetProperty("expectedTransferFiles").EnumerateArray().Select(entry =>
    {
        string path = Path.GetFullPath(Path.Combine(canonicalRoot, entry.GetProperty("originalPath").GetString()!));
        RequireWithin(canonicalRoot, path);
        return new ExpectedAsset(path, entry.GetProperty("filename").GetString()!, entry.GetProperty("sha256").GetString()!,
            entry.GetProperty("crc32").GetUInt32(), entry.GetProperty("length").GetInt32());
    }).ToArray();
    Require(files.Length == 46 && files.Select(file => file.Name).Distinct(StringComparer.Ordinal).Count() == 46, "Expected 46 unique canonical transfer basenames.");
    foreach (var file in files) VerifySource(file);
    if (!Log.init()) throw new IOException("Logger initialization failed.");
    Log.assume(Log.createClient("NativeDeliveryChecks"));
    var manager = new AssetManager();
    var assets = files.Select(file => new AssetInfo(file.Path, file.Crc32)).ToList();
    manager.AssetCache = new AssetManager.Cache(manager);
    manager.AssetCache.prepareCache(assets);
    var results = new List<object>();
    var metadataDifferences = new List<string>();
    foreach (var file in files)
    {
        var cached = manager.AssetCache[file.Name];
        Require(cached != null && cached.bCached, $"Cache missing: {file.Name}");
        Require(cached!.uncompressedSize == file.Length && cached.checksum == file.Crc32, $"Cache metadata differs: {file.Name}");
        var packet = new SC_AssetUpdate { filename = file.Name, uncompressedLen = cached.uncompressedSize, compressedData = cached.data };
        packet.MakeSerialized(null!, null!);
        byte[] wire = packet.Data;
        byte[] restored = DecodePacket(wire, file.Name, file.Length);
        Require(Hash(restored) == file.Sha256 && Crc32(restored) == file.Crc32, $"Independent packet decoding differs: {file.Name}");
        Require(wire.AsSpan(74).SequenceEqual(cached.data), $"Serialized compressed payload differs: {file.Name}");
        Require(ReferenceEquals(manager.AssetCache[file.Name], cached), $"Cache reuse differs: {file.Name}");
        object? trailer = null;
        if (file.Name == "Rogue.lvl")
        {
            // Prior raw/parser audit measured the read boundary; this check
            // validates the untouched entire source and every trailing byte.
            const int parserEnd = 910942, trailerBytes = 380052;
            Require(restored.Length == parserEnd + trailerBytes, "Original LVL extent differs.");
            byte[] source = File.ReadAllBytes(file.Path);
            Require(restored.AsSpan(parserEnd).SequenceEqual(source.AsSpan(parserEnd)), "LVL trailer differs.");
            trailer = new { ParserEnd = parserEnd, Length = trailerBytes, Sha256 = Hash(restored.AsSpan(parserEnd)) };
        }
        bool metadataMatches = packet.Length == wire.Length;
        if (!metadataMatches) metadataDifferences.Add(file.Name);
        Require(packet.Length == (args[2] == "baseline" ? 74 + (ushort)cached.data.Length : wire.Length),
            $"Unexpected {args[2]} packet length accounting: {file.Name}");
        results.Add(new { Filename = file.Name, UncompressedLength = restored.Length, CompressedLength = cached.data.Length,
            SerializedLength = wire.Length, ReportedPacketLength = packet.Length, LengthMetadataMatches = packet.Length == wire.Length,
            Sha256 = Hash(restored), Crc32 = Crc32(restored), Trailer = trailer });
    }
    var ready = ReadyChecks.Run(assets);
    var boundaries = CheckBoundaries(args[2]);
    Require(args[2] != "baseline" || metadataDifferences.Count > 0, "Baseline did not reproduce canonical packet length discrepancies.");
    foreach (var file in files) VerifySource(file);
    string sourceRoot = Path.Combine(repo, ".tools", "compatibility", "server-working", "dotnetcore");
    var report = new
    {
        SchemaVersion = 1, Mode = args[2], Accepted = true, CanonicalFileCount = files.Length, Files = results, ReadyChecks = ready,
        BoundaryChecks = boundaries, LengthMetadataMismatchFiles = metadataDifferences,
        LengthInvariantPassed = metadataDifferences.Count == 0 && args[2] == "fixed",
        ExpectedBaselineLengthFailures = args[2] == "baseline" ? metadataDifferences.Concat(new[] { "NEW-boundary-65536", "NEW-boundary-65537" }).ToArray() : [],
        CanonicalCommit = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35", InventorySha256 = Hash(File.ReadAllBytes(inventoryPath)),
        SourceHashes = new Dictionary<string, string>
        {
            ["Cache"] = Hash(File.ReadAllBytes(Path.Combine(sourceRoot, "ZoneServer", "Game", "Assets", "AssetManager.Cache.cs"))),
            ["AssetUpdate"] = Hash(File.ReadAllBytes(Path.Combine(sourceRoot, "ZoneServer", "Protocol", "Packets", "Login", "SC_AssetUpdate.cs"))),
            ["Ready"] = Hash(File.ReadAllBytes(Path.Combine(sourceRoot, "ZoneServer", "Logic", "Packets", "Login", "Login.cs"))),
            ["PacketBase"] = Hash(File.ReadAllBytes(Path.Combine(sourceRoot, "Network", "Network", "PacketBase.cs")))
        },
        AssemblyHashes = new { Runner = Hash(File.ReadAllBytes(typeof(ExpectedAsset).Assembly.Location)),
            Server = Hash(File.ReadAllBytes(typeof(ZoneServer).Assembly.Location)), Network = Hash(File.ReadAllBytes(typeof(PacketBase).Assembly.Location)),
            Assets = Hash(File.ReadAllBytes(typeof(Assets.CRC32).Assembly.Location)) },
        NativeDeliveryVerified = false, NativeReadyHandshakeVerified = false, OriginalsUnchanged = true,
        SocketBeginCalls = 0, PollCalls = 0, TransportSendCalls = 0, PhysicsTicks = 0, NativeClients = 0,
        Scope = "Real cache/packet serialization and inert actual ready handler only. Packet length metadata discrepancies are recorded, not repaired. No transport fragmentation/reassembly, receiver, authentication, native rendering or original gameplay acceptance."
    };
    Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
    File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"NATIVE_DELIVERY_CHECKS_ACCEPTED: mode={args[2]}; canonicalFiles={files.Length}; lengthMetadataMismatches={metadataDifferences.Count}; nativeDeliveryVerified=false");
    Log.close();
    return 0;
}
catch (Exception exception) { Console.Error.WriteLine($"NATIVE_DELIVERY_CHECKS_ERROR: {exception}"); return 2; }

static byte[] DecodePacket(byte[] data, string name, int expectedLength)
{
    Require(data.Length >= 74 && data[0] == (byte)SC_AssetUpdate.TypeID && data[1] == 0, "Asset packet header differs.");
    Require(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(2, 4)) == expectedLength &&
        BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(6, 4)) == expectedLength, "Asset packet length fields differ.");
    var nameField = data.AsSpan(10, 64);
    int terminator = nameField.IndexOf((byte)0);
    Require(terminator >= 0 && Encoding.ASCII.GetString(nameField[..terminator]) == name, "Asset packet basename differs.");
    Require(nameField[terminator..].IndexOfAnyExcept((byte)0) < 0, "Asset filename padding differs.");
    using var input = new MemoryStream(data, 74, data.Length - 74, false);
    using var zlib = new ZLibStream(input, CompressionMode.Decompress);
    using var output = new MemoryStream();
    byte[] buffer = new byte[8192];
    int count;
    while ((count = zlib.Read(buffer)) != 0)
    {
        Require(output.Length + count <= expectedLength, "Decompression exceeds declared original extent.");
        output.Write(buffer, 0, count);
    }
    Require(output.Length == expectedLength, "Decompressed size differs.");
    return output.ToArray();
}

static object[] CheckBoundaries(string mode)
{
    // NEW/provisional synthetic byte-array sizes isolate the historical ushort
    // length accounting. These are test boundaries, never game tuning/data.
    return new[] { 65535, 65536, 65537 }.Select(length =>
    {
        var packet = new SC_AssetUpdate { filename = "NEW-boundary.bin", uncompressedLen = length, compressedData = new byte[length] };
        packet.MakeSerialized(null!, null!);
        Require(packet.Data.Length == 74 + length, "Byte-array boundary lost serialized bytes.");
        Require(packet.Length == (mode == "baseline" ? 74 + (ushort)length : packet.Data.Length), "Unexpected synthetic boundary length accounting.");
        // Record observed metadata; do not encode its current defect as success.
        return (object)new { Fixture = "NEW/provisional byte array; intentionally not zlib", PayloadLength = length,
            SerializedLength = packet.Data.Length, ReportedPacketLength = packet.Length, LengthMetadataMatches = packet.Length == packet.Data.Length };
    }).ToArray();
}

static void VerifySource(ExpectedAsset file)
{
    byte[] bytes = File.ReadAllBytes(file.Path);
    Require(Path.GetFileName(file.Path) == file.Name && bytes.Length == file.Length && Hash(bytes) == file.Sha256 && Crc32(bytes) == file.Crc32,
        $"Canonical source integrity failed: {file.Name}");
}
static uint Crc32(ReadOnlySpan<byte> data)
{
    uint crc = uint.MaxValue;
    foreach (byte value in data) { crc ^= value; for (int bit = 0; bit < 8; ++bit) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0u : 0xedb88320u); }
    return ~crc;
}
static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
static void RequireWithin(string root, string path)
{
    string relative = Path.GetRelativePath(Path.GetFullPath(root), path);
    Require(!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar), "Path escapes permitted directory.");
}
static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
public sealed record ExpectedAsset(string Path, string Name, string Sha256, uint Crc32, int Length);
