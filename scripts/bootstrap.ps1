[CmdletBinding()]
param([switch]$IncludeExportTemplates)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $repoRoot 'toolchain.lock.json') -Raw | ConvertFrom-Json
$toolsRoot = Join-Path $repoRoot '.tools'
$downloads = Join-Path $toolsRoot 'downloads'
New-Item -ItemType Directory -Force -Path $downloads | Out-Null

function Get-VerifiedArchive($Entry, [string]$Algorithm, [string]$ExpectedHash) {
    $archive = Join-Path $downloads $Entry.file
    if (-not (Test-Path -LiteralPath $archive)) {
        Write-Host "Downloading $($Entry.file)..."
        Invoke-WebRequest -Uri $Entry.url -OutFile $archive
    }
    $actual = (Get-FileHash -LiteralPath $archive -Algorithm $Algorithm).Hash
    if ($actual -ne $ExpectedHash) { throw "Checksum mismatch for $archive. Remove the partial archive and retry." }
    return $archive
}

$dotnetRoot = Join-Path $toolsRoot 'dotnet'
if (-not (Test-Path -LiteralPath (Join-Path $dotnetRoot "sdk/$($lock.dotnetSdkVersion)"))) {
    $archive = Get-VerifiedArchive $lock.dotnet 'SHA512' $lock.dotnet.sha512
    Expand-Archive -LiteralPath $archive -DestinationPath $dotnetRoot -Force
}
$godotRoot = Join-Path $toolsRoot 'godot'
$godotExe = Join-Path $godotRoot "Godot_v$($lock.godotVersion)-stable_mono_win64/Godot_v$($lock.godotVersion)-stable_mono_win64_console.exe"
if (-not (Test-Path -LiteralPath $godotExe)) {
    $archive = Get-VerifiedArchive $lock.godot 'SHA256' $lock.godot.sha256
    Expand-Archive -LiteralPath $archive -DestinationPath $godotRoot -Force
}
if ($IncludeExportTemplates) {
    $templateRoot = Join-Path $toolsRoot 'export-templates'
    $marker = Join-Path $templateRoot '.verified-4.5.1'
    if (-not (Test-Path -LiteralPath $marker)) {
        $archive = Get-VerifiedArchive $lock.templates 'SHA256' $lock.templates.sha256
        # Extract only Windows x64 templates and their associated managed support files.
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
        try {
            foreach ($entry in $zip.Entries) {
                if ($entry.FullName -notmatch '^templates/(version\.txt|windows_(debug|release)_x86_64.*|data\.mono\.windows\.x86_64/.*)$') { continue }
                $destination = [IO.Path]::GetFullPath((Join-Path $templateRoot $entry.FullName))
                $allowed = [IO.Path]::GetFullPath($templateRoot) + [IO.Path]::DirectorySeparatorChar
                if (-not $destination.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid template archive path.' }
                if ($entry.FullName.EndsWith('/')) { continue }
                New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
                [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $true)
            }
        } finally { $zip.Dispose() }
        $releaseExe = Join-Path $templateRoot 'templates/windows_release_x86_64.exe'
        if (-not (Test-Path -LiteralPath $releaseExe)) { throw 'Windows release export template was not found.' }
        Set-Content -LiteralPath $marker -Value $lock.templates.sha256
    }
}
Write-Host "Portable toolchain ready: Godot $($lock.godotVersion), .NET SDK $($lock.dotnetSdkVersion)."
