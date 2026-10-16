namespace RogueTrader.OriginalData;

/// <summary>
/// Raw view of one HyperCore / warp item row of ITM/Rogue_mp.itm for RT-F054 (data only). The rows are the single
/// type-4 HyperCore (id 2011, 31 fields) and every type-17 row of the file (8 rows, 113 fields): the five
/// "HyperDrive Systems" abilities (ids 25, 29, 30, 31, 33) plus the three base-warp rows of other categories
/// (Portal 26 "BaseLimitedUse", Warp-Start 27 and Warp-Team 28 "BaseWarp"). Nothing is scaled or defaulted; negative
/// values (-1, -2 ...) keep their raw value and are flagged by <see cref="OriginalInt.IsNegativeSentinel"/> only as
/// unvalidated sentinel candidates.
/// </summary>
/// <remarks>
/// Established from the pinned file: the row/id/name/category/type facts, the textual logic (f[5]) and description
/// (f[6]), buy f[8] and sell f[15] (catalog names), and which fields differ between the type-17 rows (an independent
/// Python audit lists them: <see cref="DistinguishingIntFields"/>, <see cref="DistinguishingTextFields"/>). Inferred only:
/// f[34] = 2011 in exactly the five HyperDrive Systems rows equals the HyperCore id and NWS row 37 says all HyperDrive
/// systems require a HyperCore (see <see cref="HyperCoreReferenceCandidate"/>). Everything else (energy cost, targeting,
/// the -2 block f[36..51], f[93] / f[94] / f[97] / f[98] / f[111], art and sound names) is undecoded: the indexes are
/// exposed so the native evaluator can be matched against them. No jump behaviour is derived here.
/// </remarks>
public sealed class OriginalWarpItem
{
    public const int HyperCoreId = 2011;
    public const int HyperCoreRecordType = 4;
    public const int HyperCoreFieldCount = 31;
    public const int AbilityRecordType = 17;
    public const int AbilityFieldCount = 113;
    /// <summary>f[34]: raw int that equals the HyperCore id 2011 in the five HyperDrive Systems rows (inferred item reference).</summary>
    public const int HyperCoreReferenceField = 34;

    /// <summary>
    /// Integer fields (besides the id f[2]) whose values are not identical across the eight type-17 rows of the pinned
    /// file, in index order (independent audit: scripts/original-assets/audit_rt_f054.py). Field 8 is the catalog buy,
    /// field 15 the catalog sell; f[36..51] is a 16-value block.
    /// </summary>
    public static readonly IReadOnlyList<int> DistinguishingIntFields =
        [8, 11, 13, 14, 15, 20, 27, 30, 31, 32, 34, 35,
         36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51,
         57, 60, 61, 93, 94, 97, 98, 111];

    /// <summary>Text fields that differ between the type-17 rows: f[5] logic text, then art / effect / sound names.</summary>
    public static readonly IReadOnlyList<int> DistinguishingTextFields = [5, 23, 24, 101, 102, 109, 110];

    internal OriginalWarpItem(OriginalItem item) => Item = item;

    public OriginalItem Item { get; }
    public bool IsHyperCore => Item.Id == HyperCoreId;
    public bool IsAbility => Item.RecordType == AbilityRecordType;
    /// <summary>True for the five "HyperDrive Systems" abilities (RT-F048's warp rows); false for Portal / Warp-Start / Warp-Team (categories BaseLimitedUse / BaseWarp).</summary>
    public bool IsHyperDriveSystem => IsAbility && Item.Category == "HyperDrive Systems";
    /// <summary>Raw f[5] skill-logic text ("" when empty); same field OriginalUpgrade exposes as Logic.</summary>
    public string Logic => Item.Record.Value(5);
    public string Description => Item.Record.Value(6);

    /// <summary>
    /// Raw f[34] for type-17 rows (null for the 31-field HyperCore). Value 2011 equals the HyperCore id in the five
    /// HyperDrive Systems rows and 0 in the three base-warp rows. INFERRED reference: the hull Excelsior (VEH) and an RPG
    /// row also use 2011, so it is not proven to be an item-id lookup; f[35] = 1 beside it is unexplained.
    /// </summary>
    public OriginalInt? HyperCoreReferenceCandidate =>
        IsAbility ? Item.Record.Int(HyperCoreReferenceField) : null;

    /// <summary>Raw ints at <see cref="DistinguishingIntFields"/> that exist in this record (all 36 for type 17; the first indexes below 31 for HyperCore).</summary>
    public IReadOnlyList<OriginalInt> DistinguishingInts =>
        DistinguishingIntFields.Where(i => i < Item.Record.FieldCount).Select(Item.Record.Int).ToList();

    public OriginalInt Int(int index) => Item.Record.Int(index);
    public string Text(int index) => Item.Record.Value(index);

    /// <summary>f[36..51] as raw ints (16 values) for type-17 rows: -2 in seven rows, a different mix in Portal; meaning undecoded.</summary>
    public IReadOnlyList<OriginalInt> SixteenValueBlock => IsAbility ? Item.Record.Ints(36, 16) : [];
}

/// <summary>
/// The RT-F054 item set: ITM ids 25 (row 110), 2011 (111), 26-28 (112-114), 29-31 (116-118), 33 (119). The catalog row
/// numbers 110, 111, 116-119 are correct; rows 112-114 are the Portal / Warp-Start / Warp-Team warp rows of other
/// categories that the catalog does not list and RT-F048's category filter excludes, and row 115 is an unrelated
/// "Skithzar ClusterBurst-Child" (id 3009), not a warp row.
/// </summary>
public static class OriginalWarpItems
{
    /// <summary>Item ids in file order.</summary>
    public static readonly IReadOnlyList<int> ItemIds = [25, 2011, 26, 27, 28, 29, 30, 31, 33];
    /// <summary>The rows the catalog names (Hyper Jump, HyperCore, Beacon, Controlled, Recall, Gate).</summary>
    public static readonly IReadOnlyList<int> CatalogRows = [110, 111, 116, 117, 118, 119];
    /// <summary>Warp rows between them that the catalog omits (ids 26, 27, 28).</summary>
    public static readonly IReadOnlyList<int> UncatalogedWarpRows = [112, 113, 114];

    public static IReadOnlyList<OriginalWarpItem> Resolve(OriginalItemCatalog catalog) =>
        catalog.Items.Where(i => ItemIds.Contains(i.Id)).Select(i => new OriginalWarpItem(i)).ToList();

    /// <summary>Every type-17 row of the catalog, in file order (all of them belong to this set in the pinned file).</summary>
    public static IReadOnlyList<OriginalWarpItem> AllAbilityRows(OriginalItemCatalog catalog) =>
        catalog.Items.Where(i => i.RecordType == OriginalWarpItem.AbilityRecordType
            && i.Record.FieldCount == OriginalWarpItem.AbilityFieldCount).Select(i => new OriginalWarpItem(i)).ToList();
}
