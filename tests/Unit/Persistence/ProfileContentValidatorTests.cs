using System.Collections.Immutable;
using RogueTrader.Persistence;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>RT-F068 binding seam, without original assets. Every id and number here is a NEW synthetic fixture, not original data.</summary>
public sealed class ProfileContentValidatorTests
{
    private static readonly HashSet<string> Hulls = new(StringComparer.Ordinal) { Fx.Hull };
    private static readonly HashSet<string> Items = new(StringComparer.Ordinal) { Fx.OreA, Fx.OddItem };
    private static readonly HashSet<string> Upgrades = new(StringComparer.Ordinal) { Fx.UpgradeX };

    private static ProfileContentValidator Validator() => new(Hulls.Contains, Items.Contains, Upgrades.Contains);

    private static PlayerProfile Profile(string? hull, (string Item, int Qty)[] items, params string[] upgrades) =>
        PlayerProfile.CreateNew(Fx.ProfileId, 50, hull) with // NEW fixture wallet
        {
            Inventory = Env.Items(items),
            Upgrades = ImmutableSortedSet.CreateRange(StringComparer.Ordinal, upgrades),
        };

    [Fact]
    public void KnownIdsPass()
    {
        var p = Profile(Fx.Hull, new[] { (Fx.OreA, 3), (Fx.OddItem, 1) }, Fx.UpgradeX); // NEW fixture quantities
        Assert.Empty(Validator().Validate(p));
        Assert.True(Validator().IsValid(p));
    }

    [Fact]
    public void NoHullChosenIsAccepted()
    {
        Assert.Empty(Validator().Validate(Profile(null, Array.Empty<(string, int)>())));
    }

    [Fact]
    public void UnknownIdsAreReportedPerFieldInOrder()
    {
        var p = Profile("fixture-hull-unknown", new[] { (Fx.OreA, 1), ("fixture-item-unknown", 2), (Fx.UpgradeX, 1) }, Fx.UpgradeY, "fixture-upgrade-a");
        var v = Validator().Validate(p);
        Assert.Equal(new[]
        {
            new ProfileContentViolation(ProfileContentField.Hull, "fixture-hull-unknown"),
            new ProfileContentViolation(ProfileContentField.Inventory, "fixture-item-unknown"),
            new ProfileContentViolation(ProfileContentField.Inventory, Fx.UpgradeX),
            new ProfileContentViolation(ProfileContentField.Upgrade, "fixture-upgrade-a"),
            new ProfileContentViolation(ProfileContentField.Upgrade, Fx.UpgradeY),
        }, v);
        Assert.False(Validator().IsValid(p));
    }

    [Fact]
    public void NamespacesAreSeparate()
    {
        // A hull id in the inventory, or an item id as the hull, is unknown in that field.
        var p = Profile(Fx.OreA, new[] { (Fx.Hull, 1) });
        Assert.Equal(new[]
        {
            new ProfileContentViolation(ProfileContentField.Hull, Fx.OreA),
            new ProfileContentViolation(ProfileContentField.Inventory, Fx.Hull),
        }, Validator().Validate(p));
    }

    [Fact]
    public void NullCollectionsAreLeftToInvariants()
    {
        var p = PlayerProfile.CreateNew(Fx.ProfileId, 0, Fx.Hull) with { Inventory = null!, Upgrades = null! };
        Assert.Empty(Validator().Validate(p));
        Assert.NotEmpty(ProfileInvariants.Validate(p, new PersistenceOptions()));
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new ProfileContentValidator(null!, Items.Contains, Upgrades.Contains));
        Assert.Throws<ArgumentNullException>(() => new ProfileContentValidator(Hulls.Contains, null!, Upgrades.Contains));
        Assert.Throws<ArgumentNullException>(() => new ProfileContentValidator(Hulls.Contains, Items.Contains, null!));
        Assert.Throws<ArgumentNullException>(() => Validator().Validate(null!));
    }
}
