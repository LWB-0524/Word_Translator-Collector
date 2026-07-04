# WordCollector Build Script
# Generates a standalone Windows x64 single-file executable

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "..\release"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WordCollector Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectDir

Write-Host "[1/3] Restoring packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed!" -ForegroundColor Red
    exit 1
}

Write-Host "[2/3] Building Release configuration..." -ForegroundColor Yellow
dotnet publish -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -o $OutputDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "[3/3] Copying README..." -ForegroundColor Yellow
Copy-Item -Path "README.txt" -Destination "$OutputDir\README.txt" -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "  Output: $OutputDir\WordCollector.exe" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Run the app: .\$OutputDir\WordCollector.exe" -ForegroundColor White
