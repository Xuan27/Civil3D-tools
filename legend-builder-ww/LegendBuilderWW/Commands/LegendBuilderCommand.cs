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
                List<LegendRow> templateRows = parser.Parse(db, templateBtrId);
                if (templateRows.Count == 0)
                {
                    ErrorHandler.ShowWarning(editor,
                        "No legend rows were parsed from the template. Check the source block contents.");
                    return;
                }

                ObjectId tableId = PromptForSincpacTable(editor);
                if (tableId.IsNull)
                {
                    ErrorHandler.ShowMessage(editor, "Legend Builder cancelled — no table selected.");
                    return;
                }

                DrawingUsage usage = new SincpacTableReader().Read(db, tableId);

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
        /// Prompts the user to select the SincpacC3D symbols Table on screen. Returns ObjectId.Null
        /// if the user cancels or picks nothing.
        /// </summary>
        private static ObjectId PromptForSincpacTable(Editor editor)
        {
            PromptEntityOptions opts = new PromptEntityOptions(
                "\nSelect the SincpacC3D symbols table: ");
            opts.SetRejectMessage("\nThat is not a table — select the symbols table created by SincpacC3D.");
            opts.AddAllowedClass(typeof(Table), false);

            PromptEntityResult res = editor.GetEntity(opts);
            return res.Status == PromptStatus.OK ? res.ObjectId : ObjectId.Null;
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
                List<LegendRow> rows = parser.Parse(db, templateBtrId);

                ObjectId tableId = PromptForSincpacTable(editor);
                if (tableId.IsNull)
                {
                    ErrorHandler.ShowMessage(editor, "Dump cancelled — no table selected.");
                    return;
                }

                DrawingUsage usage = new SincpacTableReader().Read(db, tableId);

                editor.WriteMessage("\nBlock tally read from SincpacC3D table:");
                editor.WriteMessage(string.Format("\n  Blocks: {0} distinct", usage.BlockCounts.Count));
                foreach (KeyValuePair<string, int> kv in usage.BlockCounts)
                {
                    editor.WriteMessage(string.Format("\n    block: {0,-35} count={1}", kv.Key, kv.Value));
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
    }
}
