using System.Reflection;
using System.Runtime.CompilerServices;
using Assets;
using InfServer;
using InfServer.Game;
using InfServer.Protocol;
using Microsoft.VisualBasic.FileIO;

// Root-approved G9 diagnostic composition. Canonical definitions stay unchanged;
// players, team, map, positions, inventory and health=1 are NEW test conditions.
// No server/arena init, socket, poll, scheduler, Deserialize or physics ticks are invoked; actual explosion damage math runs.
public static class MakerChecks
{
    public static IEnumerable<CheckResult> Run(string itemPath, string vehiclePath)
    {
        foreach (int id in new[] { 83, 84, 85 })
            yield return Check($"Maker.Packet{id}.ExplosionMissing66", itemPath, vehiclePath, id, f =>
            {
                f.PacketAndDrain();
                f.AssertCreated();
                f.Explode(expectedMissingWarning: true);
                return f.Evidence();
            });
        yield return Check("Maker.NoAmmo83", itemPath, vehiclePath, 83, f =>
        {
            f.Player._inventory.Remove(83);
            f.PacketAndDrain(owned: 0);
            f.AssertNoCreation(0, 0, reload: 1, inventory: 0);
            return f.Evidence();
        });
        yield return Check("Maker.MakeVehicleVeto83", itemPath, vehiclePath, 83, f =>
        {
            f.Hooks.AllowMake = false;
            f.PacketAndDrain();
            f.AssertNoCreation(0, 1, reload: 1, inventory: 1);
            return f.Evidence();
        });
        yield return Check("Maker.PacketSkillReject84", itemPath, vehiclePath, 84, f =>
        {
            f.Player._skills.Add(1, new Player.SkillItem { quantity = 1 });
            f.Packet(expectQueued: false);
            f.AssertNoCreation(1, 0, reload: 0, inventory: 0);
            Require(f.Security.Count == 1 && f.Security[0].Contains("failed skill check"), "Expected actual packet skill rejection log.");
            return f.Evidence();
        });
        yield return Check("Maker.DirectMake84PacketGuardBypass", itemPath, vehiclePath, 84, f =>
        {
            f.Player._skills.Add(1, new Player.SkillItem { quantity = 1 });
            f.Packet(expectQueued: false);
            f.Arena.handlePlayerMakeVehicle(f.Player, f.Maker, 32, 32);
            f.AssertCreated();
            Require(f.Security.Count == 1, "Expected the preceding actual packet skill rejection.");
            return f.Evidence();
        });
        yield return Check("Maker.VehicleDeathVeto83", itemPath, vehiclePath, 83, f =>
        {
            f.Hooks.AllowDeath = false;
            f.PacketAndDrain(); f.AssertCreated(); f.Explode(expectedMissingWarning: false);
            return f.Evidence();
        });
        yield return Check("Maker.DirectOneArgumentKill83", itemPath, vehiclePath, 83, f =>
        {
            f.PacketAndDrain(); f.AssertCreated();
            f.Created.kill(null!);
            f.AssertDestroyed(deathCalls: 0, missingWarnings: 0);
            return f.Evidence();
        });
    }

    static CheckResult Check(string name, string items, string vehicles, int id, Func<Fixture, string> action)
    {
        var priorServer = Helpers._server;
        var priorAssets = AssetManager.Manager;
        int priorCapacity = InfServer.Game.Arena.maxVehicles;
        try
        {
            string detail;
            using (var f = new Fixture(items, vehicles, id))
            {
                detail = action(f);
                f.AssertInert();
                Require(f.Errors.Count == 0, "Unexpected logged error/exception: " + string.Join("; ", f.Errors));
            }
            Require(ReferenceEquals(Helpers._server, priorServer) && ReferenceEquals(AssetManager.Manager, priorAssets) && InfServer.Game.Arena.maxVehicles == priorCapacity,
                "Diagnostic leaked server, AssetManager singleton or vehicle capacity.");
            return new(name, true, detail);
        }
        catch (Exception e)
        {
            bool restored = ReferenceEquals(Helpers._server, priorServer) && ReferenceEquals(AssetManager.Manager, priorAssets) && InfServer.Game.Arena.maxVehicles == priorCapacity;
            // A failed scenario stays failed. Best-effort static restoration is
            // safety cleanup, never a replacement for the original exception.
            Helpers._server = priorServer;
            InfServer.Game.Arena.maxVehicles = priorCapacity;
            typeof(AssetManager).GetField("_singleton", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, priorAssets);
            return new(name, false, $"{e}; fixture rollback restored={restored}");
        }
    }

