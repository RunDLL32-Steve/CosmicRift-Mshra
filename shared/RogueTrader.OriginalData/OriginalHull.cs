namespace RogueTrader.OriginalData;

/// <summary>Field indexes in an RPG/Rogue.rpg (v6) hull record.</summary>
public static class RpgFields
{
    /// <summary>f[1]: the skill id (Asp = 2). Identified in FIELD_INTERPRETATION.md ("selected skill 2").</summary>
    public const int SkillId = 1;
    /// <summary>f[2]: the pinned active parser names this field Price (value 1 in all twelve rows). Used here only as the hull-row marker (f[2] == "1"), an inferred rule: its native meaning is not established.</summary>
    public const int RecordType = 2;
    /// <summary>f[3]: the default vehicle id (pinned parser: DefaultVehicleId; FIELD_INTERPRETATION: selected skill 2 to vehicle 2002), linked to VEH f[2].</summary>
    public const int VehicleId = 3;
    /// <summary>f[39]: the hull name. Inferred: position located in the file contents and the pinned parser field order (Name follows CashAdjustment); not stated in FIELD_INTERPRETATION.md.</summary>
    public const int Name = 39;
    /// <summary>f[40]: category text such as "Ship Hull - Terran". Inferred like <see cref="Name"/> (pinned parser: Category follows Name).</summary>
    public const int Category = 40;
}

/// <summary>Field indexes in a VEH/Rogue.veh v53 Car record. Each index is documented in docs/original-research/FIELD_INTERPRETATION.md or FEATURE_CATALOG.md, or is marked inferred (Weight f[7], Energy f[94..96]) from the pinned parser order and the documented Asp values.</summary>
public static class VehFields
{
    public const int RecordType = 0;
    public const int VehicleId = 2;
    public const int Name = 3;
    public const int HullPoints = 6;
    /// <summary>f[7]: Weight (10000 for all twelve). Index inferred from the pinned parser order (Name f[3], logic f[4], description f[5], Hitpoints f[6], Weight f[7]) and the documented Asp value; units unvalidated.</summary>
    public const int Weight = 7;
    public const int NormalWeight = 47;
    public const int StopWeight = 48;
    /// <summary>f[94..96] EnergyMax, EnergyRate, EnergyMin: positions located by the documented Asp values 1700/1050/1700 and the active parser's field order.</summary>
    public const int EnergyMax = 94;
    public const int EnergyRate = 95;
    public const int EnergyMin = 96;
    public const int FirstTerrainSpeedField = 202;
    public const int TerrainBlockCount = 16;
    public const int TerrainBlockSize = 10;
    public const int BouncePercent = 362;
    public const int HyperEnergyCost = 373;
    /// <summary>Highest index the typed accessors read; a Car row must have more fields than this.</summary>
    public const int MaxTypedField = HyperEnergyCost;
}

/// <summary>
/// The ten raw speed fields of one terrain block (VEH f[202 + 10t .. 211 + 10t]). Property names are the active v53
/// parser's field names from FIELD_INTERPRETATION.md; units, scaling and sentinel semantics are not validated.
/// </summary>
public sealed class TerrainSpeedBlock
{
    internal TerrainSpeedBlock(int terrainIndex, IReadOnlyList<OriginalInt> values)
    {
        TerrainIndex = terrainIndex;
        Values = values;
    }

    /// <summary>0..15.</summary>
    public int TerrainIndex { get; }

    /// <summary>Index of the block's first field in the VEH record.</summary>
    public int FirstFieldIndex => Values[0].FieldIndex;

    /// <summary>All ten raw values in source order.</summary>
    public IReadOnlyList<OriginalInt> Values { get; }

    public OriginalInt RollAllowed => Values[0];
    public OriginalInt RollFriction => Values[1];
    public OriginalInt RollThrust => Values[2];
    public OriginalInt RollRotate => Values[3];
    public OriginalInt RollTopSpeed => Values[4];
    public OriginalInt StrafeThrust => Values[5];
    public OriginalInt HyperTopSpeed => Values[6];
    public OriginalInt HyperThrust => Values[7];
    public OriginalInt BackwardThrust => Values[8];
    public OriginalInt BackwardHyperThrust => Values[9];
}

