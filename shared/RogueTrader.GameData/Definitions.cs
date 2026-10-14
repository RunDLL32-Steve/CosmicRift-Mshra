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
