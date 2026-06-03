using System.Collections.Generic;
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
    /// of a standard AutoCAD Table. We walk that table, collect the block name out of each
    /// block-content cell, and (for blocks missing from our template) the description text in the
    /// same row so an orphan symbol can be labelled. This replaces DrawingScanner for blocks.
    /// </summary>
    public class SincpacTableReader
    {
        /// <summary>
        /// Block name (as read from a symbol cell) → description text from the same table row.
        /// Populated by Read; used to label orphan blocks. Case-insensitive keys.
        /// </summary>
        public Dictionary<string, string> BlockDescriptions { get; } =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

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
                    // Gather this row's block cells and text cells, then pair each block with the
                    // nearest description text so an orphan block can carry a readable label.
                    List<KeyValuePair<int, string>> rowBlocks = new List<KeyValuePair<int, string>>();
                    List<KeyValuePair<int, string>> rowTexts = new List<KeyValuePair<int, string>>();

                    for (int c = 0; c < cols; c++)
                    {
                        ObjectId btrId = TryGetCellBlock(table, r, c);
                        if (!btrId.IsNull)
                        {
                            string blockName = ResolveBlockName(tr, btrId);
                            if (!string.IsNullOrEmpty(blockName))
                            {
                                Increment(usage, blockName);
                                rowBlocks.Add(new KeyValuePair<int, string>(c, blockName));
                            }
                        }
                        else
                        {
                            string text = TryGetCellText(table, r, c);
                            if (!string.IsNullOrEmpty(text))
                            {
                                rowTexts.Add(new KeyValuePair<int, string>(c, text));
                            }
                        }
                    }

                    PairBlocksWithDescriptions(rowBlocks, rowTexts);
                }

                tr.Commit();
            }

            return usage;
        }

        private void PairBlocksWithDescriptions(
            List<KeyValuePair<int, string>> blocks,
            List<KeyValuePair<int, string>> texts)
        {
            foreach (KeyValuePair<int, string> block in blocks)
            {
                if (texts.Count == 0) continue;
                if (BlockDescriptions.ContainsKey(block.Value)) continue;

                // Prefer the nearest text to the right of the symbol cell; else the nearest overall.
                string best = null;
                int bestDist = int.MaxValue;
                foreach (KeyValuePair<int, string> text in texts)
                {
                    if (text.Key <= block.Key) continue;
                    int dist = text.Key - block.Key;
                    if (dist < bestDist) { bestDist = dist; best = text.Value; }
                }
                if (best == null)
                {
                    bestDist = int.MaxValue;
                    foreach (KeyValuePair<int, string> text in texts)
                    {
                        int dist = System.Math.Abs(text.Key - block.Key);
                        if (dist < bestDist) { bestDist = dist; best = text.Value; }
                    }
                }

                if (!string.IsNullOrEmpty(best))
                {
                    BlockDescriptions[block.Value] = best.Trim();
                }
            }
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

        private static string TryGetCellText(Table table, int row, int col)
        {
            try
            {
                Cell cell = table.Cells[row, col];
                if (cell == null) return null;
                object value = cell.Value;
                if (value == null) return null;
                string s = value as string;
                return (s ?? value.ToString()).Trim();
            }
            catch
            {
                return null;
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
