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

                editor.WriteMessage(string.Format("\nParsed {0} legend row(s):", rows.Count));
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
    }
}
