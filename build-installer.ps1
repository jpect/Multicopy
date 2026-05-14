<#
.SYNOPSIS
    Publishes Multicopy2 and compiles the Inno Setup installer.

.PARAMETER Version
    Version string written into the EXE and installer (default: 1.0.0).

.PARAMETER InnoSetupPath
    Full path to ISCC.exe. Script searches common install locations automatically.

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -Version 1.2.0
#>
param(
    [string]$Version       = "1.0.0",
    [string]$InnoSetupPath = ""
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

# ── Locate dotnet ─────────────────────────────────────────────────────────────
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnet = if ($dotnetCmd) { $dotnetCmd.Source } else { "C:\Program Files\dotnet\dotnet.exe" }
if (-not (Test-Path $dotnet)) {
    Write-Error "dotnet.exe not found. Install the .NET 9 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

# ── Locate Inno Setup ─────────────────────────────────────────────────────────
if (-not $InnoSetupPath) {
    foreach ($c in @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )) {
        if (Test-Path $c) { $InnoSetupPath = $c; break }
    }
}
if (-not $InnoSetupPath -or -not (Test-Path $InnoSetupPath)) {
    Write-Error @"
Inno Setup 6 not found. Download and install from:
  https://jrsoftware.org/isdl.php
Then re-run this script.
"@
    exit 1
}

# ── Publish (framework-dependent, win-x64) ────────────────────────────────────
Write-Host ""
Write-Host "  Publishing Multicopy2 $Version (framework-dependent, win-x64)..." -ForegroundColor Cyan

& $dotnet publish "$Root\Multicopy2\Multicopy2.csproj" `
    -c Release `
    -r win-x64 `
    --no-self-contained `
    -p:PublishReadyToRun=true `
    -p:Version=$Version

if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed"; exit 1 }

$publishDir = "$Root\Multicopy2\bin\Release\net9.0-windows\win-x64\publish"
$exePath    = "$publishDir\Multicopy2.exe"

if (-not (Test-Path $exePath)) {
    Write-Error "Published EXE not found at: $exePath"
    exit 1
}

Write-Host "  Published to $publishDir" -ForegroundColor Green

# ── Compile installer ─────────────────────────────────────────────────────────
Write-Host ""
Write-Host "  Building installer..." -ForegroundColor Cyan

$outputDir = "$Root\installer\output"
New-Item -ItemType Directory -Force $outputDir | Out-Null

& $InnoSetupPath "$Root\installer\multicopy2.iss" /DMyAppVersion=$Version /Q

if ($LASTEXITCODE -ne 0) { Write-Error "Inno Setup failed"; exit 1 }

$installerPath = "$outputDir\Multicopy2-$Version-Setup.exe"
if (-not (Test-Path $installerPath)) {
    Write-Error "Expected installer not found: $installerPath"
    exit 1
}

$sizeMB = [math]::Round((Get-Item $installerPath).Length / 1MB, 1)

Write-Host ""
Write-Host "  Build complete!" -ForegroundColor Green
Write-Host "  Installer : installer\output\Multicopy2-$Version-Setup.exe  ($sizeMB MB)" -ForegroundColor Green
Write-Host ""
Write-Host "  NOTE: Windows SmartScreen will warn users on first run until" -ForegroundColor Yellow
Write-Host "  the installer is signed with a code-signing certificate." -ForegroundColor Yellow
Write-Host "  The installer will prompt users to install .NET 9 Desktop" -ForegroundColor Yellow
Write-Host "  Runtime if it is not already present on their PC." -ForegroundColor Yellow
Write-Host ""
