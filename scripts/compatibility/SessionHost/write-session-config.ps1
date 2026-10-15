param([Parameter(Mandatory)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
# Shared NEW/provisional diagnostic config; canonical Rogue CFG/data unchanged.
@'
<xml>
  <server>
    <zoneConfig value="rogue.cfg" /><zoneName value="Rogue Trader local native check" />
    <zoneDescription value="Bounded original-content local native session; standalone no persistence" />
    <zoneIsAdvanced value="false" /><bindIP value="127.0.0.1" /><bindPort value="21337" />
    <maxPlayers value="1" /><gameType value="" /><loadBlobs value="true" />
    <updateGlobalNws value="" /><copyServerFrom value="" />
    <pathFindingEnabled value="false" /><pathFindingEnabledNew value="false" />
    <ipAllowlistFile value="loopback-allowlist.txt" /><logPackets value="false" />
  </server>
  <database><connectionDelay value="0" /></database><assets><location value="assets" /></assets>
  <protocol><udpMaxSize value="512" /><crcLength value="2" /><connectionTimeout value="10000" />
    <clientPingFreq value="1000" /><logUnknownPackets value="false" /></protocol>
  <arena><maxArenaItems value="512" /><maxArenaVehicles value="512" /><allowArenaCreation value="false" />
    <gameCheckInterval value="1000" />
    <!-- NEW/provisional diagnostic routing only; original Rogue CFG and gameplay values remain unchanged. -->
    <routing><routeRange value="1000" /><routeWeaponRange value="1000" />
      <routeRadarRange value="2000" /><routeRadarRangeFactor value="2" />
      <routeRadarRangeFar value="4000" /><routeRadarRangeFarFactor value="4" /></routing></arena>
</xml>
'@ | Set-Content -LiteralPath $OutputPath -Encoding utf8
