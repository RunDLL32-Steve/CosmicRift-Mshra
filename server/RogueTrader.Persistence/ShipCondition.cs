namespace RogueTrader.Persistence;

/// <summary>
/// Saved ship condition (schema 3, queue M8 follow-up): hull and where the ship was left, in simulation world units and
/// radians. Which fields the original game retained across sessions is unrecoverable from the pinned package (RT-F068), so
/// retaining these is a NEW/provisional engineering choice. Velocity is not saved: a reloaded ship starts at rest.
/// </summary>
public sealed record ShipCondition(float Hull, float X, float Y, float Heading)
{
    public bool IsValid => float.IsFinite(Hull) && Hull >= 0 && float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Heading);
}
