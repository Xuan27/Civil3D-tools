namespace MeasureDownLabel.Models
{
    /// <summary>
    /// Holds the user-supplied data needed to build a measure-down label
    /// </summary>
    public class MeasureDownInput
    {
        /// <summary>Top of structure (rim) elevation, feet</summary>
        public double TopElevation { get; set; }

        /// <summary>Flow line (invert) elevation, feet</summary>
        public double BottomElevation { get; set; }

        /// <summary>Nominal pipe diameter in inches, e.g. 12 for 12"</summary>
        public double PipeSize { get; set; }

        /// <summary>Pipe direction descriptor, e.g. "N", "NE", "S45W"</summary>
        public string PipeDirection { get; set; }
    }
}
