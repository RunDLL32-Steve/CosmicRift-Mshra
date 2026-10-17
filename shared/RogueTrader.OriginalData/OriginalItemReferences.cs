using System.Globalization;

namespace RogueTrader.OriginalData;

/// <summary>
/// A pair of consecutive text fields of an ITM row read as <c>archive, member</c> with the integers that follow, as raw text
/// (RT-F076 reusable ITEM resolver). The pair shape follows the VEH graphic/sound tuples and the computer sprite blobs
/// (<see cref="OriginalVehicleAssetRef"/>, <see cref="VehComputerFields.BlobName"/>); that the ITM pairs have the same meaning is an
/// inference from the identical shape and the archive names (Ebombs, ExplosionEffects, WeaponSounds exist as BLO files). Detection
/// rule (structural, no field is named by it): a run of exactly two consecutive non-integer fields at index 7 or higher.
/// <see cref="TrailingInts"/> are the integers after the pair, up to <see cref="MaxTrailingInts"/>, fewer when the next text field
/// or the record end comes first (that makes the pinned type-6 tuples 8 fields for graphics and 4 for the last two sounds and the
/// trailing "None" tuple; the length is inferred, not documented). Nothing is read from the BLO archive (queue C1).
/// </summary>
public sealed record OriginalItemAssetPair(int FirstFieldIndex, string Archive, string Member, IReadOnlyList<OriginalInt> TrailingInts)
{
    public const int MaxTrailingInts = 6;

    /// <summary>True for the empty-slot marker (archive and member both "None").</summary>
    public bool IsNone => Archive == "None" && Member == "None";

    /// <summary>The expected <c>BLO/&lt;archive&gt;.blo</c> path; null for an empty slot. Member existence is not verified.</summary>
    public string? BloRelativePath => IsNone ? null : $"BLO/{Archive}.blo";

    public int LastFieldIndex => FirstFieldIndex + 1 + TrailingInts.Count;
}

/// <summary>A skill id found in a skill-logic string, with the hull whose RPG skill id it equals (null when no hull has that id).</summary>
public sealed record OriginalItemSkillReference(int SkillId, OriginalHull? Hull);

/// <summary>
/// ITM f[20] (raw) and the CFG [HeldCategory] row with that id. This is a CANDIDATE link: f[20] is a per-category constant that lines
/// up with the HeldCategory ids (reviewer observation recorded for RT-F033), but the mapping of ids to ITM categories is not
/// established and no restriction is enforced from it.
/// </summary>
public sealed record OriginalItemHeldCategoryCandidate(OriginalInt CategoryConstant, OriginalHeldCategoryLimit? Limit);

/// <summary>Why a reference could not be (fully) resolved from data available to the reader.</summary>
public sealed record OriginalItemUnresolved(string Kind, int? FieldIndex, string Raw, string Reason);

/// <summary>
/// Another original record that names the item. <see cref="Kind"/> is one of <see cref="OriginalItemReverseKinds"/>; <see cref="Row"/> is the
/// 1-based physical row/line of <see cref="SourceFile"/>; <see cref="Detail"/> is the raw text or a short factual note.
/// </summary>
public sealed record OriginalItemReverseReference(string Kind, string SourceFile, int Row, int? FieldIndex, int? RecordId, string RecordName, string Detail);

public static class OriginalItemReverseKinds
{
    /// <summary>A type-6 row whose f[31] equals the item id (ammo reference; the item itself when a mine or defensive weapon).</summary>
    public const string AmmoOf = "ItmAmmoItem";
    /// <summary>A type-6 row whose f[142] explodeItem equals the item id.</summary>
    public const string ExplodeChildOf = "ItmExplodeItem";
    /// <summary>A type-8 row listing the item as a sub-projectile entry.</summary>
    public const string MultiItemEntryOf = "ItmMultiItemEntry";
    /// <summary>A VEH hull whose f[68] DropItemId equals the item id.</summary>
    public const string HullDropItem = "VehDropItem";
    /// <summary>An event string (VEH f[129..131], ITM f[147], CFG) with an addinv/wipeinv argument equal to the item name (upstream-emulator reading).</summary>
    public const string EventItemName = "EventItemName";
}

/// <summary>
/// The resolved view of one ITM row for the ITEM acceptance profile (RT-F076; reused by every later single-item entry). Everything is raw and
/// reference-only: it says what the row points at and what points at the row, never what the pointed-at things do.
/// </summary>
public sealed class OriginalItemReferences
{
    internal OriginalItemReferences(OriginalItem item) => Item = item;

