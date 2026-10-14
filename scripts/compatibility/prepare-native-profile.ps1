param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$nativeRoot = Join-Path $repoRoot '.tools/native-client-inspection'
$sourceClient = Join-Path $nativeRoot 'local-working/client'
$originalExe = Join-Path $sourceClient 'infantry.exe'
if ((Get-FileHash -LiteralPath $originalExe -Algorithm SHA256).Hash.ToLowerInvariant() -ne '33d97aab9ffad1ff62252cfa0bf1acdf091b71da246460bfe74f55e3e901b1ba') { throw 'Original engine hash differs from verified extracted executable.' }
$namespace = 'RTTest' + [Guid]::NewGuid().ToString('N').Substring(0,7)
$privateKey = "Registry::HKEY_CURRENT_USER\Software\$namespace"
if (Test-Path -LiteralPath $privateKey) { throw 'Unique test namespace unexpectedly exists.' }
$runDirectory = Join-Path $nativeRoot ('isolated-runtime/' + $namespace)
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
Get-ChildItem -LiteralPath $sourceClient -Force | Copy-Item -Destination $runDirectory -Recurse -Force
$bytes = [IO.File]::ReadAllBytes($originalExe)
$replacement = [Text.Encoding]::ASCII.GetBytes($namespace)
$originalLiteral = [Text.Encoding]::ASCII.GetBytes('HarmlessGames')
$offsets = @(1127917,1128113)
foreach ($offset in $offsets) {
    for ($i=0; $i -lt 13; $i++) { if ($bytes[$offset+$i] -ne $originalLiteral[$i]) { throw 'Native registry literal differs from inspected offset.' } }
    [Array]::Copy($replacement,0,$bytes,$offset,13)
}
$latin = [Text.Encoding]::Latin1.GetString($bytes)
if ($latin.Contains('HarmlessGames')) { throw 'Unmapped original game registry namespace remains.' }
$derivedExe = Join-Path $runDirectory 'infantry.exe'
[IO.File]::WriteAllBytes($derivedExe,$bytes)
$wrapperIni = Join-Path $runDirectory 'ddraw.ini'
$wrapperText = Get-Content -LiteralPath $wrapperIni -Raw
$wrapperText = $wrapperText.Replace('windowed=false','windowed=true').Replace('devmode=false','devmode=true').Replace('savesettings=1','savesettings=0')
Set-Content -LiteralPath $wrapperIni -Value $wrapperText -Encoding ascii
$report = [ordered]@{
    OriginalExecutableSha256 = '33d97aab9ffad1ff62252cfa0bf1acdf091b71da246460bfe74f55e3e901b1ba'
    DerivedExecutableSha256 = (Get-FileHash -LiteralPath $derivedExe -Algorithm SHA256).Hash.ToLowerInvariant()
    ModifiedOffsets = $offsets
    ModificationScope = 'Two same-length data-section registry literals only; original engine copy unchanged.'
    RunDirectory = $runDirectory
    Executable = $derivedExe
    PrivateRegistryNamespace = $privateKey
    Namespace = $namespace
    PrivateNamespaceAbsentBefore = $true
    ExistingProfilesWritten = $false
    UserApprovedNativeTest = $true
    DirectoriesBlank = $true
    NativeProcessStarted = $false
    ProfilePreparationComplete = $false
}
$reportPath = Join-Path $nativeRoot 'isolated-run-preparation.json'
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
for ($profile=0; $profile -lt 6; $profile++) {
    $optionsKey = "$privateKey\Infantry\Profile$profile\Options"
    New-Item -Path $optionsKey -Force | Out-Null
    New-ItemProperty -LiteralPath $optionsKey -Name SDirectoryAddress -Value '' -PropertyType String -Force | Out-Null
    New-ItemProperty -LiteralPath $optionsKey -Name SDirectoryAddressBackup -Value '' -PropertyType String -Force | Out-Null
}
$report.ProfilePreparationComplete = $true
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Write-Output "PRIVATE_NATIVE_PROFILE_READY: $runDirectory"
