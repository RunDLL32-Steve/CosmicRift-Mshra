using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>Reader edge cases on small inline fixtures; none of these need the original assets.</summary>
public sealed class RawRecordReaderTests
{
    [Fact]
    public void QuotedStringsMayContainCommasAndKeepTheirText()
    {
        var record = Assert.Single(RawRecordReader.Parse("2,v53,2002,\"Asp, the fast\",x\r\n", "t"));
        Assert.Equal(5, record.FieldCount);
        Assert.Equal("Asp, the fast", record.Value(3));
        Assert.True(record.Fields[3].WasQuoted);
        Assert.Equal("\"Asp, the fast\"", record.Fields[3].RawText);
        Assert.Equal("x", record.Value(4));
    }

    [Fact]
    public void StrayTextAfterClosingQuoteAndUnterminatedQuoteAreRejected()
    {
        Assert.Throws<OriginalDataFormatException>(() => RawRecordReader.Parse("a,\"b\"c,d\r\n", "t"));
        Assert.Throws<OriginalDataFormatException>(() => RawRecordReader.Parse("a,\"b,c\r\n", "t"));
        var ex = Assert.Throws<OriginalDataFormatException>(() => RawRecordReader.Parse("a\r\nb,\"c\r\n", "file.x"));
        Assert.Equal("file.x", ex.SourceFile);
        Assert.Equal(2, ex.Row);
    }

    [Fact]
    public void CrlfLfAndMissingFinalTerminatorGiveOneRecordPerLine()
    {
        Assert.Equal(3, RawRecordReader.Parse("a,1\r\nb,2\r\nc,3\r\n", "t").Count);
        Assert.Equal(3, RawRecordReader.Parse("a,1\nb,2\nc,3", "t").Count);
        var records = RawRecordReader.Parse("a,1\r\nb,2\r\nc,3", "t");
        Assert.Equal(["a", "b", "c"], records.Select(r => r.Value(0)));
        Assert.DoesNotContain(records.SelectMany(r => r.Fields), f => f.RawText.Contains('\r') || f.RawText.Contains('\n'));
    }

    [Fact]
    public void RowNumbersArePhysicalAndOneBasedAndBlankLinesAreSkipped()
    {
        var records = RawRecordReader.Parse("a,1\r\n\r\n  \r\nb,2\r\n", "SRC/x.dat");
        Assert.Equal([1, 4], records.Select(r => r.RowNumber));
        Assert.All(records, r => Assert.Equal("SRC/x.dat", r.SourceFile));
    }

    [Fact]
    public void PaddingAndTrailingWhitespaceAreTrimmedInValueButKeptInRawText()
    {
        var record = Assert.Single(RawRecordReader.Parse("v6,  0,0 ,  \"x\"  ,  7  \r\n", "t"));
        Assert.Equal(5, record.FieldCount);
        Assert.Equal("  0", record.Fields[1].RawText);
        Assert.Equal("0", record.Fields[1].Value);
        Assert.Equal("0 ", record.Fields[2].RawText);
        Assert.Equal("x", record.Fields[3].Value);
        Assert.Equal("  \"x\"  ", record.Fields[3].RawText);
        Assert.Equal("7", record.Value(4));
        Assert.Equal("  7  ", record.Fields[4].RawText);
        Assert.Equal(7, record.Int(4).Value);
    }

    [Fact]
    public void EmptyFieldsAndTrailingCommaAreKeptByIndex()
    {
        var record = Assert.Single(RawRecordReader.Parse("a,,\"\",  ,b,\r\n", "t"));
        Assert.Equal(6, record.FieldCount);
        Assert.Equal(["a", "", "", "", "b", ""], record.Fields.Select(f => f.Value));
        Assert.Equal([false, false, true, false, false, false], record.Fields.Select(f => f.WasQuoted));
        Assert.Equal([0, 1, 2, 3, 4, 5], record.Fields.Select(f => f.Index));
    }

    [Fact]
    public void VersionTokenIsRetainedWhetherInFieldZeroOrOne()
    {
        var records = RawRecordReader.Parse("v6,2,1\r\n2,v53,2002\r\n5,6,7\r\n", "t");
        Assert.Equal(["v6", "v53", null], records.Select(r => r.VersionToken));
    }

    [Fact]
    public void IntegersAreReadUnscaledWithSentinelFlagAndBadTextIsRejected()
    {
        var record = Assert.Single(RawRecordReader.Parse("0,-1,3500,x,99999999999\r\n", "t"));
        Assert.Equal(new OriginalInt(1, -1, true), record.Int(1));
        Assert.Equal(new OriginalInt(2, 3500, false), record.Int(2));
        Assert.Equal(new OriginalInt(0, 0, false), record.Int(0));
        Assert.Throws<OriginalDataFormatException>(() => record.Int(3));
        Assert.Throws<OriginalDataFormatException>(() => record.Int(4));
        Assert.Throws<OriginalDataFormatException>(() => record.Int(5));
    }

    [Fact]
    public void BytesAreDecodedAsLatin1SoEveryByteRoundTrips()
    {
        var bytes = new byte[] { (byte)'a', (byte)',', 0xE9, 0xFF, (byte)'\r', (byte)'\n' };
        var record = Assert.Single(RawRecordReader.Parse(bytes, "t"));
        Assert.Equal(Encoding.Latin1.GetString([0xE9, 0xFF]), record.Value(1));
    }
}
