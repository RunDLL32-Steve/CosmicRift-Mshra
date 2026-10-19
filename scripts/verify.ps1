[CmdletBinding()]
param([switch]$SkipClientSmoke)
. (Join-Path $PSScriptRoot 'environment.ps1')
Push-Location $repoRoot
try {
    if (-not (Test-Path -LiteralPath 'node_modules/prettier')) { npm ci --no-audit --no-fund; if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' } }
    python scripts/format-check.py
    if ($LASTEXITCODE -ne 0) { throw 'Prettier check failed: run python scripts/format-check.py --fix and commit.' }
    & $dotnetExe build RogueTrader2.sln --configuration Debug --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }
    & $dotnetExe test tests/Unit/RogueTrader.UnitTests.csproj --configuration Debug --no-build --nologo --logger 'trx;LogFileName=unit.trx' --results-directory artifacts/test-results
    if ($LASTEXITCODE -ne 0) { throw 'Unit tests failed.' }
    if (-not $SkipClientSmoke) {
        & $godotExe --headless --path client --editor --import --quit
        if ($LASTEXITCODE -ne 0) { throw 'Godot project import failed.' }
        & $godotExe --headless --path client -- --smoke-test
        if ($LASTEXITCODE -ne 0) { throw 'Godot client smoke test failed.' }
    }
} finally { Pop-Location }
