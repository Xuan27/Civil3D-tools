using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using PtmLeader.Services;
using PtmLeader.Utilities;

namespace PtmLeader.Commands
{
    /// <summary>
    /// AutoCAD command that creates a multileader labelling the point-number
    /// range of the selected COGO points (e.g. "1-5" or "1-3, 5, 7-9").
    /// </summary>
    public class PtmLeaderCommand
    {
        private readonly PointRangeService    _rangeSvc   = new PointRangeService();
        private readonly MLeaderCreationService _mleaderSvc = new MLeaderCreationService();

        /// <summary>
        /// Command: PTMLEADER
        /// Select COGO points → computes range string → pick arrow tip →
        /// pick landing/text position → places a new MLeader.
        /// </summary>
        [CommandMethod("PTMLEADER")]
        public void PtmLeader()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor   editor   = doc.Editor;
            Database database = doc.Database;

            try
            {
                ErrorHandler.ShowBanner(editor, "PTMLEADER  -  Point Range Multileader");

                // ── Step 1: Select COGO points ───────────────────────────────────
                SelectionFilter filter = new SelectionFilter(new[]
                {
                    new Autodesk.AutoCAD.DatabaseServices.TypedValue(
                        (int)DxfCode.Start, "AECC_COGO_POINT")
                });

                PromptSelectionOptions selOpts = new PromptSelectionOptions
                {
                    MessageForAdding  = "\n  Select COGO points: ",
                    MessageForRemoval = "\n  Remove COGO points: "
                };

                PromptSelectionResult selResult = editor.GetSelection(selOpts, filter);

                if (selResult.Status == PromptStatus.Cancel)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                if (selResult.Status != PromptStatus.OK ||
                    selResult.Value == null || selResult.Value.Count == 0)
                {
                    ErrorHandler.ShowMessage(editor, "No COGO points selected.");
                    return;
                }

                ObjectIdCollection selectedIds = new ObjectIdCollection();
                foreach (SelectedObject obj in selResult.Value)
                {
                    if (obj != null)
                        selectedIds.Add(obj.ObjectId);
                }

                // ── Step 2: Read point numbers and build range ───────────────────
                List<uint> pointNumbers;
                if (!_rangeSvc.TryGetPointNumbers(database, selectedIds, out pointNumbers))
                {
                    ErrorHandler.ShowWarning(editor,
                        "Could not read point numbers from the selected entities.");
                    return;
                }

                string rangeText = _rangeSvc.BuildRangeString(pointNumbers);

                editor.WriteMessage(string.Format(
                    "\n  {0} point(s) selected.  Range: {1}", pointNumbers.Count, rangeText));

                // ── Step 3: Pick arrow tip ───────────────────────────────────────
                PromptPointOptions arrowOpts = new PromptPointOptions(
                    "\n  Pick the arrow tip location (on or near the points): ");
                PromptPointResult arrowResult = editor.GetPoint(arrowOpts);

                if (arrowResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                Point3d arrowPoint = arrowResult.Value;

                // ── Step 4: Pick landing / text position ─────────────────────────
                PromptPointOptions landingOpts =
                    new PromptPointOptions("\n  Pick the text landing position: ");
                landingOpts.UseBasePoint = true;
                landingOpts.BasePoint    = arrowPoint;

                PromptPointResult landingResult = editor.GetPoint(landingOpts);

                if (landingResult.Status != PromptStatus.OK)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                Point3d landingPoint = landingResult.Value;

                // ── Step 5: Create the MLeader ───────────────────────────────────
                bool created = _mleaderSvc.CreateMLeader(
                    database, arrowPoint, landingPoint, rangeText, editor);

                if (created)
                {
                    ErrorHandler.ShowSuccess(editor,
                        string.Format("MLeader created with content: \"{0}\"", rangeText));
                }

                editor.WriteMessage(string.Format("\n{0}\n", new string('-', 60)));
            }
            catch (System.Exception ex)
            {
                ErrorHandler.HandleException(editor, ex, "PTMLEADER");
            }
        }
    }
}
