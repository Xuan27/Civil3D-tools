using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;

namespace MeasureDownLabel.Services
{
    /// <summary>
    /// Handles interactive elevation acquisition — either by clicking a COGO point
    /// directly (entity selection with filter) or by typing a value.
    /// </summary>
    public class ElevationPickService
    {
        /// <summary>
        /// Prompts the user to pick a COGO point or type an elevation manually.
        /// Uses keyword selection so the user can explicitly choose the input method.
        /// Returns true on success.
        /// </summary>
        public bool TryGetElevation(
            Editor editor,
            Database database,
            string promptLabel,
            out double elevation,
            out Point3d pickedPoint)
        {
            elevation = double.NaN;
            pickedPoint = Point3d.Origin;

            // Ask which input method the user wants
            PromptKeywordOptions kwOpts = new PromptKeywordOptions(
                string.Format("\n  {0} — input method [Point/Type] <Point>: ", promptLabel));
            kwOpts.Keywords.Add("Point");
            kwOpts.Keywords.Add("Type");
            kwOpts.Keywords.Default = "Point";
            kwOpts.AllowNone = true;

            PromptResult kwResult = editor.GetKeywords(kwOpts);

            if (kwResult.Status == PromptStatus.Cancel)
                return false;

            bool usePointPick = kwResult.Status == PromptStatus.None ||
                                string.Equals(kwResult.StringResult, "Point",
                                    System.StringComparison.OrdinalIgnoreCase);

            if (usePointPick)
                return TryPickCogoPoint(editor, database, promptLabel, out elevation, out pickedPoint);
            else
                return TryTypeElevation(editor, promptLabel, out elevation, out pickedPoint);
        }

        // -----------------------------------------------------------------------
        // Entity-pick path: user clicks directly on a COGO point.
        // GetEntity with a DXF type filter ensures only COGO points are accepted.
        // -----------------------------------------------------------------------
        private bool TryPickCogoPoint(
            Editor editor,
            Database database,
            string promptLabel,
            out double elevation,
            out Point3d pickedPoint)
        {
            elevation = double.NaN;
            pickedPoint = Point3d.Origin;

            PromptEntityOptions entOpts = new PromptEntityOptions(
                string.Format("\n  Click the COGO point for {0}: ", promptLabel));
            entOpts.SetRejectMessage("\n  That is not a COGO point. Please select a COGO point.");
            entOpts.AddAllowedClass(typeof(CogoPoint), exactMatch: false);

            PromptEntityResult entResult = editor.GetEntity(entOpts);

            if (entResult.Status == PromptStatus.Cancel)
                return false;

            if (entResult.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n  No COGO point selected.");
                return TryTypeElevation(editor, promptLabel, out elevation, out pickedPoint);
            }

            // Read elevation directly from the CogoPoint entity
            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                CogoPoint cogo = tr.GetObject(entResult.ObjectId, OpenMode.ForRead) as CogoPoint;
                if (cogo == null)
                {
                    editor.WriteMessage("\n  Could not read COGO point. Please type the elevation.");
                    tr.Commit();
                    return TryTypeElevation(editor, promptLabel, out elevation, out pickedPoint);
                }

                elevation = cogo.Elevation;
                pickedPoint = cogo.Location;
                tr.Commit();
            }

            editor.WriteMessage(string.Format(
                "\n  COGO point elevation: {0:0.00}'", elevation));
            return true;
        }

        // -----------------------------------------------------------------------
        // Typed-value path: user enters the elevation as a number.
        // -----------------------------------------------------------------------
        private bool TryTypeElevation(
            Editor editor,
            string promptLabel,
            out double elevation,
            out Point3d pickedPoint)
        {
            elevation = double.NaN;
            pickedPoint = Point3d.Origin;

            PromptDoubleOptions dpo = new PromptDoubleOptions(
                string.Format("\n  Enter {0} elevation: ", promptLabel))
            {
                AllowNone = false,
                AllowNegative = true
            };

            PromptDoubleResult dpr = editor.GetDouble(dpo);
            if (dpr.Status != PromptStatus.OK)
                return false;

            elevation = dpr.Value;
            pickedPoint = new Point3d(0, 0, elevation);
            return true;
        }
    }
}