    public OriginalItem Item { get; }
    public string SourceFile => Item.Record.SourceFile;
    public int RowNumber => Item.RowNumber;
    /// <summary>The raw version token (v57).</summary>
    public string VersionToken => Item.Record.VersionToken!;
    public int RecordType => Item.RecordType;
    public int FieldCount => Item.Record.FieldCount;
    /// <summary>Every field of the row in source order (nothing dropped or scaled).</summary>
    public IReadOnlyList<RawField> Fields => Item.Record.Fields;

    public int Id => Item.Id;
    public string Name => Item.Name;
    public string Category => Item.Category;
    /// <summary>f[6] (position inferred from the row text; the upstream order is name, category, logic, description).</summary>
    public string Description { get; internal set; } = "";
    public OriginalInt Mass => Item.Mass;
    public OriginalInt Buy => Item.Buy;
    /// <summary>f[15], raw: a negative value (-1) is a sentinel candidate whose native meaning is unvalidated.</summary>
    public OriginalInt Sell => Item.Sell;

    /// <summary>f[5] verbatim ("" when empty).</summary>
    public string LogicRaw { get; internal set; } = "";
    /// <summary>The parsed logic; null when <see cref="LogicRaw"/> is outside the grammar (then listed in <see cref="Unresolved"/>).</summary>
    public SkillLogicNode? Logic { get; internal set; }
    public IReadOnlyList<OriginalItemSkillReference> SkillReferences { get; internal set; } = [];
    /// <summary>The <c>%n</c> indexes of the logic (their meaning is undecided; no resolution is attempted).</summary>
    public IReadOnlyList<int> PercentReferences { get; internal set; } = [];

    /// <summary>For a type-16 hull exchange only: the f[31] logic copy, f[32] target skill and the hull with that skill id.</summary>
    public string? ExchangeLogicCopy { get; internal set; }
    public OriginalInt? ExchangeTargetSkill { get; internal set; }
    public OriginalHull? ExchangeTargetHull { get; internal set; }

    public OriginalItemHeldCategoryCandidate? HeldCategory { get; internal set; }

    /// <summary>Raw f[31] (type-6/227 layout only; 0 = none, same id = self).</summary>
    public OriginalInt? AmmoItemRaw { get; internal set; }
    /// <summary>Raw f[142] (type-6/227 layout only; 0 = none).</summary>
    public OriginalInt? ExplodeItemRaw { get; internal set; }
    /// <summary>The ammo/child chain from the existing resolver; null for a non-projectile row or when a reference is unresolved.</summary>
    public OriginalWeaponChain? Chain { get; internal set; }
    /// <summary>For a type-8 row: its own sub-projectile entries (empty otherwise).</summary>
    public IReadOnlyList<OriginalMultiItemEntry> MultiItemEntries { get; internal set; } = [];

    /// <summary>f[147] of a type-6/227 row, tokenized by the RT-F067 reader; null for other layouts.</summary>
    public OriginalEventScript? EventString { get; internal set; }

    public IReadOnlyList<OriginalItemAssetPair> AssetPairs { get; internal set; } = [];
    /// <summary>Text fields (index 7 or higher) that are not part of an asset pair and not otherwise named, as (index, raw value).</summary>
    public IReadOnlyList<(int Index, string Value)> OtherTextFields { get; internal set; } = [];

    /// <summary>
    /// ITM-to-VEH id references. No ITM field is named as a VEH id in the existing field maps, so this is empty for every row; a numeric
    /// equality between some integer field and a VEH id is a coincidence unless a field map says otherwise (see the RT-F076 audit).
    /// </summary>
    public IReadOnlyList<int> VehicleReferences => [];

    public IReadOnlyList<OriginalItemReverseReference> ReverseReferences { get; internal set; } = [];
    public IReadOnlyList<OriginalItemUnresolved> Unresolved { get; internal set; } = [];

    /// <summary>Field index to the existing map that names it (ItmFields, WeaponChainFields, ItmProjectileFields, OriginalSkillLogic, event f[147], asset pairs, held-category candidate f[20], inferred f[6]).</summary>
    public IReadOnlyDictionary<int, string> NamedFields { get; internal set; } = new Dictionary<int, string>();
    /// <summary>Indexes with no name in any existing map (raw only).</summary>
    public IReadOnlyList<int> UnnamedFieldIndices { get; internal set; } = [];
}

