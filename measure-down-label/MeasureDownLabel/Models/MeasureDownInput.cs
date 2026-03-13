using System.Collections.Generic;

namespace MeasureDownLabel.Models
{
    /// <summary>
    /// A single flow-line (invert) entry: pipe size, direction, and elevation.
    /// </summary>
    public class FlowLineEntry
    {
        /// <summary>Flow line (invert) elevation, feet</summary>
        public double Elevation { get; set; }

        /// <summary>Nominal pipe diameter in inches, e.g. 12 for 12"</summary>
        public double PipeSize { get; set; }

        /// <summary>Pipe direction descriptor, e.g. "N", "NE", "S45W"</summary>
        public string PipeDirection { get; set; }
    }

    /// <summary>
    /// Holds the user-supplied data needed to build a measure-down label.
    /// Supports one or more flow-line entries.
    /// </summary>
    public class MeasureDownInput
    {
        /// <summary>Structure type label: "TOP", "INLET", or "RIM"</summary>
        public string StructureType { get; set; } = "TOP";

        /// <summary>Top of structure elevation, feet</summary>
        public double TopElevation { get; set; }

        /// <summary>One or more flow-line (invert) entries</summary>
        public List<FlowLineEntry> FlowLines { get; set; } = new List<FlowLineEntry>();
    }
}
