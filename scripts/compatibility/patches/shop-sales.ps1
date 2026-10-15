# RT-F388: transform only the pinned upstream sell branch. Do not edit the reference.
$shopSalesTransform = {
    param([string]$text)
    $methodPattern = '(?s)(public override void handlePlayerShop\(Player from, ItemInfo item, int quantity\)\s*\{)(.*?)(\r?\n        #endregion)'
    $method = [regex]::Matches($text, $methodPattern)
    if ($method.Count -ne 1) { throw 'Expected one original shop handler.' }
    $body = $method[0].Value
    $sellPattern = '(?s)            else\r?\n            \{\s*//Sellable\?.*?(?=\r?\n        #endregion)'
    if ([regex]::Matches($body, $sellPattern).Count -ne 1 -or $body.Contains('long saleQuantity')) { throw 'Expected one unpatched original shop sell branch.' }
    $replacement = @'
            else
            {
                // Reject empty/oversized sales before scripts or inventory side effects.
                long saleQuantity = -(long)quantity;
                if (saleQuantity == 0 || item.sellPrice < 0 || ii == null || saleQuantity > ii.quantity)
                    return;

                long proceeds = (long)item.sellPrice * saleQuantity;
                if (proceeds > (long)int.MaxValue - from.Cash)
                    return;

                if (exists("Shop.Sell") && !(bool)callsync("Shop.Sell", false, from, item, (int)saleQuantity))
                    return;

                // A script may have changed ownership or cash; validate the current state.
                ii = from.getInventory(item);
                if (ii == null || saleQuantity > ii.quantity || proceeds > (long)int.MaxValue - from.Cash)
                    return;

                // Remove successfully before crediting, then publish the committed cash/inventory.
                if (!from.inventoryModify(false, item, quantity))
                    return;
                from.Cash += (int)proceeds;
                from.syncInventory();
            }
        }
'@
    $body = [regex]::Replace($body, $sellPattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($match) $replacement })
    $newMethod = $body.Replace($method[0].Groups[1].Value, $method[0].Groups[1].Value + "`n            if (quantity == 0) return;")
    $text.Substring(0, $method[0].Index) + $newMethod + $text.Substring($method[0].Index + $method[0].Length)
}