    static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    static string[] Row(string path, int row)
    {
        using var parser = new TextFieldParser(new StringReader(File.ReadLines(path).ElementAt(row - 1)));
        parser.SetDelimiters(","); parser.HasFieldsEnclosedInQuotes = true;
        return parser.ReadFields() ?? throw new InvalidDataException("Missing canonical row.");
    }
    static void Raw(string[] row, int field, string expected) => Require(row[field] == expected, $"Canonical raw field {field}: expected {expected}, actual {row[field]}.");
    static void SetMap(AssetManager assets, string field, object map) => typeof(AssetManager).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(assets, map);

    sealed class Fixture : IDisposable
    {
        public readonly DiagnosticServer Server;
        public readonly DiagnosticArena Arena;
        public readonly Player Player;
        public readonly ItemInfo.VehicleMaker Maker;
        public readonly MakerHooks Hooks = new();
        public readonly List<string> Warnings = new(), Security = new(), Errors = new();
        public Computer Created => (Computer)Arena.Vehicles.Single();
        readonly ZoneServer previousServer;
        readonly AssetManager? previousAssets;
        readonly int previousMaxVehicles;
        readonly Log.LogMessage warningSink, securitySink, errorSink;
        int destroyed;

        public Fixture(string itemPath, string vehiclePath, int id)
        {
            var items = ItemInfo.Load(itemPath).ToDictionary(i => i.id);
            var vehicles = VehInfo.Load(vehiclePath).ToDictionary(v => v.Id);
            Maker = (ItemInfo.VehicleMaker)items[id];
            var type = (VehInfo.Computer)vehicles[id + 342];
            var raw = Row(itemPath, id + 113);
            Raw(raw, 0, "7"); Raw(raw, 1, "v57"); Raw(raw, 2, id.ToString());
            Raw(raw, 31, id.ToString()); Raw(raw, 32, "1"); Raw(raw, 34, "2023"); Raw(raw, 35, "1"); Raw(raw, 85, "0"); Raw(raw, 93, type.Id.ToString());
            Raw(raw, 5, id == 84 ? "!1&!2&!3&!4&!5&!6&!8&!9&!10&!11&!15&!16" : "");
            Require(Maker.vehicleID == id + 342 && Maker.requiredItemID == 2023 && Maker.requiredItemAmount == 1 && Maker.cashCost == 0 && Maker.getAmmoType(out int ammo, out int amount) && ammo == id && amount == 1, "Canonical maker parsed preconditions differ.");
            Require(items[2023].name == "Nanobot Control System" && !items.ContainsKey(66), "Canonical required-item identity or missing66 premise differs.");
            var vr = Row(vehiclePath, id - 68);
            Raw(vr, 0, "5"); Raw(vr, 1, "v53"); Raw(vr, 2, type.Id.ToString()); Raw(vr, 68, "66"); Raw(vr, 69, "1");
            Require(type.Hitpoints == 250 && type.Destroyable == 1 && type.DropItemId == 66 && type.DropItemQuantity == 1 && type.InventoryItems.All(i => i == 0) && type.ChildVehicles.All(i => i == 0), "Canonical computer preconditions differ.");
            for (int a = 0; a < 6; a++)
            {
                Raw(vr, 9 + a * 4, a < 3 ? "3000" : "0"); Raw(vr, 11 + a * 4, "0");
                Require(type.Armors[a].SelfIgnore == (a < 3 ? 3000 : 0) && type.Armors[a].SelfReduction == 0, "Canonical armor parse differs.");
            }
            var projectile = (ItemInfo.Projectile)items[1002]; var pr = Row(itemPath, 30);
            Raw(pr, 0, "6"); Raw(pr, 1, "v57"); Raw(pr, 2, "1002"); Raw(pr, 116, "0"); Raw(pr, 151, "16"); Raw(pr, 152, "5000"); Raw(pr, 153, "3000"); Raw(pr, 171, "56");
            for (int i = 154; i <= 170; i++) Raw(pr, i, "0");
            Require(projectile.name == "ChunkA" && projectile.damageMode == 0 && projectile.kineticDamageRadius == 16 && projectile.kineticDamageInner == 5000 && projectile.kineticDamageOuter == 3000 && projectile.energyDamageRadius == 56, "Canonical projectile parse differs.");

            previousServer = Helpers._server;
            previousAssets = AssetManager.Manager;
            previousMaxVehicles = InfServer.Game.Arena.maxVehicles;
            // Upstream log events cascade; classify by actual TLog, not event name.
            warningSink = (type, _, message) => { if (type == TLog.Warning) Warnings.Add(message); };
            securitySink = (type, _, message) => { if (type == TLog.Security) Security.Add(message); };
            errorSink = (type, _, message) => { if (type is TLog.Error or TLog.Exception) Errors.Add(message); };
            try
            {
                // Actual constructor with NEW tiny map/config; no init() or poll().
                var sections = new Dictionary<string, Dictionary<string, string>> { ["Arena"] = new(), ["Owner"] = new() };
                var assets = new AssetManager { Level = new LvlInfo { Width = 4, Height = 4, Tiles = new LvlInfo.Tile[16] } };
                SetMap(assets, "_idToItem", new SortedDictionary<int, ItemInfo>(items));
                SetMap(assets, "_idToVehicle", new SortedDictionary<int, VehInfo>(vehicles));
                Server = new DiagnosticServer { _assets = assets, _zoneConfig = new CfgInfo { arena = new CfgInfo.Arena(ref sections), owner = new CfgInfo.Owner(ref sections) } };
                // NEW diagnostic capacity, restored after every case; no arena init.
                InfServer.Game.Arena.maxVehicles = 10;
                Helpers._server = Server;
                typeof(AssetManager).GetField("_singleton", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, assets);
                Arena = new DiagnosticArena(Server);
                Arena._turretGroups = new Arena.TurretGroupMan();
                var team = new Team(Arena, Server) { _name = "NEW diagnostic team" };
                Player = new Player { _id = 1, _alias = "NEW diagnostic maker", _server = Server, _arena = Arena, _team = team,
                    _state = new Helpers.ObjectState { positionX = 32, positionY = 32 }, _client = new Client(false) { _C2S_UDPSize = 4096 },
                    _inventory = new(), _skills = new(), _bSpectator = false };
                Player.WipeStats(); Player.Cash = 0;
                Player._baseVehicle = new Vehicle(vehicles[2002], Arena);
                Player._inventory.Add(id, new Player.InventoryItem { item = Maker, quantity = 1 });
                Player._inventory.Add(2023, new Player.InventoryItem { item = items[2023], quantity = 1 });
                Arena.events["Player.MakeVehicle"] += new CEventHandler(Hooks, nameof(MakerHooks.Make));
                Arena.events["Vehicle.Creation"] += new CEventHandler(Hooks, nameof(MakerHooks.Creation));
                Arena.events["Vehicle.Death"] += new CEventHandler(Hooks, nameof(MakerHooks.Death));
                Log.eWarning += warningSink; Log.eSecurity += securitySink; Log.eError += errorSink;
            }
            catch
            {
                // A failed constructor is never acquired by using/Dispose.
                // Roll back all static mutations and any partial event setup.
                Cleanup();
                throw;
            }
        }

