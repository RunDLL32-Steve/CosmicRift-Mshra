namespace RogueTrader.OriginalData;

/// <summary>
/// One 14-field ComputerProduct block of the public Trading Outpost (VEH Rogue.veh row 5, id 404) for RT-F045, read with the
/// emulator's ComputerProduct layout (<see cref="VehComputerFields"/>): f+0 title, f+1 ProductToCreate (the ITM id of the credit
/// effect), f+2 Quantity, f+4 Time, f+5 PlayerItemNeededId (the resource handed in), f+6 PlayerItemNeededQuantity, f+7 Cost.
/// The four pinned blocks hold Quantity -1, needed quantity -1 and cost 0; the emulator's ArenaComputer reads needed -1 as
/// "every unit the player holds" and Quantity -1 as "that many credit items", and a credit item is a type 1 MultiItem whose
/// f[31] Cash is paid per item (Player.applyMultiItem). <see cref="RawField4"/> is the Time field, in units of 10 ms.
/// </summary>
public sealed record OriginalTradeRecipe(
    string Name,
    int FirstFieldIndex,
    OriginalInt CreditItemId,
    OriginalInt RawField4,
    OriginalInt InputItemId,
    OriginalItem CreditItem,
    OriginalItem InputItem)
{
    /// <summary>f+2 Quantity: negative means units handed in x |Quantity| credit items (emulator ArenaComputer.produceItem).</summary>
    public OriginalInt OutputQuantity { get; init; }
    /// <summary>f+6 PlayerItemNeededQuantity: -1 means every unit of the input the player holds.</summary>
    public OriginalInt InputQuantity { get; init; }
    /// <summary>f+7 Cost in credits.</summary>
    public OriginalInt Cost { get; init; }
    /// <summary>f+4 Time, in units of 10 ms (the emulator queues completion at Time x 10 ms).</summary>
    public OriginalInt Time => RawField4;
    /// <summary>The credit item's f[31] Cash (emulator MultiItem.Cash): credits paid per credit item.</summary>
    public OriginalInt Cash => CreditItem.Record.Int(31);
}

public static class OriginalTradingPost
{
    public const int VehicleId = 404;
    public const int FirstRecipeField = 253;
    public const int RecipeStride = 14;
    public const int RecipeCount = 4;

    /// <summary>The Trading Outpost's recipes (VEH row with id 404) resolved against the item catalog.</summary>
    public static IReadOnlyList<OriginalTradeRecipe> Load(string assetsRoot, OriginalItemCatalog items)
    {
        var veh = AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh);
        return FromRecords(RawRecordReader.Parse(veh, PinnedAssets.Veh.RelativePath), items);
    }

    public static IReadOnlyList<OriginalTradeRecipe> FromRecords(IReadOnlyList<RawRecord> vehRecords, OriginalItemCatalog items)
    {
        var rows = vehRecords.Where(v => v.FieldCount > 2 && v.Value(2) == VehicleId.ToString()).ToList();
        if (rows.Count != 1) throw new OriginalDataException($"VEH id {VehicleId} matches {rows.Count} rows, expected exactly one.");
        var r = rows[0];
        var list = new List<OriginalTradeRecipe>();
        for (var k = 0; k < RecipeCount; k++)
        {
            var f = FirstRecipeField + k * RecipeStride;
            var credit = r.Int(f + 1);
            var input = r.Int(f + 5);
            list.Add(new OriginalTradeRecipe(r.Value(f), f, credit, r.Int(f + VehComputerFields.ProductTime), input, items.ById(credit.Value), items.ById(input.Value))
            {
                OutputQuantity = r.Int(f + VehComputerFields.ProductQuantity),
                InputQuantity = r.Int(f + VehComputerFields.ProductPlayerItemNeededQuantity),
                Cost = r.Int(f + VehComputerFields.ProductCost),
            });
        }
        return list;
    }
}
