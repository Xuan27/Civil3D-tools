# PtmLeader Deployment Script
# Builds the plugin and copies it to the Civil 3D ApplicationPlugins folder for auto-loading

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PtmLeader - Build and Deploy" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Building PtmLeader..." -ForegroundColor Cyan
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Please fix errors and try again." -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Locate the compiled DLL - check Debug and Release, x64 and non-x64
$possiblePaths = @(
    "PtmLeader\bin\x64\Debug\net48\PtmLeader.dll",
    "PtmLeader\bin\Debug\net48\PtmLeader.dll",
    "PtmLeader\bin\x64\Release\net48\PtmLeader.dll",
    "PtmLeader\bin\Release\net48\PtmLeader.dll"
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
    Write-Host "Error: Could not find PtmLeader.dll" -ForegroundColor Red
    Write-Host "Checked:" -ForegroundColor Yellow
    foreach ($path in $possiblePaths) {
        Write-Host "  $path" -ForegroundColor White
    }
    exit 1
}

$bundlePath   = "C:\ProgramData\Autodesk\ApplicationPlugins\PtmLeader.bundle"
$contentsPath = "$bundlePath\Contents"
$xmlSource    = "PtmLeader.bundle\PackageContents.xml"

Write-Host ""
Write-Host "Deploying to: $bundlePath" -ForegroundColor Cyan

# Create bundle directory structure
New-Item -ItemType Directory -Force -Path $contentsPath | Out-Null

# Copy DLL
Write-Host "Copying DLL..." -ForegroundColor Cyan
Copy-Item $dllSource $contentsPath -Force

# Copy PackageContents.xml
Write-Host "Copying PackageContents.xml..." -ForegroundColor Cyan
Copy-Item $xmlSource $bundlePath -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Plugin deployed to:" -ForegroundColor Yellow
Write-Host "  $bundlePath" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Restart Civil 3D completely" -ForegroundColor White
Write-Host "  2. Open any drawing" -ForegroundColor White
Write-Host "  3. Type PTMLEADER at the command line" -ForegroundColor White
Write-Host ""
