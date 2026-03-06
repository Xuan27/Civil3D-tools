namespace MeasureDownLabel.Models
{
    /// <summary>
    /// Holds all user-supplied inputs for a single measure-down label placement
    /// </summary>
    public class MeasureDownInput
    {
        /// <summary>Top of structure (rim) elevation, feet</summary>
        public double TopElevation { get; set; }

        /// <summary>Flow line (invert) elevation at bottom, feet</summary>
        public double BottomElevation { get; set; }

        /// <summary>Nominal pipe diameter in inches, e.g. 12 for 12"</summary>
        public double PipeSize { get; set; }

        /// <summary>Pipe direction descriptor, e.g. "N", "NE", "S 45°W"</summary>
        public string PipeDirection { get; set; }

        /// <summary>World-space insertion point for the multileader</summary>
        public Autodesk.AutoCAD.Geometry.Point3d InsertionPoint { get; set; }

        /// <summary>World-space landing / leader end point</summary>
        public Autodesk.AutoCAD.Geometry.Point3d LeaderPoint { get; set; }
    }
}
