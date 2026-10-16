namespace RogueTrader.OriginalData;

/// <summary>
/// Zero-based field map of a VEH <c>v53</c> type-5 (Computer) row, 477 fields (RT-F058 data clause; also the layout the
/// RT-F055/F057 block offsets belong to). EVERY NAME HERE IS AN UPSTREAM-PARSER FIELD NAME (InfantryOnline server,
/// Infantry-Online-Server commit bb97579e, dotnetcore/Assets/Veh/VehInfo.cs, VehInfo.Computer.cs, VehInfo.ComputerProduct.cs and the
/// shared sprite/HSV/armor readers), NOT ORIGINAL-EXECUTABLE EVIDENCE. Only the order of reads was transcribed
/// (scripts/original-assets/veh_computer_field_map.py reproduces the full index to name table and its --check mode verifies, over the
/// 17 pinned rows, that the string slots hold text and every other slot an integer); no upstream code is copied. Name, position and
/// value ranges say nothing about what the native game does with a field. Units, scaling and sentinels (-1, "None", "") are not
/// interpreted anywhere in this class.
/// </summary>
public static class VehComputerFields
{
    public const int RecordType = 5;
    public const string Version = "v53";
    public const int FieldCount = 477;

    // ---- shared vehicle header and base block (f[0]..f[201]) ----
    public const int TypeField = 0;
    public const int VersionField = 1;
    public const int IdField = 2;
    public const int NameField = 3;
    public const int SkillLogic = 4;
    public const int Description = 5;
    public const int Hitpoints = 6;
    public const int Weight = 7;
    public const int ClassId = 8;
    /// <summary>Armor0..Armor5, four ints each (SelfIgnore, PassIgnore, SelfReduction, Pass): f[9]..f[32].</summary>
    public const int ArmorFirst = 9;
    public const int ArmorStride = 4;
    public const int ArmorCount = 6;
    public const int BarrelLength = 33;
    public const int FireHeight = 34;
    public const int PhysicalRadius = 35;
    public const int TriggerRadius = 36;
    public const int VehiclePhysicsOwned = 37;
    public const int VehiclePhysicsUnowned = 38;
    public const int WeaponPhysicsOwned = 39;
    public const int WeaponPhysicsUnowned = 40;
    public const int LowZ = 41;
    public const int HighZ = 42;
    public const int PickupItemId = 43;
    public const int IsControllable = 44;
    public const int IsWarpable = 45;
    public const int EnergyCostPerTick = 46;
    public const int NormalWeight = 47;
    public const int StopWeight = 48;
    public const int DisplayHealth = 49;
    /// <summary>InventoryItems0..5: f[50]..f[55].</summary>
    public const int InventoryItemsFirst = 50;
    public const int InventoryItemsCount = 6;
    /// <summary>ChildVehicles0..7: f[56]..f[63].</summary>
    public const int ChildVehiclesFirst = 56;
    public const int ChildVehiclesCount = 8;
    public const int ExplodeItemId = 64;
    public const int RadarPrize = 65;
    public const int VehicleGetInMode = 66;
    public const int VisualHeight = 67;
    public const int DropItemId = 68;
    public const int DropItemQuantity = 69;
    /// <summary>Sprites0 (f[70]..f[81]) and Sprites1 (f[82]..f[93]): TickDistance, TickFrequency, Speed, Color, then an 8-field sprite blob.</summary>
    public const int AnimatedSpriteFirst = 70;
    public const int AnimatedSpriteStride = 12;
    public const int AnimatedSpriteCount = 2;
    public const int EnergyMax = 94;
    public const int EnergyRate = 95;
    public const int EnergyMin = 96;
    public const int ThrowTime = 97;
    public const int AllowWeapons = 98;
    public const int Style1BallSpeed = 99;
    public const int Style1LowFireAngle = 100;
    public const int Style1HighFireAngle = 101;
    public const int Style1KeyAssignment = 102;
    public const int Style1BallFriction = 103;
    public const int Style2BallSpeed = 104;
    public const int Style2LowFireAngle = 105;
    public const int Style2HighFireAngle = 106;
    public const int Style2KeyAssignment = 107;
    public const int Style2BallFriction = 108;
    /// <summary>TerrainModifiers0..15: f[109]..f[124].</summary>
    public const int TerrainModifiersFirst = 109;
    public const int TerrainModifiersCount = 16;
    public const int BallCarryRollVehicleItem = 125;
    public const int BallCarryArmorVehicleItem = 126;
    public const int FlagCarryRollVehicleItem = 127;
    public const int FlagCarryArmorVehicleItem = 128;
    public const int EventString1 = 129;
    public const int EventString2 = 130;
    public const int EventString3 = 131;
    public const int LosDistance = 132;
    public const int LosAngle = 133;
    public const int LosXRay = 134;
    public const int CombatAwarenessTime = 135;
    public const int SiblingKillsShared = 136;
    public const int RelativeId = 137;
    public const int DisplayOnFriendlyRadar = 138;
    public const int FriendlyRadarColor = 139;
    public const int DisplayOnEnemyRadar = 140;
    public const int EnemyRadarColor = 141;
    /// <summary>30 pairs (HoldItemLimits, HoldItemExtendedLimits): f[142]..f[201].</summary>
    public const int HoldItemFirst = 142;
    public const int HoldItemStride = 2;
    public const int HoldItemCount = 30;
    public const int BaseEnd = 202;

