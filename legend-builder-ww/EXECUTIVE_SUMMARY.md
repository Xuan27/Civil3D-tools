# Executive Summary — LegendBuilderWW

**A Westwood-built Civil 3D plugin that auto-generates drawing legends containing only the symbols actually used in a drawing.**

## The problem it solves

Drafters build legend tables by hand — a slow, error-prone task where legends routinely list symbols that aren't in the drawing (or omit ones that are). Civil 3D ships a built-in `LegendBuilder`, but it only understands *style-driven Civil 3D objects* (pipe networks, surfaces, alignments). Westwood's legends are built from plain AutoCAD geometry — block references, linetypes, and hatches — which the native tool can't see. **LegendBuilderWW fills that gap.**

## How it works

1. The user first runs **SincpacC3D**'s `LegendBuilder` (a licensed third-party tool) to produce a *symbols table*. SincpacC3D reliably detects every symbol category — inserted, xref'd, and nested blocks, pipe-network structures, and COGO point markers — which a naive scan can't.
2. The user runs the **`LEGENDBUILDERWW`** command (named to avoid colliding with the native command) and selects that table.
3. The plugin reads the used symbols from the table, adds a model-space scan for linetypes and hatches, loads Westwood's master **"Vertical Legend"** template block, and matches the two.
4. A dialog opens: used symbols are pre-checked, unused ones unchecked (so a drafter can force-include something they're about to add), and "orphans" (used in the drawing but missing from the template) are offered for opt-in. Descriptions are editable and remembered across runs.
5. On generate, the plugin emits a **fresh, clean legend block** into paper space at a picked point — grouped by type (symbols → linetypes → hatches), in single- or two-column layout, with a live **Preview** before committing.

## Key characteristics

- **Non-destructive:** every run creates a new timestamped block (`LEGEND_WW_<timestamp>`); it never edits an existing legend in place.
- **Remembers user intent:** description edits and checkbox selections persist between runs via a settings file.
- **Zero-friction deploy:** installs as an Autodesk ApplicationPlugin bundle that auto-loads at Civil 3D startup — no admin rights, environment variables, or manual `NETLOAD`.
- **Diagnostics built in:** a `LEGENDBUILDERWW_DUMP` command reports the block tally against parsed template rows to troubleshoot name mismatches.

## Technical profile

- **Platform:** C# .NET plugin against the AutoCAD/Civil 3D managed API (~3,100 lines).
- **Architecture:** cleanly layered — Commands, Services (template resolve/parse, table read, scan, match, emit), Models, WinForms UI, and JSON-backed settings.
- **Maturity:** active development from Dec 2025 through June 2026, with recent work on preview rendering, editable/remembered legend descriptions, and layout options.

## Dependencies & limitations

- **Hard dependency on a licensed SincpacC3D install** for block detection — a deliberate design trade-off that leverages SincpacC3D's proven symbol resolution rather than reinventing it.
- Block matching is **by name**, so template and table names must agree.
- Linetypes/hatches living only in xrefs or paper space aren't counted (the scan is model-space only).
- Template parsing assumes a consistent two-column, fixed-pitch layout; drifting source layouts require a tolerance tweak in settings.

## Bottom line

LegendBuilderWW turns a tedious, mistake-prone manual drafting chore into a reviewable, one-command operation that guarantees the legend reflects what's genuinely in the drawing — standardizing Westwood's legend output while keeping the drafter in full control of the final content.
