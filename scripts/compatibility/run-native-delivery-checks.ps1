param([ValidateSet('baseline','fixed')][string]$Mode='fixed')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compatRoot = Join-Path $repoRoot '.tools/compatibility'
$taskRoot = Join-Path $compatRoot 'native-delivery-checks'
$outputRoot = Join-Path $repoRoot 'artifacts/native-delivery-checks'
$dotnetExe = Join-Path $repoRoot '.tools/dotnet10/dotnet.exe'
New-Item -ItemType Directory -Path $taskRoot,$outputRoot -Force | Out-Null
& (Join-Path $repoRoot 'scripts/verify-reference-assets.ps1')
$manifest = Get-Content -LiteralPath (Join-Path $compatRoot 'compatibility-manifest.json') -Raw | ConvertFrom-Json
$reference = Join-Path $repoRoot '.tools/references/infantry-online-server'
if ((& git -C $reference rev-parse HEAD).Trim() -ne $manifest.Commit) { throw 'Pinned upstream commit mismatch.' }
& git -C $reference diff --quiet --no-ext-diff HEAD --
if ($LASTEXITCODE -ne 0) { throw 'Pinned upstream files changed.' }
foreach ($patch in $manifest.Patches) {
    if ((Get-FileHash -LiteralPath (Join-Path $compatRoot $patch.Path) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $patch.DerivedSha256) { throw "Derived source provenance mismatch: $($patch.Path)" }
}
$packetRelative = 'dotnetcore/Network/Network/PacketBase.cs'
$originalPacket = Join-Path $reference $packetRelative
$derivedPacket = Join-Path $compatRoot "server-working/$packetRelative"
$originalPacketHash = (Get-FileHash -LiteralPath $originalPacket -Algorithm SHA256).Hash.ToLowerInvariant()
$derivedPacketHash = (Get-FileHash -LiteralPath $derivedPacket -Algorithm SHA256).Hash.ToLowerInvariant()
$packetEntries = @($manifest.Patches | Where-Object Path -eq "server-working/$packetRelative")
if ($Mode -eq 'baseline') {
    if ($derivedPacketHash -ne $originalPacketHash -or $packetEntries.Count -ne 0) { throw 'Baseline requires pristine original PacketBase and no applied PacketBase transform.' }
} else {
    if ($packetEntries.Count -ne 1 -or $packetEntries[0].OriginalPath -ne $packetRelative -or
        $packetEntries[0].OriginalSha256 -ne $originalPacketHash -or $packetEntries[0].DerivedSha256 -ne $derivedPacketHash -or
        $originalPacketHash -eq $derivedPacketHash) { throw 'Fixed requires exactly one original/derived PacketBase provenance entry.' }
    . (Join-Path $PSScriptRoot 'patches/packet-length.ps1')
    $expectedPacket = & $packetLengthTransform ([IO.File]::ReadAllText($originalPacket))
    if ([IO.File]::ReadAllText($derivedPacket) -cne $expectedPacket) { throw 'Fixed PacketBase must match only the reviewed two-site transform.' }
}
$env:DOTNET_ROOT = Split-Path $dotnetExe -Parent
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_CLI_HOME = Join-Path $compatRoot 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $compatRoot 'nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:RT_COMPATIBILITY_OFFLINE = '1'
$project = Join-Path $PSScriptRoot 'NativeDeliveryChecks/LegacyNativeDeliveryChecks.csproj'
$runner = Join-Path $PSScriptRoot 'NativeDeliveryChecks/bin/Release/net10.0/LegacyNativeDeliveryChecks.dll'
$report = Join-Path $outputRoot "$Mode.json"
Push-Location $compatRoot
try {
    # Always compile current source before execution; hashes alone cannot authenticate stale DLLs.
    & $dotnetExe build $project --configuration Release --nologo --verbosity quiet -p:UseSharedCompilation=false *> (Join-Path $taskRoot "$Mode-build.log")
    if ($LASTEXITCODE -ne 0) { Get-Content -LiteralPath (Join-Path $taskRoot "$Mode-build.log") -Tail 12; throw 'Native delivery checks build failed.' }
    if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
    $arguments = @($runner,$repoRoot,$report,$Mode) | ForEach-Object { '"' + $_ + '"' }
    $process = Start-Process -FilePath $dotnetExe -ArgumentList $arguments -WorkingDirectory $taskRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskRoot "$Mode.stdout.log") -RedirectStandardError (Join-Path $taskRoot "$Mode.stderr.log") -PassThru
    $null = $process.Handle  # PS 5.1: cache the handle so ExitCode is populated after WaitForExit
    if (-not $process.WaitForExit(30000)) { $process.Kill(); throw 'Owned delivery checks exceeded 30 seconds and were stopped.' }
    $process.Refresh()
    Get-Content -LiteralPath (Join-Path $taskRoot "$Mode.stdout.log") -Tail 8
    Get-Content -LiteralPath (Join-Path $taskRoot "$Mode.stderr.log") -Tail 12
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report)) { throw "Delivery checks rejected (exit $($process.ExitCode)); inspect $taskRoot." }
    Write-Output "NATIVE_DELIVERY_REPORT: $report"
} finally { Pop-Location }
