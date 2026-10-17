namespace RogueTrader.Persistence;

/// <summary>
/// Checks that a profile's chosen hull, inventory item ids and upgrade ids are known to a content catalog (RT-F068 binding). The
/// persistence core stays data-agnostic: the catalog is supplied as three predicates, so original data (for example
/// <c>RogueTrader.OriginalData.OriginalProgressionIds</c>) can be bound without this project referencing it. Additive: repositories and the
/// transaction processor do not call it; a caller decides when to validate (for example before create/save or after load).
/// <para>
/// Only membership is checked. Quantities, cash and the structural invariants stay with <see cref="ProfileInvariants"/>; hull-change,
/// wipe, cargo and price rules are not decided here. A null <see cref="PlayerProfile.ShipHullId"/> (no hull chosen) is accepted; a null collection is skipped (ProfileInvariants reports it).
/// </para>
/// </summary>
public sealed class ProfileContentValidator
{
    private readonly Func<string, bool> _isKnownHull;
    private readonly Func<string, bool> _isKnownItem;
    private readonly Func<string, bool> _isKnownUpgrade;

    public ProfileContentValidator(Func<string, bool> isKnownHull, Func<string, bool> isKnownItem, Func<string, bool> isKnownUpgrade)
    {
        ArgumentNullException.ThrowIfNull(isKnownHull);
        ArgumentNullException.ThrowIfNull(isKnownItem);
        ArgumentNullException.ThrowIfNull(isKnownUpgrade);
        _isKnownHull = isKnownHull;
        _isKnownItem = isKnownItem;
        _isKnownUpgrade = isKnownUpgrade;
    }

    /// <summary>Every unknown id, hull first, then inventory and upgrades in their ordinal order. Empty == all ids known.</summary>
    public IReadOnlyList<ProfileContentViolation> Validate(PlayerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var violations = new List<ProfileContentViolation>();
        if (profile.ShipHullId is { } hull && !_isKnownHull(hull))
            violations.Add(new ProfileContentViolation(ProfileContentField.Hull, hull));
        foreach (var item in profile.Inventory?.Keys ?? Enumerable.Empty<string>())
            if (!_isKnownItem(item)) violations.Add(new ProfileContentViolation(ProfileContentField.Inventory, item));
        foreach (var upgrade in profile.Upgrades ?? Enumerable.Empty<string>())
            if (!_isKnownUpgrade(upgrade)) violations.Add(new ProfileContentViolation(ProfileContentField.Upgrade, upgrade));
        return violations;
    }

    public bool IsValid(PlayerProfile profile) => Validate(profile).Count == 0;
}
