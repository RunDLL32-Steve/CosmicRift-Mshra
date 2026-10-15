param([switch]$Build)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compatRoot = Join-Path $repoRoot '.tools/compatibility'
$dotnetExe = Join-Path $repoRoot '.tools/dotnet10/dotnet.exe'
$projectPath = Join-Path $PSScriptRoot 'Loader/LegacyCompatibilityLoader.csproj'
$runnerDll = Join-Path $PSScriptRoot 'Loader/bin/Release/net10.0/LegacyCompatibilityLoader.dll'
$env:DOTNET_ROOT = Split-Path $dotnetExe -Parent
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_CLI_HOME = Join-Path $compatRoot 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $compatRoot 'nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
Push-Location $compatRoot
try {
    if ($Build) {
        & $dotnetExe build $projectPath --configuration Release --nologo --verbosity quiet -p:UseSharedCompilation=false *> (Join-Path $compatRoot 'logs/data-loader-build.log')
        if ($LASTEXITCODE -ne 0) { throw 'Loader build failed; see data-loader-build.log.' }
    }
    $ownedProcess = Start-Process -FilePath $dotnetExe -ArgumentList @($runnerDll, (Join-Path $compatRoot 'working-zones/rogue-pristine/assets'), (Join-Path $compatRoot 'reports'), (Join-Path $compatRoot 'server-working')) -WorkingDirectory $compatRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $compatRoot 'logs/data-loader.stdout.log') -RedirectStandardError (Join-Path $compatRoot 'logs/data-loader.stderr.log') -PassThru
    $null = $ownedProcess.Handle  # PS 5.1: cache the handle so ExitCode is populated after WaitForExit
    if (-not $ownedProcess.WaitForExit(30000)) { $ownedProcess.Kill(); throw 'Owned parser exceeded 30 seconds and was stopped.' }
    $ownedProcess.Refresh()
    Write-Output "DataLoaderPid=$($ownedProcess.Id); ControlledExitCode=$($ownedProcess.ExitCode)"
    Get-Content -LiteralPath (Join-Path $compatRoot 'logs/data-loader.stdout.log')
    Get-Content -LiteralPath (Join-Path $compatRoot 'logs/data-loader.stderr.log')
    if ($ownedProcess.ExitCode -ne 0) { throw 'Controlled parser failure; inspect captured logs.' }
} finally { Pop-Location }
