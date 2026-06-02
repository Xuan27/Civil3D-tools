# LegendBuilderWW

Westwood-built Civil 3D plugin that generates a legend block in paper space, filtered to only the symbols actually present in the current drawing's model space.

## Why not Civil 3D's built-in `LegendBuilder`?

Civil 3D's `LegendBuilder` / `AddLegendTable` only operates on Civil 3D objects driven by styles (pipe networks, pressure networks, surfaces, alignments, etc.). It does not see plain AutoCAD block references, linetypes, or hatches. Our legend rows are all plain AutoCAD geometry, so the built-in command is not applicable.

The command is named **`LEGENDBUILDERWW`** specifically to avoid colliding with Civil 3D's `LEGENDBUILDER`.

## Commands

| Command | Description |
|---|---|
| `LEGENDBUILDERWW` | Open the Legend Builder dialog and insert a new legend block into the active paper-space layout. |
| `LEGENDBUILDERWW_DUMP` | Diagnostic: dump every parsed template row to the command line, with drawing-usage counts. |

## Workflow

1. Switch to a paper-space layout (Model space is not a valid target).
2. Run `LEGENDBUILDERWW`.
3. The plugin reads the **Vertical Legend** block (from the current drawing if present, otherwise from the configured source DWG), scans model space for used symbols, and opens a dialog.
4. Rows present in the drawing are pre-checked; unused rows appear unchecked so you can force-include a symbol you are about to add.
5. Filter the list with the `Show` and `Type` dropdowns and the search box, adjust checkboxes, and click `Generate`.
6. Pick an insertion point in paper space. The plugin creates a new `LEGEND_WW_<timestamp>` block and inserts one reference at the point.

Each run produces a fresh block — the plugin never edits an existing legend in place.

## Settings

Settings live in `%APPDATA%\WPS\LegendBuilderWW\settings.json` and are created on first run from an embedded default seed.

| Key | Default | Purpose |
|---|---|---|
| `SourceDwgPath` | `C:\WPS-CAD-LAND\2023-WPS\Tool Palettes\Land Survey\Source\Legends and Notes.dwg` | DWG holding the master Vertical Legend block. |
| `SourceBlockName` | `Vertical Legend` | Block to read from the source DWG. |
| `OutputBlockNamePrefix` | `LEGEND_WW_` | Prefix for newly created legend blocks. |
| `RowGroupingTolerance` | `0.1` | Y-distance within which entities are clustered into the same row when parsing the template. |
| `TitleEntityYThreshold` | `null` | Optional Y cutoff above which template entities are treated as the title (LEGEND text + bar). `null` auto-detects. |

Edit them through the `Settings...` button in the dialog or by hand in the JSON file. The plugin needs no admin rights and no environment variables.

## Build & deploy

```powershell
cd legend-builder-ww
.\deploy.ps1
```

The script runs `dotnet build` and copies the DLL + `PackageContents.xml` into `C:\ProgramData\Autodesk\ApplicationPlugins\LegendBuilderWW.bundle`. Civil 3D auto-loads from there at startup; restart Civil 3D after the first deploy and the commands are available without `NETLOAD`.

## Project layout

```
LegendBuilderWW/
├── Commands/LegendBuilderCommand.cs        Entry point + diagnostic dump
├── Config/
│   ├── Settings.cs                         JSON load/save in %APPDATA%
│   └── settings.default.json               Embedded first-run seed
├── Models/                                 LegendRow, MatchedRow, RowType, DrawingUsage
├── Services/
│   ├── TemplateResolver.cs                 Current-DB lookup, else side-load source DWG
│   ├── TemplateReader.cs                   Side-load + WblockClone the block
│   ├── RowParser.cs                        Y-cluster entities → typed rows
│   ├── DrawingScanner.cs                   Tally model-space blocks/linetypes/hatches
│   ├── LegendMatcher.cs                    Join template rows ↔ drawing usage
│   └── LegendEmitter.cs                    Build new BTR, insert in paper space
├── UI/
│   ├── LegendBuilderForm.{cs,Designer.cs,resx}
│   └── SettingsDialog.{cs,Designer.cs,resx}
└── Utilities/ErrorHandler.cs
```

## Known limitations

- Template parsing assumes two columns laid out left-to-right with consistent row pitch. If the source block's layout drifts, increase `RowGroupingTolerance` or hand-set `TitleEntityYThreshold` in `settings.json`.
- Model-space-only scan. Symbols placed in paper space are not counted.
- All blocks are assumed static. Dynamic-block effective-name resolution is not implemented.
