using System.Reflection;
using InfServer;
using InfServer.Game;
using InfServer.Protocol;

public static class ReadyChecks
{
    public static object[] Run(List<AssetInfo> assets)
    {
        string? offline = Environment.GetEnvironmentVariable("RT_COMPATIBILITY_OFFLINE");
        ZoneServer? previousServer = Helpers._server;
        try
        {
            var manager = new AssetManager();
            // Isolate this handler from asset loading, web fallbacks and arena
            // initialization. Its real getAssetList returns the reviewed list.
            typeof(AssetManager).GetField("_assetList", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, assets);
            var server = new InertServer { _assets = manager, _logger = Log.createClient("ReadyFixture") };
            Helpers._server = server;
            var handler = typeof(ZoneServer).Assembly.GetType("InfServer.Logic.Logic_Login", true)!
                .GetMethod("Handle_CS_Ready", BindingFlags.Public | BindingFlags.Static)!;
            var observations = new List<object>();
            // NEW/provisional in-memory cases. The online control only changes
            // a process-local flag; no transport, identity, ticket or account.
            foreach (string? flag in new string?[] { "1", null, "0" })
            {
                Environment.SetEnvironmentVariable("RT_COMPATIBILITY_OFFLINE", flag);
                var client = new Client(false) { _C2S_UDPSize = int.MaxValue };
                var player = new Player { _server = server, _client = client };
                Require(!player._bLoggedIn, "Fixture ready state must start false.");
                handler.Invoke(null, new object[] { new CS_Ready(), player });
                var queued = client._streams.SelectMany(stream => stream.reliableQueue).ToArray();
                bool expectPatch = flag != "1";
                Require(queued.Length == (expectPatch ? 2 : 1) && queued.All(entry => entry.dataStream == null), "Unexpected ready queue extent.");
                Require(queued[^1].packet is SC_AssetInfo info && ReferenceEquals(info.assets, assets) && !info.bOptionalUpdate,
                    "Ready handler must queue the canonical list with ordinary-player update policy.");
                if (expectPatch)
                {
                    Require(queued[0].packet is SC_PatchInfo patch && patch.patchServer == "patch.station.sony.com" &&
                        patch.patchPort == 0x1b58 && patch.patchXml == "patch/infantry/Zone7130.xml", "Online control must retain the upstream legacy announcement.");
                }
                Require(player._bLoggedIn, "Actual ready handler did not advance its in-memory ready flag.");
                Require(!server.HasSocket && server._clients.Count == 0 && server._totalBytesSent == 0 && server._totalBytesReceived == 0 &&
                    client._handler == null && client._ipe == null && client._packetsSent == 0 && client._bytesSent == 0 && client._packetQueue.Count == 0,
                    "Inert fixture unexpectedly acquired a transport or sent bytes.");
                observations.Add(new { OfflineFlag = flag ?? "<unset>", Passed = true, PatchQueued = expectPatch,
                    AssetInfoQueued = true, ReadyFlagSet = player._bLoggedIn, QueueCount = queued.Length,
                    SerializedPacketTypes = queued.Select(entry => entry.packet.GetType().Name).ToArray(), TransportBytesSent = client._bytesSent });
            }
            return observations.ToArray();
        }
        finally
        {
            Environment.SetEnvironmentVariable("RT_COMPATIBILITY_OFFLINE", offline);
            Helpers._server = previousServer!;
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    private sealed class InertServer : ZoneServer { public bool HasSocket => _sock != null; }
}
