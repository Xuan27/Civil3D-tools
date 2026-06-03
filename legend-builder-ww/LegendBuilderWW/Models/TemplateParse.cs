using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace LegendBuilderWW.Models
{
    /// <summary>
    /// Output of RowParser: the parsed legend rows plus the template's title block
    /// (the "LEGEND" text and underline bar) and the Y of the top-most row, which the
    /// emitter needs to place the title a consistent distance above the re-stacked rows.
    /// </summary>
    public class TemplateParse
    {
        public List<LegendRow> Rows { get; } = new List<LegendRow>();

        /// <summary>
        /// ObjectIds of the title entities (LEGEND text + bar) inside the template BTR.
        /// Empty if no title was detected.
        /// </summary>
        public List<ObjectId> TitleEntityIds { get; } = new List<ObjectId>();

        /// <summary>
        /// RowOrigin.Y of the top-most parsed row in the template's coordinate space. The emitter
        /// maps this to the top of the output legend, so the title keeps its template gap above
        /// the first row regardless of which rows are dropped.
        /// </summary>
        public double TopRowOriginY { get; set; }
    }
}
