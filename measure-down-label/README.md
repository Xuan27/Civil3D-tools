# MeasureDownLabel

A Civil 3D plugin that places formatted **INLET multileader labels** for measure-down structure data directly from the command line.

---

## Command

```
MEASUREDOWN
```

---

## Label Format

```
TOP = [TOP ELEVATION]'
FL [PIPE SIZE]" ([PIPE DIRECTION]) = [BOTTOM ELEVATION]'±
```

**Example:**

```
TOP = 312.45'
FL 12" (NE) = 309.12'±
```

- `%%P` renders as the **±** symbol (standard AutoCAD special character)
- `\P` is the MTEXT paragraph break between lines
- The **INLET** MLeader style is applied automatically if it exists in the drawing

---

## Interaction Flow

| Step | Prompt | Input Method |
|------|--------|-------------|
| 1 | Top of structure (rim) elevation | Click COGO point **or** type value |
| 2 | Flow line (invert) elevation | Click COGO point **or** type value |
| 3 | Pipe diameter (inches) | Type numeric value |
| 4 | Pipe direction | Type string (e.g. `N`, `NE`, `S45W`) |
| 5 | Arrow point on structure | Click in drawing |
| 6 | Label landing point | Click in drawing |
| 7 | Confirm label text preview | `Yes` / `No` |

A text preview of the formatted label is shown **before** placement so you can confirm or cancel.

---

## Requirements

- Autodesk Civil 3D 2023–2025
- .NET Framework 4.8
- An **INLET** MLeader style defined in your drawing template (the command will warn and use the drawing default if the style is missing)

---

## Build & Deploy

```powershell
.\deploy.ps1
```

This script:
1. Builds the project with `dotnet build`
2. Copies the DLL and `PackageContents.xml` to
   `C:\ProgramData\Autodesk\ApplicationPlugins\MeasureDownLabel.bundle\`

The plugin auto-loads with Civil 3D — no `NETLOAD` required.

---

## Project Structure

```
measure-down-label/
├── MeasureDownLabel/
│   ├── Commands/
│   │   └── MeasureDownCommand.cs      # MEASUREDOWN command entry point
│   ├── Models/
│   │   └── MeasureDownInput.cs        # Input data model
│   ├── Services/
│   │   ├── ElevationPickService.cs    # COGO point / typed elevation acquisition
│   │   └── MultiLeaderService.cs      # MLeader creation and label formatting
│   └── Utilities/
│       └── ErrorHandler.cs            # Consistent user-facing messages
├── MeasureDownLabel.bundle/
│   └── PackageContents.xml            # Auto-load bundle manifest
├── deploy.ps1                         # Build + deploy script
└── MeasureDownLabel.sln
```
