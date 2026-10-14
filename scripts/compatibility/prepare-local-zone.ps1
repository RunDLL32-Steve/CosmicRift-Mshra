$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compatRoot = Join-Path $repoRoot '.tools/compatibility'
$zoneRoot = Join-Path $compatRoot 'working-zones/rogue-pristine'
$globalRoot = Join-Path $compatRoot 'working-zones/Global'
New-Item -ItemType Directory -Path (Join-Path $zoneRoot 'scripts'), $globalRoot -Force | Out-Null

# These are NEW diagnostic-host settings, not replacements for original RT balance/config.
# Original rogue.cfg and its data files stay unchanged in the copied assets directory.
@'
<xml>
  <server>
    <zoneConfig value="rogue.cfg" />
    <zoneName value="RT local compatibility probe" />
    <zoneDescription value="Core-data startup only; no native client/gameplay proof" />
    <zoneIsAdvanced value="false" />
    <bindIP value="127.0.0.1" /><bindPort value="21337" />
    <maxPlayers value="8" /><gameType value="" />
    <loadBlobs value="false" />
    <updateGlobalNws value="" /><copyServerFrom value="" />
    <pathFindingEnabled value="false" /><pathFindingEnabledNew value="false" />
    <ipAllowlistFile value="loopback-allowlist.txt" />
  </server>
  <database><connectionDelay value="0" /></database>
  <assets><location value="assets" /></assets>
  <protocol>
    <udpMaxSize value="512" /><crcLength value="2" />
    <connectionTimeout value="10000" /><clientPingFreq value="1000" />
    <logUnknownPackets value="false" />
  </protocol>
  <arena>
    <maxArenaItems value="512" /><maxArenaVehicles value="512" />
    <allowArenaCreation value="false" /><gameCheckInterval value="1000" />
    <routing><routeRange value="1000" /><routeWeaponRange value="1000" /></routing>
  </arena>
</xml>
'@ | Set-Content -LiteralPath (Join-Path $zoneRoot 'server.xml') -Encoding utf8
'<xml><scripts /></xml>' | Set-Content -LiteralPath (Join-Path $zoneRoot 'scripts.xml') -Encoding utf8
'<xml />' | Set-Content -LiteralPath (Join-Path $globalRoot 'assets.xml') -Encoding utf8
'127.0.0.1' | Set-Content -LiteralPath (Join-Path $zoneRoot 'loopback-allowlist.txt') -Encoding utf8
Write-Output 'Separate standalone diagnostic-host configuration prepared; original data unchanged; graphics loading remains pending.'
