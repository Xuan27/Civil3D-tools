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
        private readonly MultiLeaderService   _mleaderSvc = new MultiLeaderService();

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

            Editor   editor   = doc.Editor;
            Database database = doc.Database;

            try
            {
                ErrorHandler.ShowBanner(editor, "MEASUREDOWN  -  Inlet Measure-Down Label");
                editor.WriteMessage(
                    "\n  This tool places a formatted INLET multileader label.\n" +
                    "  Elevation inputs accept:\n" +
                    "    Point   - click a COGO point (reads elevation + description)\n" +
                    "    Surface - select a Civil 3D surface then pick a location\n" +
                    "    Type    - enter a value manually\n");

                // ── Step 1: Top of structure elevation ───────────────────────────
                double topElevation;
                Point3d topPoint;
                string  topDescription;

                editor.WriteMessage("\n  STEP 1 of 4 - Top of Structure (Rim) Elevation");

                if (!_elevPicker.TryGetElevation(editor, database,
                        "Top of Structure",
                        out topElevation, out topPoint, out topDescription))
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                // ── Step 2: Flow line (invert) elevation ─────────────────────────
                double  bottomElevation;
                Point3d bottomPoint;
                string  bottomDescription;

                editor.WriteMessage("\n  STEP 2 of 4 - Flow Line (Invert) Elevation");

                if (!_elevPicker.TryGetElevation(editor, database,
                        "Flow Line",
                        out bottomElevation, out bottomPoint, out bottomDescription))
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                // Sanity check
                if (bottomElevation >= topElevation)
                {
                    ErrorHandler.ShowWarning(editor,
                        string.Format(
                            "Flow line ({0:0.0}') is at or above top of structure ({1:0.00}'). " +
                            "Please verify your inputs.",
                            bottomElevation, topElevation));
                }

                // ── Show collected COGO descriptions as reference ─────────────────
                // Displayed before pipe size/direction so the user can reference
                // coded description data (e.g. pipe size/direction from field coding).
                bool hasTopDesc    = !string.IsNullOrEmpty(topDescription);
                bool hasBottomDesc = !string.IsNullOrEmpty(bottomDescription);

                if (hasTopDesc || hasBottomDesc)
                {
                    editor.WriteMessage("\n\n  --- Point Reference Information ---");
                    if (hasTopDesc)
                        editor.WriteMessage(string.Format(
                            "\n  Top point desc :  {0}", topDescription));
                    if (hasBottomDesc)
                        editor.WriteMessage(string.Format(
                            "\n  FL  point desc :  {0}", bottomDescription));
                    editor.WriteMessage("\n");
                }

                // ── Step 3: Pipe size ────────────────────────────────────────────
                editor.WriteMessage("\n  STEP 3 of 4 - Pipe Size & Direction");

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

                // ── Step 4: Pipe direction ───────────────────────────────────────
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

                // ── Label placement ──────────────────────────────────────────────
                editor.WriteMessage("\n  STEP 4 of 4 - Label Placement");
                editor.WriteMessage("\n  Pick the arrowhead point on the structure: ");

                PromptPointOptions insertOpts = new PromptPointOptions(string.Empty)
                {
                    AllowNone = false
                };
                PromptPointResult insertResult = editor.GetPoint(insertOpts);

                if (insertResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                Point3d arrowPoint = insertResult.Value;

                editor.WriteMessage("\n  Pick where the label text should land: ");
                PromptPointOptions landingOpts = new PromptPointOptions(string.Empty)
                {
                    AllowNone     = false,
                    UseBasePoint  = true,
                    BasePoint     = arrowPoint
                };
                PromptPointResult landingResult = editor.GetPoint(landingOpts);

                if (landingResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                Point3d labelPoint = landingResult.Value;

                // ── Assemble model ───────────────────────────────────────────────
                MeasureDownInput input = new MeasureDownInput
                {
                    TopElevation    = topElevation,
                    BottomElevation = bottomElevation,
                    PipeSize        = pipeSize,
                    PipeDirection   = pipeDirection,
                    InsertionPoint  = arrowPoint,
                    LeaderPoint     = labelPoint
                };

                // ── Preview ──────────────────────────────────────────────────────
                string preview = _mleaderSvc.BuildLabelText(input);
                editor.WriteMessage(string.Format(
                    "\n\n  Label preview:\n    {0}\n",
                    preview.Replace("\\P", "  |  ").Replace("%%P", "+/-")));

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
                     string.Equals(confirmResult.StringResult, "No",
                         StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorHandler.ShowMessage(editor, "Label placement cancelled.");
                    return;
                }

                // ── Place MLeader ────────────────────────────────────────────────
                ObjectId placedId = _mleaderSvc.PlaceMultiLeader(database, input, editor);

                if (!placedId.IsNull)
                {
                    double drop = topElevation - bottomElevation;
                    ErrorHandler.ShowSuccess(editor,
                        string.Format(
                            "Label placed.\n" +
                            "  Top : {0:0.00}'\n" +
                            "  FL {1}\" ({2}) = {3:0.0}'\n" +
                            "  Drop: {4:0.00}'",
                            topElevation, pipeSize, pipeDirection, bottomElevation, drop));
                }
                else
                {
                    ErrorHandler.ShowWarning(editor, "Label could not be placed. Check drawing for errors.");
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