/// <summary>
/// Reusable resolver of every reference an ITM row makes and every reference made to it (RT-F076). Built once from the four loaded
/// catalogs, it indexes the whole package for reverse references and then resolves any item by id. It reuses
/// <see cref="OriginalSkillLogic"/>, <see cref="OriginalHullCatalog"/>, <see cref="OriginalHeldCategoryLimit"/>,
/// <see cref="OriginalWeaponChain"/>, <see cref="OriginalProjectile"/>, <see cref="OriginalEventInventory"/>; it names no new field.
/// Field names that come from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence.
/// </summary>
public sealed class OriginalItemReferenceResolver
{
    public const int DescriptionField = 6;
    public const int LogicField = 5;
    public const int HeldCategoryConstantField = 20;
    public const int HullExchangeLogicCopyField = OriginalHullExchange.LogicCopyField;

    private readonly OriginalItemCatalog _items;
    private readonly OriginalHullCatalog _hulls;
    private readonly IReadOnlyList<OriginalHeldCategoryLimit> _held;
    private readonly OriginalEventInventory _events;

    private OriginalItemReferenceResolver(OriginalItemCatalog items, OriginalHullCatalog hulls, OriginalVehicleCatalog vehicles, OriginalCfg cfg)
    {
        _items = items;
        _hulls = hulls;
        Vehicles = vehicles;
        _held = OriginalHeldCategoryLimit.All(cfg);
        UtilitySlots = OriginalUtilitySlots.From(cfg);
        _events = OriginalEventInventory.From(vehicles, cfg, items);
    }

    public OriginalVehicleCatalog Vehicles { get; }
    public OriginalUtilitySlots UtilitySlots { get; }

    /// <summary>Loads ITM, RPG/VEH hulls, VEH and CFG through their hash-verified loaders.</summary>
    public static OriginalItemReferenceResolver Load(string assetsRoot) =>
        new(OriginalItemCatalog.Load(assetsRoot), OriginalHullCatalog.Load(assetsRoot), OriginalVehicleCatalog.Load(assetsRoot), OriginalCfg.LoadRogue(assetsRoot));

    public static OriginalItemReferenceResolver From(OriginalItemCatalog items, OriginalHullCatalog hulls, OriginalVehicleCatalog vehicles, OriginalCfg cfg) =>
        new(items, hulls, vehicles, cfg);

    public OriginalItemReferences Resolve(int itemId) => Resolve(_items.ById(itemId));

