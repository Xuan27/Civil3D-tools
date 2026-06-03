using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using LegendBuilderWW.Config;
using LegendBuilderWW.Models;
using LegendBuilderWW.Services;
using LegendBuilderWW.UI;
using LegendBuilderWW.Utilities;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;
// Alias to disambiguate from Autodesk.AutoCAD.DatabaseServices.RowType, and to avoid the
// namespace name "LegendBuilderWW" colliding with this class's LegendBuilderWW() method.
using RowType = LegendBuilderWW.Models.RowType;

namespace LegendBuilderWW.Commands
{
    /// <summary>
    /// AutoCAD command entry points for LegendBuilderWW.
    /// LEGENDBUILDERWW is named to avoid clashing with Civil 3D's built-in LegendBuilder.
    ///
    /// Workflow: the user first runs SincpacC3D's LegendBuilder to produce a symbols Table (which
    /// reliably captures every used symbol — blocks, xrefs, pipe structures, COGO markers). Our
    /// command then reads the block names out of that table to learn what's in use, matches them
    /// against the Vertical Legend template, and emits a clean legend block.
    /// </summary>
    public class LegendBuilderCommand
    {
        [CommandMethod("LEGENDBUILDERWW")]
        public void LegendBuilderWW()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor editor = doc.Editor;
            Database db = doc.Database;

            try
            {
                Settings settings = Settings.Load();

                TemplateResolver resolver = new TemplateResolver(settings);
                ObjectId templateBtrId;
                try
                {
                    templateBtrId = resolver.Resolve(db);
                }
                catch (Exception ex)
                {
                    ErrorHandler.ShowWarning(editor, ex.Message);
                    return;
                }

                RowParser parser = new RowParser(settings);
                TemplateParse parse = parser.Parse(db, templateBtrId);
                if (parse.Rows.Count == 0)
                {
                    ErrorHandler.ShowWarning(editor,
                        "No legend rows were parsed from the template. Check the source block contents.");
                    return;
                }

                ObjectId tableId = PromptForSincpacTable(editor, db);
                if (tableId.IsNull)
                {
                    ReportTablesInDrawing(editor, db);
                    ErrorHandler.ShowMessage(editor, "Legend Builder cancelled — no table selected.");
                    return;
                }

                // Blocks (incl. point markers / pipe structures) come from the SincpacC3D table;
                // linetypes and hatches come from a plain model-space scan and merge into the tally.
                DrawingUsage usage = new SincpacTableReader().Read(db, tableId);
                new LinetypeHatchScanner().ScanInto(db, usage);

                LegendMatcher matcher = new LegendMatcher();
                List<MatchedRow> matched = matcher.Match(parse.Rows, usage);

                List<string> orphans = FindOrphanBlocks(usage, parse.Rows);
                if (orphans.Count > 0)
                {
                    ErrorHandler.ShowWarning(editor, string.Format(
                        "{0} block(s) are used in the drawing but are NOT in the Vertical Legend template:\n  {1}\n\n" +
                        "Add them to the template to include them in the legend.",
                        orphans.Count, string.Join("\n  ", orphans.ToArray())));
                }

                using (LegendBuilderForm form = new LegendBuilderForm(matched, settings))
                {
                    DialogResult result = Application.ShowModalDialog(form);
                    if (result != DialogResult.OK)
                    {
                        ErrorHandler.ShowMessage(editor, "Legend Builder cancelled.");
                        return;
                    }

                    LegendEmitter emitter = new LegendEmitter(settings);
                    emitter.Emit(doc, matched, parse.TitleEntityIds, parse.TopRowOriginY);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(editor, ex, "LEGENDBUILDERWW");
            }
        }

