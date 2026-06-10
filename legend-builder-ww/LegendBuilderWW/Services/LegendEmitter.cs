using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using LegendBuilderWW.Config;
using LegendBuilderWW.Models;
// Disambiguate from Autodesk.AutoCAD.DatabaseServices.RowType (table row kinds).
using RowType = LegendBuilderWW.Models.RowType;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Builds a new BlockTableRecord from the selected MatchedRows and inserts a BlockReference
    /// in paper space at a user-picked point.
    /// </summary>
    public class LegendEmitter
    {
        private readonly Settings _settings;

        public LegendEmitter(Settings settings)
        {
            _settings = settings;
        }

        public void Emit(Document doc, List<MatchedRow> allRows, List<ObjectId> titleEntityIds, double templateTopRowY)
        {
            List<MatchedRow> rows = allRows
                .Where(r => r.IncludeInOutput && r.Source != null)
                .ToList();

            if (rows.Count == 0)
            {
                doc.Editor.WriteMessage("\nNo rows selected — nothing to emit.");
                return;
            }

            Database db = doc.Database;
            Editor editor = doc.Editor;

            // Pitch and column positions come from the FULL template, not just the kept rows —
            // otherwise the gaps left by dropped rows inflate the computed row pitch. Synthetic
            // orphan rows have no template geometry, so they're excluded from the layout math.
            List<LegendRow> allTemplateRows = allRows
                .Where(r => r.Source != null && !r.Source.IsSynthetic)
                .Select(r => r.Source)
                .ToList();
            RowLayout layout = ComputeLayout(allTemplateRows);

            ObjectId paperSpaceId = GetActivePaperSpaceId(db);
            if (paperSpaceId.IsNull)
            {
                editor.WriteMessage("\nActivate a paper-space layout before generating the legend, then re-run.");
                return;
            }

            // Pick the insertion point; the same prompt offers a keyword to switch single/two-column.
            bool singleColumn = _settings.SingleColumn;
            Point3d insertPoint = PromptInsertionPoint(editor, ref singleColumn);
            if (!PromptedPointWasValid)
            {
                return;
            }

            string newBlockName = BuildOutputBlockName();
            ObjectId newBtrId = CreateOutputBlock(
                db, newBlockName, rows, allTemplateRows, layout, titleEntityIds, templateTopRowY, singleColumn);

            InsertBlockReference(db, paperSpaceId, newBtrId, insertPoint);

            // Remember the column choice for next time.
            if (_settings.SingleColumn != singleColumn)
            {
                _settings.SingleColumn = singleColumn;
                try { _settings.Save(); } catch { /* preference is best-effort */ }
            }

            editor.WriteMessage(string.Format(
                "\nLegend '{0}' created with {1} row(s).",
                newBlockName, rows.Count));
        }

        /// <summary>
        /// Builds the legend into a throwaway block, renders it to a bitmap, then erases the block —
        /// for the dialog's "Preview Legend" button. Takes the full matched list (same as Emit) so
        /// layout pitch is computed from the whole template; emits only the checked rows. Returns null
        /// if nothing is checked or rendering fails.
        /// </summary>
        public System.Drawing.Bitmap RenderPreview(
            Document doc,
            List<MatchedRow> allRows,
            List<ObjectId> titleEntityIds,
            double templateTopRowY,
            System.Drawing.Size size)
        {
            Database db = doc.Database;

            List<MatchedRow> rows = allRows
                .Where(r => r.IncludeInOutput && r.Source != null)
                .ToList();
            if (rows.Count == 0) return null;

            List<LegendRow> allTemplateRows = allRows
                .Where(r => r.Source != null && !r.Source.IsSynthetic)
                .Select(r => r.Source)
                .ToList();
            RowLayout layout = ComputeLayout(allTemplateRows);

            ObjectId btrId = CreateOutputBlock(
                db, "LEGEND_WW_PREVIEW", rows, allTemplateRows, layout,
                titleEntityIds, templateTopRowY, _settings.SingleColumn);

            try
            {
                return SymbolRenderer.RenderBlock(doc, btrId, size, System.Drawing.Color.White);
            }
            finally
            {
                EraseBlock(db, btrId);
            }
        }

        private static void EraseBlock(Database db, ObjectId btrId)
        {
            if (btrId.IsNull) return;
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForWrite, false, true) as BlockTableRecord;
                    if (btr != null) btr.Erase();
                    tr.Commit();
                }
            }
            catch { /* best-effort cleanup of the throwaway preview block */ }
        }

        private bool PromptedPointWasValid;

        private Point3d PromptInsertionPoint(Editor editor, ref bool singleColumn)
        {
            while (true)
            {
                PromptPointOptions opts = new PromptPointOptions(string.Format(
                    "\nSpecify legend insertion point or layout [Single-column/Two-column] <{0}>: ",
                    singleColumn ? "Single" : "Two"));
                opts.AllowNone = false;
                opts.Keywords.Add("Single");
                opts.Keywords.Add("Two");

                PromptPointResult res = editor.GetPoint(opts);
                if (res.Status == PromptStatus.Keyword)
                {
                    singleColumn = string.Equals(res.StringResult, "Single", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                PromptedPointWasValid = res.Status == PromptStatus.OK;
                return PromptedPointWasValid ? res.Value : Point3d.Origin;
            }
        }

        private string BuildOutputBlockName()
        {
            string prefix = string.IsNullOrEmpty(_settings.OutputBlockNamePrefix)
                ? "LEGEND_WW_"
                : _settings.OutputBlockNamePrefix;
            return prefix + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        private RowLayout ComputeLayout(List<LegendRow> allTemplateRows)
        {
            // Pull row pitch and column X positions from the template originals — keeps the new
            // legend looking identical to the source, just shorter.
            double rowPitch = ComputeRowPitch(allTemplateRows);
            double leftColumnX = allTemplateRows
                .Where(r => r.ColumnIndex == 0)
                .Select(r => r.RowOrigin.X)
                .DefaultIfEmpty(0)
                .Min();
            double rightColumnX = allTemplateRows
                .Where(r => r.ColumnIndex == 1)
                .Select(r => r.RowOrigin.X)
                .DefaultIfEmpty(leftColumnX + 6.5)
                .Min();

            return new RowLayout
            {
                RowPitch = rowPitch > 0 ? rowPitch : 0.4,
                LeftColumnX = leftColumnX,
                RightColumnX = rightColumnX
            };
        }

        private static double ComputeRowPitch(List<LegendRow> rows)
        {
            // Use a single column so the two columns' interleaved Y origins don't pollute the
            // spacing, and fall back to all rows if one column is too sparse.
            List<double> ys = rows
                .Where(r => r.ColumnIndex == 0)
                .Select(r => r.RowOrigin.Y)
                .OrderByDescending(y => y)
                .ToList();
            if (ys.Count < 2)
            {
                ys = rows.Select(r => r.RowOrigin.Y).Distinct().OrderByDescending(y => y).ToList();
            }
            if (ys.Count < 2) return 0;

            List<double> deltas = new List<double>();
            for (int i = 1; i < ys.Count; i++)
            {
                deltas.Add(ys[i - 1] - ys[i]);
            }
            deltas.Sort();
            return deltas[deltas.Count / 2];
        }

        private ObjectId CreateOutputBlock(
            Database db,
            string blockName,
            List<MatchedRow> rows,
            List<LegendRow> allTemplateRows,
            RowLayout layout,
            List<ObjectId> titleEntityIds,
            double templateTopRowY,
            bool singleColumn)
        {
            ObjectId newBtrId;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);

                if (bt.Has(blockName))
                {
                    // Defensive — the timestamp suffix should prevent this, but if a user generates
                    // two within the same second, append a counter.
                    int n = 1;
                    while (bt.Has(blockName + "_" + n)) n++;
                    blockName = blockName + "_" + n;
                }

                BlockTableRecord newBtr = new BlockTableRecord
                {
                    Name = blockName,
                    Origin = Point3d.Origin
                };
                newBtrId = bt.Add(newBtr);
                tr.AddNewlyCreatedDBObject(newBtr, true);

                // Re-flow all selected rows grouped by type — point/block symbols, then linetypes,
                // then hatches. OrderBy is a stable sort, so within each type the template's order is
                // preserved and orphan rows fall at the end of their group. Layout is column-major:
                // the left column fills top-to-bottom, then the right (or one tall column if single).
                List<MatchedRow> ordered = rows.OrderBy(r => TypeRank(r.Source.RowType)).ToList();

                int total = ordered.Count;
                int rowsPerColumn = singleColumn ? total : (total + 1) / 2;
                if (rowsPerColumn < 1) rowsPerColumn = 1;

                for (int i = 0; i < total; i++)
                {
                    int col = i / rowsPerColumn;            // 0 = left, 1 = right
                    int slot = i - col * rowsPerColumn;
                    double colX = (col == 0) ? layout.LeftColumnX : layout.RightColumnX;
                    double y = -slot * layout.RowPitch;
                    PlaceRow(tr, ordered[i].Source, allTemplateRows, colX, y, newBtr);
                }

                CloneTitle(tr, titleEntityIds, templateTopRowY, newBtr);

                tr.Commit();
            }

            return newBtrId;
        }

        /// <summary>Type grouping order for the output: point/block symbols, then linetypes, then hatches.</summary>
        private static int TypeRank(RowType type)
        {
            switch (type)
            {
                case RowType.Block: return 0;
                case RowType.Linetype: return 1;
                case RowType.Hatch: return 2;
                default: return 3;
            }
        }

        /// <summary>
        /// Places one row at (columnX, y): a template row is cloned and translated there; a synthetic
        /// orphan row is built from a same-type prototype at that spot.
        /// </summary>
        private static void PlaceRow(
            Transaction tr,
            LegendRow row,
            List<LegendRow> allTemplateRows,
            double columnX,
            double y,
            BlockTableRecord target)
        {
            if (row.IsSynthetic)
            {
                EmitSyntheticRow(tr, row, allTemplateRows, columnX, y, target);
                return;
            }

            Vector3d offset = new Vector3d(columnX - row.RowOrigin.X, y - row.RowOrigin.Y, 0);
            CloneEntitiesInto(tr, row.SymbolEntityIds, target, offset);

            // Clone the template's description entity (keeps its style/height/layer) but stamp the
            // row's current Description text, so edits made in the dialog are honored.
            List<Entity> descClones = CloneEntitiesInto(
                tr, new List<ObjectId> { row.DescriptionEntityId }, target, offset);
            SetDescriptionText(descClones, row.Description);
        }

        /// <summary>
        /// Emits one orphan row by cloning a same-type template row (prototype) and retargeting it:
        /// a Block clone is pointed at the orphan's block, a Hatch clone's pattern is swapped, a
        /// Linetype clone's curve gets the orphan linetype; the cloned description text is replaced.
        /// Reusing a prototype keeps the orphan visually consistent with the rest of the legend.
        /// </summary>
        private static void EmitSyntheticRow(
            Transaction tr,
            LegendRow synth,
            List<LegendRow> allTemplateRows,
            double columnX,
            double y,
            BlockTableRecord target)
        {
            LegendRow proto = FindPrototype(allTemplateRows, synth.RowType);
            if (proto == null) return; // no same-type template row to model the orphan on

            Vector3d offset = new Vector3d(columnX - proto.RowOrigin.X, y - proto.RowOrigin.Y, 0);

            List<Entity> symbolClones = CloneEntitiesInto(tr, proto.SymbolEntityIds, target, offset);
            List<Entity> descClones = CloneEntitiesInto(
                tr, new List<ObjectId> { proto.DescriptionEntityId }, target, offset);

            RetargetSymbol(symbolClones, synth);
            SetDescriptionText(descClones, synth.Description);
        }

        private static LegendRow FindPrototype(List<LegendRow> templateRows, RowType type)
        {
            foreach (LegendRow r in templateRows)
            {
                if (r.RowType == type &&
                    r.SymbolEntityIds != null && r.SymbolEntityIds.Count > 0 &&
                    !r.DescriptionEntityId.IsNull)
                {
                    return r;
                }
            }
            return null;
        }

        private static void RetargetSymbol(List<Entity> symbolClones, LegendRow synth)
        {
            if (synth.RowType == RowType.Block)
            {
                if (synth.TargetBlockId.IsNull) return;
                foreach (Entity e in symbolClones)
                {
                    BlockReference br = e as BlockReference;
                    if (br != null) { br.BlockTableRecord = synth.TargetBlockId; return; }
                }
            }
            else if (synth.RowType == RowType.Hatch)
            {
                foreach (Entity e in symbolClones)
                {
                    Hatch h = e as Hatch;
                    if (h != null)
                    {
                        try
                        {
                            h.SetHatchPattern(HatchPatternType.PreDefined, synth.Key);
                            h.EvaluateHatch(true);
                        }
                        catch { /* pattern not resolvable — leave prototype's fill */ }
                        return;
                    }
                }
            }
            else if (synth.RowType == RowType.Linetype)
            {
                foreach (Entity e in symbolClones)
                {
                    Curve c = e as Curve;
                    if (c != null)
                    {
                        try { c.Linetype = synth.Key; } catch { /* linetype not loaded */ }
                    }
                }
            }
        }

        private static void SetDescriptionText(List<Entity> descClones, string text)
        {
            foreach (Entity e in descClones)
            {
                DBText dbt = e as DBText;
                if (dbt != null) { dbt.TextString = text ?? string.Empty; continue; }

                MText mt = e as MText;
                if (mt != null) { mt.Contents = text ?? string.Empty; }
            }
        }

        /// <summary>
        /// Re-stamps the template title (LEGEND text + bar) above the re-stacked rows. The rows map
        /// the template's top-row origin to Y=0, so shifting the title by -templateTopRowY preserves
        /// the template's title-to-first-row gap no matter which rows were dropped.
        /// </summary>
        private static void CloneTitle(
            Transaction tr,
            List<ObjectId> titleEntityIds,
            double templateTopRowY,
            BlockTableRecord target)
        {
            if (titleEntityIds == null || titleEntityIds.Count == 0) return;

            Vector3d offset = new Vector3d(0, -templateTopRowY, 0);
            CloneEntitiesInto(tr, titleEntityIds, target, offset);
        }

        private static List<Entity> CloneEntitiesInto(
            Transaction tr,
            List<ObjectId> sourceIds,
            BlockTableRecord target,
            Vector3d offset)
        {
            List<Entity> clones = new List<Entity>();
            foreach (ObjectId id in sourceIds)
            {
                if (id.IsNull) continue;
                Entity src = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (src == null) continue;

                Entity clone = (Entity)src.Clone();
                clone.TransformBy(Matrix3d.Displacement(offset));

                target.AppendEntity(clone);
                tr.AddNewlyCreatedDBObject(clone, true);
                clones.Add(clone);
            }
            return clones;
        }

        private static ObjectId GetActivePaperSpaceId(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayoutManager mgr = LayoutManager.Current;
                string activeLayout = mgr.CurrentLayout;
                if (string.Equals(activeLayout, "Model", StringComparison.OrdinalIgnoreCase))
                {
                    tr.Commit();
                    return ObjectId.Null;
                }

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                    if (btr.IsLayout)
                    {
                        Layout layout = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);
                        if (string.Equals(layout.LayoutName, activeLayout, StringComparison.OrdinalIgnoreCase))
                        {
                            tr.Commit();
                            return btrId;
                        }
                    }
                }
                tr.Commit();
            }
            return ObjectId.Null;
        }

        private static void InsertBlockReference(Database db, ObjectId spaceId, ObjectId blockDefId, Point3d insertPoint)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = (BlockTableRecord)tr.GetObject(spaceId, OpenMode.ForWrite);
                using (BlockReference br = new BlockReference(insertPoint, blockDefId))
                {
                    space.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);
                }
                tr.Commit();
            }
        }

        private class RowLayout
        {
            public double RowPitch;
            public double LeftColumnX;
            public double RightColumnX;
        }
    }
}
