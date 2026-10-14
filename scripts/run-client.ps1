[CmdletBinding()]
param([switch]$NoBuild)
. (Join-Path $PSScriptRoot 'environment.ps1')
Push-Location $repoRoot
try {
    if (-not $NoBuild) {
        & $dotnetExe build client/RogueTrader.Client.csproj --configuration Debug --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Client build failed.' }
        & $godotExe --headless --path client --editor --import --quit
        if ($LASTEXITCODE -ne 0) { throw 'Godot import failed.' }
    }
    & $godotExe --path client
    if ($LASTEXITCODE -ne 0) { throw 'Game exited with an error.' }
} finally { Pop-Location }
