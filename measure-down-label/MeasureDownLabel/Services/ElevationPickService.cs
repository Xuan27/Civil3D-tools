using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;

namespace MeasureDownLabel.Services
{
    /// <summary>
    /// Handles interactive elevation acquisition via four modes:
    ///   Point   — click directly on a COGO point (reads Elevation + Description)
    ///   Surface — select a Civil 3D surface, then pick an XY location
    ///   Type    — enter a numeric value at the keyboard
    ///   Invert  — pick a COGO point to view its description, then type the invert
    ///             elevation; computes and displays the drop from the top elevation
    ///             (only available when topElevation is supplied)
    /// </summary>
    public class ElevationPickService
    {
        /// <summary>
        /// Prompts the user to choose an input method and returns the elevation,
        /// the world-space point, and (for COGO picks) the point description.
        /// Pass <paramref name="topElevation"/> when collecting the flow-line value
        /// so the "Invert" option is available and the drop can be shown.
        /// Returns false if the user cancels.
        /// </summary>
        public bool TryGetElevation(
            Editor editor,
            Database database,
            string promptLabel,
            out double elevation,
            out Point3d pickedPoint,
            out string pointDescription,
            double topElevation = double.NaN)
        {
            elevation = double.NaN;
            pickedPoint = Point3d.Origin;
            pointDescription = string.Empty;

            bool hasTop = !double.IsNaN(topElevation);

            string menuText = hasTop
                ? string.Format("\n  {0} [Point/Surface/Type/Invert] <Point>: ", promptLabel)
                : string.Format("\n  {0} [Point/Surface/Type] <Point>: ", promptLabel);

            PromptKeywordOptions kwOpts = new PromptKeywordOptions(menuText);
            kwOpts.Keywords.Add("Point");
            kwOpts.Keywords.Add("Surface");
            kwOpts.Keywords.Add("Type");
            if (hasTop)
                kwOpts.Keywords.Add("Invert");
            kwOpts.Keywords.Default = "Point";
            kwOpts.AllowNone = true;

            PromptResult kwResult = editor.GetKeywords(kwOpts);

            if (kwResult.Status == PromptStatus.Cancel)
                return false;

            string choice = (kwResult.Status == PromptStatus.None)
                ? "Point"
                : kwResult.StringResult;

            if (string.Equals(choice, "Surface", StringComparison.OrdinalIgnoreCase))
                return TryPickSurfaceElevation(editor, database, promptLabel,
                    out elevation, out pickedPoint, out pointDescription);

            if (string.Equals(choice, "Type", StringComparison.OrdinalIgnoreCase))
                return TryTypeElevation(editor, promptLabel,
                    out elevation, out pickedPoint, out pointDescription);

            if (string.Equals(choice, "Invert", StringComparison.OrdinalIgnoreCase))
                return TryPickInvertFromDescription(editor, database, topElevation,
                    out elevation, out pickedPoint, out pointDescription);

            // Default: Point
            return TryPickCogoPoint(editor, database, promptLabel,
                out elevation, out pickedPoint, out pointDescription);
        }

