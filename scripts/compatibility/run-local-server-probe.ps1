param([switch]$Build)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compatRoot = Join-Path $repoRoot '.tools/compatibility'
$dotnetExe = Join-Path $repoRoot '.tools/dotnet10/dotnet.exe'
$projectPath = Join-Path $PSScriptRoot 'ServerProbe/LegacyServerProbe.csproj'
$probeDll = Join-Path $PSScriptRoot 'ServerProbe/bin/Release/net10.0/LegacyServerProbe.dll'
$env:DOTNET_ROOT = Split-Path $dotnetExe -Parent
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_CLI_HOME = Join-Path $compatRoot 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $compatRoot 'nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:RT_COMPATIBILITY_OFFLINE = '1'
& (Join-Path $PSScriptRoot 'prepare-derived-source.ps1')
& (Join-Path $PSScriptRoot 'prepare-local-zone.ps1')
Push-Location $compatRoot
try {
    if ($Build) {
        & $dotnetExe build $projectPath --configuration Release --nologo --verbosity quiet -p:UseSharedCompilation=false *> (Join-Path $compatRoot 'logs/server-probe-build.log')
        if ($LASTEXITCODE -ne 0) { throw 'Probe build failed; see server-probe-build.log.' }
    }
    $reportPath = Join-Path $compatRoot 'reports/local-server-probe.json'
    $ownedProcess = Start-Process -FilePath $dotnetExe -ArgumentList @($probeDll, $reportPath) -WorkingDirectory (Join-Path $compatRoot 'working-zones/rogue-pristine') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $compatRoot 'logs/server-probe.stdout.log') -RedirectStandardError (Join-Path $compatRoot 'logs/server-probe.stderr.log') -PassThru
    if (-not $ownedProcess.WaitForExit(30000)) {
        $ownedProcess.Kill()
        throw 'Owned compatibility probe exceeded 30 seconds and was stopped.'
    }
    $ownedProcess.Refresh()
    [pscustomobject]@{
        Utc = [DateTime]::UtcNow.ToString('O')
        ProcessId = $ownedProcess.Id
        ControlledExitCode = $ownedProcess.ExitCode
        ReportExists = ($ownedProcess.ExitCode -eq 0 -and (Test-Path -LiteralPath $reportPath))
        Stdout = 'logs/server-probe.stdout.log'
        Stderr = 'logs/server-probe.stderr.log'
        NoPersistentServerRequested = $true
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $compatRoot 'reports/local-server-probe-result.json') -Encoding utf8
    Write-Output "ServerProbePid=$($ownedProcess.Id); ControlledExitCode=$($ownedProcess.ExitCode)"
    Get-Content -LiteralPath (Join-Path $compatRoot 'logs/server-probe.stdout.log') -Tail 18
    Get-Content -LiteralPath (Join-Path $compatRoot 'logs/server-probe.stderr.log') -Tail 10
    if ($ownedProcess.ExitCode -ne 0) { throw 'Controlled server compatibility failure; inspect captured logs.' }
    Get-Content -LiteralPath $reportPath
} finally { Pop-Location }
