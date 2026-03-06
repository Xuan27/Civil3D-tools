using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using MeasureDownLabel.Models;
using MeasureDownLabel.Services;
using MeasureDownLabel.Utilities;

namespace MeasureDownLabel.Commands
{
    /// <summary>
    /// AutoCAD command class for placing measure-down inlet labels as MLeaders
    /// </summary>
    public class MeasureDownCommand
    {
        private readonly ElevationPickService _elevPicker = new ElevationPickService();
        private readonly MultiLeaderService _mleaderSvc = new MultiLeaderService();

        /// <summary>
        /// Command: MEASUREDOWN
        /// Guides the user through picking/entering top and bottom elevations, pipe size,
        /// and direction, then places a formatted INLET MLeader label in the drawing.
        /// </summary>
        [CommandMethod("MEASUREDOWN")]
        public void MeasureDown()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor editor = doc.Editor;
            Database database = doc.Database;

            try
            {
                ErrorHandler.ShowBanner(editor, "MEASUREDOWN  —  Inlet Measure-Down Label");
                editor.WriteMessage(
                    "\n  This tool places a formatted INLET multileader label.\n" +
                    "  You will be asked for:\n" +
                    "    • Top of structure elevation  (pick COGO point or type)\n" +
                    "    • Flow-line invert elevation   (pick COGO point or type)\n" +
                    "    • Pipe size  (inches)\n" +
                    "    • Pipe direction  (e.g. N, NE, S45W)\n" +
                    "    • Label insertion point\n");

                // ── Step 1: Top elevation ────────────────────────────────────────
                double topElevation;
                Point3d topPoint;
                editor.WriteMessage("\n  STEP 1 of 4 — Top of Structure (Rim) Elevation");

                if (!_elevPicker.TryGetElevation(editor, database,
                        "Top of Structure", out topElevation, out topPoint))
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled at top elevation.");
                    return;
                }

                // ── Step 2: Bottom (invert) elevation ────────────────────────────
                double bottomElevation;
                Point3d bottomPoint;
                editor.WriteMessage("\n  STEP 2 of 4 — Flow Line (Invert) Elevation");

                if (!_elevPicker.TryGetElevation(editor, database,
                        "Flow Line", out bottomElevation, out bottomPoint))
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled at invert elevation.");
                    return;
                }

                // Sanity check — invert should be below rim
                if (bottomElevation >= topElevation)
                {
                    ErrorHandler.ShowWarning(editor,
                        string.Format(
                            "Flow line ({0:0.00}') is at or above top of structure ({1:0.00}'). " +
                            "Please verify your inputs.",
                            bottomElevation, topElevation));
                }

                // ── Step 3: Pipe size ────────────────────────────────────────────
                editor.WriteMessage("\n  STEP 3 of 4 — Pipe Size & Direction");

                PromptDoubleOptions sizeOpts = new PromptDoubleOptions(
                    "\n  Pipe diameter (inches): ")
                {
                    AllowNegative = false,
                    AllowZero = false
                };
                PromptDoubleResult sizeResult = editor.GetDouble(sizeOpts);

                if (sizeResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled at pipe size.");
                    return;
                }

                double pipeSize = sizeResult.Value;

                // ── Step 4: Pipe direction ───────────────────────────────────────
                PromptStringOptions dirOpts = new PromptStringOptions(
                    "\n  Pipe direction (e.g. N, NE, S45W): ")
                {
                    AllowSpaces = true
                };
                PromptResult dirResult = editor.GetString(dirOpts);

                if (dirResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled at pipe direction.");
                    return;
                }

                string pipeDirection = dirResult.StringResult.Trim().ToUpper();

                // ── Step 5: Label insertion / leader anchor point ────────────────
                editor.WriteMessage("\n  STEP 4 of 4 — Label Placement");
                editor.WriteMessage("\n  Pick the point on the structure to attach the leader arrow: ");

                PromptPointOptions insertOpts = new PromptPointOptions(string.Empty)
                {
                    AllowNone = false
                };
                PromptPointResult insertResult = editor.GetPoint(insertOpts);

                if (insertResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled at insertion point.");
                    return;
                }

                Point3d arrowPoint = insertResult.Value;

                editor.WriteMessage("\n  Pick where the label text should land: ");
                PromptPointOptions landingOpts = new PromptPointOptions(string.Empty)
                {
                    AllowNone = false,
                    UseBasePoint = true,
                    BasePoint = arrowPoint
                };
                PromptPointResult landingResult = editor.GetPoint(landingOpts);

                if (landingResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled at label landing point.");
                    return;
                }

                Point3d labelPoint = landingResult.Value;

                // ── Assemble input model ─────────────────────────────────────────
                MeasureDownInput input = new MeasureDownInput
                {
                    TopElevation    = topElevation,
                    BottomElevation = bottomElevation,
                    PipeSize        = pipeSize,
                    PipeDirection   = pipeDirection,
                    InsertionPoint  = arrowPoint,
                    LeaderPoint     = labelPoint
                };

                // ── Preview the label text ───────────────────────────────────────
                string preview = _mleaderSvc.BuildLabelText(input);
                editor.WriteMessage(string.Format(
                    "\n\n  Label preview (MTEXT format):\n    {0}\n",
                    preview.Replace("\\P", "  |  ")));

                // ── Confirm ──────────────────────────────────────────────────────
                PromptKeywordOptions confirmOpts = new PromptKeywordOptions(
                    "\n  Place label? [Yes/No] <Yes>: ");
                confirmOpts.Keywords.Add("Yes");
                confirmOpts.Keywords.Add("No");
                confirmOpts.Keywords.Default = "Yes";
                confirmOpts.AllowNone = true;

                PromptResult confirmResult = editor.GetKeywords(confirmOpts);

                if (confirmResult.Status == PromptStatus.Cancel ||
                    (confirmResult.Status == PromptStatus.OK &&
                     string.Equals(confirmResult.StringResult, "No", StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorHandler.ShowMessage(editor, "Label placement cancelled.");
                    return;
                }

                // ── Place the MLeader ────────────────────────────────────────────
                ObjectId placedId = _mleaderSvc.PlaceMultiLeader(database, input, editor);

                if (!placedId.IsNull)
                {
                    double dropFt = topElevation - bottomElevation;
                    ErrorHandler.ShowSuccess(editor,
                        string.Format(
                            "Label placed successfully.\n" +
                            "  Top:    {0:0.00}'\n" +
                            "  FL {1}\" ({2}) = {3:0.00}'\n" +
                            "  Drop:   {4:0.00}'",
                            topElevation, pipeSize, pipeDirection,
                            bottomElevation, dropFt));
                }
                else
                {
                    ErrorHandler.ShowWarning(editor, "Label could not be placed. Check drawing for errors.");
                }

                editor.WriteMessage(string.Format("\n{0}\n", new string('─', 60)));
            }
            catch (System.Exception ex)
            {
                ErrorHandler.HandleException(editor, ex, "MEASUREDOWN");
            }
        }
    }
}
