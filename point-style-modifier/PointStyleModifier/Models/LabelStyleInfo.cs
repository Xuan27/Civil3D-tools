using Autodesk.AutoCAD.DatabaseServices;

namespace PointStyleModifier.Models
{
    /// <summary>
    /// Data transfer object representing a Civil 3D label style
    /// </summary>
    public class LabelStyleInfo
    {
        /// <summary>
        /// The ObjectId of the label style in the Civil 3D database
        /// </summary>
        public ObjectId StyleId { get; set; }

        /// <summary>
        /// The name of the label style
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the label style
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
