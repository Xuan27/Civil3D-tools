using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using LegendBuilderWW.Config;
using LegendBuilderWW.Models;

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

        public void Emit(Document doc, List<MatchedRow> selected)
        {
            List<MatchedRow> rows = selected
                .Where(r => r.IncludeInOutput && r.Source != null)
                .ToList();

            if (rows.Count == 0)
            {
                doc.Editor.WriteMessage("\nNo rows selected — nothing to emit.");
                return;
            }

            Database db = doc.Database;
            Editor editor = doc.Editor;

            RowLayout layout = ComputeLayout(rows);

            string newBlockName = BuildOutputBlockName();
            ObjectId newBtrId = CreateOutputBlock(db, newBlockName, rows, layout);

            ObjectId paperSpaceId = GetActivePaperSpaceId(db);
            if (paperSpaceId.IsNull)
            {
                editor.WriteMessage("\nActivate a paper-space layout before generating the legend, then re-run.");
                return;
            }

            Point3d insertPoint = PromptInsertionPoint(editor);
            if (insertPoint.Equals(Point3d.Origin) && !PromptedPointWasValid)
            {
                return;
            }

            InsertBlockReference(db, paperSpaceId, newBtrId, insertPoint);

            editor.WriteMessage(string.Format(
                "\nLegend '{0}' created with {1} row(s).",
                newBlockName, rows.Count));
        }

        private bool PromptedPointWasValid;

        private Point3d PromptInsertionPoint(Editor editor)
        {
            PromptPointOptions opts = new PromptPointOptions("\nSpecify legend insertion point: ");
            opts.AllowNone = false;
            PromptPointResult res = editor.GetPoint(opts);
            PromptedPointWasValid = res.Status == PromptStatus.OK;
            return PromptedPointWasValid ? res.Value : Point3d.Origin;
        }

        private string BuildOutputBlockName()
        {
            string prefix = string.IsNullOrEmpty(_settings.OutputBlockNamePrefix)
                ? "LEGEND_WW_"
                : _settings.OutputBlockNamePrefix;
            return prefix + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        private RowLayout ComputeLayout(List<MatchedRow> rows)
        {
            // Pull row pitch and column X positions from the template originals — keeps the new
            // legend looking identical to the source, just shorter.
            var allTemplateRows = rows.Select(r => r.Source).ToList();
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
            List<double> ys = rows.Select(r => r.RowOrigin.Y).Distinct().OrderByDescending(y => y).ToList();
            if (ys.Count < 2) return 0;
            List<double> deltas = new List<double>();
            for (int i = 1; i < ys.Count; i++)
            {
                deltas.Add(ys[i - 1] - ys[i]);
            }
            deltas.Sort();
            return deltas[deltas.Count / 2];
        }

        private ObjectId CreateOutputBlock(Database db, string blockName, List<MatchedRow> rows, RowLayout layout)
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

                // Split rows back into left/right columns, then stack them top-down so the output
                // mirrors the template's column flow even if the user excluded some rows.
                List<MatchedRow> leftCol = rows.Where(r => r.Source.ColumnIndex == 0).ToList();
                List<MatchedRow> rightCol = rows.Where(r => r.Source.ColumnIndex == 1).ToList();

                CloneColumn(tr, leftCol, layout.LeftColumnX, layout, newBtr);
                CloneColumn(tr, rightCol, layout.RightColumnX, layout, newBtr);

                tr.Commit();
            }

            return newBtrId;
        }

        private static void CloneColumn(
            Transaction tr,
            List<MatchedRow> column,
            double newColumnX,
            RowLayout layout,
            BlockTableRecord target)
        {
            double currentY = 0;
            for (int i = 0; i < column.Count; i++)
            {
                LegendRow row = column[i].Source;
                Vector3d offset = new Vector3d(
                    newColumnX - row.RowOrigin.X,
                    currentY - row.RowOrigin.Y,
                    0);

                CloneEntitiesInto(tr, row.SymbolEntityIds, target, offset);
                CloneEntitiesInto(tr, new List<ObjectId> { row.DescriptionEntityId }, target, offset);

                currentY -= layout.RowPitch;
            }
        }

        private static void CloneEntitiesInto(
            Transaction tr,
            List<ObjectId> sourceIds,
            BlockTableRecord target,
            Vector3d offset)
        {
            foreach (ObjectId id in sourceIds)
            {
                if (id.IsNull) continue;
                Entity src = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (src == null) continue;

                Entity clone = (Entity)src.Clone();
                clone.TransformBy(Matrix3d.Displacement(offset));

                target.AppendEntity(clone);
                tr.AddNewlyCreatedDBObject(clone, true);
            }
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
