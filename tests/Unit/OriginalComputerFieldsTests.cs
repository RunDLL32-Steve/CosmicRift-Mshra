using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F058 (data clause only): the v53 type-5 field map and raw typed view. Field names are upstream-parser names (InfantryOnline
/// bb97579e), not original-executable evidence. Oracle values were read from VEH/Rogue.veh at eb1514ea with the stdlib script
/// scripts/original-assets/veh_computer_field_map.py (--check and the index table); asset tests are Skipped without the assets,
/// the synthetic tests always run.
/// </summary>
public sealed class OriginalComputerFieldsTests
{
    private static readonly Lazy<OriginalVehicleCatalog> Vehicles = new(() => OriginalVehicleCatalog.Load(OriginalAssets.Root!));

    internal static string Fingerprint(OriginalComputerRecord r, int from, int toExclusive) =>
        string.Join(";", Enumerable.Range(from, toExclusive - from)
            .Select(i => (Index: i, r.Field(i).Value))
            .Where(x => x.Value != "0" && x.Value != "")
            .Select(x => $"{x.Index}={x.Value}"));

    private static string Line(int type, string version, int id, int fieldCount)
    {
        var fields = new List<string> { type.ToString(), version, id.ToString(), "\"Synthetic\"" };
        while (fields.Count < fieldCount) fields.Add(fieldCount == VehComputerFields.FieldCount && VehComputerFields.IsStringSlot(fields.Count) ? "\"\"" : "0");
        return string.Join(",", fields);
    }

    private static OriginalVehicleRecord Synthetic(int type, int fieldCount, string version = "v53") =>
        Assert.Single(OriginalVehicleCatalog.FromRecords(RawRecordReader.Parse(Line(type, version, 900, fieldCount), "VEH/t.veh")).All);

    [Fact]
    public void LayoutConstantsAreInternallyConsistent()
    {
        Assert.Equal(VehComputerFields.FieldCount, VehComputerFields.ProductStart + VehComputerFields.ProductCount * VehComputerFields.ProductStride);
        Assert.Equal((253, 14, 16, 477), (VehComputerFields.ProductStart, VehComputerFields.ProductStride, VehComputerFields.ProductCount, VehComputerFields.FieldCount));
        Assert.Equal(VehComputerFields.SpriteTurretFirst + VehComputerFields.SpriteBlobLength, VehComputerFields.ProductStart);
        Assert.Equal(VehComputerFields.SpriteBaseFirst + VehComputerFields.SpriteBlobLength, VehComputerFields.SpriteTurretFirst);
        Assert.Equal(VehComputerFields.SpriteTurretZAdjust + 1, VehComputerFields.SpriteBaseFirst);
        Assert.Equal(VehComputerFields.ComputerScalarFirst + VehComputerFields.ComputerScalarCount, VehComputerFields.SpriteBaseFirst);
        Assert.Equal(VehComputerFields.HoldItemFirst + VehComputerFields.HoldItemCount * VehComputerFields.HoldItemStride, VehComputerFields.BaseEnd);
        Assert.Equal(VehComputerFields.BaseEnd, VehComputerFields.ComputerScalarFirst);
        Assert.Equal(VehComputerFields.TerrainModifiersFirst + VehComputerFields.TerrainModifiersCount, VehComputerFields.BallCarryRollVehicleItem);
        Assert.Equal(VehComputerFields.AnimatedSpriteFirst + VehComputerFields.AnimatedSpriteCount * VehComputerFields.AnimatedSpriteStride, VehComputerFields.EnergyMax);
        Assert.Equal(VehComputerFields.ArmorFirst + VehComputerFields.ArmorCount * VehComputerFields.ArmorStride, VehComputerFields.BarrelLength);
        Assert.Equal(VehComputerFields.ChildVehiclesFirst + VehComputerFields.ChildVehiclesCount, VehComputerFields.ExplodeItemId);
        Assert.Equal((9, 18, 32), (VehComputerFields.Armor(0, 0), VehComputerFields.Armor(2, 1), VehComputerFields.Armor(5, 3)));
        Assert.Equal((253 + 14 * 15 + 13, 142 + 29 * 2, 142 + 29 * 2 + 1), (VehComputerFields.Product(15, 13), VehComputerFields.HoldItemLimit(29), VehComputerFields.HoldItemExtendedLimit(29)));
        Assert.Throws<ArgumentOutOfRangeException>(() => VehComputerFields.Product(16, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => VehComputerFields.Product(0, 14));
        Assert.Throws<ArgumentOutOfRangeException>(() => VehComputerFields.Armor(6, 0));
    }

    [Fact]
    public void StringSlotMapNamesExactly64SlotsLikeTheStdlibScript()
    {
        var slots = Enumerable.Range(0, VehComputerFields.FieldCount).Where(VehComputerFields.IsStringSlot).ToList();
        Assert.Equal(64, slots.Count);
        Assert.Equal(new[] { 3, 4, 5, 74, 75, 86, 87, 129, 130, 131, 234, 235, 237, 238, 245, 246, 253, 261, 264 }, slots.Take(19));
        Assert.False(VehComputerFields.IsStringSlot(VehComputerFields.VersionField));
    }

    [Fact]
    public void SyntheticGuardAcceptsOnlyV53Type5With477Fields()
    {
        var ok = new OriginalComputerRecord(Synthetic(5, 477));
        Assert.Equal((900, "Synthetic", 477, 16), (ok.Id, ok.Name, ok.Record.FieldCount, ok.Products.Count));
        Assert.True(ok.Products.All(p => p.IsEmpty));
        Assert.Throws<OriginalDataException>(() => new OriginalComputerRecord(Synthetic(5, 476)));
        Assert.Throws<OriginalDataException>(() => new OriginalComputerRecord(Synthetic(5, 478)));
        Assert.Throws<OriginalDataException>(() => new OriginalComputerRecord(Synthetic(2, 477)));
        Assert.Throws<OriginalDataException>(() => new OriginalComputerRecord(Synthetic(4, 477)));
        // the VEH catalog itself already refuses any other version token
        Assert.Throws<OriginalVehicleException>(() => Synthetic(5, 477, "v54"));
        Assert.Throws<ArgumentNullException>(() => new OriginalComputerRecord(null!));
    }

    [Fact]
    public void SyntheticRawAccessorsKeepSentinelsAndTextUnchanged()
    {
        var fields = new List<string> { "5", "v53", "900", "\"Synthetic\"", "\"\"", "\"Desc, with comma\"", "  250" };
        while (fields.Count < VehComputerFields.FieldCount) fields.Add(VehComputerFields.IsStringSlot(fields.Count) ? "\"\"" : "0");
        fields[VehComputerFields.HitpointsRequiredToOperate] = "-1";
        fields[VehComputerFields.LogicStealOwnership] = "\"!1&!2\"";
        fields[VehComputerFields.SpriteTurretFirst + VehComputerFields.BlobName] = "None";
        fields[VehComputerFields.Product(1, VehComputerFields.ProductTitle)] = "\"Make\"";
        fields[VehComputerFields.Product(1, VehComputerFields.ProductQuantity)] = "-1";
        var veh = Assert.Single(OriginalVehicleCatalog.FromRecords(RawRecordReader.Parse(string.Join(",", fields), "VEH/t.veh")).All);
        var r = new OriginalComputerRecord(veh);
        Assert.Equal(250, r.Hitpoints.Value);
        Assert.Equal(-1, r.HitpointsRequiredToOperate.Value);
        Assert.True(r.HitpointsRequiredToOperate.IsNegativeSentinel);
        Assert.Equal("Desc, with comma", r.Description.Value);
        Assert.Equal("!1&!2", r.LogicStealOwnership.Value);
        Assert.Equal("None", r.SpriteTurretBlobName.Value);
        Assert.Equal(0, r.LogicTakeOwnership.Value.Length);
        Assert.True(r.Products[0].IsEmpty);
        Assert.False(r.Products[1].IsEmpty);
        Assert.Equal("Make", r.Products[1].Title.Value);
        Assert.Equal(VehComputerFields.Product(1, 0), r.Products[1].FirstFieldIndex);
    }

    [RequiresOriginalAssetsFact]
    public void All17Type5RowsAre477FieldsWithTextInStringSlotsAndIntegersElsewhere()
    {
        var rows = OriginalComputerRecord.All(Vehicles.Value);
        Assert.Equal(17, rows.Count);
        Assert.Equal(new[] { 400, 401, 402, 403, 404, 405, 407, 408, 409, 410, 411, 412, 413, 414, 425, 426, 427 }, rows.Select(r => r.Id));
        Assert.Equal(Enumerable.Range(1, 17), rows.Select(r => r.RowNumber));
        foreach (var r in rows)
        {
            Assert.Equal(477, r.Record.FieldCount);
            Assert.Equal("v53", r.Record.VersionToken);
            for (var i = 0; i < VehComputerFields.FieldCount; i++)
            {
                if (i == VehComputerFields.VersionField) continue;
                if (VehComputerFields.IsStringSlot(i))
                {
                    // text: either quoted, or not a plain integer (the literal "None" is text)
                    var f = r.Field(i);
                    Assert.True(f.WasQuoted || f.Value.Length == 0 || f.Value == "None" || !int.TryParse(f.Value, out _), $"row {r.Id} f[{i}] '{f.Value}'");
                }
                else
                {
                    Assert.Equal(i, r.Int(i).FieldIndex);
                }
            }
        }
        // the single quoted-number text slot: MeteorShower's Description is the text "0"
        var meteor = rows.Single(r => r.Id == 402);
        Assert.True(meteor.Description.WasQuoted);
        Assert.Equal("0", meteor.Description.Value);
    }

    [RequiresOriginalAssetsFact]
    public void BasicFieldsOfAll17RowsArePinned()
    {
        var rows = OriginalComputerRecord.All(Vehicles.Value);
        // (id, name, Hitpoints f6, HitpointsRequiredToOperate f211, Destroyable f217, VehicleGetInMode f66, RepairRate f210, DropItemId f68, DropItemQuantity f69, ExplodeItemId f64)
        var expected = new (int, string, int, int, int, int, int, int, int, int)[]
        {
            (400, "Refinery", 500, 0, 0, 0, 0, 0, 0, 0),
            (401, "Null Death Orb", 500, 50, 1, 0, 33, 2008, 1, 3004),
            (402, "MeteorShower", 50, 25, 0, 0, 10, 0, 0, 0),
            (403, "PirateStation", 300, 50, 1, 0, 33, 2008, 1, 3004),
            (404, "Trading Outpost", 500, 0, 0, 0, 0, 0, 0, 0),
            (405, "Null Kill Station", 500, 50, 1, 0, 33, 2008, 1, 3004),
            (407, "Repair Dock", 500, 0, 0, 0, 0, 0, 0, 0),
            (408, "Micro Repair Dock", 250, 5, 1, 0, 50, 0, 0, 0),
            (409, "Solar Mold", 500, 0, 1, 0, 100, 0, 0, 0),
            (410, "GunSat", 50, 5, 1, 0, 50, 2008, 1, 3004),
            (411, "BeamSat", 75, 5, 1, 0, 50, 2008, 1, 3004),
            (412, "Micro Refinery", 250, 5, 1, 0, 50, 0, 0, 3004),
            (413, "Rogue Trader Outpost", 500, 10, 1, 0, 250, 2008, 1, 3004),
            (414, "Mold Spore", 75, 5, 1, 0, 50, 2008, 1, 3004),
            (425, "Manufactory - Missile", 250, 25, 1, 0, 25, 66, 1, 3004),
            (426, "Manufactory - Munitions", 250, 25, 1, 0, 25, 66, 1, 3004),
            (427, "Manufactory - Defense", 250, 25, 1, 0, 25, 66, 1, 3004),
        };
        Assert.Equal(expected, rows.Select(r => (r.Id, r.Name, r.Hitpoints.Value, r.HitpointsRequiredToOperate.Value, r.Destroyable.Value, r.VehicleGetInMode.Value,
            r.RepairRate.Value, r.DropItemId.Value, r.DropItemQuantity.Value, r.ExplodeItemId.Value)));
        // ownership logic text (f[234] LogicTakeOwnership, f[235] LogicStealOwnership), raw
        const string group = "5|6|9|10|1|3|4|11|2|8|15|16";
        Assert.Equal(new (int, string, string)[]
        {
            (403, group, ""), (410, "!", "!"), (411, "!", "!"), (414, "!", "!"),
            (425, group, group), (426, group, group), (427, group, group),
        }, rows.Where(r => r.LogicTakeOwnership.Value.Length > 0 || r.LogicStealOwnership.Value.Length > 0)
            .Select(r => (r.Id, r.LogicTakeOwnership.Value, r.LogicStealOwnership.Value)));
        Assert.All(rows, r => Assert.Equal(0, r.EventString1.Value.Length + r.EventString2.Value.Length + r.EventString3.Value.Length));
        Assert.Equal(new[] { "None", "SpaceStations", "defstructures", "spacestations", "SpaceMold" }.OrderBy(x => x),
            rows.Select(r => r.SpriteTurretBlobName.Value).Distinct().OrderBy(x => x));
    }

    [RequiresOriginalAssetsFact]
    public void ProductBlocksStartAtF253AndAgreeWithTheOutpostAndManufactoryViews()
    {
        var rows = OriginalComputerRecord.All(Vehicles.Value);
        Assert.All(rows, r =>
        {
            Assert.Equal(16, r.Products.Count);
            Assert.Equal(Enumerable.Range(0, 16).Select(k => 253 + 14 * k), r.Products.Select(p => p.FirstFieldIndex));
            // non-empty blocks form a prefix: nothing is hidden after a gap
            var n = r.Products.TakeWhile(p => !p.IsEmpty).Count();
            Assert.All(r.Products.Skip(n), p => Assert.True(p.IsEmpty && p.Title.Value.Length == 0));
        });
        Assert.Equal(new[] { 3, 0, 0, 0, 4, 0, 3, 3, 0, 0, 0, 3, 8, 0, 9, 5, 2 }, rows.Select(r => r.Products.Count(p => !p.IsEmpty)));

        var outpost = OriginalRogueTraderOutpost.Resolve(OriginalItemCatalog.Load(OriginalAssets.Root!), Vehicles.Value, OriginalNws.LoadRogue(OriginalAssets.Root!));
        var op = new OriginalComputerRecord(outpost.Vehicle);
        for (var k = 0; k < 8; k++)
        {
            var b = outpost.Blocks[k];
            var p = op.Products[k];
            Assert.Equal(b.FirstFieldIndex, p.FirstFieldIndex);
            Assert.Equal(b.Name, p.Title.Value);
            Assert.Equal(b.ItemIdField.Value, op.Int(p.FirstFieldIndex + VehComputerFields.ProductToCreate).Value);
            Assert.Equal(b.Raw2.Value, op.Int(p.FirstFieldIndex + VehComputerFields.ProductQuantity).Value);
            Assert.Equal(b.InputItemIdField.Value, op.Int(p.FirstFieldIndex + VehComputerFields.ProductPlayerItemNeededId).Value);
            Assert.Equal(b.Raw6.Value, op.Int(p.FirstFieldIndex + VehComputerFields.ProductPlayerItemNeededQuantity).Value);
            Assert.Equal(b.Raw7.Value, op.Int(p.FirstFieldIndex + VehComputerFields.ProductCost).Value);
        }

        var manufactories = OriginalManufactories.Load(OriginalAssets.Root!, OriginalItemCatalog.Load(OriginalAssets.Root!));
        foreach (var m in manufactories)
        {
            var rec = rows.Single(r => r.Id == int.Parse(m.Vehicle.Value(2)));
            Assert.Equal(m.Recipes.Count, rec.Products.Count(p => !p.IsEmpty));
            for (var k = 0; k < m.Recipes.Count; k++)
            {
                var recipe = m.Recipes[k];
                var p = rec.Products[k];
                Assert.Equal(recipe.FirstFieldIndex, p.FirstFieldIndex);
                Assert.Equal(recipe.Name, p.Title.Value);
                Assert.Equal(recipe.OutputItemId.Value, rec.Int(p.FirstFieldIndex + VehComputerFields.ProductToCreate).Value);
                Assert.Equal(recipe.OutputQuantity.Value, rec.Int(p.FirstFieldIndex + VehComputerFields.ProductQuantity).Value);
                Assert.Equal(recipe.RawField4.Value, rec.Int(p.FirstFieldIndex + VehComputerFields.ProductTime).Value);
                Assert.Equal(recipe.InputItemId.Value, rec.Int(p.FirstFieldIndex + VehComputerFields.ProductPlayerItemNeededId).Value);
                Assert.Equal(recipe.InputQuantity.Value, rec.Int(p.FirstFieldIndex + VehComputerFields.ProductPlayerItemNeededQuantity).Value);
                Assert.Equal(recipe.RawField7.Value, rec.Int(p.FirstFieldIndex + VehComputerFields.ProductCost).Value);
            }
        }
    }

    [RequiresOriginalAssetsFact]
    public void NonType5RowsAreRejectedByTheComputerView()
    {
        var cars = Vehicles.Value.CarRows.ToList();
        Assert.Equal(12, cars.Count);
        Assert.All(cars, c => Assert.Throws<OriginalDataException>(() => new OriginalComputerRecord(c)));
        var spectator = Vehicles.Value.All.Single(v => v.IsSpectator);
        Assert.Equal(202, spectator.FieldCount);
        Assert.Throws<OriginalDataException>(() => new OriginalComputerRecord(spectator));
    }
}
