using System.Security.Cryptography;
using InfServer.Network;
using InfServer.Protocol;

public static class PacketLengthChecks
{
    public static LengthCheckReport Run(string mode)
    {
        var checks = new List<LengthObservation>();
        var failures = new List<string>();
        foreach (bool indexed in new[] { false, true })
        foreach (int count in new[] { 0, 3, 65535, 65536, 65537 })
        foreach (byte crc in new byte[] { 0, 4 })
        foreach (string threshold in new[] { "default512", "exact", "one-over" })
        {
            var packet = new ByteArrayFixture(indexed, count);
            byte[] expected = packet.ExpectedBytes;
            int udpLimit = threshold == "default512" ? 512 : expected.Length + 4 + crc - (threshold == "one-over" ? 1 : 0);
            var client = new Client(false) { _C2S_UDPSize = udpLimit, _CRCLength = crc };
            // Actual queue-selection code, never poll/flush/sendQueuedPackets.
            client.sendReliable(packet);
            var queued = client._streams.SelectMany(stream => stream.reliableQueue).ToArray();
            Require(queued.Length == 1, "Expected exactly one queued fixture.");
            Require(packet.Data.AsSpan().SequenceEqual(expected), "Whole/indexed serialization changed bytes or included sentinels.");
            int oldLength = 2 + (ushort)count;
            Require(packet.Length == (mode == "baseline" ? oldLength : expected.Length), "Unexpected length accounting.");
            bool expectedStream = 4 + expected.Length + crc > udpLimit;
            bool actualStream = queued[0].dataStream != null;
            bool oldStream = 4 + oldLength + crc > udpLimit;
            Require(actualStream == (mode == "baseline" ? oldStream : expectedStream), "Unexpected queue selection.");
            if (actualStream)
                Require(queued[0].packet == null && queued[0].dataStream.amountSent == 0 && queued[0].dataStream.buffer.AsSpan().SequenceEqual(expected), "DataStream did not retain the full wire buffer.");
            else
                Require(ReferenceEquals(queued[0].packet, packet), "Reliable queue lost the original packet.");
            Require(client._handler == null && client._ipe == null && client._bytesSent == 0 && client._packetsSent == 0 && client._packetQueue.Count == 0,
                "Inert queue fixture acquired transport activity.");
            string id = $"{(indexed ? "indexed-offset3" : "whole")}.{count}.crc{crc}.{threshold}";
            bool lengthPassed = packet.Length == expected.Length;
            bool queuePassed = actualStream == expectedStream;
            if (!lengthPassed) failures.Add(id + ".length");
            if (!queuePassed) failures.Add(id + ".queue-selection");
            checks.Add(new(id, count, crc, udpLimit, packet.Length, expected.Length, lengthPassed, queuePassed,
                expectedStream, actualStream, mode == "baseline" && (!lengthPassed || !queuePassed),
                Convert.ToHexString(SHA256.HashData(expected)).ToLowerInvariant()));
        }
        Require(mode != "baseline" || failures.Any(name => name.EndsWith(".queue-selection")), "Baseline failed to reproduce incorrect queue selection.");
        Require(mode != "fixed" || failures.Count == 0, "Fixed length/queue invariants failed.");
        return new(checks.ToArray(), failures.ToArray());
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }

    // NEW/provisional diagnostic bytes, not original asset or gameplay values.
    private sealed class ByteArrayFixture : PacketBase
    {
        private readonly byte[] source;
        private readonly int count;
        private readonly bool indexed;
        public byte[] ExpectedBytes { get; }
        public ByteArrayFixture(bool indexed, int count) : base(0x11)
        {
            this.indexed = indexed; this.count = count;
            source = Enumerable.Repeat((byte)0xef, count + (indexed ? 6 : 0)).ToArray();
            int offset = indexed ? 3 : 0;
            for (int index = 0; index < count; ++index) source[offset + index] = (byte)(index % 251);
            ExpectedBytes = new byte[count + 2];
            ExpectedBytes[0] = 0x11; ExpectedBytes[^1] = 0x22;
            source.AsSpan(offset, count).CopyTo(ExpectedBytes.AsSpan(1));
        }
        public override void Serialize()
        {
            Write((byte)0x11);
            if (indexed) Write(source, 3, count); else Write(source);
            Write((byte)0x22);
        }
    }
}

public sealed record LengthCheckReport(LengthObservation[] Checks, string[] DesiredInvariantFailures);
public sealed record LengthObservation(string Id, int PayloadCount, byte CrcLength, int UdpLimit, int ReportedLength, int WireLength,
    bool LengthPassed, bool QueueSelectionPassed, bool ExpectedDataStream, bool ActualDataStream, bool ReproducedBaselineDefect, string WireSha256);
