param([switch]$Build)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compatRoot = Join-Path $repoRoot '.tools/compatibility'
$taskRoot = Join-Path $compatRoot 'arena-working'
$zoneRoot = Join-Path $taskRoot 'rogue'
$sourceZone = Join-Path $compatRoot 'working-zones/rogue-pristine'
$dotnetExe = Join-Path $repoRoot '.tools/dotnet10/dotnet.exe'
New-Item -ItemType Directory -Path (Join-Path $zoneRoot 'assets'), (Join-Path $zoneRoot 'scripts'), (Join-Path $taskRoot 'Global'), (Join-Path $compatRoot 'logs'), (Join-Path $compatRoot 'reports') -Force | Out-Null
# Separate copies only. Existing source-zone originals must have recorded hash provenance.
$hashes = @()
Get-ChildItem -LiteralPath (Join-Path $sourceZone 'assets') -File | ForEach-Object {
    $target = Join-Path (Join-Path $zoneRoot 'assets') $_.Name
    Copy-Item -LiteralPath $_.FullName -Destination $target -Force
    $sourceHash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $sourceHash) { throw "Original copy mismatch: $($_.Name)" }
    $hashes += [pscustomobject]@{Name=$_.Name; Sha256=$sourceHash}
}
$hashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'copied-source-hashes.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $sourceZone 'server.xml') -Destination (Join-Path $zoneRoot 'server.xml') -Force
Copy-Item -LiteralPath (Join-Path $sourceZone 'scripts.xml') -Destination (Join-Path $zoneRoot 'scripts.xml') -Force
Copy-Item -LiteralPath (Join-Path $sourceZone 'loopback-allowlist.txt') -Destination (Join-Path $zoneRoot 'loopback-allowlist.txt') -Force
'<xml />' | Set-Content -LiteralPath (Join-Path $taskRoot 'Global/assets.xml') -Encoding utf8
$env:DOTNET_ROOT = Split-Path $dotnetExe -Parent
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_CLI_HOME = Join-Path $compatRoot 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $compatRoot 'nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:RT_COMPATIBILITY_OFFLINE = '1'
$projectPath = Join-Path $PSScriptRoot 'ArenaProbe/LegacyArenaProbe.csproj'
$probeDll = Join-Path $PSScriptRoot 'ArenaProbe/bin/Release/net10.0/LegacyArenaProbe.dll'
Push-Location $compatRoot
try {
    if ($Build) {
        & $dotnetExe build $projectPath --configuration Release --nologo --verbosity quiet -p:UseSharedCompilation=false *> (Join-Path $compatRoot 'logs/arena-probe-build.log')
        if ($LASTEXITCODE -ne 0) { throw 'Arena probe build failed; see arena-probe-build.log.' }
    }
    $report = Join-Path $compatRoot 'reports/original-arena-probe.json'
    $process = Start-Process -FilePath $dotnetExe -ArgumentList @($probeDll, $report) -WorkingDirectory $zoneRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $compatRoot 'logs/arena-probe.stdout.log') -RedirectStandardError (Join-Path $compatRoot 'logs/arena-probe.stderr.log') -PassThru
    if (-not $process.WaitForExit(30000)) { $process.Kill(); throw 'Owned arena probe exceeded 30 seconds and was stopped.' }
    $process.Refresh()
    [pscustomobject]@{Utc=[DateTime]::UtcNow.ToString('O'); ProcessId=$process.Id; ControlledExitCode=$process.ExitCode; ReportExists=($process.ExitCode -eq 0 -and (Test-Path -LiteralPath $report)); NoBeginOrPollCalls=$true; NoRealClient=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $compatRoot 'reports/original-arena-probe-result.json') -Encoding utf8
    Get-Content -LiteralPath (Join-Path $compatRoot 'logs/arena-probe.stdout.log') -Tail 8
    Get-Content -LiteralPath (Join-Path $compatRoot 'logs/arena-probe.stderr.log') -Tail 12
    if ($process.ExitCode -ne 0) { throw "Controlled arena failure exit $($process.ExitCode); inspect captured logs." }
    Get-Content -LiteralPath $report
} finally { Pop-Location }
