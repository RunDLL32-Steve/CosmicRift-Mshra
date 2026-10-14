$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $repoRoot 'toolchain.lock.json') -Raw | ConvertFrom-Json
$localDotnetRoot = Join-Path $repoRoot '.tools/dotnet'
$dotnetExe = Join-Path $localDotnetRoot 'dotnet.exe'
$godotExe = Join-Path $repoRoot ".tools/godot/Godot_v$($lock.godotVersion)-stable_mono_win64/Godot_v$($lock.godotVersion)-stable_mono_win64_console.exe"
if (-not (Test-Path -LiteralPath $dotnetExe)) { throw 'Run scripts/bootstrap.ps1 to install the pinned portable SDK.' }
if (-not (Test-Path -LiteralPath $godotExe)) { throw 'Run scripts/bootstrap.ps1 to install the pinned Godot .NET build.' }
$env:DOTNET_ROOT = $localDotnetRoot
$env:DOTNET_ROOT_X64 = $localDotnetRoot
$env:PATH = "$localDotnetRoot;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.tools/dotnet-home'
$env:NUGET_PACKAGES = Join-Path $repoRoot '.tools/nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
# Godot editor settings, game logs and key bindings remain portable and writable.
$env:APPDATA = Join-Path $repoRoot '.tools/user-profile/AppData/Roaming'
$env:LOCALAPPDATA = Join-Path $repoRoot '.tools/user-profile/AppData/Local'
New-Item -ItemType Directory -Force -Path $env:APPDATA,$env:LOCALAPPDATA | Out-Null
