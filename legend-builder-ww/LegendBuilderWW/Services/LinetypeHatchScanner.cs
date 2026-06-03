using System;
using Autodesk.AutoCAD.DatabaseServices;
using LegendBuilderWW.Models;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Tallies the linetypes and hatch patterns used in model space and folds them into an existing
    /// DrawingUsage.
    ///
    /// SincpacC3D's symbols table only carries block symbols (inserted blocks, point markers, pipe
    /// structures), so it cannot tell us which linetypes/hatches are in use. Unlike block detection
    /// — which the old DrawingScanner got wrong for xrefs/nested blocks/COGO markers — linetype and
    /// hatch detection is a straightforward model-space walk, so we keep doing that ourselves and
    /// merge the result with the block tally read from the SincpacC3D table.
    /// </summary>
    public class LinetypeHatchScanner
    {
        public void ScanInto(Database db, DrawingUsage usage)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in modelSpace)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    Hatch hatch = ent as Hatch;
                    if (hatch != null)
                    {
                        Increment(usage.HatchPatternCounts, hatch.PatternName);
                        continue;
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
                    }
                }

                tr.Commit();
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
