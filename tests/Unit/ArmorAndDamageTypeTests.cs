using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F041: every weapon does one of three hull damage types (NWS/Rogue.nws row 133) and armour protects against them. The
/// protection is ITM f[52..57] of the nine Hull Armor rows (the emulator's utility-item Kinetic, Explosive and Electronic
/// Ignore/Percent pairs), applied in the emulator's order: ignored points first, then the reduction of the rest.
/// </summary>
public sealed class ArmorAndDamageTypeTests
{
    private static ShipArmor Armor(DamageType type, float ignore, float reduction)
    {
        var p = new ArmorProtection { Ignore = ignore, Reduction = reduction };
        return type switch
        {
            DamageType.Kinetic => new ShipArmor { Kinetic = p },
            DamageType.Explosive => new ShipArmor { Explosive = p },
            _ => new ShipArmor { Energy = p },
        };
    }

    [Fact]
    public void ArmourIgnoresPointsFirstThenReducesTheRest()
    {
        // Tiathamate against a Mass Driver round: (5 - 2) * (1 - 0.1) = 2.7.
        Assert.Equal(2.7f, ShipArmor.HullDamage(5, DamageType.Kinetic, Armor(DamageType.Kinetic, 2, 0.1f)), 4);
        // Only the matching type counts.
        Assert.Equal(5f, ShipArmor.HullDamage(5, DamageType.Energy, Armor(DamageType.Kinetic, 2, 0.1f)));
        // No armour, or an untyped weapon, takes it all.
        Assert.Equal(5f, ShipArmor.HullDamage(5, DamageType.Kinetic, null));
        Assert.Equal(5f, ShipArmor.HullDamage(5, null, Armor(DamageType.Kinetic, 2, 0.1f)));
    }

    [Fact]
    public void ArmourNeverHealsAndNeverAddsDamage()
    {
        Assert.Equal(0f, ShipArmor.HullDamage(1, DamageType.Energy, Armor(DamageType.Energy, 2, 0)));
        Assert.Equal(0f, ShipArmor.HullDamage(5, DamageType.Energy, Armor(DamageType.Energy, 0, 1.5f)));
        Assert.Equal(5f, ShipArmor.HullDamage(5, DamageType.Energy, Armor(DamageType.Energy, -3, -0.5f)));
    }

    [Fact]
    public void HullAndUpgradeArmourAddTypeByType()
    {
        var a = Armor(DamageType.Kinetic, 1, 0.05f);
        var b = new ShipArmor { Kinetic = new ArmorProtection { Ignore = 1, Reduction = 0.04f }, Energy = new ArmorProtection { Reduction = 0.1f } };
        var both = ShipArmor.Combine(a, b)!;
        Assert.Equal(2f, both.Kinetic.Ignore);
        Assert.Equal(0.09f, both.Kinetic.Reduction, 4);
        Assert.Equal(0.1f, both.Energy.Reduction, 4);
        Assert.Same(a, ShipArmor.Combine(a, null));
        Assert.Null(ShipArmor.Combine(null, null));
    }

    [Fact]
    public void AnArmouredTargetTakesLessHullDamageFromAMatchingWeapon()
    {
        // A Plasma Cannon round (5 Energy hull damage, 266 drain) against Ablative Shell's Energy protection (1 point, 7%):
        // the first hit empties the 40 shield and reaches the hull for (5 - 1) * 0.93 = 3.72.
        WorldSimulation World(ShipArmor? armor, DamageType? type) => new(TestWorld.Catalog(
            weapon: new WeaponDefinition { MuzzleVelocity = 6000, CooldownSeconds = 1f / 60, Damage = 5, ShieldDrain = 266, DamageType = type },
            sector: new SectorDefinition
            {
                PlayerSpawn = new SpawnDefinition { Heading = 0 },
                Targets = [new TargetDefinition { X = 100, Y = 0, Hull = 100, Shield = 40, Armor = armor }],
            }));
        var ablative = Armor(DamageType.Energy, 1, 0.07f);

        var world = World(ablative, DamageType.Energy);
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(100 - 3.72f, world.Targets[0].Hull, 3);

        var kinetic = World(ablative, DamageType.Kinetic);
        kinetic.Tick(new ShipInput(0, 0, true));
        Assert.Equal(95, kinetic.Targets[0].Hull, 3);

        var bare = World(null, DamageType.Energy);
        bare.Tick(new ShipInput(0, 0, true));
        Assert.Equal(95, bare.Targets[0].Hull, 3);
    }

