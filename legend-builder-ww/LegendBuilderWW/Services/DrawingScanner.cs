using System;
using Autodesk.AutoCAD.DatabaseServices;
using LegendBuilderWW.Models;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Walks the current drawing's model space and tallies which block names, linetypes,
    /// and hatch patterns are in use.
    /// </summary>
    public class DrawingScanner
    {
        public DrawingUsage Scan(Database db)
        {
            DrawingUsage usage = new DrawingUsage();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in modelSpace)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    Tally(ent, tr, usage);
                }

                tr.Commit();
            }

            return usage;
        }

        private static void Tally(Entity ent, Transaction tr, DrawingUsage usage)
        {
            BlockReference br = ent as BlockReference;
            if (br != null)
            {
                // All template blocks are static per the spec, so BlockReference.Name is sufficient.
                Increment(usage.BlockCounts, br.Name);
                return;
            }

            Hatch hatch = ent as Hatch;
            if (hatch != null)
            {
                Increment(usage.HatchPatternCounts, hatch.PatternName);
                return;
            }

            Curve curve = ent as Curve;
            if (curve != null)
            {
                string linetype = ResolveLinetype(curve, tr);
                if (!string.IsNullOrEmpty(linetype) &&
                    !string.Equals(linetype, "Continuous", StringComparison.OrdinalIgnoreCase))
                {
                    Increment(usage.LinetypeCounts, linetype);
                }
                return;
            }
        }

        private static string ResolveLinetype(Curve curve, Transaction tr)
        {
            string lt = curve.Linetype;
            if (!string.IsNullOrEmpty(lt) &&
                !string.Equals(lt, "BYLAYER", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(lt, "BYBLOCK", StringComparison.OrdinalIgnoreCase))
            {
                return lt;
            }

            try
            {
                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(curve.LayerId, OpenMode.ForRead);
                LinetypeTableRecord ltRec = (LinetypeTableRecord)tr.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead);
                return ltRec.Name;
            }
            catch
            {
                return null;
            }
        }

        private static void Increment(System.Collections.Generic.Dictionary<string, int> dict, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            int count;
            dict[key] = dict.TryGetValue(key, out count) ? count + 1 : 1;
        }
    }
}
