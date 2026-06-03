using System.Collections.Generic;
using LegendBuilderWW.Models;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Joins parsed template rows with the block tally read from the SincpacC3D symbols table
    /// (SincpacTableReader). Rows that are used in the drawing
    /// are returned pre-checked; rows that aren't are still included (unchecked) so the user can
    /// force-include symbols they're about to place.
    /// </summary>
    public class LegendMatcher
    {
        public List<MatchedRow> Match(List<LegendRow> templateRows, DrawingUsage usage)
        {
            List<MatchedRow> result = new List<MatchedRow>(templateRows.Count);

            foreach (LegendRow row in templateRows)
            {
                int count = usage.GetCount(row.RowType, row.Key);
                MatchedRow matched = new MatchedRow
                {
                    Source = row,
                    CountInDrawing = count,
                    IsUsedInDrawing = count > 0,
                    IncludeInOutput = count > 0
                };
                result.Add(matched);
            }
            return result;
        }
    }
}