    // ---- computer scalar block, 35 fields f[202]..f[236] ----
    public const int RotateSpeed = 202;
    public const int AngleStart = 203;
    public const int AngleLength = 204;
    public const int TrackingTime = 205;
    public const int TrackingRadius = 206;
    public const int TrackingWeightLow = 207;
    public const int TrackingWeightHigh = 208;
    public const int FireRadius = 209;
    public const int RepairRate = 210;
    public const int HitpointsRequiredToOperate = 211;
    public const int RemoveGlobalTimer = 212;
    public const int RandomizeAim = 213;
    public const int ObeyLos = 214;
    public const int ComputerEnergyMax = 215;
    public const int ComputerEnergyRate = 216;
    public const int Destroyable = 217;
    public const int DensityRadius = 218;
    public const int DensityMaxType = 219;
    public const int DensityMaxActive = 220;
    public const int DensityMaxInactive = 221;
    public const int MaxTypeByPlayerRegardlessOfTeam = 222;
    public const int FrequencyMaxType = 223;
    public const int FrequencyMaxActive = 224;
    public const int FrequencyMaxInactive = 225;
    public const int FrequencyDensityMaxType = 226;
    public const int FrequencyDensityMaxActive = 227;
    public const int FrequencyDensityMaxInactive = 228;
    public const int ChainedTurretRequiredForBuilding = 229;
    public const int ChainedTurretRequiredForOperation = 230;
    public const int ChainedTurretId = 231;
    public const int ChainedTurretRadius = 232;
    public const int NumChainedTurretsRequired = 233;
    /// <summary>String slot (non-empty in 7 of the 17 pinned rows, empty in the others, including 408/412/413).</summary>
    public const int LogicTakeOwnership = 234;
    /// <summary>String slot (non-empty in 6 of the 17 pinned rows, empty in the other 11, including all three NWS-295 structures).</summary>
    public const int LogicStealOwnership = 235;
    public const int SpriteTurretZAdjust = 236;
    public const int ComputerScalarFirst = 202;
    public const int ComputerScalarCount = 35;

    // ---- computer sprites: two 8-field sprite blobs ----
    public const int SpriteBaseFirst = 237;
    public const int SpriteTurretFirst = 245;
    public const int SpriteBlobLength = 8;
    /// <summary>Offsets inside an 8-field sprite blob (BlobName and BlobId are string slots; "None" appears in BlobName).</summary>
    public const int BlobName = 0;
    public const int BlobId = 1;
    public const int BlobLightPermutation = 2;
    public const int BlobPaletteOffset = 3;
    public const int BlobHue = 4;
    public const int BlobSaturation = 5;
    public const int BlobValue = 6;
    public const int BlobAnimationTime = 7;

