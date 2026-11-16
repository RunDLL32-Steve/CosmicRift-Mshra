namespace RogueTrader.OriginalData;

/// <summary>
/// One 14-field ComputerProduct block of the public Refinery (VEH Rogue.veh row 1, id 400) for RT-F044, read with the
/// emulator's ComputerProduct layout (<see cref="VehComputerFields"/>): f+0 title, f+1 ProductToCreate, f+2 Quantity, f+4 Time,
/// f+5 PlayerItemNeededId, f+6 PlayerItemNeededQuantity, f+7 Cost. The pinned blocks all hold Quantity -1, needed quantity -1
/// and cost 0; the emulator's ArenaComputer reads needed -1 as "every unit the player holds" and Quantity -1 as "that many
/// units times 1", and queues a product with Time &gt; 0 for Time x 10 ms. <see cref="RawField4"/> is that Time field.
/// </summary>
public sealed record OriginalRefineRecipe(
    string Name,
    int FirstFieldIndex,
    OriginalInt OutputItemId,
    OriginalInt RawField4,
    OriginalInt InputItemId,
    OriginalItem Output,
    OriginalItem Input)
{
    /// <summary>f+2 Quantity: negative means units used x |Quantity| (emulator ArenaComputer.produceItem).</summary>
    public OriginalInt OutputQuantity { get; init; }
    /// <summary>f+6 PlayerItemNeededQuantity: -1 means every unit of the input the player holds.</summary>
    public OriginalInt InputQuantity { get; init; }
    /// <summary>f+7 Cost in credits.</summary>
    public OriginalInt Cost { get; init; }
    /// <summary>f+4 Time, in units of 10 ms (the emulator queues completion at Time x 10 ms).</summary>
    public OriginalInt Time => RawField4;
}

public static class OriginalRefinery
{
    public const int VehicleId = 400;
    public const int RecipeCount = 3;
    /// <summary>LIO type 5 rows 13 'RefineryA' and 14 'RefineryB' each place actor -400.</summary>
    public const int PlacementCount = 2;

    public static IReadOnlyList<OriginalRefineRecipe> Load(string assetsRoot, OriginalItemCatalog items)
    {
        var veh = AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh);
        return FromRecords(RawRecordReader.Parse(veh, PinnedAssets.Veh.RelativePath), items);
    }

    public static IReadOnlyList<OriginalRefineRecipe> FromRecords(IReadOnlyList<RawRecord> vehRecords, OriginalItemCatalog items)
    {
        var rows = vehRecords.Where(v => v.FieldCount > 2 && v.Value(2) == VehicleId.ToString()).ToList();
        if (rows.Count != 1) throw new OriginalDataException($"VEH id {VehicleId} matches {rows.Count} rows, expected exactly one.");
        var r = rows[0];
        var list = new List<OriginalRefineRecipe>();
        for (var k = 0; k < RecipeCount; k++)
        {
            var f = OriginalTradingPost.FirstRecipeField + k * OriginalTradingPost.RecipeStride;
            var output = r.Int(f + 1);
            var input = r.Int(f + 5);
            list.Add(new OriginalRefineRecipe(r.Value(f), f, output, r.Int(f + VehComputerFields.ProductTime), input, items.ById(output.Value), items.ById(input.Value))
            {
                OutputQuantity = r.Int(f + VehComputerFields.ProductQuantity),
                InputQuantity = r.Int(f + VehComputerFields.ProductPlayerItemNeededQuantity),
                Cost = r.Int(f + VehComputerFields.ProductCost),
            });
        }
        return list;
    }
}
