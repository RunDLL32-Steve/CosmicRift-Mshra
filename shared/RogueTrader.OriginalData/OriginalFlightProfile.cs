namespace RogueTrader.OriginalData;

/// <summary>
/// The F028 group of one terrain block (strafe, hyper and reverse fields), raw and unscaled. A negative value is only
/// flagged (<see cref="OriginalInt.IsNegativeSentinel"/>); what a raw -1 reverse value means natively (fallback,
/// disabled, derived from forward thrust) is OPEN and is not resolved here.
/// </summary>
public sealed record OriginalStrafeHyperReverse(
    int TerrainIndex,
    OriginalInt StrafeThrust,
    OriginalInt HyperTopSpeed,
    OriginalInt HyperThrust,
    OriginalInt BackwardThrust,
    OriginalInt BackwardHyperThrust);

/// <summary>
/// Raw, uninterpreted flight-field view of one hull for RT-F027 (momentum/thrust/rotation) and RT-F028 (strafe, hyper
/// thrust, reverse defaults): which terrain blocks carry data, every negative value with its source field index, and
/// the F028 group per terrain plus HyperEnergyCost (VEH f[373]). No units, no scaling, no fallback resolution.
/// </summary>
public sealed class OriginalFlightProfile
{
    private OriginalFlightProfile(OriginalHull hull)
    {
        Hull = hull;
        var blocks = hull.TerrainSpeedBlocks;
        NonZeroTerrainIndexes = blocks.Where(b => b.Values.Any(v => v.Value != 0)).Select(b => b.TerrainIndex).ToArray();
        ActiveTerrainIndexes = blocks.Where(b => b.Values.Take(8).Any(v => v.Value != 0)).Select(b => b.TerrainIndex).ToArray();
        Sentinels = blocks.SelectMany(b => b.Values).Append(hull.HyperEnergyCost).Where(v => v.IsNegativeSentinel).ToArray();
        StrafeHyperReverse = blocks.Select(b => new OriginalStrafeHyperReverse(
            b.TerrainIndex, b.StrafeThrust, b.HyperTopSpeed, b.HyperThrust, b.BackwardThrust, b.BackwardHyperThrust)).ToArray();
        HyperEnergyCost = hull.HyperEnergyCost;
    }

    public static OriginalFlightProfile From(OriginalHull hull) => new(hull);

    public OriginalHull Hull { get; }

    /// <summary>Terrain indexes whose ten raw values are not all zero. This is every terrain in the shipped data, because the two reverse fields hold -1 in all 16 blocks of all 12 hulls.</summary>
    public IReadOnlyList<int> NonZeroTerrainIndexes { get; }

    /// <summary>Terrain indexes with a non-zero value among the first eight block positions (RollAllowed..HyperThrust), i.e. ignoring the -1 reverse fields.</summary>
    public IReadOnlyList<int> ActiveTerrainIndexes { get; }

    /// <summary>Every negative raw value in f[202..361] (terrain blocks) and f[373], in field order, each with its source field index.</summary>
    public IReadOnlyList<OriginalInt> Sentinels { get; }

    /// <summary>The F028 fields for terrains 0..15.</summary>
    public IReadOnlyList<OriginalStrafeHyperReverse> StrafeHyperReverse { get; }

    /// <summary>VEH f[373], raw (energy units and timing unvalidated).</summary>
    public OriginalInt HyperEnergyCost { get; }
}
