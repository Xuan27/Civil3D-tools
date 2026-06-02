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

namespace LegendBuilderWW.Commands
{
    /// <summary>
    /// AutoCAD command entry points for LegendBuilderWW.
    /// LEGENDBUILDERWW is named to avoid clashing with Civil 3D's built-in LegendBuilder.
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
                List<LegendRow> templateRows = parser.Parse(db, templateBtrId);
                if (templateRows.Count == 0)
                {
                    ErrorHandler.ShowWarning(editor,
                        "No legend rows were parsed from the template. Check the source block contents.");
                    return;
                }

                DrawingScanner scanner = new DrawingScanner();
                DrawingUsage usage = scanner.Scan(db);

                LegendMatcher matcher = new LegendMatcher();
                List<MatchedRow> matched = matcher.Match(templateRows, usage);

                using (LegendBuilderForm form = new LegendBuilderForm(matched, settings))
                {
                    DialogResult result = Application.ShowModalDialog(form);
                    if (result != DialogResult.OK)
                    {
                        ErrorHandler.ShowMessage(editor, "Legend Builder cancelled.");
                        return;
                    }

                    LegendEmitter emitter = new LegendEmitter(settings);
                    emitter.Emit(doc, form.SelectedRows);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(editor, ex, "LEGENDBUILDERWW");
            }
        }

        /// <summary>
        /// Diagnostic command: dumps every parsed row from the template to the AutoCAD command line.
        /// Useful for validating the parser against the real Vertical Legend block without running the GUI.
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
                List<LegendRow> rows = parser.Parse(db, templateBtrId);

                DrawingScanner scanner = new DrawingScanner();
                DrawingUsage usage = scanner.Scan(db);

                editor.WriteMessage(string.Format("\nDrawing usage tally:"));
                editor.WriteMessage(string.Format("\n  Blocks:    {0} distinct", usage.BlockCounts.Count));
                editor.WriteMessage(string.Format("\n  Linetypes: {0} distinct", usage.LinetypeCounts.Count));
                editor.WriteMessage(string.Format("\n  Hatches:   {0} distinct", usage.HatchPatternCounts.Count));
                foreach (System.Collections.Generic.KeyValuePair<string, int> kv in usage.BlockCounts)
                {
                    editor.WriteMessage(string.Format("\n    block: {0,-35} count={1}", kv.Key, kv.Value));
                }
                foreach (System.Collections.Generic.KeyValuePair<string, int> kv in usage.LinetypeCounts)
                {
                    editor.WriteMessage(string.Format("\n    ltype: {0,-35} count={1}", kv.Key, kv.Value));
                }
                foreach (System.Collections.Generic.KeyValuePair<string, int> kv in usage.HatchPatternCounts)
                {
                    editor.WriteMessage(string.Format("\n    hatch: {0,-35} count={1}", kv.Key, kv.Value));
                }

                editor.WriteMessage(string.Format("\n\nParsed {0} legend row(s):", rows.Count));
                for (int i = 0; i < rows.Count; i++)
                {
                    LegendRow r = rows[i];
                    int count = usage.GetCount(r.RowType, r.Key);
                    editor.WriteMessage(string.Format(
                        "\n  [{0,3}] col={1} type={2,-8} key={3,-30} count={4,-4} desc=\"{5}\"",
                        i + 1, r.ColumnIndex, r.RowType, r.Key, count, r.Description));
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(editor, ex, "LEGENDBUILDERWW_DUMP");
            }
        }

        /// <summary>
        /// Diagnostic: finds the first CogoPoint in model space and dumps every public instance
        /// property of its PointStyle (and one level of nested object properties). Use this when
        /// LEGENDBUILDERWW_DUMP shows zero blocks despite the drawing being full of COGO points —
        /// the output reveals which API property holds the marker block name on this Civil 3D
        /// version, so DrawingScanner's reflection can be pointed at it.
        /// </summary>
        [CommandMethod("LEGENDBUILDERWW_PROBESTYLE")]
        public void ProbePointStyle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor editor = doc.Editor;
            Database db = doc.Database;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    int pointCount = 0;
                    System.Collections.Generic.HashSet<ObjectId> seenStyles =
                        new System.Collections.Generic.HashSet<ObjectId>();

                    foreach (ObjectId id in ms)
                    {
                        Autodesk.AutoCAD.DatabaseServices.DBObject obj =
                            tr.GetObject(id, OpenMode.ForRead);
                        Autodesk.Civil.DatabaseServices.CogoPoint cp =
                            obj as Autodesk.Civil.DatabaseServices.CogoPoint;
                        if (cp == null) continue;
                        pointCount++;
                        if (!cp.StyleId.IsNull) seenStyles.Add(cp.StyleId);
                    }

                    editor.WriteMessage(string.Format(
                        "\nFound {0} COGO point(s) using {1} distinct PointStyle(s).",
                        pointCount, seenStyles.Count));

                    int dumped = 0;
                    foreach (ObjectId styleId in seenStyles)
                    {
                        if (dumped >= 3) break;
                        Autodesk.Civil.DatabaseServices.Styles.PointStyle ps =
                            tr.GetObject(styleId, OpenMode.ForRead)
                            as Autodesk.Civil.DatabaseServices.Styles.PointStyle;
                        if (ps == null) continue;

                        editor.WriteMessage(string.Format(
                            "\n\n=== PointStyle [{0}] \"{1}\" ({2}) ===",
                            dumped + 1, ps.Name, ps.GetType().FullName));
                        DumpObjectProperties(editor, ps, "  ", maxDepth: 2);
                        dumped++;
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(editor, ex, "LEGENDBUILDERWW_PROBESTYLE");
            }
        }

        private static void DumpObjectProperties(Editor editor, object instance, string indent, int maxDepth)
        {
            if (instance == null || maxDepth <= 0) return;
            System.Reflection.PropertyInfo[] props = instance.GetType().GetProperties(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (System.Reflection.PropertyInfo prop in props)
            {
                if (prop.GetIndexParameters().Length > 0) continue;

                string typeName = prop.PropertyType.Name;
                string valueStr;
                object value = null;
                try
                {
                    value = prop.GetValue(instance, null);
                    valueStr = value == null ? "<null>" : value.ToString();
                }
                catch (Exception ex)
                {
                    valueStr = string.Format("<getter threw: {0}>", ex.GetType().Name);
                }
                if (valueStr.Length > 80) valueStr = valueStr.Substring(0, 77) + "...";

                editor.WriteMessage(string.Format("\n{0}{1,-22} {2,-32} = {3}", indent, typeName, prop.Name, valueStr));

                // Recurse into non-primitive, non-string, non-AutoCAD-handle properties one level deep.
                if (value != null &&
                    maxDepth > 1 &&
                    !prop.PropertyType.IsPrimitive &&
                    prop.PropertyType != typeof(string) &&
                    !prop.PropertyType.IsEnum &&
                    !prop.PropertyType.IsValueType &&
                    !prop.PropertyType.FullName.StartsWith("System."))
                {
                    DumpObjectProperties(editor, value, indent + "    ", maxDepth - 1);
                }
            }
        }
    }
}
