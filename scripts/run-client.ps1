[CmdletBinding()]
param([switch]$NoBuild)
. (Join-Path $PSScriptRoot 'environment.ps1')
Push-Location $repoRoot
try {
    $levelArt = Join-Path $repoRoot 'client/OriginalContent/level/level.json'
    # Re-stage when the staged copy is missing or predates schema 3 (the full Gibbed entity fields: frame timing and Value).
    $levelArtCurrent = (Test-Path -LiteralPath $levelArt) -and (Select-String -LiteralPath $levelArt -Pattern '"schemaVersion":3,' -SimpleMatch -Quiet)
    if (-not $levelArtCurrent -and (Test-Path -LiteralPath (Join-Path $repoRoot '.tools/references/cosmic-rift-assets/LVL/Rogue.lvl'))) {
        # Original map art (asteroids, stations, walls); without it the game draws its plain NEW tiles.
        & python (Join-Path $PSScriptRoot 'original-assets/stage_level_art.py')
        if ($LASTEXITCODE -ne 0) { Write-Warning 'Original map art was not staged; run scripts/sync-reference-assets.ps1, then try again.' }
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
