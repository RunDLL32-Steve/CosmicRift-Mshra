[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content -LiteralPath (Join-Path $repoRoot 'docs/original-research/ASSET_MANIFEST.json') -Raw | ConvertFrom-Json
$referenceRoot = Join-Path $repoRoot '.tools/references/cosmic-rift-assets'
$previousSkipSmudge = $env:GIT_LFS_SKIP_SMUDGE
$env:GIT_LFS_SKIP_SMUDGE = '1'
try {
    if (-not (Test-Path -LiteralPath $referenceRoot)) {
        # Disable newline conversion so even original text assets retain exact bytes.
        git -c core.autocrlf=false clone --no-checkout "$($manifest.canonicalSource.url).git" $referenceRoot
        if ($LASTEXITCODE -ne 0) { throw 'Reference clone failed.' }
        git -C $referenceRoot config core.autocrlf false
        git -C $referenceRoot config core.eol lf
        git -C $referenceRoot checkout $manifest.canonicalSource.commit
        if ($LASTEXITCODE -ne 0) { throw 'Pinned reference checkout failed.' }
    }
    $commit = git -C $referenceRoot rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $commit -ne $manifest.canonicalSource.commit) { throw 'Existing reference uses a different commit; no files were reset.' }
    $dirty = git -C $referenceRoot ls-files --others --exclude-standard
    if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Existing reference has changes; no files were overwritten.' }
    git -C $referenceRoot diff --quiet --no-ext-diff HEAD --
    if ($LASTEXITCODE -ne 0) { throw 'Existing reference has tracked changes; no originals were overwritten.' }
    $include = ($manifest.assets | Where-Object gitLfs | ForEach-Object originalPath) -join ','
    git -C $referenceRoot lfs pull "--include=$include" '--exclude='
    if ($LASTEXITCODE -ne 0) { throw 'Original Git LFS retrieval failed.' }
} finally { $env:GIT_LFS_SKIP_SMUDGE = $previousSkipSmudge }
& (Join-Path $PSScriptRoot 'verify-reference-assets.ps1')

