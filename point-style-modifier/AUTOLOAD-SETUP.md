# Auto-Load Setup Instructions

This guide shows you how to automatically load PointStyleModifier every time you open Civil 3D.

## Method 1: Plugin Bundle (Recommended)

### One-Time Setup:

1. **Build your project first:**
   ```powershell
   cd C:\Users\JMartinez\git\Civil3D-tools\point-style-modifier
   dotnet build
   ```

2. **Copy the DLL to the bundle folder:**
   ```powershell
   # Create the bundle directory if it doesn't exist
   New-Item -ItemType Directory -Force -Path "C:\ProgramData\Autodesk\ApplicationPlugins\PointStyleModifier.bundle\Contents"

   # Copy the DLL
   Copy-Item "PointStyleModifier\bin\Debug\net48\PointStyleModifier.dll" `
             "C:\ProgramData\Autodesk\ApplicationPlugins\PointStyleModifier.bundle\Contents\"

   # Copy the PackageContents.xml
   Copy-Item "PointStyleModifier.bundle\PackageContents.xml" `
             "C:\ProgramData\Autodesk\ApplicationPlugins\PointStyleModifier.bundle\"
   ```

3. **Restart Civil 3D** - The plugin will now load automatically!

### After Each Build:

Just copy the new DLL to the bundle folder:
```powershell
Copy-Item "PointStyleModifier\bin\Debug\net48\PointStyleModifier.dll" `
          "C:\ProgramData\Autodesk\ApplicationPlugins\PointStyleModifier.bundle\Contents\"
```

---

## Method 2: Quick PowerShell Script (Easiest)

Save this as `deploy.ps1` in your project folder:

```powershell
# Build the project
dotnet build

# Deploy to plugin folder
$bundlePath = "C:\ProgramData\Autodesk\ApplicationPlugins\PointStyleModifier.bundle"
$contentsPath = "$bundlePath\Contents"

# Create directories
New-Item -ItemType Directory -Force -Path $contentsPath | Out-Null

# Copy files
Copy-Item "PointStyleModifier\bin\Debug\net48\PointStyleModifier.dll" $contentsPath -Force
Copy-Item "PointStyleModifier.bundle\PackageContents.xml" $bundlePath -Force

Write-Host "Deployed successfully to $bundlePath" -ForegroundColor Green
Write-Host "Restart Civil 3D to load the updated plugin" -ForegroundColor Yellow
```

Then just run:
```powershell
.\deploy.ps1
```

---

## Verification

1. Open Civil 3D
2. Type `MODIFYPOINTSTYLE` - it should work without NETLOAD!
3. The command is now available in every drawing

---

## Troubleshooting

### Plugin not loading?
- Check if the bundle folder exists: `C:\ProgramData\Autodesk\ApplicationPlugins\PointStyleModifier.bundle`
- Verify the DLL is in: `...PointStyleModifier.bundle\Contents\PointStyleModifier.dll`
- Check PackageContents.xml is in: `...PointStyleModifier.bundle\PackageContents.xml`
- Restart Civil 3D completely

### Command not found?
- Type `NETLOAD` once manually and select the DLL to verify it works
- Check the Civil 3D command line for any error messages on startup

### Updates not showing?
- Make sure you copied the new DLL to the bundle folder
- Completely close and reopen Civil 3D (don't just open a new drawing)
