$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compatRoot = Join-Path $repoRoot '.tools/compatibility'
$referenceRoot = Join-Path $repoRoot '.tools/references/infantry-online-server'
$workingRoot = Join-Path $compatRoot 'server-working'
$manifestPath = Join-Path $compatRoot 'compatibility-manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$referenceCommit = & git -C $referenceRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $referenceCommit.Trim() -ne $manifest.Commit) { throw 'Pinned reference commit mismatch.' }

function Write-DerivedPatch([string]$relativePath, [string]$purpose, [scriptblock]$transform) {
    $originalPath = Join-Path $referenceRoot $relativePath
    $derivedPath = Join-Path $workingRoot $relativePath
    $originalText = [IO.File]::ReadAllText($originalPath)
    $patchedText = & $transform $originalText
    if ($patchedText -eq $originalText) { throw "Patch did not change $relativePath" }
    [IO.File]::WriteAllText($derivedPath, $patchedText, [Text.UTF8Encoding]::new($false))
    $entry = [pscustomobject]@{
        Path = "server-working/$relativePath"
        OriginalPath = $relativePath
        OriginalSha256 = (Get-FileHash -LiteralPath $originalPath -Algorithm SHA256).Hash.ToLowerInvariant()
        DerivedSha256 = (Get-FileHash -LiteralPath $derivedPath -Algorithm SHA256).Hash.ToLowerInvariant()
        Purpose = $purpose
    }
    $manifest.Patches = @($manifest.Patches | Where-Object Path -ne $entry.Path) + $entry
}

Write-DerivedPatch 'dotnetcore/ZoneServer/Game/ZoneServer.cs' 'Compatibility only: bind both sockets to IPv4 loopback, independent of upstream wildcard/config.' {
    param($text)
    if ([regex]::Matches($text, 'IPAddress.Parse\("0.0.0.0"\)').Count -ne 2) { throw 'Expected exactly two original wildcard bind sites.' }
    $text.Replace('IPAddress.Parse("0.0.0.0")', 'IPAddress.Loopback')
}

Write-DerivedPatch 'dotnetcore/ZoneServer/Game/Assets/AssetManager.cs' 'Compatibility only: fail closed for remote news/directory methods when mandatory offline environment flag is set.' {
    param($text)
    $newsPattern = '(public bool grabGlobalNews\(string fileUrl, string fileLocation\)\s*\{)'
    $directoryPattern = '(private void sendDataToDirectory\(\)\s*\{)'
    if ([regex]::Matches($text, $newsPattern).Count -ne 1 -or [regex]::Matches($text, $directoryPattern).Count -ne 1) { throw 'Expected original outbound method signatures.' }
    $text = [regex]::Replace($text, $newsPattern, '$1' + "`n            if (Environment.GetEnvironmentVariable(`"RT_COMPATIBILITY_OFFLINE`") == `"1`") return false;")
    [regex]::Replace($text, $directoryPattern, '$1' + "`n            if (Environment.GetEnvironmentVariable(`"RT_COMPATIBILITY_OFFLINE`") == `"1`") return;")
}

$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8
Write-Output 'Derived-only loopback/offline source patches recorded with original and working SHA-256.'