    [Fact]
    public void ArmourStillOnlyMattersOnceTheShieldIsGone()
    {
        var world = new WorldSimulation(TestWorld.Catalog(
            weapon: new WeaponDefinition { MuzzleVelocity = 6000, CooldownSeconds = 1f / 60, Damage = 5, ShieldDrain = 10, DamageType = DamageType.Energy },
            sector: new SectorDefinition
            {
                PlayerSpawn = new SpawnDefinition { Heading = 0 },
                Targets = [new TargetDefinition { X = 100, Y = 0, Hull = 100, Shield = 40, Armor = Armor(DamageType.Energy, 1, 0.07f) }],
            }));
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(100, world.Targets[0].Hull);
        Assert.Equal(30, world.Targets[0].Shield);
    }

    [Fact]
    public void ArmourOutsideItsRangeIsRefused()
    {
        var bad = new ShipDefinition { Defense = new ShipDefense { Armor = Armor(DamageType.Kinetic, 0, 1.2f) } };
        Assert.Throws<ArgumentException>(() => TestWorld.Catalog(ship: bad));
        var negative = new ShipDefinition { Defense = new ShipDefense { Armor = Armor(DamageType.Explosive, -1, 0) } };
        Assert.Throws<ArgumentException>(() => TestWorld.Catalog(ship: negative));
    }

    [Fact]
    public void DamageTypesRoundTripAsNamesInWeaponJson()
    {
        var json = OriginalWeaponBuilder.ToJson(new WeaponDefinition { DamageType = DamageType.Explosive });
        Assert.Contains("\"damageType\": \"Explosive\"", json, StringComparison.Ordinal);
    }

    // ---- Pinned originals ----

    private static OriginalItemCatalog Items => OriginalItemCatalog.Load(OriginalAssets.Root!);

    [RequiresOriginalAssetsFact]
    public void TheNineArmoursCarryTheProtectionTheirHelpTextDescribes()
    {
        // (Kinetic ignore, %), (Explosive ignore, %), (Electronic = Energy ignore, %) raw, ITM f[52..57].
        var expected = new Dictionary<string, int[]>
        {
            ["Duraplas Armor"] = [0, 45, 0, 25, 0, 15],
            ["Carbolux Armor"] = [1000, 90, 0, 50, 0, 30],
            ["Maxilite Armor"] = [1000, 40, 0, 20, 0, 10],
            ["Sillicate Armor"] = [1000, 50, 1000, 50, 0, 0],
            ["Tiathamate Armor"] = [2000, 100, 1000, 100, 0, 40],
            ["Bio Carapace"] = [1000, 90, 0, 10, 0, 80],
            ["Enhanced Bio Carapace"] = [1000, 100, 0, 45, 0, 100],
            ["Ablative Shell"] = [0, 0, 0, 45, 1000, 70],
            ["Absorption Shell"] = [0, 0, 0, 60, 2000, 120],
        };
        var armours = OriginalArmor.All(Items);
        Assert.Equal(expected.Keys.Order(), armours.Select(a => a.Item.Name).Order());
        foreach (var a in armours)
        {
            int[] raw = [a.Kinetic.Ignore.Value, a.Kinetic.Percent.Value, a.Explosive.Ignore.Value, a.Explosive.Percent.Value,
                a.Electronic.Ignore.Value, a.Electronic.Percent.Value];
            Assert.Equal(expected[a.Item.Name], raw);
            // Psionic, Bypass and Energy (shield drain) pairs are unused.
            for (var f = OriginalArmor.PsionicIgnoreField; f < OriginalArmor.PsionicIgnoreField + 6; f++)
                Assert.Equal(0, a.Item.Record.Int(f).Value);
        }
        // The help text's "no protection versus Kinetic" is a zero kinetic pair; "no protection versus Energy" a zero electronic pair.
        Assert.Contains("no protection versus Kinetic", armours.Single(a => a.Item.Name == "Ablative Shell").Description, StringComparison.Ordinal);
        Assert.Contains("no protection versus Energy", armours.Single(a => a.Item.Name == "Sillicate Armor").Description, StringComparison.Ordinal);
    }

