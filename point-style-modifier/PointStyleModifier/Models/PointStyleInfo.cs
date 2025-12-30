using Autodesk.AutoCAD.DatabaseServices;

namespace PointStyleModifier.Models
{
    /// <summary>
    /// Data transfer object representing a Civil 3D point style
    /// </summary>
    public class PointStyleInfo
    {
        /// <summary>
        /// The ObjectId of the point style in the Civil 3D database
        /// </summary>
        public ObjectId StyleId { get; set; }

        /// <summary>
        /// The name of the point style
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the point style
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Returns the name for display in UI controls (ListBox, ComboBox, etc.)
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