        /// <summary>
        /// Prompts the user to select the SincpacC3D symbols Table on screen. Returns ObjectId.Null
        /// if the user cancels, picks nothing, or picks something that is not an AutoCAD table.
        ///
        /// No pick-time class filter is used on purpose: with a filter, picking a non-table object is
        /// rejected silently and is easily confused with picking empty space. Instead we accept any
        /// entity and report its actual class name, which tells us exactly what SincpacC3D produced
        /// when the pick is not a table.
        /// </summary>
        private static ObjectId PromptForSincpacTable(Editor editor, Database db)
        {
            PromptEntityOptions opts = new PromptEntityOptions(
                "\nSelect the SincpacC3D symbols table: ");
            opts.AllowNone = false;

            PromptEntityResult res = editor.GetEntity(opts);
            if (res.Status != PromptStatus.OK) return ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBObject obj = tr.GetObject(res.ObjectId, OpenMode.ForRead);
                bool isTable = obj is Table;
                if (!isTable)
                {
                    string cls = obj == null ? "<null>" : obj.GetRXClass().Name;
                    editor.WriteMessage(string.Format(
                        "\nSelected object is a '{0}', not an AutoCAD table. " +
                        "Pick the symbols table created by SincpacC3D.", cls));
                }
                tr.Commit();
                return isTable ? res.ObjectId : ObjectId.Null;
            }
        }

