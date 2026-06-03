using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace LegendBuilderWW.Models
{
    /// <summary>
    /// A single row parsed from the Vertical Legend block: one symbol cell + one description.
    /// Holds ObjectIds of the source entities inside the template BTR; cloning copies them
    /// (with new positions) into the output BTR.
    /// </summary>
    public class LegendRow
    {
        public RowType RowType { get; set; }

        /// <summary>
        /// Matching key. Block name for RowType.Block, linetype name for Linetype, hatch pattern for Hatch.
        /// Compared case-insensitively against the block tally read from the SincpacC3D table.
        /// </summary>
        public string Key { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// ObjectIds of entities inside the template BTR that make up the symbol cell. Multiple ids
        /// support symbols built from several entities (e.g. POWER POLE WITH LIGHT).
        /// </summary>
        public List<ObjectId> SymbolEntityIds { get; set; } = new List<ObjectId>();

        /// <summary>
        /// ObjectId of the description Text/MText entity inside the template BTR.
        /// </summary>
        public ObjectId DescriptionEntityId { get; set; }

        /// <summary>
        /// Bottom-left of the row's bounding box in the template's coordinate space. Used to compute
        /// row pitch and column X positions when re-emitting.
        /// </summary>
        public Point3d RowOrigin { get; set; }

        /// <summary>
        /// Which column (0-based) the row was in within the template (the template has two columns).
        /// </summary>
        public int ColumnIndex { get; set; }
    }
}
