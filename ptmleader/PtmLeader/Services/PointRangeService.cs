using System.Collections.Generic;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;

namespace PtmLeader.Services
{
    /// <summary>
    /// Reads COGO point numbers from selected entities and computes a
    /// condensed range string (e.g. "1-5, 7, 9-11").
    /// </summary>
    public class PointRangeService
    {
        /// <summary>
        /// Opens each ObjectId as a CogoPoint and collects its PointNumber.
        /// Returns false (with an empty list) if no valid COGO points are found.
        /// </summary>
        public bool TryGetPointNumbers(Database database,
            ObjectIdCollection ids, out List<uint> pointNumbers)
        {
            pointNumbers = new List<uint>();

            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    CogoPoint pt = tr.GetObject(id, OpenMode.ForRead) as CogoPoint;
                    if (pt != null)
                        pointNumbers.Add(pt.PointNumber);
                }

                tr.Commit();
            }

            return pointNumbers.Count > 0;
        }

        /// <summary>
        /// Converts a list of point numbers into a condensed range string.
        /// Examples:
        ///   [1,2,3,4,5]   → "1-5"
        ///   [1,2,4,5,7]   → "1-2, 4-5, 7"
        ///   [1,3,5]       → "1, 3, 5"
        /// </summary>
        public string BuildRangeString(List<uint> pointNumbers)
        {
            if (pointNumbers == null || pointNumbers.Count == 0)
                return string.Empty;

            pointNumbers.Sort();

            StringBuilder sb = new StringBuilder();
            uint rangeStart = pointNumbers[0];
            uint prev       = pointNumbers[0];

            for (int i = 1; i < pointNumbers.Count; i++)
            {
                uint current = pointNumbers[i];

                if (current == prev + 1)
                {
                    // Still in a consecutive run
                    prev = current;
                    continue;
                }

                // Gap — flush the current run
                AppendRun(sb, rangeStart, prev);
                sb.Append(", ");
                rangeStart = current;
                prev       = current;
            }

            // Flush the final run
            AppendRun(sb, rangeStart, prev);

            return sb.ToString();
        }

        private static void AppendRun(StringBuilder sb, uint start, uint end)
        {
            if (start == end)
                sb.Append(start);
            else
                sb.AppendFormat("{0}-{1}", start, end);
        }
    }
}
