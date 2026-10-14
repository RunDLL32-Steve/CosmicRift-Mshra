param([switch]$Build, [switch]$Start, [ValidateRange(10,120)][int]$DurationSeconds=120)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compatRoot = Join-Path $repoRoot '.tools/compatibility'
$taskRoot = Join-Path $compatRoot 'local-working/session-host'
$zoneRoot = Join-Path $taskRoot 'rogue'
$assetRoot = Join-Path $zoneRoot 'assets'
$referenceRoot = Join-Path $repoRoot '.tools/references/cosmic-rift-assets'
$sourceZone = Join-Path $compatRoot 'working-zones/rogue-pristine'
$reportRoot = Join-Path $taskRoot 'reports'
$logRoot = Join-Path $taskRoot 'logs'
$dotnetExe = Join-Path $repoRoot '.tools/dotnet10/dotnet.exe'
$project = Join-Path $PSScriptRoot 'SessionHost/LegacySessionHost.csproj'
$probeDll = Join-Path $PSScriptRoot 'SessionHost/bin/Release/net10.0/LegacySessionHost.dll'
# Fail closed before backend launch if required derived loopback/offline/parser patches changed.
$manifest = Get-Content -LiteralPath (Join-Path $compatRoot 'compatibility-manifest.json') -Raw | ConvertFrom-Json
foreach ($required in @('server-working/dotnetcore/ZoneServer/Game/ZoneServer.cs', 'server-working/dotnetcore/ZoneServer/Game/Assets/AssetManager.cs', 'server-working/dotnetcore/Assets/Itm/ItmInfo.VehicleMaker.cs', 'server-working/dotnetcore/ZoneServer/Logic/Packets/Login/Login.cs')) {
    $patch = @($manifest.Patches | Where-Object Path -eq $required)
    if ($patch.Count -ne 1 -or (Get-FileHash -LiteralPath (Join-Path $compatRoot $required) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $patch[0].DerivedSha256) { throw "Required derived patch provenance mismatch: $required" }
}
if (Test-Path -LiteralPath (Join-Path $taskRoot 'owned-process.json')) {
    $previous = Get-Content -LiteralPath (Join-Path $taskRoot 'owned-process.json') -Raw | ConvertFrom-Json
    if (Get-Process -Id $previous.ProcessId -ErrorAction SilentlyContinue) { throw 'Previously recorded helper PID is still running; stop that owned session before restaging.' }
}
& (Join-Path $repoRoot 'scripts/verify-reference-assets.ps1')
if ((& git -C $referenceRoot rev-parse HEAD).Trim() -ne 'eb1514eaae7f9b9425307d9cc043c74ecdd15e35') { throw 'Canonical commit mismatch.' }
New-Item -ItemType Directory -Path $assetRoot, (Join-Path $zoneRoot 'scripts'), (Join-Path $taskRoot 'Global'), $reportRoot, $logRoot -Force | Out-Null
$copied = @()
function Copy-Unchanged([string]$sourcePath) {
    $destination = Join-Path $assetRoot (Split-Path $sourcePath -Leaf)
    $sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $sourcePath -Destination $destination -Force
    if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $sourceHash) { throw "Byte mismatch copying $sourcePath" }
    $script:copied += [pscustomobject]@{Name=(Split-Path $sourcePath -Leaf); Sha256=$sourceHash.ToLowerInvariant(); Source=$sourcePath}
}
# Seven proven canonical core records plus five separately pinned original help files.
Get-ChildItem -LiteralPath (Join-Path $sourceZone 'assets') -File | ForEach-Object { Copy-Unchanged $_.FullName }
$blobs = @(Get-ChildItem -LiteralPath (Join-Path $referenceRoot 'BLO') -Filter '*.blo' -File)
if ($blobs.Count -ne 89) { throw "Expected 89 canonical BLO files, found $($blobs.Count)." }
foreach ($blob in $blobs) {
    $pointer = (& git -C $referenceRoot show "HEAD:BLO/$($blob.Name)") -join "`n"
    $oid = [regex]::Match($pointer, 'oid sha256:([0-9a-f]{64})').Groups[1].Value
    if ($oid.Length -ne 64 -or (Get-FileHash -LiteralPath $blob.FullName -Algorithm SHA256).Hash.ToLowerInvariant() -ne $oid) { throw "Canonical BLO LFS mismatch: $($blob.Name)" }
    Copy-Unchanged $blob.FullName
}
# Shared engine dependency, separate from the 89 immutable canonical Rogue archives.
$sharedSoccer = Join-Path $repoRoot '.tools/native-client-inspection/native-generic-support/$_0_/soccer.blo'
if ((Get-FileHash -LiteralPath $sharedSoccer -Algorithm SHA256).Hash.ToLowerInvariant() -ne '93ec29ba6ea7618b6f298253fa75ef397ec934911231370d961a960f7d050784') { throw 'Authentic shared soccer.blo provenance mismatch.' }
Copy-Unchanged $sharedSoccer
$copied | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskRoot 'copied-source-hashes.json') -Encoding utf8
# NEW local diagnostic host config. Original Rogue CFG/data names/values never edited.
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
    <gameCheckInterval value="1000" /><routing><routeRange value="1000" /><routeWeaponRange value="1000" /></routing></arena>
