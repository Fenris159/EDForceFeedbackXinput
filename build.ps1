# EDForceFeedback Build Script
# Keeps packages in workspace (./packages) and builds the main application.

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore EDForceFeedback.sln --packages .\packages | Out-Null

Write-Host "Building EDForceFeedback ($Configuration)..." -ForegroundColor Cyan
dotnet build EDForceFeedback\EDForceFeedback.csproj -c $Configuration

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild succeeded. Output: EDForceFeedback\bin\$Configuration\net48\EDForceFeedback.exe" -ForegroundColor Green
}