        public void Packet(bool expectQueued = true, int owned = 1)
        {
            // Direct packet fields are NEW diagnostics, not wire decoding proof.
            var packet = (CS_PlayerUseItem)RuntimeHelpers.GetUninitializedObject(typeof(CS_PlayerUseItem));
            packet.itemID = (short)Maker.id; packet.posX = 32; packet.posY = 32;
            var method = typeof(ScriptArena).Assembly.GetType("InfServer.Logic.Logic_Game", true)!.GetMethod("Handle_CS_PlayerUseItem", BindingFlags.Public | BindingFlags.Static)!;
            Require(Arena._events.Count == 0 && Queue().Length == 0, "Fixture must start without queued work.");
            method.Invoke(null, new object[] { packet, Player });
            Require(Arena._events.Count == (expectQueued ? 1 : 0), "Actual packet enqueue/skill guard differed.");
            Require(Player.getInventoryAmount(Maker.id) == owned && Player.Cash == 0 && !Arena.Vehicles.Any() && Hooks.MakeCalls == 0 && Queue().Length == 0, "Packet changed state before queued action.");
        }
        public void PacketAndDrain(int owned = 1)
        {
            Packet(owned: owned);
            Require(Arena._events.TryTake(out var action), "Actual queued action missing.");
            action!(Arena); // exactly one action; deliberately never Arena.poll()
            Require(Arena._events.Count == 0, "Unexpected additional arena actions.");
        }
        object[] Queue() => Player._client._streams.SelectMany(s => s.reliableQueue).Select(q => (object)q.packet).ToArray();
        void AssertPackets(int reload, int inventory)
        {
            var queued = Queue();
            Require(queued.Length == reload + inventory && queued.Count(p => p is SC_ItemReload) == reload && queued.Count(p => p is SC_Inventory) == inventory, "Reload/inventory packet count or type differed.");
            if (reload > 0) Require(queued[0] is SC_ItemReload r && r.itemID == Maker.id && r.Data.Length > 0, "Reload ordering/serialization differed.");
            if (inventory > 0)
            {
                var sync = (SC_Inventory)queued.Last();
                Require(sync.cash == 0 && BitConverter.ToInt32(sync.Data, 1) == 0 && BitConverter.ToInt16(sync.Data, 7) == 1 && BitConverter.ToInt16(sync.Data, 9) == 2023 && BitConverter.ToUInt16(sync.Data, 11) == 1, "Actual inventory serialization must retain only canonical Nanobot2023.");
            }
        }
        public void AssertCreated()
        {
            Require(Arena.Vehicles.Count() == 1 && Arena.Vehicles.Single().GetType() == typeof(Computer), "Expected one actual Computer.");
            Require(Created._type.Id == Maker.vehicleID && Created._creator == Player && Created._team == Player._team && Created._state.positionX == 32 && Created._state.positionY == 32 && Created._state.health == 250 && Created._childs.Count == 0, "Factory identity/state/owner differed.");
            Require(Hooks.MakeCalls == 1 && Hooks.CreationCalls == 1 && Player.getInventoryAmount(Maker.id) == 0 && Player.getInventoryAmount(2023) == 1 && Player.Cash == 0, "Make/event/ammo/cash boundary differed.");
            AssertPackets(1, 1);
            Created.Destroyed += _ => destroyed++;
        }
        public void AssertNoCreation(int ammo, int makeCalls, int reload, int inventory)
        {
            Require(!Arena.Vehicles.Any() && Hooks.CreationCalls == 0 && Hooks.MakeCalls == makeCalls && Player.getInventoryAmount(Maker.id) == ammo && Player.getInventoryAmount(2023) == 1 && Player.Cash == 0 && Warnings.Count == 0, "Rejected make state differed.");
            AssertPackets(reload, inventory);
        }
        public void Explode(bool expectedMissingWarning)
        {
            // Only runtime state changes; canonical Hitpoints250 is untouched.
            Created._state.health = 1;
            var explosion = (CS_Explosion)RuntimeHelpers.GetUninitializedObject(typeof(CS_Explosion));
            explosion.explosionID = 1002; explosion.positionX = 32; explosion.positionY = 32;
            Arena.handlePlayerExplosion(Player, explosion);
            AssertDestroyed(1, expectedMissingWarning ? 1 : 0);
        }
        public void AssertDestroyed(int deathCalls, int missingWarnings)
        {
            Require(Hooks.DeathCalls == deathCalls && destroyed == 1 && Created._state.health == 0 && Created.bCondemned, "Death overload/Destroyed/condemned state differed.");
            Require(Arena._items.Count == 0 && Player.getInventoryAmount(66) == 0 && Warnings.Count == missingWarnings && Warnings.All(w => w.Contains("Unable to get item by ID: 66")), "Unexpected drop or missing-item warning path.");
            Require(Arena.Vehicles.Count() == 1, "No-poll fixture must retain condemned tracker entry.");
            AssertPackets(1, 1);
        }
        public void AssertInert() => Require(!Server.HasSocket && Server._clients.Count == 0 && Server._totalBytesSent == 0 && Server._totalBytesReceived == 0 && Player._client._packetsSent == 0 && Player._client._bytesSent == 0 && Arena._events.Count == 0, "Unexpected transport or queued work.");
        public string Evidence() => $"Actual handlers: maker{Maker.id}->VEH{Maker.vehicleID}; ammo={Player.getInventoryAmount(Maker.id)}, Nanobot2023={Player.getInventoryAmount(2023)}, cash={Player.Cash}, vehicles={Arena.Vehicles.Count()}, make/creation/death={Hooks.MakeCalls}/{Hooks.CreationCalls}/{Hooks.DeathCalls}, destroyed={destroyed}, drops={Arena._items.Count}, queuedPackets={Queue().Length}, warnings=[{string.Join("; ", Warnings)}], security=[{string.Join("; ", Security)}]; NEW composition/health boundary; no poll/socket/physics ticks/native; actual explosion damage math when exercised.";
        public void Dispose() => Cleanup();
        void Cleanup()
        {
            Log.eWarning -= warningSink; Log.eSecurity -= securitySink; Log.eError -= errorSink;
            Helpers._server = previousServer;
            InfServer.Game.Arena.maxVehicles = previousMaxVehicles;
            typeof(AssetManager).GetField("_singleton", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, previousAssets);
            Arena?._events?.Dispose();
        }
    }
    sealed class DiagnosticServer : ZoneServer { public bool HasSocket => _sock != null; }
    sealed class DiagnosticArena(ZoneServer server) : ScriptArena(server, null!)
    {
        public override void poll() => throw new InvalidOperationException("G9 diagnostic must never poll.");
    }
}

// Observation/veto event subscribers, not replacements for the actual handlers.
public sealed class MakerHooks
{
    public bool AllowMake = true, AllowDeath = true;
    public int MakeCalls, CreationCalls, DeathCalls;
    public bool Make(Player p, ItemInfo.VehicleMaker item, short x, short y) { MakeCalls++; return AllowMake; }
    public bool Creation(Vehicle v, Team team, Player creator) { CreationCalls++; return true; }
    public bool Death(Vehicle dead, Player killer) { DeathCalls++; return AllowDeath; }
}