    [RequiresOriginalAssetsFact]
    public void NoOtherType13RowProtects()
    {
        foreach (var item in Items.Items.Where(i => i.RecordType == OriginalArmor.RecordType && !OriginalArmor.TryOf(i, out _)))
            for (var f = OriginalArmor.KineticIgnoreField; f < OriginalArmor.KineticIgnoreField + 12; f++)
                Assert.Equal(0, item.Record.Int(f).Value);
    }

    [RequiresOriginalAssetsFact]
    public void TheTwelveHullsHaveNoArmourOfTheirOwn()
    {
        foreach (var hull in OriginalHullCatalog.Load(OriginalAssets.Root!).Hulls)
            for (var f = VehComputerFields.ArmorFirst; f < VehComputerFields.ArmorFirst + VehComputerFields.ArmorStride * VehComputerFields.ArmorCount; f++)
                Assert.Equal(0, hull.VehRecord.Int(f).Value);
    }

    [RequiresOriginalAssetsFact]
    public void EveryWeaponHasExactlyOneDamageType()
    {
        var byName = Items.Items.Where(i => i.RecordType == 6).GroupBy(i => i.Name).ToDictionary(g => g.Key, g => g.First());
        Assert.Equal(DamageType.Energy, OriginalWeaponBuilder.Weapon(byName["Plasma Cannon"]).DamageType);
        Assert.Equal(DamageType.Energy, OriginalWeaponBuilder.Weapon(byName["Bio Cannon"]).DamageType);
        Assert.Equal(DamageType.Energy, OriginalWeaponBuilder.Weapon(byName["D-Cannon"]).DamageType);
        Assert.Equal(DamageType.Kinetic, OriginalWeaponBuilder.Weapon(byName["Mass Driver"]).DamageType);
        Assert.Equal(DamageType.Explosive, OriginalWeaponBuilder.Weapon(byName["Moltor Bomb"]).DamageType);
    }

    [RequiresOriginalAssetsFact]
    public void TheStationSellsArmourWithItsProtectionAndNoHullBonus()
    {
        var station = OriginalStationBuilder.Build(OriginalAssets.Root!, TestWorld.RogueMap, TestWorld.RogueCatalog().Sector.Teams);
        var armours = station.Upgrades.Where(u => u.Family == OriginalArmor.ArmorCategory).ToList();
        Assert.Equal(9, armours.Count);
        Assert.All(armours, u =>
        {
            Assert.Equal(0, u.Effect.MaximumHull);
            Assert.NotNull(u.Effect.Armor);
        });
        var absorption = armours.Single(u => u.Name == "Absorption Shell").Effect.Armor!;
        Assert.Equal(2f, absorption.Energy.Ignore);
        Assert.Equal(0.12f, absorption.Energy.Reduction, 4);
        Assert.Equal(ArmorProtection.None, absorption.Kinetic);
        Assert.All(station.Upgrades.Where(u => u.Family != OriginalArmor.ArmorCategory), u => Assert.Null(u.Effect.Armor));
    }
}
