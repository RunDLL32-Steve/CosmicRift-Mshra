using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueTrader.GameData;

/// <summary>NEW/provisional finite deposit association; no original spawn or regeneration semantics.</summary>
public sealed record MiningDepositDefinition(string AsteroidId, string ResourceItemId, int Quantity);

/// <summary>NEW/provisional installed capability. Range is the gap between ship and asteroid surfaces in world units.</summary>
public sealed record MiningEquipmentDefinition(string Id, IReadOnlyList<string> CompatibleItemIds,
    float Range, int CycleTicks, int UnitsPerCycle, float DropRadius);

/// <summary>Optional external mining rules; omission from the world leaves mining disabled.</summary>
public sealed class MiningCatalog
{
    public IReadOnlyList<MiningDepositDefinition> Deposits { get; }
    public MiningEquipmentDefinition? Equipment { get; }

    public MiningCatalog(IEnumerable<MiningDepositDefinition> deposits, MiningEquipmentDefinition? equipment = null)
    {
        ArgumentNullException.ThrowIfNull(deposits);
        Deposits = Array.AsReadOnly(deposits.ToArray());
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var deposit in Deposits)
        {
            if (deposit is null || string.IsNullOrWhiteSpace(deposit.AsteroidId)
                || string.IsNullOrWhiteSpace(deposit.ResourceItemId) || deposit.Quantity <= 0 || !ids.Add(deposit.AsteroidId))
                throw new ArgumentException("Deposits require unique asteroid ids, item ids and positive quantities.", nameof(deposits));
        }
        if (equipment is null)
            return;
        if (string.IsNullOrWhiteSpace(equipment.Id) || equipment.CompatibleItemIds is null
            || equipment.CompatibleItemIds.Count == 0 || equipment.CompatibleItemIds.Any(string.IsNullOrWhiteSpace)
            || equipment.CompatibleItemIds.Distinct(StringComparer.Ordinal).Count() != equipment.CompatibleItemIds.Count
            || !float.IsFinite(equipment.Range) || equipment.Range <= 0 || equipment.CycleTicks <= 0
            || equipment.UnitsPerCycle <= 0 || !float.IsFinite(equipment.DropRadius) || equipment.DropRadius <= 0)
            throw new ArgumentException("Invalid mining equipment capability.", nameof(equipment));
        Equipment = equipment with { CompatibleItemIds = Array.AsReadOnly(equipment.CompatibleItemIds.ToArray()) };
    }

    /// <summary>Strict JSON with required deposits and optional equipment; all tuning is supplied explicitly.</summary>
    public static MiningCatalog FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<MiningDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new JsonException("Empty mining definition.");
        return new MiningCatalog(document.Deposits ?? throw new JsonException("Missing deposits."), document.Equipment);
    }

    private sealed class MiningDocument
    {
        public List<MiningDepositDefinition>? Deposits { get; set; }
        public MiningEquipmentDefinition? Equipment { get; set; }
    }
}