    // ---- 16 product blocks of 14 fields ----
    public const int ProductStart = 253;
    public const int ProductStride = 14;
    public const int ProductCount = 16;
    /// <summary>Offsets inside a product block. Title, SkillLogic and TeamBuildingLogic are string slots.</summary>
    public const int ProductTitle = 0;
    public const int ProductToCreate = 1;
    public const int ProductQuantity = 2;
    public const int ProductTeam = 3;
    public const int ProductTime = 4;
    public const int ProductPlayerItemNeededId = 5;
    public const int ProductPlayerItemNeededQuantity = 6;
    public const int ProductCost = 7;
    public const int ProductSkillLogic = 8;
    public const int ProductTeamItemNeededId = 9;
    public const int ProductTeamItemNeededQuantity = 10;
    public const int ProductTeamBuildingLogic = 11;
    public const int ProductTeamQueueRequest = 12;
    public const int ProductConfirm = 13;

    public static int Armor(int slot, int part)
    {
        Check(slot, ArmorCount, nameof(slot));
        Check(part, ArmorStride, nameof(part));
        return ArmorFirst + slot * ArmorStride + part;
    }

    public static int InventoryItem(int k) { Check(k, InventoryItemsCount, nameof(k)); return InventoryItemsFirst + k; }
    public static int ChildVehicle(int k) { Check(k, ChildVehiclesCount, nameof(k)); return ChildVehiclesFirst + k; }
    public static int TerrainModifier(int k) { Check(k, TerrainModifiersCount, nameof(k)); return TerrainModifiersFirst + k; }
    public static int HoldItemLimit(int k) { Check(k, HoldItemCount, nameof(k)); return HoldItemFirst + k * HoldItemStride; }
    public static int HoldItemExtendedLimit(int k) => HoldItemLimit(k) + 1;
    public static int AnimatedSprite(int k) { Check(k, AnimatedSpriteCount, nameof(k)); return AnimatedSpriteFirst + k * AnimatedSpriteStride; }
    public static int Product(int k, int offset)
    {
        Check(k, ProductCount, nameof(k));
        Check(offset, ProductStride, nameof(offset));
        return ProductStart + k * ProductStride + offset;
    }

    private static void Check(int value, int count, string name)
    {
        if ((uint)value >= (uint)count) throw new ArgumentOutOfRangeException(name, value, $"expected 0..{count - 1}");
    }

    /// <summary>
    /// True for the slots the upstream parser reads as text: Name, SkillLogic, Description, EventString1..3, the two ownership logics, every
    /// sprite BlobName/BlobId, and each product's Title, SkillLogic and TeamBuildingLogic. Every other slot is an integer in the 17 pinned rows.
    /// </summary>
    public static bool IsStringSlot(int index)
    {
        if (index is NameField or SkillLogic or Description or EventString1 or EventString2 or EventString3 or LogicTakeOwnership or LogicStealOwnership) return true;
        for (var k = 0; k < AnimatedSpriteCount; k++)
        {
            var b = AnimatedSprite(k) + 4;
            if (index == b + BlobName || index == b + BlobId) return true;
        }
        if (index == SpriteBaseFirst + BlobName || index == SpriteBaseFirst + BlobId || index == SpriteTurretFirst + BlobName || index == SpriteTurretFirst + BlobId) return true;
        if (index >= ProductStart && index < ProductStart + ProductCount * ProductStride)
        {
            var o = (index - ProductStart) % ProductStride;
            return o is ProductTitle or ProductSkillLogic or ProductTeamBuildingLogic;
        }
        return false;
    }
}

/// <summary>One 14-field product block of a type-5 row, every field kept raw. Layout names are upstream-parser names (InfantryOnline bb97579e), not executable evidence.</summary>
public sealed record OriginalComputerProduct(int Index, int FirstFieldIndex, IReadOnlyList<RawField> Fields)
{
    public RawField Title => Fields[VehComputerFields.ProductTitle];
    public RawField SkillLogic => Fields[VehComputerFields.ProductSkillLogic];
    public RawField TeamBuildingLogic => Fields[VehComputerFields.ProductTeamBuildingLogic];

    /// <summary>True when the title is empty and every other field of the block is "" or "0" (the all-default block; nothing is dropped, empty blocks stay in <see cref="OriginalComputerRecord.Products"/>).</summary>
    public bool IsEmpty => Title.Value.Length == 0 && Fields.Skip(1).All(f => f.Value.Length == 0 || f.Value == "0");
}

