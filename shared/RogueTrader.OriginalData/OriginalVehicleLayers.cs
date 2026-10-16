namespace RogueTrader.OriginalData;

/// <summary>
/// One <c>&lt;archive&gt;,&lt;member&gt;</c> reference with its trailing integers, read verbatim from a VEH Car record.
/// Graphic layers have six integers (8-field tuple), sound references two (4-field tuple). <see cref="Archive"/> and
/// <see cref="Member"/> are the file's text; "None" / "None" is the empty slot marker and is kept as-is (see
/// <see cref="IsNone"/>). Whether <see cref="Archive"/> names BLO/&lt;archive&gt;.blo and whether the member exists in it
/// is NOT verified here (BLO content is not available to this reader).
/// </summary>
public sealed class OriginalVehicleAssetRef
{
    internal OriginalVehicleAssetRef(int slot, string slotName, int firstFieldIndex, string archive, string member, IReadOnlyList<OriginalInt> ints)
    {
        Slot = slot;
        SlotName = slotName;
        FirstFieldIndex = firstFieldIndex;
        Archive = archive;
        Member = member;
        Ints = ints;
    }

    /// <summary>0-based slot number within its group (graphic layers 0..6, sound references 0..3).</summary>
    public int Slot { get; }

    /// <summary>Evidence-limited slot name; see <see cref="OriginalVehicleLayers"/> for which names are documented and which are position-only.</summary>
    public string SlotName { get; }

    /// <summary>Index of the archive field; the member is at +1 and the integers follow.</summary>
    public int FirstFieldIndex { get; }

    /// <summary>Index of the last field of the tuple.</summary>
    public int LastFieldIndex => FirstFieldIndex + 1 + Ints.Count;

    public string Archive { get; }
    public string Member { get; }

    /// <summary>The trailing integers in source order, unscaled (6 for a graphic layer, 2 for a sound reference).</summary>
    public IReadOnlyList<OriginalInt> Ints { get; }

    /// <summary>True for the empty-slot marker (archive and member both "None"); the integers are then all 0 in the pinned file.</summary>
    public bool IsNone => Archive == "None" && Member == "None";

    /// <summary>The expected BLO file name (<c>BLO/&lt;archive&gt;.blo</c>); null for an empty slot.</summary>
    public string? BloRelativePath => IsNone ? null : $"BLO/{Archive}.blo";
}

/// <summary>
/// Graphic layer tuples and sound reference tuples of one VEH/Rogue.veh v53 Car record (RT-F024, raw data only).
/// <para>
/// Layout (identical in all twelve hull rows, 454 fields): f[380]=8 and f[381]=30 precede the first layer (undecoded,
/// kept in the raw record). Seven graphic layers of 8 fields start at f[382] (<c>archive, member, i0..i5</c>); four
/// sound references of 4 fields start at f[438] (<c>archive, member, i0, i1</c>); the record ends at f[453].
/// </para>
/// <para>
/// Evidence for names and integers. FIELD_INTERPRETATION.md "Graphics and pose mapping": the active SpriteBlob.ParseV3
/// reads BlobName, BlobId, LightPermutation, PaletteOffset, HSV and AnimationTime; the Asp roll tuple
/// <c>TerranShipsSmall,Asp,0,0,0,0,5,0</c> means HSV Value 5 and the thrust tuple has HSV Value -10 with animation/time
/// 51. So the six layer integers are, in order, LightPermutation, PaletteOffset, three HSV components and AnimationTime;
/// only the fifth (Value) and sixth (time) are named by that document, the first four and the H/S order are inferred
/// from the parser field order. Their units, scaling and effect are NOT decoded. The sound tuple integers have no
/// parser evidence and stay raw. Slot names: slot 0 "Roll" (catalog RT-F023: the roll sequence), slot 2 "Death explosion"
/// and slot 3 "Thrust" (catalog RT-F024 and FIELD_INTERPRETATION "broken/thrust sequences"); slots 1, 4, 5, 6 and every
/// sound slot are position-named only. Sound slot members (for example UtilitySounds/Thrust, /Rev, WeaponSounds/explode2)
/// are the file's text; which game event plays which slot is not established.
/// </para>
/// </summary>
public sealed class OriginalVehicleLayers
{
    public const int FirstLayerField = 382;
    public const int LayerFieldCount = 8;
    public const int LayerCount = 7;
    public const int LayerIntCount = 6;
    public const int FirstSoundField = FirstLayerField + LayerFieldCount * LayerCount;
    public const int SoundFieldCount = 4;
    public const int SoundCount = 4;
    public const int SoundIntCount = 2;
    /// <summary>Required exact field count of the record (the last sound tuple ends at f[453]).</summary>
    public const int RecordFieldCount = FirstSoundField + SoundFieldCount * SoundCount;

