using Autodesk.AutoCAD.DatabaseServices;
using LegendBuilderWW.Models;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Reads which symbols are in use from a SincpacC3D "LegendBuilder" symbols Table.
    ///
    /// SincpacC3D already resolves every symbol category that our own DrawingScanner could not
    /// reliably find — inserted blocks, xref'd blocks, nested blocks, pipe-network structure
    /// symbols, and COGO point markers — and emits them as real block references inside the cells
    /// of a standard AutoCAD Table. We simply walk that table, collect the block name out of each
    /// block-content cell, and hand the tally to LegendMatcher (which keys template rows by block
    /// name). This replaces DrawingScanner entirely.
    /// </summary>
    public class SincpacTableReader
    {
        public DrawingUsage Read(Database db, ObjectId tableId)
        {
            DrawingUsage usage = new DrawingUsage();
            if (tableId.IsNull) return usage;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Table table = tr.GetObject(tableId, OpenMode.ForRead) as Table;
                if (table == null)
                {
                    tr.Commit();
                    return usage;
                }

                int rows = table.Rows.Count;
                int cols = table.Columns.Count;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        // A cell holding a block symbol exposes its block via BlockTableRecordId;
                        // text/title/header cells return ObjectId.Null and are skipped.
                        ObjectId btrId = TryGetCellBlock(table, r, c);
                        if (btrId.IsNull) continue;

                        string blockName = ResolveBlockName(tr, btrId);
                        if (!string.IsNullOrEmpty(blockName))
                        {
                            Increment(usage, blockName);
                        }
                    }
                }

                tr.Commit();
            }

            return usage;
        }

        private static ObjectId TryGetCellBlock(Table table, int row, int col)
        {
            try
            {
                Cell cell = table.Cells[row, col];
                return cell == null ? ObjectId.Null : cell.BlockTableRecordId;
            }
            catch
            {
                return ObjectId.Null;
            }
        }

        /// <summary>
        /// Returns the block name behind a cell's block content. SincpacC3D points the cell at the
        /// named block definition, so BlockTableRecord.Name matches the name RowParser reads from the
        /// template's block references (br.Name). Anonymous blocks (names beginning with '*') cannot
        /// be matched against the template and are skipped.
        /// </summary>
        private static string ResolveBlockName(Transaction tr, ObjectId btrId)
        {
            BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
            if (btr == null) return null;

            string name = btr.Name;
            if (string.IsNullOrEmpty(name) || name.StartsWith("*")) return null;
            return name;
        }

        private static void Increment(DrawingUsage usage, string blockName)
        {
            int existing;
            usage.BlockCounts[blockName] = usage.BlockCounts.TryGetValue(blockName, out existing)
                ? existing + 1
                : 1;
        }
    }
}
