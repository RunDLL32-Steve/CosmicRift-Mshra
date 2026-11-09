[CmdletBinding()]
param([switch]$NoBuild)
. (Join-Path $PSScriptRoot 'environment.ps1')
Push-Location $repoRoot
try {
    $levelArt = Join-Path $repoRoot 'client/OriginalContent/level/level.json'
    if (-not (Test-Path -LiteralPath $levelArt) -and (Test-Path -LiteralPath (Join-Path $repoRoot '.tools/references/cosmic-rift-assets/LVL/Rogue.lvl'))) {
        # Original map art (asteroids, stations, walls); without it the game draws its plain NEW tiles.
        & python (Join-Path $PSScriptRoot 'original-assets/stage_level_art.py')
        if ($LASTEXITCODE -ne 0) { Write-Warning 'Original map art was not staged; run scripts/sync-reference-assets.ps1, then try again.' }
    }
    $uiArt = Join-Path $repoRoot 'client/OriginalContent/ui/ui.json'
    if (-not (Test-Path -LiteralPath $uiArt) -and (Test-Path -LiteralPath (Join-Path $repoRoot '.tools/references/cosmic-rift-assets/BLO/cruiart.blo'))) {
        # Original radar frame (CFG [Uiart] RadarBorder); without it the radar keeps its plain NEW frame.
        & python (Join-Path $PSScriptRoot 'original-assets/stage_ui_art.py')
        if ($LASTEXITCODE -ne 0) { Write-Warning 'Original radar frame was not staged; run scripts/sync-reference-assets.ps1, then try again.' }
    }
    if (-not $NoBuild) {
        & $dotnetExe build client/RogueTrader.Client.csproj --configuration Debug --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Client build failed.' }
        & $godotExe --headless --path client --editor --import --quit
        if ($LASTEXITCODE -ne 0) { throw 'Godot import failed.' }
    }
    & $godotExe --path client
    if ($LASTEXITCODE -ne 0) { throw 'Game exited with an error.' }
} finally { Pop-Location }
