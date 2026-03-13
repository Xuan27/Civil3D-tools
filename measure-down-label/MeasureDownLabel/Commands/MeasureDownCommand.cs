using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using MeasureDownLabel.Models;
using MeasureDownLabel.Services;
using MeasureDownLabel.Utilities;

namespace MeasureDownLabel.Commands
{
    /// <summary>
    /// AutoCAD command class for populating measure-down inlet labels on existing MLeaders
    /// </summary>
    public class MeasureDownCommand
    {
        private readonly ElevationPickService _elevPicker = new ElevationPickService();
        private readonly MultiLeaderService   _mleaderSvc = new MultiLeaderService();

        /// <summary>
        /// Command: MEASUREDOWN
        /// Collects top-of-structure and one or more flow-line elevations, pipe sizes,
        /// and directions, then writes the formatted label into a user-selected existing MLeader.
        /// The leader geometry, style, and position are completely preserved.
        /// </summary>
        [CommandMethod("MEASUREDOWN")]
        public void MeasureDown()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor   editor   = doc.Editor;
            Database database = doc.Database;

            try
            {
                ErrorHandler.ShowBanner(editor, "MEASUREDOWN  -  Inlet Measure-Down Label");
                editor.WriteMessage(
                    "\n  Collects elevation and pipe data, then writes the formatted\n" +
                    "  label into an existing INLET multileader you select.\n" +
                    "\n  Elevation inputs: Point (COGO) | Surface | Type | Invert\n");

                // ── Structure type ───────────────────────────────────────────────
                PromptKeywordOptions structOpts = new PromptKeywordOptions(
                    "\n  Structure type [TOP/INLET/RIM] <TOP>: ");
                structOpts.Keywords.Add("TOP");
                structOpts.Keywords.Add("INLET");
                structOpts.Keywords.Add("RIM");
                structOpts.Keywords.Default = "TOP";
                structOpts.AllowNone = true;

                PromptResult structResult = editor.GetKeywords(structOpts);

                if (structResult.Status == PromptStatus.Cancel)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                string structureType = (structResult.Status == PromptStatus.None)
                    ? "TOP"
                    : structResult.StringResult.ToUpper();

                // ── Step 1: Top of structure elevation ───────────────────────────
                double topElevation;
                string topDescription;
                Autodesk.AutoCAD.Geometry.Point3d topPoint;

                editor.WriteMessage(string.Format(
                    "\n  STEP 1 - {0} Elevation", structureType));

                if (!_elevPicker.TryGetElevation(editor, database,
                        "Top of Structure", out topElevation, out topPoint, out topDescription))
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                if (!string.IsNullOrEmpty(topDescription))
                    editor.WriteMessage(string.Format("\n  Top point: {0}", topDescription));

                // ── Step 2: One or more flow-line entries ────────────────────────
                List<FlowLineEntry> flowLines = new List<FlowLineEntry>();
                int flIndex = 1;

                while (true)
                {
                    editor.WriteMessage(string.Format(
                        "\n  STEP 2.{0} - Flow Line #{0} (Invert) Elevation", flIndex));

                    double   flElevation;
                    string   flDescription;
                    Autodesk.AutoCAD.Geometry.Point3d flPoint;

                    if (!_elevPicker.TryGetElevation(editor, database,
                            string.Format("Flow Line #{0}", flIndex),
                            out flElevation, out flPoint, out flDescription,
                            topElevation))
                    {
                        ErrorHandler.ShowMessage(editor, "Command cancelled.");
                        return;
                    }

                    if (flElevation >= topElevation)
                    {
                        ErrorHandler.ShowWarning(editor,
                            string.Format(
                                "Flow line #{0} ({1:0.0}') >= top of structure ({2:0.00}'). " +
                                "Please verify your inputs.",
                                flIndex, flElevation, topElevation));
                    }

                    if (!string.IsNullOrEmpty(flDescription))
                        editor.WriteMessage(string.Format(
                            "\n  FL #{0} point: {1}", flIndex, flDescription));

                    // ── Pipe size & direction for this flow line ─────────────────
                    PromptDoubleOptions sizeOpts = new PromptDoubleOptions(
                        string.Format("\n  Pipe diameter for FL #{0} (inches): ", flIndex))
                    {
                        AllowNegative = false,
                        AllowZero     = false
                    };
                    PromptDoubleResult sizeResult = editor.GetDouble(sizeOpts);

                    if (sizeResult.Status != PromptStatus.OK)
                    {
                        ErrorHandler.ShowMessage(editor, "Command cancelled.");
                        return;
                    }

                    PromptStringOptions dirOpts = new PromptStringOptions(
                        string.Format("\n  Pipe direction for FL #{0} (e.g. N, NE, S45W): ",
                            flIndex))
                    {
                        AllowSpaces = true
                    };
                    PromptResult dirResult = editor.GetString(dirOpts);

                    if (dirResult.Status != PromptStatus.OK)
                    {
                        ErrorHandler.ShowMessage(editor, "Command cancelled.");
                        return;
                    }

                    flowLines.Add(new FlowLineEntry
                    {
                        Elevation     = flElevation,
                        PipeSize      = sizeResult.Value,
                        PipeDirection = dirResult.StringResult.Trim().ToUpper()
                    });

                    flIndex++;

                    // ── Ask whether to add another flow line ─────────────────────
                    PromptKeywordOptions addMoreOpts = new PromptKeywordOptions(
                        "\n  Add another flow line? [Yes/No] <No>: ");
                    addMoreOpts.Keywords.Add("Yes");
                    addMoreOpts.Keywords.Add("No");
                    addMoreOpts.Keywords.Default = "No";
                    addMoreOpts.AllowNone = true;

                    PromptResult addMoreResult = editor.GetKeywords(addMoreOpts);

                    if (addMoreResult.Status == PromptStatus.Cancel)
                    {
                        ErrorHandler.ShowMessage(editor, "Command cancelled.");
                        return;
                    }

                    bool addMore = addMoreResult.Status == PromptStatus.OK &&
                                   string.Equals(addMoreResult.StringResult, "Yes",
                                       StringComparison.OrdinalIgnoreCase);

                    if (!addMore)
                        break;
                }

                // ── Assemble and preview ─────────────────────────────────────────
                MeasureDownInput input = new MeasureDownInput
                {
                    StructureType = structureType,
                    TopElevation  = topElevation,
                    FlowLines     = flowLines
                };

                string preview = _mleaderSvc.BuildLabelText(input);
                editor.WriteMessage(string.Format(
                    "\n\n  Label preview:\n    {0}\n",
                    preview.Replace("\\P", "\n    ").Replace("%%P", "+/-")));

                // ── Confirm ──────────────────────────────────────────────────────
                PromptKeywordOptions confirmOpts = new PromptKeywordOptions(
                    "\n  Apply label? [Yes/No] <Yes>: ");
                confirmOpts.Keywords.Add("Yes");
                confirmOpts.Keywords.Add("No");
                confirmOpts.Keywords.Default = "Yes";
                confirmOpts.AllowNone = true;

                PromptResult confirmResult = editor.GetKeywords(confirmOpts);

                if (confirmResult.Status == PromptStatus.Cancel ||
                    (confirmResult.Status == PromptStatus.OK &&
                     string.Equals(confirmResult.StringResult, "No",
                         StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorHandler.ShowMessage(editor, "Cancelled.");
                    return;
                }

                // ── Select existing MLeader ──────────────────────────────────────
                PromptEntityOptions entOpts = new PromptEntityOptions(
                    "\n  Select the INLET multileader to update: ");
                entOpts.SetRejectMessage(
                    "\n  That is not a multileader. Please select a multileader.");
                entOpts.AddAllowedClass(typeof(MLeader), exactMatch: false);

                PromptEntityResult entResult = editor.GetEntity(entOpts);

                if (entResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "No multileader selected. Cancelled.");
                    return;
                }

                // ── Update the MLeader content ───────────────────────────────────
                bool updated = _mleaderSvc.UpdateExistingMLeader(
                    database, entResult.ObjectId, input, editor);

                if (updated)
                {
                    System.Text.StringBuilder summary =
                        new System.Text.StringBuilder();
                    summary.AppendFormat("Label updated.\n  {0}: {1:0.00}'\n",
                        structureType, topElevation);

                    foreach (FlowLineEntry fl in flowLines)
                        summary.AppendFormat(
                            "  FL {0}\" ({1}) = {2:0.0}'  (drop {3:0.00}')\n",
                            fl.PipeSize.ToString("0.##"), fl.PipeDirection,
                            fl.Elevation, topElevation - fl.Elevation);

                    ErrorHandler.ShowSuccess(editor, summary.ToString().TrimEnd());
                }

                editor.WriteMessage(string.Format("\n{0}\n", new string('-', 60)));
            }
            catch (System.Exception ex)
            {
                ErrorHandler.HandleException(editor, ex, "MEASUREDOWN");
            }
        }
    }
}