</xml>
'@ | Set-Content -LiteralPath (Join-Path $zoneRoot 'server.xml') -Encoding utf8
'<xml><scripts /></xml>' | Set-Content -LiteralPath (Join-Path $zoneRoot 'scripts.xml') -Encoding utf8
'127.0.0.1' | Set-Content -LiteralPath (Join-Path $zoneRoot 'loopback-allowlist.txt') -Encoding utf8
'<xml />' | Set-Content -LiteralPath (Join-Path $taskRoot 'Global/assets.xml') -Encoding utf8
$env:DOTNET_ROOT = Split-Path $dotnetExe -Parent
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_CLI_HOME = Join-Path $compatRoot 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $compatRoot 'nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:RT_COMPATIBILITY_OFFLINE = '1'
Push-Location $compatRoot
try {
    if ($Build) {
        & $dotnetExe build $project --configuration Release --nologo --verbosity quiet -p:UseSharedCompilation=false *> (Join-Path $logRoot 'build.log')
        if ($LASTEXITCODE -ne 0) { throw 'SessionHost build failed; inspect local-working/session-host/logs/build.log.' }
    }
    if ($Start) {
        # Starts backend only. Native UI launch remains separate under its authorization.
        $stopPath = Join-Path $taskRoot 'stop.request'
        foreach ($stale in @($stopPath, (Join-Path $reportRoot 'ready.json'), (Join-Path $reportRoot 'exit.json'))) {
            if (Test-Path -LiteralPath $stale) { Remove-Item -LiteralPath $stale }
        }
        $owned = Start-Process -FilePath $dotnetExe -ArgumentList @($probeDll, $reportRoot, $stopPath, $DurationSeconds) -WorkingDirectory $zoneRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logRoot 'host.stdout.log') -RedirectStandardError (Join-Path $logRoot 'host.stderr.log') -PassThru
        $deadline = [DateTime]::UtcNow.AddSeconds(20)
        while (-not (Test-Path -LiteralPath (Join-Path $reportRoot 'ready.json')) -and -not $owned.HasExited -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 200; $owned.Refresh() }
        if ($owned.HasExited) { Get-Content -LiteralPath (Join-Path $logRoot 'host.stderr.log') -Tail 12; throw "Controlled session startup exit $($owned.ExitCode)." }
        if (-not (Test-Path -LiteralPath (Join-Path $reportRoot 'ready.json'))) { $owned.Kill(); throw 'Owned session startup exceeded readiness deadline and was stopped.' }
        [pscustomobject]@{ProcessId=$owned.Id; BackendReady=$true; DurationLimitSeconds=$DurationSeconds; ReadyReport=(Join-Path $reportRoot 'ready.json'); ExitReport=(Join-Path $reportRoot 'exit.json'); StopMarker=$stopPath; NativeUiLaunched=$false} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'owned-process.json') -Encoding utf8
        Get-Content -LiteralPath (Join-Path $taskRoot 'owned-process.json')
    } else { Write-Output 'SESSION_HOST_PREPARED: 12 original core/help files + 89 canonical BLO copies + authentic shared soccer.blo; no server or native process started.' }
} finally { Pop-Location }