    public OriginalItemReferences Resolve(OriginalItem item)
    {
        var r = item.Record;
        var unresolved = new List<OriginalItemUnresolved>();
        var named = new Dictionary<int, string>
        {
            [ItmFields.RecordType] = "ItmFields.RecordType",
            [1] = "version token",
            [ItmFields.Id] = "ItmFields.Id",
            [ItmFields.Name] = "ItmFields.Name",
            [ItmFields.Category] = "ItmFields.Category",
            [LogicField] = "skill logic (RT-F034)",
            [DescriptionField] = "description (inferred position)",
            [ItmFields.Mass] = "ItmFields.Mass",
            [ItmFields.Buy] = "ItmFields.Buy",
            [ItmFields.Sell] = "ItmFields.Sell",
        };
        var res = new OriginalItemReferences(item) { Description = r.Value(DescriptionField), LogicRaw = r.Value(LogicField) };

        // skill logic and the hulls it names
        try
        {
            var node = OriginalSkillLogic.Parse(res.LogicRaw);
            res.Logic = node;
            var skills = new List<int>();
            var percents = new List<int>();
            Collect(node, skills, percents);
            res.SkillReferences = skills.Distinct().Select(id => new OriginalItemSkillReference(id, _hulls.Hulls.FirstOrDefault(h => h.SkillId == id))).ToList();
            res.PercentReferences = percents.Distinct().ToList();
            foreach (var s in res.SkillReferences.Where(s => s.Hull is null))
                unresolved.Add(new("SkillId", LogicField, s.SkillId.ToString(CultureInfo.InvariantCulture), "no hull in the RPG hull catalog has this skill id (RPG Rogue.rpg holds only the twelve hull rows)."));
        }
        catch (SkillLogicException e)
        {
            unresolved.Add(new("SkillLogic", LogicField, res.LogicRaw, e.Message));
        }

        // category constant -> CFG [HeldCategory] candidate
        if (r.FieldCount > HeldCategoryConstantField)
        {
            named[HeldCategoryConstantField] = "held-category constant (candidate, RT-F033)";
            var c = r.Int(HeldCategoryConstantField);
            var limit = _held.FirstOrDefault(h => h.CategoryId == c.Value);
            res.HeldCategory = new(c, limit);
            unresolved.Add(new("HeldCategoryMapping", HeldCategoryConstantField, c.Value.ToString(CultureInfo.InvariantCulture),
                limit is null
                    ? $"no CFG [HeldCategory] row has id {c.Value}; the mapping of ids to ITM categories is not established."
                    : "the link from f[20] to the CFG [HeldCategory] row of the same id is a candidate (RT-F033); the mapping of ids to ITM categories is not established."));
        }

        // type-16 hull exchange
        if (item.RecordType == OriginalHullExchange.RecordType)
        {
            named[OriginalHullExchange.LogicCopyField] = "hull-exchange logic copy (RT-F035)";
            named[OriginalHullExchange.TargetSkillField] = "hull-exchange target skill (RT-F035)";
            res.ExchangeLogicCopy = r.Value(OriginalHullExchange.LogicCopyField);
            var t = r.Int(OriginalHullExchange.TargetSkillField);
            res.ExchangeTargetSkill = t;
            res.ExchangeTargetHull = _hulls.Hulls.FirstOrDefault(h => h.SkillId == t.Value);
            if (res.ExchangeTargetHull is null)
                unresolved.Add(new("ExchangeTargetSkill", OriginalHullExchange.TargetSkillField, t.Value.ToString(CultureInfo.InvariantCulture), "no hull has this skill id."));
        }

        // projectile layout: ammo item, explode child chain, damage event string
        if (OriginalProjectile.TryOf(item, out var view))
        {
            foreach (var (idx, name) in new[]
            {
                (WeaponChainFields.AmmoItem, "WeaponChainFields.AmmoItem"),
                (ItmProjectileFields.FireDelay, "ItmProjectileFields.FireDelay"), (ItmProjectileFields.InheritedSpeed, "ItmProjectileFields.InheritedSpeed"),
                (ItmProjectileFields.MuzzleVelocity, "ItmProjectileFields.MuzzleVelocity"), (ItmProjectileFields.AliveTime, "ItmProjectileFields.AliveTime"),
                (ItmProjectileFields.MaxLivePerPlayer, "ItmProjectileFields.MaxLivePerPlayer"), (ItmProjectileFields.AntiRecharge, "ItmProjectileFields.AntiRecharge"),
                (ItmProjectileFields.AntiFire, "ItmProjectileFields.AntiFire"), (ItmProjectileFields.AntiThrust, "ItmProjectileFields.AntiThrust"),
                (ItmProjectileFields.AntiRotate, "ItmProjectileFields.AntiRotate"), (ItmProjectileFields.ExplodeItem, "ItmProjectileFields.ExplodeItem"),
                (OriginalEventInventory.ItmDamageEventStringField, "damage event string (RT-F067)"),
            })
                named[idx] = name;
            res.AmmoItemRaw = r.Int(WeaponChainFields.AmmoItem);
            res.ExplodeItemRaw = view!.ExplodeItem;
            res.EventString = OriginalEventScript.Parse(r.Value(OriginalEventInventory.ItmDamageEventStringField));
            try { res.Chain = OriginalWeaponChain.Resolve(_items, item); }
            catch (OriginalItemException e)
            {
                unresolved.Add(new("AmmoOrChild", null, $"f[{WeaponChainFields.AmmoItem}]={res.AmmoItemRaw.Value.Value}, f[{ItmProjectileFields.ExplodeItem}]={res.ExplodeItemRaw.Value.Value}", e.Message));
            }
        }
        else if (item.RecordType == OriginalWeaponChain.MultiItemType)
        {
            for (var f = OriginalWeaponChain.MultiItemFirstEntryField; f + OriginalWeaponChain.MultiItemOffsetInEntry < r.FieldCount; f += OriginalWeaponChain.MultiItemEntryStride)
                if (r.Int(f).Value != 0)
                {
                    named[f] = "multi-item entry id";
                    named[f + OriginalWeaponChain.MultiItemOffsetInEntry] = "multi-item entry raw offset";
                }
            try { res.MultiItemEntries = OriginalWeaponChain.MultiItemEntries(_items, item); }
            catch (OriginalItemException e) { unresolved.Add(new("MultiItemEntry", null, "", e.Message)); }
        }

        // asset pairs (structural) and other text fields
        var textIdx = new List<int>();
        for (var i = 7; i < r.FieldCount; i++)
            if (!int.TryParse(r.Value(i), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)) textIdx.Add(i);
        var pairs = new List<OriginalItemAssetPair>();
        var inPair = new HashSet<int>();
        for (var k = 0; k < textIdx.Count;)
        {
            var j = k;
            while (j + 1 < textIdx.Count && textIdx[j + 1] == textIdx[j] + 1) j++;
            if (j - k + 1 == 2)
            {
                var p = textIdx[k];
                var stop = textIdx.FirstOrDefault(x => x > p + 1, r.FieldCount);
                var n = Math.Min(OriginalItemAssetPair.MaxTrailingInts, stop - (p + 2));
                var pair = new OriginalItemAssetPair(p, r.Value(p), r.Value(p + 1), r.Ints(p + 2, n));
                pairs.Add(pair);
                inPair.Add(p); inPair.Add(p + 1);
                for (var q = p; q <= pair.LastFieldIndex; q++) named[q] = "asset pair (archive, member, ints)";
                if (!pair.IsNone)
                    unresolved.Add(new("BloMember", p + 1, $"{pair.Archive},{pair.Member}", $"BLO/{pair.Archive}.blo member '{pair.Member}' not verified: BLO content is not read (queue C1); the tuple meaning is inferred from the VEH/computer tuples."));
            }
            k = j + 1;
        }
        res.AssetPairs = pairs;
        res.OtherTextFields = textIdx.Where(i => !inPair.Contains(i) && !named.ContainsKey(i)).Select(i => (i, r.Value(i))).ToList();
        res.NamedFields = named;
        res.UnnamedFieldIndices = Enumerable.Range(0, r.FieldCount).Where(i => !named.ContainsKey(i)).ToList();
        res.Unresolved = unresolved;
        res.ReverseReferences = ReverseOf(item);
        return res;
    }