        // -----------------------------------------------------------------------
        // Invert-from-description — pick a COGO point to read its description,
        // then the user types the invert elevation shown in that description.
        // Displays the computed drop = topElevation - invertElevation.
        // -----------------------------------------------------------------------
        private bool TryPickInvertFromDescription(
            Editor editor,
            Database database,
            double topElevation,
            out double elevation,
            out Point3d pickedPoint,
            out string pointDescription)
        {
            elevation = double.NaN;
            pickedPoint = Point3d.Origin;
            pointDescription = string.Empty;

            // Step 1: pick the COGO point (for location + description reference)
            PromptEntityOptions entOpts = new PromptEntityOptions(
                "\n  Click COGO point to view description: ");
            entOpts.SetRejectMessage("\n  That is not a COGO point. Please select a COGO point.");
            entOpts.AddAllowedClass(typeof(CogoPoint), exactMatch: false);

            PromptEntityResult entResult = editor.GetEntity(entOpts);

            if (entResult.Status == PromptStatus.Cancel)
                return false;

            if (entResult.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n  No COGO point selected — switching to manual entry.");
                return TryTypeElevation(editor, "Flow Line",
                    out elevation, out pickedPoint, out pointDescription);
            }

            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                CogoPoint cogo = tr.GetObject(entResult.ObjectId, OpenMode.ForRead) as CogoPoint;
                if (cogo == null)
                {
                    editor.WriteMessage("\n  Could not read COGO point — switching to manual entry.");
                    tr.Commit();
                    return TryTypeElevation(editor, "Flow Line",
                        out elevation, out pickedPoint, out pointDescription);
                }

                pickedPoint = cogo.Location;

                string desc    = (cogo.FullDescription ?? string.Empty).Trim();
                string rawDesc = (cogo.RawDescription  ?? string.Empty).Trim();

                pointDescription = string.IsNullOrEmpty(desc) ? rawDesc : desc;

                editor.WriteMessage(string.Format(
                    "\n  Point #{0}  Z: {1:0.00}'",
                    cogo.PointNumber, cogo.Elevation));

                if (!string.IsNullOrEmpty(pointDescription))
                    editor.WriteMessage(string.Format(
                        "\n  Description: {0}", pointDescription));

                if (!string.IsNullOrEmpty(rawDesc) && rawDesc != desc)
                    editor.WriteMessage(string.Format(
                        "\n  Raw Desc:    {0}", rawDesc));

                tr.Commit();
            }

            // Step 2: user types the invert elevation from the description
            PromptDoubleOptions dpo = new PromptDoubleOptions(
                string.IsNullOrEmpty(pointDescription)
                    ? "\n  Enter invert elevation: "
                    : string.Format("\n  Enter invert elevation from description [{0}]: ",
                        pointDescription))
            {
                AllowNone     = false,
                AllowNegative = true
            };

            PromptDoubleResult dpr = editor.GetDouble(dpo);
            if (dpr.Status != PromptStatus.OK)
                return false;

            elevation = dpr.Value;
            pickedPoint = new Point3d(pickedPoint.X, pickedPoint.Y, elevation);

            double drop = topElevation - elevation;
            editor.WriteMessage(string.Format(
                "\n  Invert: {0:0.00}'  |  Drop from top: {1:0.00}'",
                elevation, drop));

            return true;
        }

        // -----------------------------------------------------------------------
        // COGO Point pick — entity selection filtered to AECC_COGO_POINT.
        // Reads Elevation, PointNumber, RawDescription, and Description.
        // -----------------------------------------------------------------------
        private bool TryPickCogoPoint(
            Editor editor,
            Database database,
            string promptLabel,
            out double elevation,
            out Point3d pickedPoint,
            out string pointDescription)
        {
            elevation = double.NaN;
            pickedPoint = Point3d.Origin;
            pointDescription = string.Empty;

            PromptEntityOptions entOpts = new PromptEntityOptions(
                string.Format("\n  Click COGO point for {0}: ", promptLabel));
            entOpts.SetRejectMessage("\n  That is not a COGO point. Please select a COGO point.");
            entOpts.AddAllowedClass(typeof(CogoPoint), exactMatch: false);

            PromptEntityResult entResult = editor.GetEntity(entOpts);

            if (entResult.Status == PromptStatus.Cancel)
                return false;

            if (entResult.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n  No COGO point selected — switching to manual entry.");
                return TryTypeElevation(editor, promptLabel,
                    out elevation, out pickedPoint, out pointDescription);
            }

            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                CogoPoint cogo = tr.GetObject(entResult.ObjectId, OpenMode.ForRead) as CogoPoint;
                if (cogo == null)
                {
                    editor.WriteMessage("\n  Could not read COGO point — switching to manual entry.");
                    tr.Commit();
                    return TryTypeElevation(editor, promptLabel,
                        out elevation, out pickedPoint, out pointDescription);
                }

                elevation = cogo.Elevation;
                pickedPoint = cogo.Location;

                string desc    = (cogo.FullDescription ?? string.Empty).Trim();
                string rawDesc = (cogo.RawDescription  ?? string.Empty).Trim();

                pointDescription = string.IsNullOrEmpty(desc) ? rawDesc : desc;

                editor.WriteMessage(string.Format(
                    "\n  Point #{0}  Elev: {1:0.00}'",
                    cogo.PointNumber, elevation));

                if (!string.IsNullOrEmpty(pointDescription))
                    editor.WriteMessage(string.Format(
                        "\n  Description: {0}", pointDescription));

                if (!string.IsNullOrEmpty(rawDesc) && rawDesc != desc)
                    editor.WriteMessage(string.Format(
                        "\n  Raw Desc:    {0}", rawDesc));

                tr.Commit();
            }