/// <summary>
/// One of the twelve selectable original hulls: its RPG row linked to its VEH Car row, with every raw field retained.
/// Typed members return raw, unscaled values with their source field index; anything else is available only through
/// <see cref="RpgRecord"/> and <see cref="VehRecord"/>.
/// </summary>
public sealed class OriginalHull
{
    internal OriginalHull(RawRecord rpg, RawRecord veh)
    {
        RpgRecord = rpg;
        VehRecord = veh;
        Name = rpg.Value(RpgFields.Name);
        SkillId = rpg.Int(RpgFields.SkillId).Value;
        VehicleId = rpg.Int(RpgFields.VehicleId).Value;
        Category = rpg.Value(RpgFields.Category);

        HullPoints = veh.Int(VehFields.HullPoints);
        Weight = veh.Int(VehFields.Weight);
        NormalWeight = veh.Int(VehFields.NormalWeight);
        StopWeight = veh.Int(VehFields.StopWeight);
        EnergyMax = veh.Int(VehFields.EnergyMax);
        EnergyRate = veh.Int(VehFields.EnergyRate);
        EnergyMin = veh.Int(VehFields.EnergyMin);
        BouncePercent = veh.Int(VehFields.BouncePercent);
        HyperEnergyCost = veh.Int(VehFields.HyperEnergyCost);

        var blocks = new TerrainSpeedBlock[VehFields.TerrainBlockCount];
        for (var t = 0; t < blocks.Length; t++)
            blocks[t] = new TerrainSpeedBlock(t, veh.Ints(VehFields.FirstTerrainSpeedField + t * VehFields.TerrainBlockSize, VehFields.TerrainBlockSize));
        TerrainSpeedBlocks = blocks;
    }

    public string Name { get; }
    /// <summary>RPG f[1].</summary>
    public int SkillId { get; }
    /// <summary>RPG f[3]; equals VEH f[2] of the linked record.</summary>
    public int VehicleId { get; }
    /// <summary>RPG f[40], for example "Ship Hull - Terran".</summary>
    public string Category { get; }

    /// <summary>The complete raw RPG row (all fields, row number and version token).</summary>
    public RawRecord RpgRecord { get; }
    /// <summary>The complete raw VEH row (all fields, row number and version token).</summary>
    public RawRecord VehRecord { get; }

    public int RpgRowNumber => RpgRecord.RowNumber;
    public int VehRowNumber => VehRecord.RowNumber;

    /// <summary>VEH f[6].</summary>
    public OriginalInt HullPoints { get; }
    /// <summary>VEH f[7].</summary>
    public OriginalInt Weight { get; }
    /// <summary>VEH f[47] (cargo normal weight threshold, raw).</summary>
    public OriginalInt NormalWeight { get; }
    /// <summary>VEH f[48] (cargo stop weight threshold, raw).</summary>
    public OriginalInt StopWeight { get; }
    /// <summary>VEH f[94].</summary>
    public OriginalInt EnergyMax { get; }
    /// <summary>VEH f[95].</summary>
    public OriginalInt EnergyRate { get; }
    /// <summary>VEH f[96].</summary>
    public OriginalInt EnergyMin { get; }
    /// <summary>VEH f[362].</summary>
    public OriginalInt BouncePercent { get; }
    /// <summary>VEH f[373].</summary>
    public OriginalInt HyperEnergyCost { get; }

    /// <summary>All sixteen terrain speed blocks (f[202..361]).</summary>
    public IReadOnlyList<TerrainSpeedBlock> TerrainSpeedBlocks { get; }

    /// <summary>Terrain 0, the block documented as f[202..211].</summary>
    public TerrainSpeedBlock Terrain0 => TerrainSpeedBlocks[0];
}
