using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using InfServer;
using InfServer.Game;
using InfServer.Protocol;

public static class RoutingChecks
{
    public static IEnumerable<CheckResult> Run(string generatedConfigPath)
    {
        var results = new List<CheckResult>();
        var previous = new[] { Arena.routeRange, Arena.routeWeaponRange, Arena.routeRadarRange,
            Arena.routeRadarRangeFactor, Arena.routeRadarRangeFar, Arena.routeRadarRangeFarFactor };
        try
        {
            using var document = new Xmlconfig(Path.GetFullPath(generatedConfigPath), false) { CommitOnUnload = false };
            var config = document.Settings;
            // Read the generated host configuration through the same reader as initArenas.
            Arena.routeRange = config["arena/routing/routeRange"].intValue;
            Arena.routeWeaponRange = config["arena/routing/routeWeaponRange"].intValue;
            Arena.routeRadarRange = config["arena/routing/routeRadarRange"].intValue;
            Arena.routeRadarRangeFactor = config["arena/routing/routeRadarRangeFactor"].intValue;
            Arena.routeRadarRangeFar = config["arena/routing/routeRadarRangeFar"].intValue;
            Arena.routeRadarRangeFarFactor = config["arena/routing/routeRadarRangeFarFactor"].intValue;

            Check("routing.generated-config", () =>
            {
                Require(Arena.routeRange > 0 && Arena.routeWeaponRange > 0 &&
                    Arena.routeRadarRange > Arena.routeRange && Arena.routeRadarRangeFar > Arena.routeRadarRange &&
                    Arena.routeRadarRangeFactor > 1 && Arena.routeRadarRangeFarFactor > Arena.routeRadarRangeFactor,
                    "Generated XML must supply ordered positive ranges and positive radar cadence divisors.");
            });

            // NEW/provisional diagnostic positions only. No arena constructor, initializer,
            // poll, simulation, socket or native client is invoked by these fixtures.
            var arena = (ScriptArena)RuntimeHelpers.GetUninitializedObject(typeof(ScriptArena));
            var players = new ObjTracker<Player>();
            foreach (var entry in new[] { (Id: (ushort)1, X: (short)10500), (Id: (ushort)2, X: (short)11500),
                         (Id: (ushort)3, X: (short)13000), (Id: (ushort)4, X: (short)14500) })
            {
                var player = new Player { _id = entry.Id };
                player._state.positionX = entry.X;
                player._state.positionY = 10000;
                players.Add(player);
            }
            typeof(Arena).GetField("_players", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(arena, players);

            Check("routing.near-update", () => ExpectAudience(1, false, 0, 1));
            Check("routing.radar-update", () => ExpectAudience(2, false, 0, 1, 2));
            Check("routing.far-update", () => ExpectAudience(4, false, 0, 1, 2, 3));
            Check("routing.radar-skipped-update", () => ExpectAudience(3, false, 0, 1));
            Check("routing.explicit-weapon-range", () => ExpectAudience(1, true, 3500, 1, 2, 3));

            void ExpectAudience(int updateNumber, bool weapon, int weaponRange, params ushort[] expected)
            {
                var state = new Helpers.ObjectState { positionX = 10000, positionY = 10000, updateNumber = updateNumber };
                var actual = Helpers.getRouteList(arena, state, weapon, weaponRange, null!, 10000, 10000)
                    .Select(player => player._id).ToHashSet();
                Require(actual.SetEquals(expected),
                    $"Update {updateNumber}: expected audience [{string.Join(",", expected)}], received [{string.Join(",", actual.Order())}].");
            }
        }
        catch (Exception error)
        {
            results.Add(new CheckResult("routing.fixture-setup", false, $"{error.GetType().Name}: {error.Message}"));
        }
        finally
        {
            Arena.routeRange = previous[0];
            Arena.routeWeaponRange = previous[1];
            Arena.routeRadarRange = previous[2];
            Arena.routeRadarRangeFactor = previous[3];
            Arena.routeRadarRangeFar = previous[4];
            Arena.routeRadarRangeFarFactor = previous[5];
        }
        return results;

        void Check(string name, Action action)
        {
            try { action(); results.Add(new CheckResult(name, true, "Actual routing helper accepted generated host configuration and expected audience.")); }
            catch (Exception error) { results.Add(new CheckResult(name, false, $"{error.GetType().Name}: {error.Message}")); }
        }
    }

    private static void Require(bool condition, string detail)
    {
        if (!condition) throw new InvalidOperationException(detail);
    }

}
