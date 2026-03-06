using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;

namespace MeasureDownLabel.Services
{
    /// <summary>
    /// Handles interactive elevation acquisition — either by picking a COGO point
    /// or by typing a value directly at the command prompt.
    /// </summary>
    public class ElevationPickService
    {
        /// <summary>
        /// Prompts the user to either click a COGO point or type an elevation.
        /// Returns true and sets <paramref name="elevation"/> on success.
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

            editor.WriteMessage(string.Format(
                "\n  {0} — click a COGO point or type a value <Enter to skip>: ", promptLabel));

            // First, try a point pick (allows snapping to COGO points)
            PromptPointOptions ppo = new PromptPointOptions(string.Empty)
            {
                AllowNone = true,
                AllowArbitraryInput = true
            };

            PromptPointResult ppr = editor.GetPoint(ppo);

            if (ppr.Status == PromptStatus.Cancel)
                return false;

            if (ppr.Status == PromptStatus.OK)
            {
                Point3d pt = ppr.Value;

                // Try to snap to a COGO point elevation at this location
                double cogoElev;
                if (TryGetCogoElevationAt(database, pt, out cogoElev))
                {
                    elevation = cogoElev;
                    pickedPoint = new Point3d(pt.X, pt.Y, cogoElev);
                    editor.WriteMessage(string.Format(
                        "\n  Elevation read from COGO point: {0:0.00}'", elevation));
                    return true;
                }

                // Use the Z of the picked point (snapped elevation) if non-zero
                if (pt.Z != 0.0)
                {
                    elevation = pt.Z;
                    pickedPoint = pt;
                    editor.WriteMessage(string.Format(
                        "\n  Elevation from pick: {0:0.00}'", elevation));
                    return true;
                }
            }

            // Fall back: ask for a typed value
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
            pickedPoint = new Point3d(
                pickedPoint.X, pickedPoint.Y, elevation);
            return true;
        }

        // -----------------------------------------------------------------------
        // Searches nearby COGO points and returns the elevation of the closest one
        // within a small tolerance of the picked XY location.
        // -----------------------------------------------------------------------
        private bool TryGetCogoElevationAt(Database database, Point3d pickPt, out double elevation)
        {
            elevation = double.NaN;
            double tolerance = 1.0; // drawing units

            try
            {
                using (Transaction tr = database.TransactionManager.StartTransaction())
                {
                    BlockTableRecord modelSpace = tr.GetObject(
                        SymbolUtilityServices.GetBlockModelSpaceId(database),
                        OpenMode.ForRead) as BlockTableRecord;

                    double bestDist = double.MaxValue;

                    foreach (ObjectId id in modelSpace)
                    {
                        CogoPoint pt = tr.GetObject(id, OpenMode.ForRead) as CogoPoint;
                        if (pt == null) continue;

                        double dx = pt.Location.X - pickPt.X;
                        double dy = pt.Location.Y - pickPt.Y;
                        double dist = System.Math.Sqrt(dx * dx + dy * dy);

                        if (dist < tolerance && dist < bestDist)
                        {
                            bestDist = dist;
                            elevation = pt.Elevation;
                        }
                    }

                    tr.Commit();
                }
            }
            catch { /* if Civil 3D objects unavailable, fall through */ }

            return !double.IsNaN(elevation);
        }
    }
}
