using RogueTrader.GameData;
using RogueTrader.Prediction;
using RogueTrader.Protocol;
using RogueTrader.Server;
using RogueTrader.Simulation;
using Xunit.Abstractions;

namespace RogueTrader.UnitTests.Prediction;

// Every identity, control, lead, capacity and integer-time delivery rule below is a NEW/provisional fixture.
// These queues model both transport directions; they use no wall clock, sockets or production pacing policy.
public sealed class PredictionNetworkScheduleTests(ITestOutputHelper output)
{
    private const uint ControlledId = 23;

    [Fact]
    public void TwoWayDelayedCorrectionsReplayWhileIssuingAndPredictFutureAuthorityBeforeItsSnapshot()
    {
        var authority = SeededAuthority();
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, Snapshot(authority), 32);
        var inputs = new List<PlayerInput>();
        var uplink = new DeliveryQueue();
        var downlink = new DeliveryQueue();
        var futureComparisons = new Dictionary<uint, EntityState>();
        var corrections = 0;
        var comparisons = 0;
        var droppedInputs = 0;
        var staleSnapshots = 0;

        void Issue(int clock)
        {
            var input = Predict(predictor);
            inputs.Add(input);
            // The first two named ticks will run neutral: one packet arrives late, one is lost.
            if (input.ClientTick == 3) droppedInputs++;
            else uplink.Send(clock, 2, input);
        }

