# PointStyleModifier Deployment Script
# This script builds and deploys the plugin for auto-loading in Civil 3D

Write-Host "Building PointStyleModifier..." -ForegroundColor Cyan
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Please fix errors and try again." -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Define paths - try x64 and non-x64, Debug and Release
$possiblePaths = @(
    "PointStyleModifier\bin\x64\Debug\net48\PointStyleModifier.dll",
    "PointStyleModifier\bin\Debug\net48\PointStyleModifier.dll",
    "PointStyleModifier\bin\x64\Release\net48\PointStyleModifier.dll",
    "PointStyleModifier\bin\Release\net48\PointStyleModifier.dll"
)

$dllSource = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $dllSource = $path
        Write-Host "Found DLL at: $path" -ForegroundColor Green
        break
    }
}

if ($null -eq $dllSource) {
    Write-Host "Error: Could not find PointStyleModifier.dll" -ForegroundColor Red
    Write-Host "Checked:" -ForegroundColor Yellow
    foreach ($path in $possiblePaths) {
        Write-Host "  $path" -ForegroundColor White
    }
    exit 1
}

$bundlePath = "C:\ProgramData\Autodesk\ApplicationPlugins\PointStyleModifier.bundle"
$contentsPath = "$bundlePath\Contents"
$xmlSource = "PointStyleModifier.bundle\PackageContents.xml"

# Create bundle directories
Write-Host "Creating bundle directories..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $contentsPath | Out-Null

# Copy DLL
Write-Host "Copying DLL to bundle..." -ForegroundColor Cyan
Copy-Item $dllSource $contentsPath -Force

# Copy PackageContents.xml
Write-Host "Copying PackageContents.xml..." -ForegroundColor Cyan
Copy-Item $xmlSource $bundlePath -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Plugin deployed to:" -ForegroundColor Yellow
Write-Host "  $bundlePath" -ForegroundColor White
Write-Host ""
Write-Host "DLL copied from:" -ForegroundColor Yellow
Write-Host "  $dllSource" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Restart Civil 3D completely" -ForegroundColor White
Write-Host "  2. Open any drawing" -ForegroundColor White
Write-Host "  3. Type MODIFYPOINTSTYLE - it should work without NETLOAD!" -ForegroundColor White
Write-Host ""