/// <summary>
/// A VEH row proven to be a v53 type-5 record with exactly 477 fields, exposed through named raw accessors (RT-F058 data clause). Field names are
/// upstream-parser names (InfantryOnline bb97579e), not original-executable evidence. Values are returned exactly as read: no scaling, no
/// sentinel replacement, no semantics. Construction throws <see cref="OriginalDataException"/> for any other record.
/// </summary>
public sealed class OriginalComputerRecord
{
    public OriginalComputerRecord(OriginalVehicleRecord vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        var r = vehicle.Record;
        if (vehicle.RecordType != VehComputerFields.RecordType)
            throw new OriginalDataException($"{r.SourceFile} row {r.RowNumber}: record type {vehicle.RecordType}, expected {VehComputerFields.RecordType} (Computer).");
        if (vehicle.VersionToken != VehComputerFields.Version)
            throw new OriginalDataException($"{r.SourceFile} row {r.RowNumber}: version '{vehicle.VersionToken}', expected {VehComputerFields.Version}.");
        if (vehicle.FieldCount != VehComputerFields.FieldCount)
            throw new OriginalDataException($"{r.SourceFile} row {r.RowNumber}: {vehicle.FieldCount} fields, expected {VehComputerFields.FieldCount}.");
        Vehicle = vehicle;
        Products = Enumerable.Range(0, VehComputerFields.ProductCount)
            .Select(k =>
            {
                var first = VehComputerFields.Product(k, 0);
                return new OriginalComputerProduct(k, first, Enumerable.Range(first, VehComputerFields.ProductStride).Select(r.Field).ToList());
            })
            .ToList();
    }

    public OriginalVehicleRecord Vehicle { get; }
    public RawRecord Record => Vehicle.Record;
    public int Id => Vehicle.Id;
    public string Name => Vehicle.Name;
    public int RowNumber => Vehicle.RowNumber;

    public RawField Field(int index) => Vehicle.Field(index);
    public OriginalInt Int(int index) => Vehicle.Int(index);

    /// <summary>The 16 product blocks f[253 + 14k] in file order (empty ones are present and flagged by <see cref="OriginalComputerProduct.IsEmpty"/>).</summary>
    public IReadOnlyList<OriginalComputerProduct> Products { get; }

    // shared header / base
    public RawField SkillLogic => Field(VehComputerFields.SkillLogic);
    public RawField Description => Field(VehComputerFields.Description);
    public OriginalInt Hitpoints => Int(VehComputerFields.Hitpoints);
    public OriginalInt Weight => Int(VehComputerFields.Weight);
    public OriginalInt ClassId => Int(VehComputerFields.ClassId);
    public OriginalInt PhysicalRadius => Int(VehComputerFields.PhysicalRadius);
    public OriginalInt TriggerRadius => Int(VehComputerFields.TriggerRadius);
    public OriginalInt PickupItemId => Int(VehComputerFields.PickupItemId);
    public OriginalInt DisplayHealth => Int(VehComputerFields.DisplayHealth);
    public OriginalInt ExplodeItemId => Int(VehComputerFields.ExplodeItemId);
    public OriginalInt RadarPrize => Int(VehComputerFields.RadarPrize);
    public OriginalInt VehicleGetInMode => Int(VehComputerFields.VehicleGetInMode);
    public OriginalInt VisualHeight => Int(VehComputerFields.VisualHeight);
    public OriginalInt DropItemId => Int(VehComputerFields.DropItemId);
    public OriginalInt DropItemQuantity => Int(VehComputerFields.DropItemQuantity);
    public OriginalInt RelativeId => Int(VehComputerFields.RelativeId);
    public RawField EventString1 => Field(VehComputerFields.EventString1);
    public RawField EventString2 => Field(VehComputerFields.EventString2);
    public RawField EventString3 => Field(VehComputerFields.EventString3);
    public IReadOnlyList<OriginalInt> InventoryItems => Record.Ints(VehComputerFields.InventoryItemsFirst, VehComputerFields.InventoryItemsCount);
    public IReadOnlyList<OriginalInt> ChildVehicles => Record.Ints(VehComputerFields.ChildVehiclesFirst, VehComputerFields.ChildVehiclesCount);

