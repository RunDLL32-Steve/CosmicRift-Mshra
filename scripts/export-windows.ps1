[CmdletBinding()]
param([ValidateSet('Prototype','OriginalContent')][string]$Mode = 'Prototype')
$assetPythonUserBase = $null
if ($Mode -eq 'OriginalContent') {
    $assetPythonUserBase = (& python -c "import PIL,site; print(site.USER_BASE)")
    if ($LASTEXITCODE -ne 0) { throw 'Original staging requires Python3.11+ with Pillow before the portable app profile is activated.' }
    $assetPythonUserBase = $assetPythonUserBase.Trim()
}
. (Join-Path $PSScriptRoot 'environment.ps1')
$template = Join-Path $repoRoot '.tools/export-templates/templates/windows_release_x86_64.exe'
if (-not (Test-Path -LiteralPath $template)) { throw 'Run scripts/bootstrap.ps1 -IncludeExportTemplates first.' }
$packageName = if ($Mode -eq 'OriginalContent') { 'OriginalContent' } else { 'FirstContact' }
$zipName = if ($Mode -eq 'OriginalContent') { 'OriginalContent-2026-10-13-win-x64.zip' } else { 'FirstContact-0.1.0-win-x64.zip' }
$outputRoot = Join-Path $repoRoot "artifacts/windows/$packageName"
$qaRoot = Join-Path $repoRoot "artifacts/qa/$packageName"
New-Item -ItemType Directory -Force -Path $outputRoot,$qaRoot | Out-Null

function Invoke-PackageCheck {
    param([string]$Name,[string[]]$CheckArguments,[string]$ExpectedMarker)
    $stdoutPath = Join-Path $qaRoot "$Name.log"
    $stderrPath = Join-Path $qaRoot "$Name.stderr.log"
    $checkProcess = Start-Process -FilePath (Join-Path $outputRoot 'RogueTrader2.console.exe') -ArgumentList $CheckArguments -WorkingDirectory $outputRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    if (-not $checkProcess.WaitForExit(45000)) {
        Stop-Process -Id $checkProcess.Id
        throw "Package check timed out: $Name."
    }
    $stdoutText = Get-Content -LiteralPath $stdoutPath -Raw
    if ($checkProcess.ExitCode -ne 0 -or -not $stdoutText.Contains($ExpectedMarker)) {
        Get-Content -LiteralPath $stderrPath -Tail 20 | Write-Host
        throw "Package check failed: $Name (exit $($checkProcess.ExitCode))."
    }
    Write-Host "PACKAGE_CHECK_PASS: $Name; exit0."
}

Push-Location $repoRoot
try {
    if ($Mode -eq 'OriginalContent') {
        & (Join-Path $PSScriptRoot 'verify-reference-assets.ps1')
        $savedPythonUserBase = $env:PYTHONUSERBASE
        try {
            # Keep Python's installed dependency lookup independent from Godot's portable APPDATA.
            $env:PYTHONUSERBASE = $assetPythonUserBase
            & python (Join-Path $PSScriptRoot 'original-assets/stage_original_assets.py')
            if ($LASTEXITCODE -ne 0) { throw 'Original asset staging/integrity failed.' }
        } finally { $env:PYTHONUSERBASE = $savedPythonUserBase }
    }
    & $dotnetExe build client/RogueTrader.Client.csproj --configuration ExportRelease --no-restore --nologo -nr:false
    if ($LASTEXITCODE -ne 0) { throw 'Release client build failed.' }
    & $godotExe --headless --path client --editor --import --quit *> (Join-Path $qaRoot 'import.log')
    if ($LASTEXITCODE -ne 0) { throw 'Godot import failed.' }
    & $godotExe --headless --path client --export-release 'Windows Desktop' (Join-Path $outputRoot 'RogueTrader2.exe') *> (Join-Path $qaRoot 'export.log')
    if ($LASTEXITCODE -ne 0) { throw 'Windows export failed.' }

    if ($Mode -eq 'OriginalContent') {
        $savedDotnetRoot = $env:DOTNET_ROOT
        $savedDotnetRootX64 = $env:DOTNET_ROOT_X64
        $savedPath = $env:PATH
        try {
            $env:DOTNET_ROOT = Join-Path $qaRoot 'absent-sdk'
            $env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
            $env:PATH = "$env:SystemRoot/system32;$env:SystemRoot"
            Invoke-PackageCheck -Name 'standalone-input' -CheckArguments @('--headless','--','--verify-original-input') -ExpectedMarker 'ORIGINAL_INPUT_PASS'
            foreach ($originalPage in @('ships','map','sprites')) {
                $capturePath = Join-Path $qaRoot "standalone-$originalPage.png"
                Invoke-PackageCheck -Name "standalone-$originalPage" -CheckArguments @('--','--verify-original-content',"--original-page=$originalPage",'--capture',$capturePath) -ExpectedMarker 'ORIGINAL_INSPECTION_PASS'
                if (-not (Test-Path -LiteralPath $capturePath)) { throw "Missing captured $originalPage page." }
            }
        } finally {
            $env:DOTNET_ROOT = $savedDotnetRoot
            $env:DOTNET_ROOT_X64 = $savedDotnetRootX64
            $env:PATH = $savedPath
        }
        Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/original-research/ASSET_MANIFEST.json') -Destination (Join-Path $outputRoot 'ORIGINAL_SOURCE_MANIFEST.json')
        Copy-Item -LiteralPath (Join-Path $repoRoot 'client/OriginalContent/runtime.json') -Destination (Join-Path $outputRoot 'ORIGINAL_RUNTIME_METADATA.json')
        [ordered]@{
            schemaVersion = 1
            artifact = 'OriginalContent-2026-10-13'
            canonicalCommit = 'eb1514eaae7f9b9425307d9cc043c74ecdd15e35'
            localOriginalContent = $true
            exactGameplayParity = $false
            validation = 'Standalone native inspector input and three rendered resource pages; world ticks unchanged.'
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputRoot 'PACKAGE_INFO.json') -Encoding utf8
        @(
            'Original-content development package'
            'Launch RogueTrader2.exe and press F2 or ORIGINAL CONTENT.'
            'Inspection shows12 original hulls,231 sprite objects and Rogue.bmp, and pauses the world.'
            'Flight tuning is provisional. Native banking/palette/blending and original gameplay remain unfinished.'
            'Derived pixels come from unchanged user-designated canonical assets.'
            'Keep the entire folder together; no editor or installed .NET SDK/runtime is required.'
            'Original-source and runtime mapping JSON files are adjacent.'
        ) | Set-Content -LiteralPath (Join-Path $outputRoot 'README.txt') -Encoding utf8
    } else {
        Invoke-PackageCheck -Name 'prototype-smoke' -CheckArguments @('--headless','--','--smoke-test') -ExpectedMarker 'CLIENT_SMOKE_PASS'
    }
    Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination (Join-Path $outputRoot 'README.md')
    Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD_PARTY_NOTICES.md') -Destination (Join-Path $outputRoot 'THIRD_PARTY_NOTICES.md')
    Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/licenses') -Destination (Join-Path $outputRoot 'licenses') -Recurse -Force
    Compress-Archive -Path (Join-Path $outputRoot '*') -DestinationPath (Join-Path $repoRoot "artifacts/windows/$zipName") -Force
    Write-Host "Windows build ready at $outputRoot"
} finally { Pop-Location }
