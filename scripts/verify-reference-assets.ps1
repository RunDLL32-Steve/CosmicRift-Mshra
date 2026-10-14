[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content -LiteralPath (Join-Path $repoRoot 'docs/original-research/ASSET_MANIFEST.json') -Raw | ConvertFrom-Json
$referenceRoot = Join-Path $repoRoot '.tools/references/cosmic-rift-assets'
if (-not (Test-Path -LiteralPath $referenceRoot)) { throw 'Run scripts/sync-reference-assets.ps1 to acquire the canonical reference.' }
$commit = git -C $referenceRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $commit -ne $manifest.canonicalSource.commit) { throw 'Reference commit does not match the canonical manifest.' }
$dirty = git -C $referenceRoot ls-files --others --exclude-standard
if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Reference checkout has untracked files. Preserve it and investigate; do not reset or overwrite originals.' }
git -C $referenceRoot diff --quiet --no-ext-diff HEAD --
if ($LASTEXITCODE -ne 0) { throw 'Tracked reference files differ from the canonical commit.' }
$failures = [Collections.Generic.List[string]]::new()
foreach ($asset in $manifest.assets) {
    $path = Join-Path $referenceRoot $asset.originalPath
    if (-not (Test-Path -LiteralPath $path)) { $failures.Add("Missing original: $($asset.originalPath)"); continue }
    $length = (Get-Item -LiteralPath $path).Length
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($length -ne $asset.originalSizeBytes -or $hash -ne $asset.originalSha256) {
        $failures.Add("Original bytes differ or LFS payload not retrieved: $($asset.originalPath)")
    }
}
if ($failures.Count) { throw ($failures -join [Environment]::NewLine) }
Write-Host "REFERENCE_INTEGRITY_PASS: $($manifest.assets.Count) originals match raw SHA-256 and byte length at $commit."

