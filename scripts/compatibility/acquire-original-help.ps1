$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compatRoot = Join-Path $repoRoot '.tools/compatibility'
$referenceRoot = Join-Path $compatRoot 'references/rogue-original-help'
$workingRoot = Join-Path $compatRoot 'working-zones/rogue-pristine/assets'
$commit = '401633b9b6829d9e5ae56b76a2563b1b95bbe4b9'
$archivePath = 'Incompatible%20Zone%20Archives/SilentClientCrash/rogue'
New-Item -ItemType Directory -Path $referenceRoot -Force | Out-Null
$entries = foreach ($filename in @('keys.txt','infcmdlst.txt','comms.txt','rules.txt','mods.txt')) {
    $url = "https://raw.githubusercontent.com/InfantryOnline/Zone-Assets/$commit/$archivePath/$filename"
    $sourcePath = Join-Path $referenceRoot $filename
    if (-not (Test-Path -LiteralPath $sourcePath)) { Invoke-WebRequest -Uri $url -OutFile $sourcePath }
    Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $workingRoot $filename) -Force
    $sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $workingHash = (Get-FileHash -LiteralPath (Join-Path $workingRoot $filename) -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($sourceHash -ne $workingHash) { throw "Original help copy differs: $filename" }
    [pscustomobject]@{Repository='https://github.com/InfantryOnline/Zone-Assets';Commit=$commit;Url=$url;Filename=$filename;Sha256=$sourceHash;Bytes=(Get-Item -LiteralPath $sourcePath).Length;OriginalRelativePath="references/rogue-original-help/$filename";WorkingRelativePath="working-zones/rogue-pristine/assets/$filename";Modified=$false}
}
$manifestPath = Join-Path $compatRoot 'compatibility-manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$manifest | Add-Member -NotePropertyName SupplementalOriginalDependencies -NotePropertyValue @($entries) -Force
$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8
$entries | Select-Object Filename, Bytes, Sha256
