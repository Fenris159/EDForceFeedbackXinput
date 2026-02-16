# EDForceFeedback Build Script
# Builds the full solution including the C++/CLI GameInputWrapper (requires Visual Studio with C++ workload).
# Uses VS MSBuild for C++ projects; falls back to dotnet build for C#-only if VS not found.

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidateSet("x64", "Any CPU")]
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# Find Visual Studio MSBuild (required for C++/CLI GameInputWrapper)
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = $null
if (Test-Path $vswhere) {
    $vsPath = & $vswhere -latest -property installationPath 2>$null
    if ($vsPath) {
        $msbuild = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
        if (-not (Test-Path $msbuild)) { $msbuild = $null }
    }
}

Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore EDForceFeedback.sln --packages .\packages 2>$null | Out-Null

if ($msbuild -and (Test-Path $msbuild)) {
    Write-Host "Building full solution ($Configuration|x64) including GameInputWrapper..." -ForegroundColor Cyan
    & $msbuild EDForceFeedback.sln -p:Configuration=$Configuration -p:Platform=$Platform -v:m
    $exitCode = $LASTEXITCODE
} else {
    Write-Host "Visual Studio MSBuild not found." -ForegroundColor Red
    Write-Host "GameInputWrapper (C++/CLI) requires Visual Studio 2022 with 'Desktop development with C++' workload." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To build the full solution:" -ForegroundColor Yellow
    Write-Host "  1. Install Visual Studio 2022 with C++ workload, then re-run: .\build.ps1" -ForegroundColor Yellow
    Write-Host "  2. Or open EDForceFeedback.sln in Visual Studio and build (Release, x64)" -ForegroundColor Yellow
    exit 1
}

if ($exitCode -eq 0) {
    Write-Host "`nBuild succeeded." -ForegroundColor Green
    Write-Host "  EDForceFeedback: EDForceFeedback\bin\$Configuration\net48\EDForceFeedback.exe" -ForegroundColor Gray
    Write-Host "  TestForceFeedback: TestForceFeedback\bin\$Configuration\net48\TestForceFeedback.exe" -ForegroundColor Gray
} else {
    exit $exitCode
}