    private IReadOnlyList<OriginalItemReverseReference> ReverseOf(OriginalItem item)
    {
        var list = new List<OriginalItemReverseReference>();
        foreach (var other in _items.Items)
        {
            if (OriginalProjectile.TryOf(other, out var view))
            {
                if (other.Record.Int(WeaponChainFields.AmmoItem).Value == item.Id)
                    list.Add(new(OriginalItemReverseKinds.AmmoOf, other.Record.SourceFile, other.RowNumber, WeaponChainFields.AmmoItem, other.Id, other.Name, other.Id == item.Id ? "self" : $"f[{WeaponChainFields.AmmoItem}]={item.Id}"));
                if (view!.ExplodeItem.Value == item.Id)
                    list.Add(new(OriginalItemReverseKinds.ExplodeChildOf, other.Record.SourceFile, other.RowNumber, ItmProjectileFields.ExplodeItem, other.Id, other.Name, $"f[{ItmProjectileFields.ExplodeItem}]={item.Id}"));
            }
            else if (other.RecordType == OriginalWeaponChain.MultiItemType)
            {
                for (var f = OriginalWeaponChain.MultiItemFirstEntryField; f + OriginalWeaponChain.MultiItemOffsetInEntry < other.Record.FieldCount; f += OriginalWeaponChain.MultiItemEntryStride)
                    if (other.Record.Int(f).Value == item.Id)
                        list.Add(new(OriginalItemReverseKinds.MultiItemEntryOf, other.Record.SourceFile, other.RowNumber, f, other.Id, other.Name, $"f[{f}]={item.Id}"));
            }
        }
        foreach (var h in _hulls.Hulls.Where(h => h.DropItemId.Value == item.Id))
            list.Add(new(OriginalItemReverseKinds.HullDropItem, h.VehRecord.SourceFile, h.VehRowNumber, VehFields.DropItemId, h.VehicleId, h.Name, $"f[{VehFields.DropItemId}]={item.Id}"));
        foreach (var e in _events.ItemReferences.Where(x => x.ExactMatch?.Id == item.Id || x.CaseInsensitiveMatch?.Id == item.Id))
        {
            var s = e.Entry.Source;
            var how = e.ExactMatch?.Id == item.Id ? "exact name" : "case-insensitive name";
            list.Add(new(OriginalItemReverseKinds.EventItemName, s.SourceFile, s.Line, s.FieldIndex, s.RecordId, s.RecordName, $"{e.Action.Raw} ({how}; {s.Key})"));
        }
        return list;
    }

    private static void Collect(SkillLogicNode n, List<int> skills, List<int> percents)
    {
        switch (n)
        {
            case SkillLogicSkill s: skills.Add(s.SkillId); break;
            case SkillLogicPercent p: percents.Add(p.Index); break;
            case SkillLogicNot x: Collect(x.Operand, skills, percents); break;
            case SkillLogicGroup g: Collect(g.Inner, skills, percents); break;
            case SkillLogicChain c: foreach (var t in c.Terms) Collect(t, skills, percents); break;
        }
    }
}
