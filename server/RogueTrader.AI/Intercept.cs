using System.Numerics;

namespace RogueTrader.AI;

public static class Intercept
{
    /// <summary>
    /// Smallest positive time for a projectile of <paramref name="projectileSpeed"/> launched from the shooter to meet a target
    /// moving at constant <paramref name="relativeVelocity"/> from <paramref name="relativePosition"/> (both relative to the shooter's
    /// launch frame). Returns false when no intercept exists.
    /// </summary>
    public static bool TrySolve(Vector2 relativePosition, Vector2 relativeVelocity, float projectileSpeed, out float time)
    {
        time = 0;
        if (!(projectileSpeed > 0) || !float.IsFinite(projectileSpeed))
            return false;
        var a = Vector2.Dot(relativeVelocity, relativeVelocity) - projectileSpeed * projectileSpeed;
        var b = 2 * Vector2.Dot(relativePosition, relativeVelocity);
        var c = Vector2.Dot(relativePosition, relativePosition);
        const float epsilon = 1e-6f;
        if (MathF.Abs(a) < epsilon)
        {
            if (MathF.Abs(b) < epsilon)
                return false;
            var linear = -c / b;
            if (linear <= 0) return false;
            time = linear;
            return true;
        }
        var discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
            return false;
        var root = MathF.Sqrt(discriminant);
        var first = (-b - root) / (2 * a);
        var second = (-b + root) / (2 * a);
        var low = MathF.Min(first, second);
        var high = MathF.Max(first, second);
        if (low > 0) { time = low; return true; }
        if (high > 0) { time = high; return true; }
        return false;
    }

    /// <summary>Signed smallest angle from <paramref name="from"/> to <paramref name="to"/>, in [-pi, pi].</summary>
    public static float AngleDifference(float from, float to) => MathF.IEEERemainder(to - from, MathF.Tau);

    public static float HeadingTo(Vector2 offset) => MathF.Atan2(offset.Y, offset.X);
}
