# RT-F396: transform only the pinned upstream buy branch. Do not edit the reference.
$shopPurchasesTransform = {
    param([string]$text)
    $methodPattern = '(?s)(public override void handlePlayerShop\(Player from, ItemInfo item, int quantity\)\s*\{)(.*?)(\r?\n        #endregion)'
    $method = [regex]::Matches($text, $methodPattern)
    if ($method.Count -ne 1) { throw 'Expected one original shop handler.' }
    $body = $method[0].Value
    $buyPattern = '(?s)                //Good to go, calculate the price\r?\n                int price = item\.buyPrice \* quantity;.*?from\.inventoryModify\(item, quantity\);\r?\n                \}'
    if ([regex]::Matches($body, $buyPattern).Count -ne 1 -or $body.Contains('long purchaseQuantity')) { throw 'Expected one unpatched original shop buy branch.' }
    $replacement = @'
                // RT-F396: widen price arithmetic, revalidate after scripts, deliver exactly, then debit.
                long purchaseQuantity = quantity;
                if (item.buyPrice < 0)
                    return;

                long cost = (long)item.buyPrice * purchaseQuantity;
                if (cost > from.Cash)
                    return;

                if (exists("Shop.Buy") && !(bool)callsync("Shop.Buy", false, from, item, quantity))
                    return;

                // A script may have changed ownership or cash; validate the current state.
                ii = from.getInventory(item);
                if (cost > from.Cash)
                    return;
                if (item.maxAllowed != 0 && purchaseQuantity > Math.Abs(item.maxAllowed) - ((ii == null) ? 0 : (int)ii.quantity))
                    return;

                if (item.itemType == ItemInfo.ItemType.Upgrade || item.itemType == ItemInfo.ItemType.Skill || item.itemType == ItemInfo.ItemType.Multi)
                {	// Effects are not a simple inventory count: debit first, refund if the grant is refused.
                    from.Cash -= (int)cost;
                    if (!from.inventoryModify(item, quantity))
                        from.Cash += (int)cost;
                    return;
                }

                // Ordinary items: the stack is a ushort, and category/stack caps may silently shrink a grant.
                int before = (ii == null) ? 0 : (int)ii.quantity;
                if (before + purchaseQuantity > ushort.MaxValue)
                    return;
                if (!from.inventoryModify(false, item, quantity))
                    return;

                Player.InventoryItem after = from.getInventory(item);
                int delivered = ((after == null) ? 0 : (int)after.quantity) - before;
                if (delivered != quantity)
                {	// Partial or no delivery: restore the previous stack and charge nothing.
                    if (after != null)
                    {
                        if (before == 0)
                            from._inventory.Remove(item.id);
                        else
                            after.quantity = (ushort)before;
                    }
                    return;
                }

                from.Cash -= (int)cost;
                from.syncInventory();
'@
    $body = [regex]::Replace($body, $buyPattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($match) $replacement })
    $text.Substring(0, $method[0].Index) + $body + $text.Substring($method[0].Index + $method[0].Length)
}
