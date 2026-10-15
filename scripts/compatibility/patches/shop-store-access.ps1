# RT-F389: apply to an unchanged pinned ScriptArena.cs string in a derived copy only.
# The packet path (Handle_CS_Shop -> handlePlayerShop) performs no location check, while the
# original /buy and /sell chat commands only allow a purchase where the terrain has
# StoreEnabled or a spectator is in a zone with SpectatorStore. Apply that same rule here.
# The helper fails closed for missing state or out-of-range positions instead of throwing.
$shopStoreAccessTransform = {
    param([string]$text)

    $signature = 'public override void handlePlayerShop(Player from, ItemInfo item, int quantity)'
    if ([regex]::Matches($text, [regex]::Escape($signature)).Count -ne 1) { throw 'Expected one original shop handler signature.' }
    if ($text.Contains('canUseStore(')) { throw 'Shop store access guard already applied.' }

    $regionPattern = '(?m)^(\s*)#region handlePlayerShop\r?\n'
    if ([regex]::Matches($text, $regionPattern).Count -ne 1) { throw 'Expected one handlePlayerShop region.' }

    $helper = @'
        #region canUseStore
        /// <summary>
        /// RT-F389: the original /buy and /sell location rule (StoreEnabled terrain, or spectator store).
        /// </summary>
        private bool canUseStore(Player from)
        {
            if (from == null || from._state == null || _tiles == null || _server == null || _server._zoneConfig == null || _server._assets == null || _server._assets.Level == null)
                return false;

            if (from._team != null && from._team.IsSpec && _server._zoneConfig.arena != null && _server._zoneConfig.arena.spectatorStore)
                return true;

            int x = from._state.positionX / 16;
            int y = from._state.positionY / 16;
            int index = (y * _levelWidth) + x;
            if (x < 0 || y < 0 || x >= _levelWidth || index < 0 || index >= _tiles.Length)
                return false;

            int lookup = _tiles[index].TerrainLookup;
            int[] terrainLookup = _server._assets.Level.TerrainLookup;
            if (lookup < 0 || lookup >= terrainLookup.Length)
                return false;

            int terrainId = terrainLookup[lookup];
            var terrains = _server._zoneConfig.terrains;
            return terrainId >= 0 && terrainId < terrains.Count && terrains[terrainId].storeEnabled;
        }
        #endregion

'@
    $text = [regex]::Replace($text, $regionPattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $helper + $m.Value })

    # Reject before any inventory, price, cash or script hook side effect.
    $method = [regex]::Match($text, [regex]::Escape($signature) + '\s*\{')
    $text.Insert($method.Index + $method.Length, "`n            if (!canUseStore(from))`n                return;`n")
}
