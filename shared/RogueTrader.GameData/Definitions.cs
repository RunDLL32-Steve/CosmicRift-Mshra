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
    /// <summary>
    /// The Shift boost (original "hyper thrust"); null when the hull has none. While it is engaged the ship thrusts and tops out
    /// with these values instead of <see cref="Physics"/> and pays energy for it.
    /// </summary>
    public ShipHyper? Hyper { get; init; }
    /// <summary>
    /// Id of the weapon the hull starts with (a file <c>data/weapons/&lt;file&gt;</c> is named by the roster loader); null means the
    /// NEW Plasma cannon fixture (<c>plasma_cannon</c>).
    /// </summary>
    public string? WeaponId { get; init; }
    /// <summary>Where the values come from (derived original hulls name their source rows; null for hand-written fixtures).</summary>
    public string? Source { get; init; }
    /// <summary>Values that are placeholders rather than original data, one note each (null when there are none listed).</summary>
    public IReadOnlyList<string>? NewValues { get; init; }
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
    /// <summary>NEW default zero for legacy fixtures; original hulls derive this force from VEH StrafeThrust.</summary>
    public float StrafeThrust { get; init; }
    public float RotationSpeed { get; init; } = 3.3f;
    public float MaximumSpeed { get; init; } = 360;
    public float AsteroidRestitution { get; init; } = 0.25f;
}

public sealed record ShipDefense
{
    public float MaximumHull { get; init; } = 100;
    public float MaximumShield { get; init; } = 50;
    /// <summary>
    /// True when the ship's energy is its shield, as in the original (CFG <c>EnergyShieldMode=2</c>; NWS/Rogue.nws rows 39-41 and
    /// 131): while any energy is left a hit drains energy and does no hull damage, and only a ship with no energy left takes hull
    /// damage. Such a ship has no separate shield pool, so <see cref="MaximumShield"/> must be 0.
    /// </summary>
    public bool EnergyShield { get; init; }
}

/// <summary>
/// The Shift boost of a hull. Thrusts are forces like <see cref="ShipPhysics.ForwardThrust"/> (divided by the hull's mass); the
/// boost applies only while the pilot thrusts and has at least one tick's worth of <see cref="EnergyPerSecond"/> left.
/// </summary>
public sealed record ShipHyper
{
    public float ForwardThrust { get; init; }
    public float ReverseThrust { get; init; }
    public float MaximumSpeed { get; init; }
    public float EnergyPerSecond { get; init; }
    /// <summary>
    /// False when energy stops recharging while the boost is engaged (the owner's rule for how the original hyper cost applies);
    /// true when the cost is taken from a still-running recharge.
    /// </summary>
    public bool RechargeWhileEngaged { get; init; }
    public string? Source { get; init; }
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
    /// <summary>Hull damage of one hit. A weapon with no <see cref="ShieldDrain"/> lets a shield absorb it point for point (NEW).</summary>
    public float Damage { get; init; } = 28;
    /// <summary>
    /// Shield (or, for an <see cref="ShipDefense.EnergyShield"/> ship, energy) drained by one hit, under the original rule: a hit on
    /// a ship with any shield left drains it and does no hull damage; a hit on a ship with none does <see cref="Damage"/> to the
    /// hull. Null keeps the NEW rule, where the shield absorbs <see cref="Damage"/> point for point and the rest reaches the hull.
    /// </summary>
    public float? ShieldDrain { get; init; }
    public float CooldownSeconds { get; init; } = 0.2f;
    public float EnergyCost { get; init; } = 6;
    public float LifetimeSeconds { get; init; } = 2.5f;
    public bool InheritsShipVelocity { get; init; } = true;
    /// <summary>Where the values come from (derived original weapons name their ITM row; null for hand-written fixtures).</summary>
    public string? Source { get; init; }
    /// <summary>Values that are placeholders rather than original data, one note each.</summary>
    public IReadOnlyList<string>? NewValues { get; init; }
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
    /// File name (next to the sector file) of the tile map the sector is flown on, for example <c>rogue_map.json</c>; null for an
    /// open practice sector with no walls.
    /// </summary>
    public string? MapFile { get; init; }
    /// <summary>
    /// The teams a joining pilot can be put on, in the order they are filled (Steve, Nov 4 2026: the first pilot joins Cold
    /// Harbor, then pilots alternate so the teams stay even). Empty for a practice sector: every pilot uses <see cref="PlayerSpawn"/>.
    /// </summary>
    public IReadOnlyList<TeamDefinition> Teams { get; init; } = [];
    /// <summary>Where each part of a derived sector came from, and which parts are NEW. Not read by the simulation.</summary>
    public IReadOnlyList<string> Evidence { get; init; } = [];

    /// <summary>The spawn of a team, or <see cref="PlayerSpawn"/> when the sector has no such team.</summary>
    public SpawnDefinition SpawnOf(int team) => Teams.FirstOrDefault(t => t.Index == team)?.Spawn ?? PlayerSpawn;
}

/// <summary>
/// A team of the sector (CFG [TeamInfoN]): its name, how many pilots it takes, where it spawns and the station it calls home.
/// </summary>
public sealed record TeamDefinition
{
    /// <summary>The CFG team number (N of [TeamInfoN]).</summary>
    public int Index { get; init; }
    public string Name { get; init; } = "";
    /// <summary>CFG [TeamInfoN] MaxPlayers; a full team takes no more pilots.</summary>
    public int MaxPlayers { get; init; }
    public SpawnDefinition Spawn { get; init; } = new();
    /// <summary>Station id of the team's home store (see the station data); null when it has none.</summary>
    public string? HomeStationId { get; init; }
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