        /// <summary>
        /// Reports how many AcDbTable objects exist and in which space (Model / each Layout). A pick
        /// returns "Nothing selected" when the table lives in a different space than the one the
        /// command is run from, so this tells the user where the table actually is.
        /// </summary>
        private static void ReportTablesInDrawing(Editor editor, Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                int total = 0;
                List<string> lines = new List<string>();

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                    if (!btr.IsLayout) continue;

                    int count = 0;
                    foreach (ObjectId id in btr)
                    {
                        if (id.ObjectClass.Name == "AcDbTable") count++;
                    }
                    if (count > 0)
                    {
                        total += count;
                        Layout lay = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);
                        lines.Add(string.Format("    {0}: {1} table(s)", lay.LayoutName, count));
                    }
                }
                tr.Commit();

                editor.WriteMessage(string.Format("\nAcDbTable objects in this drawing: {0}", total));
                foreach (string l in lines) editor.WriteMessage("\n" + l);
                if (total == 0)
                {
                    editor.WriteMessage(
                        "\n  (none found — run SincpacC3D's LegendBuilder first to create the symbols table)");
                }
                else
                {
                    editor.WriteMessage(
                        "\n  Tip: run the command from the SAME space (Model or that Layout) as the table, " +
                        "and click directly on a gridline or on a symbol/text inside a cell.");
                }
            }
        }

        /// <summary>
        /// Diagnostic: dumps the block tally read from the selected SincpacC3D table alongside every
        /// parsed template row and its match count. Use this to confirm the block names inside the
        /// table match the template's row keys without running the full GUI.
        /// </summary>
        [CommandMethod("LEGENDBUILDERWW_DUMP")]
        public void DumpRows()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor editor = doc.Editor;
            Database db = doc.Database;

            try
            {
                Settings settings = Settings.Load();
                TemplateResolver resolver = new TemplateResolver(settings);
                ObjectId templateBtrId = resolver.Resolve(db);

                RowParser parser = new RowParser(settings);
                TemplateParse parse = parser.Parse(db, templateBtrId);
                List<LegendRow> rows = parse.Rows;

                ObjectId tableId = PromptForSincpacTable(editor, db);
                if (tableId.IsNull)
                {
                    ReportTablesInDrawing(editor, db);
                    ErrorHandler.ShowMessage(editor, "Dump cancelled — no table selected.");
                    return;
                }

                DrawingUsage usage = new SincpacTableReader().Read(db, tableId);
                new LinetypeHatchScanner().ScanInto(db, usage);

                editor.WriteMessage("\nUsage tally (blocks from table, linetypes/hatches from scan):");
                editor.WriteMessage(string.Format("\n  Blocks:    {0} distinct", usage.BlockCounts.Count));
                foreach (KeyValuePair<string, int> kv in usage.BlockCounts)
                {
                    editor.WriteMessage(string.Format("\n    block: {0,-35} count={1}", kv.Key, kv.Value));
                }
                editor.WriteMessage(string.Format("\n  Linetypes: {0} distinct", usage.LinetypeCounts.Count));
                foreach (KeyValuePair<string, int> kv in usage.LinetypeCounts)
                {
                    editor.WriteMessage(string.Format("\n    ltype: {0,-35} count={1}", kv.Key, kv.Value));
                }
                editor.WriteMessage(string.Format("\n  Hatches:   {0} distinct", usage.HatchPatternCounts.Count));
                foreach (KeyValuePair<string, int> kv in usage.HatchPatternCounts)
                {
                    editor.WriteMessage(string.Format("\n    hatch: {0,-35} count={1}", kv.Key, kv.Value));
                }

                editor.WriteMessage(string.Format("\n\nParsed {0} legend row(s), {1} title entity(ies):",
                    rows.Count, parse.TitleEntityIds.Count));
                for (int i = 0; i < rows.Count; i++)
                {
                    LegendRow r = rows[i];
                    int count = usage.GetCount(r.RowType, r.Key);
                    editor.WriteMessage(string.Format(
                        "\n  [{0,3}] col={1} type={2,-8} key={3,-30} count={4,-4} desc=\"{5}\"",
                        i + 1, r.ColumnIndex, r.RowType, r.Key, count, r.Description));
                }

                List<string> orphans = FindOrphanBlocks(usage, parse.Rows);
                editor.WriteMessage(string.Format(
                    "\n\nDetected blocks NOT in template ({0}):", orphans.Count));
                foreach (string o in orphans)
                {
                    editor.WriteMessage("\n    " + o);
                }

                ProbeTemplateHatches(editor, db, templateBtrId);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(editor, ex, "LEGENDBUILDERWW_DUMP");
            }
        }

        /// <summary>
        /// Blocks used in the drawing (per the SincpacC3D table) that have no matching block row in
        /// the template — i.e. symbols that should appear in the legend but the template can't supply.
        /// </summary>
        private static List<string> FindOrphanBlocks(DrawingUsage usage, List<LegendRow> templateRows)
        {
            System.Collections.Generic.HashSet<string> templateBlocks =
                new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (LegendRow r in templateRows)
            {
                if (r.RowType == RowType.Block && !string.IsNullOrEmpty(r.Key))
                {
                    templateBlocks.Add(r.Key);
                }
            }

            List<string> orphans = new List<string>();
            foreach (string key in usage.BlockCounts.Keys)
            {
                if (!templateBlocks.Contains(key)) orphans.Add(key);
            }
            orphans.Sort();
            return orphans;
        }

        /// <summary>
        /// Diagnostic for the "surface hatch shows as Linetype" bug: for every Hatch in the template,
        /// reports its pattern, whether GeometricExtents throws, and whether the loop-based fallback
        /// (RowParser.TryGetHatchExtents) recovers bounds. Tells us why a hatch swatch is or isn't
        /// surviving the parser.
        /// </summary>
        private static void ProbeTemplateHatches(Editor editor, Database db, ObjectId templateBtrId)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(templateBtrId, OpenMode.ForRead);
                int n = 0;

                editor.WriteMessage("\n\nTemplate hatches (raw probe):");
                foreach (ObjectId id in btr)
                {
                    Autodesk.AutoCAD.DatabaseServices.Hatch h =
                        tr.GetObject(id, OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Hatch;
                    if (h == null) continue;
                    n++;

                    string pat;
                    try { pat = h.PatternName; } catch { pat = "<pattern threw>"; }

                    string geo;
                    try
                    {
                        Extents3d e = h.GeometricExtents;
                        geo = string.Format("GeomExt OK y[{0:0.##}..{1:0.##}]", e.MinPoint.Y, e.MaxPoint.Y);
                    }
                    catch (Exception ex) { geo = "GeomExt THREW " + ex.GetType().Name; }

                    string loop;
                    try
                    {
                        Extents3d? le = RowParser.TryGetHatchExtents(h);
                        loop = le.HasValue
                            ? string.Format("loops OK y[{0:0.##}..{1:0.##}]", le.Value.MinPoint.Y, le.Value.MaxPoint.Y)
                            : "loops <null>";
                    }
                    catch (Exception ex) { loop = "loops THREW " + ex.GetType().Name; }

                    editor.WriteMessage(string.Format(
                        "\n  hatch[{0}] pattern={1,-12} {2}  {3}", n, pat, geo, loop));
                }
                if (n == 0) editor.WriteMessage("\n  (no Hatch entities in template)");

                tr.Commit();
            }
        }
    }
}