            return true;
        }

        // -----------------------------------------------------------------------
        // Surface elevation pick — select a Civil 3D surface, then click a point.
        // Queries FindElevationAtXY on the surface at the picked XY location.
        // -----------------------------------------------------------------------
        private bool TryPickSurfaceElevation(
            Editor editor,
            Database database,
            string promptLabel,
            out double elevation,
            out Point3d pickedPoint,
            out string pointDescription)
        {
            elevation = double.NaN;
            pickedPoint = Point3d.Origin;
            pointDescription = string.Empty;

            // Step 1: select the surface entity
            PromptEntityOptions surfOpts = new PromptEntityOptions(
                "\n  Select the Civil 3D surface: ");
            surfOpts.SetRejectMessage("\n  That is not a surface. Please select a TIN or Grid surface.");
            surfOpts.AddAllowedClass(typeof(TinSurface), exactMatch: false);
            surfOpts.AddAllowedClass(typeof(GridSurface), exactMatch: false);

            PromptEntityResult surfResult = editor.GetEntity(surfOpts);

            if (surfResult.Status == PromptStatus.Cancel)
                return false;

            if (surfResult.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n  No surface selected — switching to manual entry.");
                return TryTypeElevation(editor, promptLabel,
                    out elevation, out pickedPoint, out pointDescription);
            }

            // Step 2: pick the XY location
            PromptPointOptions ptOpts = new PromptPointOptions(
                string.Format("\n  Pick location on surface for {0}: ", promptLabel))
            {
                AllowNone = false
            };

            PromptPointResult ptResult = editor.GetPoint(ptOpts);

            if (ptResult.Status != PromptStatus.OK)
                return false;

            Point3d clickPt = ptResult.Value;

            // Step 3: query surface elevation
            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                Autodesk.Civil.DatabaseServices.Surface surf =
                    tr.GetObject(surfResult.ObjectId, OpenMode.ForRead)
                    as Autodesk.Civil.DatabaseServices.Surface;

                if (surf == null)
                {
                    editor.WriteMessage("\n  Could not read surface — switching to manual entry.");
                    tr.Commit();
                    return TryTypeElevation(editor, promptLabel,
                        out elevation, out pickedPoint, out pointDescription);
                }

                try
                {
                    elevation = surf.FindElevationAtXY(clickPt.X, clickPt.Y);
                    pickedPoint = new Point3d(clickPt.X, clickPt.Y, elevation);
                    pointDescription = string.Format("Surface: {0}", surf.Name);

                    editor.WriteMessage(string.Format(
                        "\n  Surface \"{0}\"  Elev at point: {1:0.00}'",
                        surf.Name, elevation));
                }
                catch (Exception)
                {
                    editor.WriteMessage(
                        "\n  Point is outside surface boundary — switching to manual entry.");
                    tr.Commit();
                    return TryTypeElevation(editor, promptLabel,
                        out elevation, out pickedPoint, out pointDescription);
                }

                tr.Commit();
            }

            return true;
        }

        // -----------------------------------------------------------------------
        // Manual typed entry
        // -----------------------------------------------------------------------
        private bool TryTypeElevation(
            Editor editor,
            string promptLabel,
            out double elevation,
            out Point3d pickedPoint,
            out string pointDescription)
        {
            elevation = double.NaN;
            pickedPoint = Point3d.Origin;
            pointDescription = string.Empty;

            PromptDoubleOptions dpo = new PromptDoubleOptions(
                string.Format("\n  Enter {0} elevation: ", promptLabel))
            {
                AllowNone     = false,
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