        // The caller supplies an initial finite lead within C6's accepted window, then issues once per clock.
        for (var i = 0; i < 8; i++) Issue(0);
        Assert.Equal(40u, predictor.LastAcknowledgedSequence);
        for (var clock = 1; clock <= 48; clock++)
        {
            if (clock <= 40) Issue(clock);
            DeliverInputs(uplink, clock, authority);
            authority.Step();
            var currentTick = checked((uint)authority.ServerTick);
            if (futureComparisons.Remove(currentTick, out var expected))
            {
                // This frozen prediction was made several server ticks earlier. No snapshot for this tick has arrived.
                Assert.True(predictor.LastAcceptedServerTick < currentTick);
                Assert.Equal(expected, Ship(authority.World));
                comparisons++;
            }
            if (clock <= 40 && currentTick % 4 == 0)
            {
                var snapshot = Snapshot(authority);
                downlink.Send(clock, 3, snapshot);
                downlink.Send(clock, 4, snapshot); // A real duplicate traverses the delayed queue too.
            }
            foreach (var snapshot in downlink.Receive<WorldSnapshot>(clock))
            {
                if (snapshot.ServerTick <= predictor.LastAcceptedServerTick)
                {
                    var before = State(predictor);
                    Assert.Equal(ReconciliationVerdict.Stale, predictor.Reconcile(snapshot));
                    Assert.Equal(before, State(predictor));
                    staleSnapshots++;
                    continue;
                }
                var beforeShip = predictor.Ship;
                Assert.True(snapshot.ServerTick < currentTick);
                Assert.True(currentTick < predictor.PredictedTick);
                var expectedReplay = Replay(authority.World.Catalog, snapshot, inputs, predictor.PredictedTick);
                Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(snapshot));
                Assert.Equal(expectedReplay, predictor.Ship);
                Assert.True(predictor.PendingCount > 0);
                Assert.NotEqual(snapshot.Entities[0], predictor.Ship);
                if (corrections == 0) Assert.NotEqual(beforeShip, predictor.Ship);
                futureComparisons[predictor.PredictedTick] = predictor.Ship;
                corrections++;
            }
        }
        Assert.Equal(1, droppedInputs);
        Assert.True(authority.Counters[InputVerdict.Late] > 0);
        Assert.True(authority.Counters.MissingInputTicks >= 2);
        Assert.True(authority.Counters.Applied > 30);
        Assert.True(corrections >= 8);
        Assert.True(comparisons >= 8);
        Assert.True(staleSnapshots >= 8);
        Assert.Empty(futureComparisons);
        Assert.Equal(predictor.PredictedTick, (uint)authority.ServerTick);
        Assert.Equal(Ship(authority.World), predictor.Ship); // No terminal reconciliation is performed.
    }

    [Theory]
    [InlineData(18)]
    [InlineData(24)]
    public void RollingTwoWayLossReorderAndBlackoutKeepBoundedStateAndRecoverCapacity(int maximumPending)
    {
        var authority = SeededAuthority();
        var predictor = new ShipPredictor(authority.World.Catalog, ControlledId, Snapshot(authority), maximumPending);
        var inputs = new List<PlayerInput>();
        var uplink = new DeliveryQueue();
        var downlink = new DeliveryQueue();
        var declined = 0;
        var recovered = false;
        var droppedInputs = 0;
        var droppedSnapshots = 0;
        var usefulCorrections = 0;
        var staleSnapshots = 0;
        var reorderedInputs = 0;
        var reorderedSnapshots = 0;
        var highestArrivedSequence = 0u;
        var highestArrivedSnapshot = 0u;
        var invalidAckChecked = false;
        var invalidShipChecked = false;
        var appliedAtClock150 = 0L;

        void Issue(int clock)
        {
            var before = State(predictor);
            var tick = predictor.PredictedTick + 1;
            var (thrust, turn) = Controls(tick);
            if (!predictor.TryPredict(thrust, turn, InputFlags.None, out var input))
            {
                Assert.Equal(maximumPending, predictor.PendingCount);
                Assert.Equal(before, State(predictor));
                Assert.Null(input);
                declined++;
                return;
            }
            if (declined > 0) recovered = true;
            inputs.Add(input!);
            if (input!.Sequence % 17 == 0) { droppedInputs++; return; }
            uplink.Send(clock, 1 + (int)(input.Sequence * 7 % 5), input);
            if (input.Sequence % 11 == 0) uplink.Send(clock, 7, input);
        }

        for (var i = 0; i < 12; i++) Issue(0);
        for (var clock = 1; clock <= 600; clock++)
        {
            Issue(clock);
            var acceptedCorrection = false;
            foreach (var input in uplink.Receive<PlayerInput>(clock))
            {
                if (input.Sequence < highestArrivedSequence) reorderedInputs++;
                highestArrivedSequence = Math.Max(highestArrivedSequence, input.Sequence);
                var shipBefore = Ship(authority.World);
                var tickBefore = authority.ServerTick;
                var ackBefore = authority.LastProcessedInputSequence;
                var countBefore = authority.QueuedInputCount;
                var verdict = authority.Submit(input);
                Assert.Equal(shipBefore, Ship(authority.World));
                Assert.Equal(tickBefore, authority.ServerTick);
                Assert.Equal(ackBefore, authority.LastProcessedInputSequence);
                Assert.Equal(countBefore + (verdict == InputVerdict.Accepted ? 1 : 0), authority.QueuedInputCount);
            }
            authority.Step();
            if (clock % 3 == 0)
            {
                var snapshot = Snapshot(authority);
                // A sufficiently long snapshot blackout fills either configured client bound.
                if ((clock >= 90 && clock <= 135) || snapshot.ServerTick % 19 == 0) droppedSnapshots++;
                else
                {
                    downlink.Send(clock, 1 + (int)(snapshot.ServerTick * 5 % 11), snapshot);
                    if (snapshot.ServerTick % 7 == 0) downlink.Send(clock, 14, snapshot);
                }
            }
            foreach (var snapshot in downlink.Receive<WorldSnapshot>(clock))
            {
                Assert.True(snapshot.ServerTick < authority.ServerTick);
                if (snapshot.ServerTick < highestArrivedSnapshot) reorderedSnapshots++;
                highestArrivedSnapshot = Math.Max(highestArrivedSnapshot, snapshot.ServerTick);
                var before = State(predictor);
                if (snapshot.ServerTick <= predictor.LastAcceptedServerTick)
                {
                    Assert.Equal(ReconciliationVerdict.Stale, predictor.Reconcile(snapshot));
                    Assert.Equal(before, State(predictor));
                    staleSnapshots++;
                    continue;
                }
                if (!invalidAckChecked)
                {
                    var bad = Wire(snapshot with { LastProcessedInputSequence = uint.MaxValue });
                    Assert.Equal(ReconciliationVerdict.InvalidAcknowledgement, predictor.Reconcile(bad));
                    Assert.Equal(before, State(predictor));
                    invalidAckChecked = true;
                }
                if (!invalidShipChecked)
                {
                    var bad = Wire(snapshot with { Entities = [snapshot.Entities[0] with { Kind = EntityKind.Asteroid }] });
                    Assert.Equal(ReconciliationVerdict.InvalidSnapshot, predictor.Reconcile(bad));
                    Assert.Equal(before, State(predictor));
                    invalidShipChecked = true;
                }
                var expectedTick = Math.Max(snapshot.ServerTick, predictor.PredictedTick);
                var expectedReplay = Replay(authority.World.Catalog, snapshot, inputs, expectedTick);
                var expectedPending = inputs.Count(p => p.ClientTick > snapshot.ServerTick);
                if (snapshot.ServerTick < authority.ServerTick && authority.ServerTick < predictor.PredictedTick)
                    usefulCorrections++;
                Assert.Equal(ReconciliationVerdict.Accepted, predictor.Reconcile(snapshot));
                Assert.Equal(expectedReplay, predictor.Ship);
                Assert.Equal(expectedTick, predictor.PredictedTick);
                Assert.Equal(expectedPending, predictor.PendingCount);
                Assert.Equal(snapshot.LastProcessedInputSequence, predictor.LastAcknowledgedSequence);
                Assert.True(predictor.LastAcknowledgedSequence <= predictor.LastIssuedSequence);
                acceptedCorrection = true;
            }
            if (acceptedCorrection)
            {
                // NEW caller pacing fixture: the model clock knows the authority started at tick 1. After a
                // correction, replenish the initial 12-tick lead, bounded by the pending cap. ShipPredictor itself
                // supplies neither clock synchronization nor a lead policy. All refilled packets retain positive
                // uplink delay; the C6 queue/lead/rate limits still apply without being bypassed.
                var targetTick = checked((uint)(1 + clock + 12));
                while (predictor.PredictedTick < targetTick && predictor.PendingCount < maximumPending)
                    Issue(clock);
            }
            if (clock == 150) appliedAtClock150 = authority.Counters.Applied;
            Assert.InRange(predictor.PendingCount, 0, maximumPending);
            Assert.InRange(authority.QueuedInputCount, 0, SessionOptions.Default.InputQueueCapacity);
            Assert.All(new[] { predictor.Ship.X, predictor.Ship.Y, predictor.Ship.VelX, predictor.Ship.VelY,
                predictor.Ship.Rotation, predictor.Ship.Hull, predictor.Ship.Shield, predictor.Ship.Energy }, value => Assert.True(float.IsFinite(value)));
        }
        Assert.True(declined > 0);
        Assert.True(recovered);
        Assert.True(droppedInputs > 0);
        Assert.True(droppedSnapshots > 0);
        Assert.True(staleSnapshots > 0);
        Assert.True(reorderedInputs > 0);
        Assert.True(reorderedSnapshots > 0);
        Assert.True(usefulCorrections > 5);
        Assert.True(authority.Counters.Applied > 30); // Positive uplink delays must not turn the entire run into neutral ticks.
        var appliedAfterRecovery = authority.Counters.Applied - appliedAtClock150;
        Assert.True(appliedAfterRecovery > 200, $"Only {appliedAfterRecovery} inputs applied after clock 150.");
        Assert.True(authority.Counters.MissingInputTicks > 0);
        Assert.True(authority.Counters[InputVerdict.Duplicate] > 0);
        Assert.Equal(0, authority.Counters[InputVerdict.RateLimited]);
        Assert.Equal(0, authority.Counters[InputVerdict.TooFarAhead]);
        Assert.Equal(0, authority.Counters[InputVerdict.QueueFull]);
        Assert.True(invalidAckChecked && invalidShipChecked);
        output.WriteLine($"NEW fixture capacity={maximumPending}: applied at clock 150={appliedAtClock150}, " +
            $"applied total={authority.Counters.Applied}, applied after recovery={appliedAfterRecovery}, " +
            $"declined={declined}, useful corrections={usefulCorrections}.");
    }

    private static AuthoritativeSession SeededAuthority()
    {
        var catalog = TestWorld.Catalog(sector: new SectorDefinition
        {
            PlayerSpawn = new SpawnDefinition { Heading = 0.3f, VelocityX = 23, VelocityY = -7 }
        });
        var authority = new AuthoritativeSession(new WorldSimulation(catalog));
        Assert.Equal(InputVerdict.Accepted, authority.Submit(new PlayerInput(1, 40, 0, 0, 0, InputFlags.None)));
        authority.Step();
        return authority;
    }

    private static (float Thrust, float Turn) Controls(uint tick) =>
        ((tick % 3) switch { 0 => -1f, 1 => 0.7f, _ => 0f }, tick % 2 == 0 ? 0.4f : -0.2f);

    private static PlayerInput Predict(ShipPredictor predictor)
    {
        var (thrust, turn) = Controls(predictor.PredictedTick + 1);
        Assert.True(predictor.TryPredict(thrust, turn, InputFlags.None, out var input));
        return input!;
    }

    private static EntityState Replay(GameCatalog catalog, WorldSnapshot snapshot, List<PlayerInput> issued, uint endTick)
    {
        // Independent WorldSimulation oracle, not ShipPredictor/ShipMotion. Only motion is exercised here, so a
        // fresh world seeded with the snapshot pose/velocity faithfully supplies each subsequent C6 movement tick.
        var ship = snapshot.Entities[0];
        var replayCatalog = new GameCatalog(catalog.Ship, catalog.Weapon, catalog.Sector with
        {
            PlayerSpawn = new SpawnDefinition
            {
                X = ship.X, Y = ship.Y, VelocityX = ship.VelX, VelocityY = ship.VelY, Heading = ship.Rotation
            }
        });
        var world = new WorldSimulation(replayCatalog);
        var suffix = issued.Where(p => p.ClientTick > snapshot.ServerTick && p.ClientTick <= endTick).ToArray();
        Assert.Equal(endTick - snapshot.ServerTick, (uint)suffix.Length);
        foreach (var input in suffix) world.Tick(new ShipInput(input.Thrust, input.Turn, false));
        return Ship(world) with { Hull = ship.Hull, Shield = ship.Shield };
    }

    private static EntityState Ship(WorldSimulation world) => new(ControlledId, EntityKind.Ship,
        world.Player.Position.X, world.Player.Position.Y, world.Player.Velocity.X, world.Player.Velocity.Y,
        world.Player.Heading, world.Player.Hull, world.Player.Shield, world.Player.Energy);

    private static WorldSnapshot Snapshot(AuthoritativeSession authority) =>
        new(checked((uint)authority.ServerTick), authority.LastProcessedInputSequence, [Ship(authority.World)]);

    private static (EntityState Ship, uint Tick, uint ServerTick, uint Ack, uint Issued, int Count) State(ShipPredictor p) =>
        (p.Ship, p.PredictedTick, p.LastAcceptedServerTick, p.LastAcknowledgedSequence, p.LastIssuedSequence, p.PendingCount);

    private static T Wire<T>(T message) where T : Message => Assert.IsType<T>(MessageCodec.Decode(MessageCodec.Encode(message)));

    private static void DeliverInputs(DeliveryQueue queue, int clock, AuthoritativeSession authority)
    {
        foreach (var input in queue.Receive<PlayerInput>(clock))
        {
            var verdict = authority.Submit(input);
            Assert.True(verdict is InputVerdict.Accepted or InputVerdict.Late);
        }
    }

    private sealed class DeliveryQueue
    {
        private readonly List<(int Due, int Order, byte[] Frame)> _messages = [];
        private int _nextOrder;

        internal void Send(int clock, int delay, Message message)
        {
            Assert.True(delay > 0);
            _messages.Add((clock + delay, _nextOrder++, MessageCodec.Encode(message)));
        }

        internal IEnumerable<T> Receive<T>(int clock) where T : Message
        {
            var due = _messages.Where(m => m.Due <= clock).OrderBy(m => m.Due).ThenBy(m => m.Order).ToArray();
            foreach (var delivery in due)
            {
                _messages.Remove(delivery);
                yield return Assert.IsType<T>(MessageCodec.Decode(delivery.Frame));
            }
        }
    }
}
