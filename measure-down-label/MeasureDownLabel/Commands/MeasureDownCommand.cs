using System;
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
        /// Collects top-of-structure and flow-line elevations, pipe size, and direction,
        /// then writes the formatted label into a user-selected existing MLeader.
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
                    "\n  Elevation inputs: Point (COGO) | Surface | Type\n");

                // ── Step 1: Top of structure elevation ───────────────────────────
                double  topElevation;
                string  topDescription;
                Autodesk.AutoCAD.Geometry.Point3d topPoint;

                editor.WriteMessage("\n  STEP 1 of 3 - Top of Structure (Rim) Elevation");

                if (!_elevPicker.TryGetElevation(editor, database,
                        "Top of Structure", out topElevation, out topPoint, out topDescription))
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                // ── Step 2: Flow line (invert) elevation ─────────────────────────
                double  bottomElevation;
                string  bottomDescription;
                Autodesk.AutoCAD.Geometry.Point3d bottomPoint;

                editor.WriteMessage("\n  STEP 2 of 3 - Flow Line (Invert) Elevation");

                if (!_elevPicker.TryGetElevation(editor, database,
                        "Flow Line", out bottomElevation, out bottomPoint, out bottomDescription))
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                if (bottomElevation >= topElevation)
                {
                    ErrorHandler.ShowWarning(editor,
                        string.Format(
                            "Flow line ({0:0.0}') >= top of structure ({1:0.00}'). " +
                            "Please verify your inputs.",
                            bottomElevation, topElevation));
                }

                // ── Show COGO descriptions as reference ──────────────────────────
                bool hasTopDesc    = !string.IsNullOrEmpty(topDescription);
                bool hasBottomDesc = !string.IsNullOrEmpty(bottomDescription);

                if (hasTopDesc || hasBottomDesc)
                {
                    editor.WriteMessage("\n\n  --- Point Reference Information ---");
                    if (hasTopDesc)
                        editor.WriteMessage(string.Format(
                            "\n  Top point :  {0}", topDescription));
                    if (hasBottomDesc)
                        editor.WriteMessage(string.Format(
                            "\n  FL  point :  {0}", bottomDescription));
                    editor.WriteMessage("\n");
                }

                // ── Step 3: Pipe size & direction ────────────────────────────────
                editor.WriteMessage("\n  STEP 3 of 3 - Pipe Size & Direction");

                PromptDoubleOptions sizeOpts = new PromptDoubleOptions(
                    "\n  Pipe diameter (inches): ")
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

                double pipeSize = sizeResult.Value;

                PromptStringOptions dirOpts = new PromptStringOptions(
                    "\n  Pipe direction (e.g. N, NE, S45W): ")
                {
                    AllowSpaces = true
                };
                PromptResult dirResult = editor.GetString(dirOpts);

                if (dirResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                string pipeDirection = dirResult.StringResult.Trim().ToUpper();

                // ── Assemble and preview ─────────────────────────────────────────
                MeasureDownInput input = new MeasureDownInput
                {
                    TopElevation    = topElevation,
                    BottomElevation = bottomElevation,
                    PipeSize        = pipeSize,
                    PipeDirection   = pipeDirection
                };

                string preview = _mleaderSvc.BuildLabelText(input);
                editor.WriteMessage(string.Format(
                    "\n\n  Label preview:\n    {0}\n",
                    preview.Replace("\\P", "  |  ").Replace("%%P", "+/-")));

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
                    double drop = topElevation - bottomElevation;
                    ErrorHandler.ShowSuccess(editor,
                        string.Format(
                            "Label updated.\n" +
                            "  Top : {0:0.00}'\n" +
                            "  FL {1}\" ({2}) = {3:0.0}'\n" +
                            "  Drop: {4:0.00}'",
                            topElevation, pipeSize, pipeDirection, bottomElevation, drop));
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
