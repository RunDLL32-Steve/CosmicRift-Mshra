namespace RogueTrader.GameData;

// Numeric defaults are provisional Rogue Trader 2.0 values, not historical statistics.
public sealed record ShipDefinition
{
    public string Id { get; init; } = "starter_interceptor";
    public string Name { get; init; } = "Interceptor";
    public string EvidenceClassification { get; init; } = "NEW";
    public float Radius { get; init; } = 14;
    public ShipPhysics Physics { get; init; } = new();
    public ShipDefense Defense { get; init; } = new();
    public ShipReactor Reactor { get; init; } = new();
    public ShipCargo Cargo { get; init; } = new();
}

/// <summary>
/// The most cargo weight the hull can hold, in raw ITM weight units (the ITM item weights use the same scale). The default is the
/// original Asp's VEH f[48] StopWeight (2,500,000), the hull the original gives a new pilot (RPG row 1); reading StopWeight as a
/// hard cap is NEW (RT-F030 keeps the NormalWeight encumbrance formula open).
/// </summary>
public sealed record ShipCargo
{
    public long Capacity { get; init; } = 2_500_000;
    public string? Source { get; init; }
}

public sealed record ShipPhysics
{
    public float Mass { get; init; } = 100;
    public float ForwardThrust { get; init; } = 12000;
    public float ReverseThrust { get; init; } = 12000;
    public float RotationSpeed { get; init; } = 3.3f;
    public float MaximumSpeed { get; init; } = 360;
    public float AsteroidRestitution { get; init; } = 0.25f;
}

public sealed record ShipDefense
{
    public float MaximumHull { get; init; } = 100;
    public float MaximumShield { get; init; } = 50;
}

public sealed record ShipReactor
{
    public float MaximumEnergy { get; init; } = 100;
    public float EnergyRegenerationPerSecond { get; init; } = 20;
}

public sealed record WeaponDefinition
{
    public string Id { get; init; } = "plasma_cannon";
    public string Name { get; init; } = "Plasma cannon";
    public string EvidenceClassification { get; init; } = "NEW";
    public float MuzzleVelocity { get; init; } = 700;
    public float ProjectileRadius { get; init; } = 3;
    public float Damage { get; init; } = 28;
    public float CooldownSeconds { get; init; } = 0.2f;
    public float EnergyCost { get; init; } = 6;
    public float LifetimeSeconds { get; init; } = 2.5f;
    public bool InheritsShipVelocity { get; init; } = true;
}

public sealed record SectorDefinition
{
    public string Id { get; init; } = "first_contact";
    public string Name { get; init; } = "First Contact";
    public string EvidenceClassification { get; init; } = "NEW";
    public SpawnDefinition PlayerSpawn { get; init; } = new();
    public IReadOnlyList<TargetDefinition> Targets { get; init; } = [];
    public IReadOnlyList<AsteroidDefinition> Asteroids { get; init; } = [];
    /// <summary>
    /// Seconds after the last target of the group is destroyed before the whole group respawns at its starting positions;
    /// 0 (the default) means targets never respawn. NEW/provisional practice-range rule.
    /// </summary>
    public float TargetRespawnSeconds { get; init; }
    /// <summary>
    /// Most minerals the sector holds in space at once, in full holds of the ship (Steve, November 2, 2026: enough for about
    /// five ships to fill their cargo). Every spawn's pile limit is scaled by the same factor so the field's total weight at its
    /// limits is about this many holds; a limit never rises above the spawn's own and never falls below one pile. 0 (the
    /// default) leaves the spawns' own limits. NEW/provisional.
    /// </summary>
    public float MineralHoldsInSpace { get; init; }
}

public sealed record SpawnDefinition
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Heading { get; init; } = -MathF.PI / 2;
    public float VelocityX { get; init; }
    public float VelocityY { get; init; }
}

public sealed record TargetDefinition
{
    public string Id { get; init; } = "training_target";
    public float X { get; init; }
    public float Y { get; init; } = -280;
    public float Heading { get; init; } = MathF.PI / 2;
    public float Radius { get; init; } = 20;
    public float Hull { get; init; } = 100;
    public float Shield { get; init; } = 40;
}

public sealed record AsteroidDefinition
{
    public string Id { get; init; } = "asteroid";
    public float X { get; init; }
    public float Y { get; init; }
    public float Radius { get; init; } = 40;
}