    // computer scalars
    public OriginalInt RotateSpeed => Int(VehComputerFields.RotateSpeed);
    public OriginalInt AngleStart => Int(VehComputerFields.AngleStart);
    public OriginalInt AngleLength => Int(VehComputerFields.AngleLength);
    public OriginalInt TrackingTime => Int(VehComputerFields.TrackingTime);
    public OriginalInt TrackingRadius => Int(VehComputerFields.TrackingRadius);
    public OriginalInt TrackingWeightLow => Int(VehComputerFields.TrackingWeightLow);
    public OriginalInt TrackingWeightHigh => Int(VehComputerFields.TrackingWeightHigh);
    public OriginalInt FireRadius => Int(VehComputerFields.FireRadius);
    public OriginalInt RepairRate => Int(VehComputerFields.RepairRate);
    public OriginalInt HitpointsRequiredToOperate => Int(VehComputerFields.HitpointsRequiredToOperate);
    public OriginalInt RemoveGlobalTimer => Int(VehComputerFields.RemoveGlobalTimer);
    public OriginalInt RandomizeAim => Int(VehComputerFields.RandomizeAim);
    public OriginalInt ObeyLos => Int(VehComputerFields.ObeyLos);
    public OriginalInt ComputerEnergyMax => Int(VehComputerFields.ComputerEnergyMax);
    public OriginalInt ComputerEnergyRate => Int(VehComputerFields.ComputerEnergyRate);
    public OriginalInt Destroyable => Int(VehComputerFields.Destroyable);
    public OriginalInt DensityRadius => Int(VehComputerFields.DensityRadius);
    public OriginalInt DensityMaxType => Int(VehComputerFields.DensityMaxType);
    public OriginalInt DensityMaxActive => Int(VehComputerFields.DensityMaxActive);
    public OriginalInt DensityMaxInactive => Int(VehComputerFields.DensityMaxInactive);
    public OriginalInt MaxTypeByPlayerRegardlessOfTeam => Int(VehComputerFields.MaxTypeByPlayerRegardlessOfTeam);
    public OriginalInt FrequencyMaxType => Int(VehComputerFields.FrequencyMaxType);
    public OriginalInt FrequencyMaxActive => Int(VehComputerFields.FrequencyMaxActive);
    public OriginalInt FrequencyMaxInactive => Int(VehComputerFields.FrequencyMaxInactive);
    public OriginalInt FrequencyDensityMaxType => Int(VehComputerFields.FrequencyDensityMaxType);
    public OriginalInt FrequencyDensityMaxActive => Int(VehComputerFields.FrequencyDensityMaxActive);
    public OriginalInt FrequencyDensityMaxInactive => Int(VehComputerFields.FrequencyDensityMaxInactive);
    public OriginalInt ChainedTurretRequiredForBuilding => Int(VehComputerFields.ChainedTurretRequiredForBuilding);
    public OriginalInt ChainedTurretRequiredForOperation => Int(VehComputerFields.ChainedTurretRequiredForOperation);
    public OriginalInt ChainedTurretId => Int(VehComputerFields.ChainedTurretId);
    public OriginalInt ChainedTurretRadius => Int(VehComputerFields.ChainedTurretRadius);
    public OriginalInt NumChainedTurretsRequired => Int(VehComputerFields.NumChainedTurretsRequired);
    public RawField LogicTakeOwnership => Field(VehComputerFields.LogicTakeOwnership);
    public RawField LogicStealOwnership => Field(VehComputerFields.LogicStealOwnership);
    public OriginalInt SpriteTurretZAdjust => Int(VehComputerFields.SpriteTurretZAdjust);

    // sprite blob names (strings; "None" is a literal text value in the file)
    public RawField SpriteBaseBlobName => Field(VehComputerFields.SpriteBaseFirst + VehComputerFields.BlobName);
    public RawField SpriteBaseBlobId => Field(VehComputerFields.SpriteBaseFirst + VehComputerFields.BlobId);
    public RawField SpriteTurretBlobName => Field(VehComputerFields.SpriteTurretFirst + VehComputerFields.BlobName);
    public RawField SpriteTurretBlobId => Field(VehComputerFields.SpriteTurretFirst + VehComputerFields.BlobId);

    /// <summary>All 17 type-5 rows of the catalog as validated computer records (throws if any row does not satisfy the guard).</summary>
    public static IReadOnlyList<OriginalComputerRecord> All(OriginalVehicleCatalog catalog) =>
        catalog.Computers.Select(v => new OriginalComputerRecord(v)).ToList();
}