    public const int RollSlot = 0;
    public const int DeathExplosionSlot = 2;
    public const int ThrustSlot = 3;

    private static readonly string[] LayerNames =
        ["Roll", "Layer slot 1", "Death explosion", "Thrust", "Layer slot 4", "Layer slot 5", "Layer slot 6"];

    private OriginalVehicleLayers(IReadOnlyList<OriginalVehicleAssetRef> layers, IReadOnlyList<OriginalVehicleAssetRef> sounds)
    {
        Layers = layers;
        Sounds = sounds;
    }

    /// <summary>The seven graphic layer tuples f[382..437] in source order (empty slots included).</summary>
    public IReadOnlyList<OriginalVehicleAssetRef> Layers { get; }

    /// <summary>The four sound reference tuples f[438..453] in source order (empty slots included).</summary>
    public IReadOnlyList<OriginalVehicleAssetRef> Sounds { get; }

    public OriginalVehicleAssetRef Roll => Layers[RollSlot];
    public OriginalVehicleAssetRef DeathExplosion => Layers[DeathExplosionSlot];
    public OriginalVehicleAssetRef Thrust => Layers[ThrustSlot];

    /// <summary>Non-empty layers and sounds, layers first.</summary>
    public IEnumerable<OriginalVehicleAssetRef> NonEmpty => Layers.Concat(Sounds).Where(r => !r.IsNone);

    /// <summary>
    /// Reads the tuples of one VEH Car record. The record must have exactly <see cref="RecordFieldCount"/> fields and
    /// every integer position must hold an integer; otherwise <see cref="OriginalDataFormatException"/> is thrown.
    /// </summary>
    public static OriginalVehicleLayers Of(RawRecord veh)
    {
        if (veh.FieldCount != RecordFieldCount)
            throw new OriginalDataFormatException(veh.SourceFile, veh.RowNumber, null, $"expected {RecordFieldCount} fields for the layer/sound layout, found {veh.FieldCount}.");
        var layers = new OriginalVehicleAssetRef[LayerCount];
        for (var s = 0; s < LayerCount; s++)
            layers[s] = Read(veh, s, LayerNames[s], FirstLayerField + s * LayerFieldCount, LayerIntCount);
        var sounds = new OriginalVehicleAssetRef[SoundCount];
        for (var s = 0; s < SoundCount; s++)
            sounds[s] = Read(veh, s, $"Sound slot {s}", FirstSoundField + s * SoundFieldCount, SoundIntCount);
        return new OriginalVehicleLayers(layers, sounds);
    }

    public static OriginalVehicleLayers Of(OriginalHull hull) => Of(hull.VehRecord);

    private static OriginalVehicleAssetRef Read(RawRecord veh, int slot, string name, int first, int intCount) =>
        new(slot, name, first, veh.Value(first), veh.Value(first + 1), veh.Ints(first + 2, intCount));
}

/// <summary>
/// Filename-only check of <c>BLO/&lt;archive&gt;.blo</c> in an assets root (case-insensitive). This does NOT open the file,
/// so the member inside the archive is not verified (BLO content is Git-LFS only in the pinned clone).
/// </summary>
public static class OriginalBloFiles
{
    /// <summary>True when a file named <c>&lt;archive&gt;.blo</c> exists directly under <c>BLO/</c> of <paramref name="assetsRoot"/>.</summary>
    public static bool ArchiveFileExists(string assetsRoot, string archive)
    {
        var dir = Path.Combine(assetsRoot, "BLO");
        if (!Directory.Exists(dir)) return false;
        var wanted = archive + ".blo";
        return Directory.EnumerateFiles(dir).Any(p => string.Equals(Path.GetFileName(p), wanted, StringComparison.OrdinalIgnoreCase));
    }
}
