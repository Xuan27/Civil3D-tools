# LegendBuilderWW

Westwood-built Civil 3D plugin that generates a legend **block** in paper space, filtered to only the symbols actually present in the drawing. Symbol detection is delegated to SincpacC3D's `LegendBuilder` — the plugin reads which symbols are in use from a SincpacC3D symbols table and emits a clean, editable block from the Westwood **Vertical Legend** template.

## Requirements

- A licensed **SincpacC3D** install. You run its `LegendBuilder` command first to produce the symbols table that this plugin reads. SincpacC3D reliably resolves every symbol category — inserted blocks, xref'd blocks, nested blocks, pipe-network structure symbols, and COGO point markers — which a plain model-space scan cannot.

## Why not Civil 3D's built-in `LegendBuilder`?

Civil 3D's `LegendBuilder` / `AddLegendTable` only operates on Civil 3D objects driven by styles (pipe networks, pressure networks, surfaces, alignments, etc.). It does not see plain AutoCAD block references, linetypes, or hatches. Our legend rows are all plain AutoCAD geometry, so the built-in command is not applicable.

The command is named **`LEGENDBUILDERWW`** specifically to avoid colliding with Civil 3D's `LEGENDBUILDER`.

## Commands

| Command | Description |
|---|---|
| `LEGENDBUILDERWW` | Select a SincpacC3D symbols table, then insert a new legend block into the active paper-space layout. |
| `LEGENDBUILDERWW_DUMP` | Diagnostic: select a SincpacC3D table and dump its block tally next to every parsed template row and its match count. |

## Workflow

1. Run SincpacC3D's `LegendBuilder` to generate a **symbols table** in the drawing.
2. Switch to a paper-space layout (Model space is not a valid target).
3. Run `LEGENDBUILDERWW`.
4. When prompted, **select the SincpacC3D symbols table**. The plugin reads the block name out of each symbol cell, reads the **Vertical Legend** template block (from the current drawing if present, otherwise from the configured source DWG), and opens a dialog.
5. Rows whose block appears in the table are pre-checked; unused rows appear unchecked so you can force-include a symbol you are about to add. Symbols used in the drawing but missing from the template ("orphans") are added unchecked too — tick them to append them.
6. Filter the list with the `Show` and `Type` dropdowns and the search box, adjust checkboxes, and click `Generate`. The **Description** column is editable — change a label (e.g. `V-UTIL-STRM-CULV` → `STORM CULVERT`) and it is used in the legend; edits are remembered for that symbol across runs (stored in `DescriptionOverrides`). Use **Preview Legend** to see the result before generating.
7. At the insertion prompt, pick a point in paper space — or type `S`/`T` to switch between **Single-column** and **Two-column** layout (remembered for next time). The plugin creates a new `LEGEND_WW_<timestamp>` block and inserts one reference at the point.

The output is grouped by type — point/block symbols first, then linetypes, then hatches — laid out column-major (left column fills top-to-bottom, then the right). Each run produces a fresh block — the plugin never edits an existing legend in place.

## Settings

Settings live in `%APPDATA%\WPS\LegendBuilderWW\settings.json` and are created on first run from an embedded default seed.

| Key | Default | Purpose |
|---|---|---|
| `SourceDwgPath` | `C:\WPS-CAD-LAND\2023-WPS\Tool Palettes\Land Survey\Source\Legends and Notes.dwg` | DWG holding the master Vertical Legend block. |
| `SourceBlockName` | `Vertical Legend` | Block to read from the source DWG. |
| `OutputBlockNamePrefix` | `LEGEND_WW_` | Prefix for newly created legend blocks. |
| `RowGroupingTolerance` | `0.2` | Y-distance within which entities are clustered into the same row when parsing the template. |
| `SingleColumn` | `false` | Single-column vs two-column (column-major) output. Toggled at the insertion prompt; remembered here. |
| `DescriptionOverrides` | `{}` | Remembered description edits, keyed by `RowType\|Key`. Delete an entry to revert a label to its default. |
| `IncludeOverrides` | `{}` | Remembered check state, keyed by `RowType\|Key`. A symbol checked last run (including orphans) comes back checked. Delete an entry to fall back to the default. |
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
├── Commands/LegendBuilderCommand.cs        Entry point + table-select prompt + diagnostic dump
├── Config/
│   ├── Settings.cs                         JSON load/save in %APPDATA%
│   └── settings.default.json               Embedded first-run seed
├── Models/                                 LegendRow, MatchedRow, RowType, DrawingUsage, TemplateParse
├── Services/
│   ├── TemplateResolver.cs                 Current-DB lookup, else side-load source DWG
│   ├── TemplateReader.cs                   Side-load + WblockClone the block
│   ├── RowParser.cs                        Y-cluster entities → typed rows + title
│   ├── SincpacTableReader.cs               Read block names from a SincpacC3D symbols table
│   ├── LinetypeHatchScanner.cs             Tally model-space linetypes/hatches into usage
│   ├── LegendMatcher.cs                    Join template rows ↔ usage tally
│   └── LegendEmitter.cs                    Build new BTR, insert in paper space
├── UI/
│   ├── LegendBuilderForm.{cs,Designer.cs,resx}
│   └── SettingsDialog.{cs,Designer.cs,resx}
└── Utilities/ErrorHandler.cs
```

## Known limitations

- Template parsing assumes two columns laid out left-to-right with consistent row pitch. If the source block's layout drifts, increase `RowGroupingTolerance` or hand-set `TitleEntityYThreshold` in `settings.json`.
- **Block** detection depends on SincpacC3D: the plugin reads block names from an existing SincpacC3D symbols table rather than scanning the drawing for blocks, so you must run SincpacC3D's `LegendBuilder` first. Matching is by **block name** — a template row matches only if the block name in the table cell equals the row's block name (use `LEGENDBUILDERWW_DUMP` to spot mismatches; anonymous `*` blocks are skipped).
- **Linetypes and hatches** are detected separately, by a plain **model-space scan** (SincpacC3D's table does not carry them). Linetypes/hatches that live only in xrefs or paper space are not counted.
- The output block includes the template's title (the "LEGEND" text + underline bar), placed above the rows at the template's spacing.
