using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>RT-F034 data: parser round-trip over every pinned ITM f[5] string plus catalog examples. Evaluator semantics are inferred, not validated.</summary>
public sealed class OriginalSkillLogicTests
{
    private static bool Eval(string logic, int[] skills, int[]? percent = null) =>
        OriginalSkillLogic.Parse(logic).Evaluate(skills.Contains, (percent ?? []).Contains);

    [Theory]
    [InlineData("")]
    [InlineData("!%1")]
    [InlineData("(2|8|15|16)!%1")]
    [InlineData("(6|11|2)&!%1")]
    [InlineData("!1&!2&!3&!4&!5&!6&!8&!9&!10&!11&!15&!16")]
    [InlineData("1|3|5|9|10|16|15")]
    public void RoundTripsExactly(string s) => Assert.Equal(s, OriginalSkillLogic.Parse(s).ToString());

    [Fact]
    public void MassDriverExample()
    {
        // (2|8|15|16)!%1: group, implicit join, NOT %1. Implicit = AND is an inference.
        Assert.True(Eval("(2|8|15|16)!%1", [8]));
        Assert.False(Eval("(2|8|15|16)!%1", [3]));
        Assert.False(Eval("(2|8|15|16)!%1", [8], [1]));
        Assert.True(Eval("(6|11|2)&!%1", [11]));
    }

    [Fact]
    public void EmptyAndNegationChains()
    {
        Assert.True(Eval("", []));
        Assert.True(Eval("!1&!2", [5]));
        Assert.False(Eval("!1&!2", [2]));
        Assert.True(Eval("15|16", [16]));
    }

    [Theory]
    [InlineData("1|2&3")]
    [InlineData("(1")]
    [InlineData("1)")]
    [InlineData("a")]
    [InlineData("%")]
    [InlineData("1|")]
    [InlineData("01")]
    public void RejectsMalformedOrUnevidenced(string s) =>
        Assert.Throws<SkillLogicException>(() => OriginalSkillLogic.Parse(s));

    [RequiresOriginalAssetsFact]
    public void EveryPinnedItemLogicStringParsesAndRoundTrips()
    {
        var cat = OriginalItemCatalog.Load(OriginalAssets.Root!);
        Assert.Equal(200, cat.Items.Count);
        var distinct = new HashSet<string>();
        foreach (var i in cat.Items)
        {
            var text = i.Record.Value(5);
            distinct.Add(text);
            Assert.Equal(text, OriginalSkillLogic.Parse(text).ToString());
        }
        Assert.Equal(53, distinct.Count);
        Assert.Equal(120, cat.Items.Count(i => i.Record.Value(5).Length == 0));
        Assert.Equal("(2|8|15|16)!%1", cat.ById(1024).Record.Value(5));
    }
}
