param([switch]$Build, [switch]$Baseline)
$ErrorActionPreference = 'Stop'
if (-not $Build) { throw 'Use -Build: runtime checks must compile current source before execution.' }
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compatRoot = Join-Path $repoRoot '.tools/compatibility'
$taskRoot = Join-Path $compatRoot 'runtime-checks'
$outputRoot = Join-Path $repoRoot 'artifacts/rogue-runtime-checks'
$dotnetExe = Join-Path $repoRoot '.tools/dotnet10/dotnet.exe'
$mode = if ($Baseline) { 'baseline' } else { 'fixed' }
New-Item -ItemType Directory -Path $taskRoot, $outputRoot -Force | Out-Null
$routingConfig = Join-Path $taskRoot 'server.xml'
if ($Baseline) {
    # Historical reproduction requires the unpatched source and original generated XML.
    $routingConfig = Join-Path $compatRoot 'local-working/session-host/rogue/server.xml'
} else {
    & (Join-Path $PSScriptRoot 'SessionHost/write-session-config.ps1') -OutputPath $routingConfig
}
& (Join-Path $repoRoot 'scripts/verify-reference-assets.ps1')
$manifest = Get-Content -LiteralPath (Join-Path $compatRoot 'compatibility-manifest.json') -Raw | ConvertFrom-Json
$upstream = Join-Path $repoRoot '.tools/references/infantry-online-server'
if ((& git -C $upstream rev-parse HEAD).Trim() -ne $manifest.Commit) { throw 'Pinned upstream commit mismatch.' }
& git -C $upstream diff --quiet --no-ext-diff HEAD --
if ($LASTEXITCODE -ne 0) { throw 'Pinned upstream reference changed.' }
foreach ($patch in $manifest.Patches) {
    if ((Get-FileHash -LiteralPath (Join-Path $compatRoot $patch.Path) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $patch.DerivedSha256) { throw "Derived patch hash mismatch: $($patch.Path)" }
}
$env:DOTNET_ROOT = Split-Path $dotnetExe -Parent
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_CLI_HOME = Join-Path $compatRoot 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $compatRoot 'nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:RT_COMPATIBILITY_OFFLINE = '1'
$project = Join-Path $PSScriptRoot 'RuntimeChecks/LegacyRuntimeChecks.csproj'
$runner = Join-Path $PSScriptRoot 'RuntimeChecks/bin/Release/net10.0/LegacyRuntimeChecks.dll'
$report = Join-Path $outputRoot "$mode.json"
Push-Location $compatRoot
try {
    if ($Build) {
        & $dotnetExe build $project --configuration Release --nologo --verbosity quiet -p:UseSharedCompilation=false *> (Join-Path $taskRoot "$mode-build.log")
        if ($LASTEXITCODE -ne 0) { Get-Content -LiteralPath (Join-Path $taskRoot "$mode-build.log") -Tail 12; throw 'Runtime check build failed.' }
    }
    if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
    $arguments = @($runner, $repoRoot, $report, $mode, $routingConfig) | ForEach-Object { '"' + $_ + '"' }
    $process = Start-Process -FilePath $dotnetExe -ArgumentList $arguments -WorkingDirectory $taskRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskRoot "$mode.stdout.log") -RedirectStandardError (Join-Path $taskRoot "$mode.stderr.log") -PassThru
    if (-not $process.WaitForExit(30000)) { $process.Kill(); throw 'Owned runtime checks exceeded 30 seconds and were stopped.' }
    $process.Refresh()
    Get-Content -LiteralPath (Join-Path $taskRoot "$mode.stdout.log") -Tail 20
    Get-Content -LiteralPath (Join-Path $taskRoot "$mode.stderr.log") -Tail 12
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report)) { throw "Runtime regression failure (exit $($process.ExitCode)); inspect $taskRoot." }
    Write-Output "RUNTIME_REPORT: $report"
} finally { Pop-Location }
