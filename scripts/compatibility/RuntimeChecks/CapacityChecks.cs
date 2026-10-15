using System;
using System.Collections.Generic;
using System.Linq;
using Assets;
using InfServer.Game;
using InfServer.Protocol;

public static class CapacityChecks
{
    public static IEnumerable<CheckResult> Run()
    {
        // NEW/provisional fixture populations and limits exercise the existing
        // comparison boundary. They are not proposed original Rogue tuning.
        yield return Check("Capacity.NullScript.BelowCapacity", null, 1, 2, false);
        yield return Check("Capacity.NullScript.AtCapacity", null, 2, 2, true);
        yield return Check("Capacity.NullScript.OverCapacity", null, 3, 2, true);
        yield return Check("Capacity.NullScript.ZeroCapacity", null, 0, 0, true);
        yield return Check("Capacity.EmptyScript.AtCapacity", "", 2, 2, true);
        yield return Check("Capacity.OtherScript.AtCapacity", "GameType_Fixture", 2, 2, true);
        yield return Check("Capacity.OtherScript.BelowCapacity", "GameType_Fixture", 1, 2, false);
        yield return Check("Capacity.SportLookalike.AtCapacity", "GameType_GravballExtra", 2, 2, true);

        // These existing upstream exceptions must still reach team selection,
        // including case-insensitive bindings; they do not establish RT rules.
        yield return Check("Capacity.SoccerBrawl.AtCapacity", "gAmEtYpE_sOcCeRbRaWl", 2, 2, false);
        yield return Check("Capacity.Gravball.AtCapacity", "GAMETYPE_GRAVBALL", 2, 2, false);
        yield return Check("Capacity.BasketBall.AtCapacity", "gametype_basketball", 2, 2, false);
        yield return Check("Capacity.BoomBall.AtCapacity", "gAmEtYpE_BoOmBaLl", 2, 2, false);
    }

    private static CheckResult Check(string name, string? scriptType, int population, int limit, bool expectFull)
    {
        try
        {
            var sections = new Dictionary<string, Dictionary<string, string>>
            {
                ["Arena"] = new(),
                ["Owner"] = new()
            };
            var config = new CfgInfo
            {
                arena = new CfgInfo.Arena(ref sections),
                owner = new CfgInfo.Owner(ref sections)
            };
            config.arena.playingMax = limit;
            var server = new FixtureServer
            {
                _zoneConfig = config,
                _assets = new AssetManager
                {
                    // NEW one-tile in-memory fixture; no original files loaded.
                    Level = new LvlInfo { Width = 1, Height = 1, Tiles = new LvlInfo.Tile[1] }
                }
            };
            var arena = new CapacityArena(server, scriptType);
            for (var index = 0; index < population; ++index)
                arena.AddOccupant(new Player { _id = (ushort)(index + 1) });

            // sendReliable only serializes/enqueues until a poll. This client
            // has no packet handler, endpoint or socket and is never polled.
            var client = new Client(false) { _C2S_UDPSize = 4096 };
            var player = new Player
            {
                _id = 100,
                _server = server,
                _arena = arena,
                _client = client,
                _skills = new Dictionary<int, Player.SkillItem>(),
                _bSpectator = true
            };

            // Actual upstream entry handler: no copied decision logic.
            arena.handlePlayerJoin(player, false);

            var queued = client._streams.SelectMany(stream => stream.reliableQueue).ToArray();
            Require(queued.Length == 1 && queued[0].packet is SC_Chat,
                "Expected exactly one queued arena response.");
            var response = (SC_Chat)queued[0].packet;
            var expectedMessage = expectFull ? "Game is full." : "Unable to pick a team.";
            Require(response.message == expectedMessage,
                $"Expected '{expectedMessage}', received '{response.message}'.");
            Require(arena.TeamSelectionCalls == (expectFull ? 0 : 1),
                "Capacity rejection/team-selection boundary differed.");
            Require(player.IsSpectator && arena.PlayerCount == population,
                "The bounded check changed spectator state or population.");
            Require(!server.HasSocket && server._clients.Count == 0 &&
                server._totalBytesSent == 0 && server._totalBytesReceived == 0 &&
                client._packetsSent == 0 && client._bytesSent == 0,
                "The in-memory fixture unexpectedly used a socket/client transport.");
            return new CheckResult(name, true,
                $"Population {population}, limit {limit}: '{response.message}'; team selections {arena.TeamSelectionCalls}; no sockets, polls or spawn.");
        }
        catch (Exception exception)
        {
            return new CheckResult(name, false, $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FixtureServer : ZoneServer
    {
        public bool HasSocket => _sock != null;
    }

    private sealed class CapacityArena : ScriptArena
    {
        public CapacityArena(ZoneServer server, string? scriptType) : base(server, scriptType!) { }

        public int TeamSelectionCalls { get; private set; }

        public void AddOccupant(Player player) => _playersIngame.Add(player);

        public override Team pickAppropriateTeam(Player player)
        {
            ++TeamSelectionCalls;
            // Observe the path after the capacity gate without starting a
            // team/spawn sequence or invoking any original gameplay events.
            return null!;
        }

        public override void poll() => throw new InvalidOperationException("Capacity checks must never poll an arena.");
    }
}
