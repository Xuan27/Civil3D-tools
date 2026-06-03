using System;
using System.Collections.Generic;

namespace LegendBuilderWW.Models
{
    /// <summary>
    /// Counts of distinct symbols in use, indexed case-insensitively.
    /// Populated from the SincpacC3D symbols table by SincpacTableReader and consumed by LegendMatcher.
    /// </summary>
    public class DrawingUsage
    {
        public Dictionary<string, int> BlockCounts { get; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, int> LinetypeCounts { get; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, int> HatchPatternCounts { get; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public int GetCount(RowType type, string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;

            Dictionary<string, int> source;
            switch (type)
            {
                case RowType.Block: source = BlockCounts; break;
                case RowType.Linetype: source = LinetypeCounts; break;
                case RowType.Hatch: source = HatchPatternCounts; break;
                default: return 0;
            }

            int count;
            return source.TryGetValue(key, out count) ? count : 0;
        }
    }
}
