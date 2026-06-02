using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using LegendBuilderWW.Config;
using LegendBuilderWW.Models;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Parses the Vertical Legend BlockTableRecord into LegendRow records.
    /// Strategy:
    ///   1. Walk every entity inside the BTR, collect its ObjectId + entity bounds centroid.
    ///   2. Skip title elements (LEGEND text, green underline bar) by Y threshold.
    ///   3. Cluster the remaining entities into rows by their Y-centroid (one bucket per row,
    ///      grouped within Settings.RowGroupingTolerance).
    ///   4. For each row, split into left/right column at the row's median X.
    ///   5. Inside each column cell, classify the entities into symbol-entities + one description text.
    /// </summary>
    public class RowParser
    {
        private readonly Settings _settings;

        public RowParser(Settings settings)
        {
            _settings = settings;
        }

        public List<LegendRow> Parse(Database db, ObjectId templateBtrId)
        {
            List<LegendRow> rows = new List<LegendRow>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(templateBtrId, OpenMode.ForRead);

                List<EntityInfo> entities = CollectEntities(tr, btr);
                if (entities.Count == 0)
                {
                    tr.Commit();
                    return rows;
                }

                double titleCutoffY = ResolveTitleCutoff(entities);
                List<EntityInfo> body = entities.Where(e => e.CentroidY < titleCutoffY).ToList();

                List<List<EntityInfo>> rowBuckets = ClusterIntoRows(body, _settings.RowGroupingTolerance);

                double medianX = ComputeMedianX(body);

                foreach (List<EntityInfo> rowEntities in rowBuckets)
                {
                    List<EntityInfo> leftCol = rowEntities.Where(e => e.CentroidX < medianX).ToList();
                    List<EntityInfo> rightCol = rowEntities.Where(e => e.CentroidX >= medianX).ToList();

                    LegendRow leftRow = BuildRow(tr, leftCol, columnIndex: 0);
                    if (leftRow != null) rows.Add(leftRow);

                    LegendRow rightRow = BuildRow(tr, rightCol, columnIndex: 1);
                    if (rightRow != null) rows.Add(rightRow);
                }

                tr.Commit();
            }

            return rows;
        }

        private List<EntityInfo> CollectEntities(Transaction tr, BlockTableRecord btr)
        {
            List<EntityInfo> list = new List<EntityInfo>();
            foreach (ObjectId id in btr)
            {
                Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;

                Extents3d? bounds = TryGetBounds(ent);
                if (bounds == null) continue;

                Point3d min = bounds.Value.MinPoint;
                Point3d max = bounds.Value.MaxPoint;
                Point3d centroid = new Point3d((min.X + max.X) / 2.0, (min.Y + max.Y) / 2.0, 0);

                list.Add(new EntityInfo
                {
                    Id = id,
                    Entity = ent,
                    Bounds = bounds.Value,
                    CentroidX = centroid.X,
                    CentroidY = centroid.Y
                });
            }
            return list;
        }

        private static Extents3d? TryGetBounds(Entity ent)
        {
            try { return ent.GeometricExtents; }
            catch { return null; }
        }

        private double ResolveTitleCutoff(List<EntityInfo> entities)
        {
            if (_settings.TitleEntityYThreshold.HasValue)
            {
                return _settings.TitleEntityYThreshold.Value;
            }

            // Auto-detect: the title row sits well above the symbol rows. Find the largest Y-gap
            // between consecutive entities (by descending centroid Y). Anything above the gap is title.
            List<double> ys = entities.Select(e => e.CentroidY).OrderByDescending(y => y).ToList();
            if (ys.Count < 3) return double.PositiveInfinity;

            double biggestGap = 0;
            double cutoff = double.PositiveInfinity;
            for (int i = 1; i < ys.Count; i++)
            {
                double gap = ys[i - 1] - ys[i];
                if (gap > biggestGap)
                {
                    biggestGap = gap;
                    cutoff = (ys[i - 1] + ys[i]) / 2.0;
                }
            }

            // If the largest gap is suspiciously small, assume no title.
            double medianRowPitch = MedianAdjacentDelta(ys);
            if (biggestGap < medianRowPitch * 1.8)
            {
                return double.PositiveInfinity;
            }
            return cutoff;
        }

        private static double MedianAdjacentDelta(List<double> sortedDesc)
        {
            List<double> deltas = new List<double>();
            for (int i = 1; i < sortedDesc.Count; i++)
            {
                deltas.Add(sortedDesc[i - 1] - sortedDesc[i]);
            }
            deltas.Sort();
            return deltas.Count == 0 ? 0 : deltas[deltas.Count / 2];
        }

        private static List<List<EntityInfo>> ClusterIntoRows(List<EntityInfo> entities, double tolerance)
        {
            List<EntityInfo> sorted = entities.OrderByDescending(e => e.CentroidY).ToList();
            List<List<EntityInfo>> buckets = new List<List<EntityInfo>>();
            List<EntityInfo> current = null;
            double anchorY = double.NaN;

            foreach (EntityInfo info in sorted)
            {
                if (current == null || Math.Abs(info.CentroidY - anchorY) > tolerance)
                {
                    current = new List<EntityInfo>();
                    buckets.Add(current);
                    anchorY = info.CentroidY;
                }
                current.Add(info);
            }
            return buckets;
        }

        private static double ComputeMedianX(List<EntityInfo> entities)
        {
            if (entities.Count == 0) return 0;
            List<double> xs = entities.Select(e => e.CentroidX).OrderBy(x => x).ToList();
            return xs[xs.Count / 2];
        }

        private static LegendRow BuildRow(Transaction tr, List<EntityInfo> cellEntities, int columnIndex)
        {
            if (cellEntities == null || cellEntities.Count == 0) return null;

            // Description = the text/mtext entity. There should be exactly one per cell.
            EntityInfo descInfo = cellEntities.FirstOrDefault(e => e.Entity is DBText || e.Entity is MText);
            if (descInfo == null)
            {
                // No description means this isn't a real legend row (could be stray geometry).
                return null;
            }

            string description = ExtractText(descInfo.Entity);

            List<EntityInfo> symbolInfos = cellEntities.Where(e => e != descInfo).ToList();
            if (symbolInfos.Count == 0) return null;

            LegendRow row = new LegendRow
            {
                Description = description,
                DescriptionEntityId = descInfo.Id,
                SymbolEntityIds = symbolInfos.Select(s => s.Id).ToList(),
                ColumnIndex = columnIndex
            };

            ClassifyAndKey(tr, symbolInfos, row);

            double minX = symbolInfos.Min(s => s.Bounds.MinPoint.X);
            double minY = symbolInfos.Min(s => s.Bounds.MinPoint.Y);
            row.RowOrigin = new Point3d(minX, minY, 0);

            return row;
        }

        private static string ExtractText(Entity ent)
        {
            DBText dbt = ent as DBText;
            if (dbt != null) return (dbt.TextString ?? string.Empty).Trim();

            MText mt = ent as MText;
            if (mt != null) return (mt.Contents ?? string.Empty).Trim();

            return string.Empty;
        }

        private static void ClassifyAndKey(Transaction tr, List<EntityInfo> symbolInfos, LegendRow row)
        {
            // Priority: BlockReference > Hatch > Curve-with-linetype.
            EntityInfo blockRef = symbolInfos.FirstOrDefault(s => s.Entity is BlockReference);
            if (blockRef != null)
            {
                BlockReference br = (BlockReference)blockRef.Entity;
                row.RowType = RowType.Block;
                row.Key = br.Name;
                return;
            }

            EntityInfo hatch = symbolInfos.FirstOrDefault(s => s.Entity is Hatch);
            if (hatch != null)
            {
                Hatch h = (Hatch)hatch.Entity;
                row.RowType = RowType.Hatch;
                row.Key = h.PatternName;
                return;
            }

            EntityInfo curve = symbolInfos.FirstOrDefault(s => s.Entity is Curve);
            if (curve != null)
            {
                Curve c = (Curve)curve.Entity;
                row.RowType = RowType.Linetype;
                row.Key = ResolveLinetypeName(tr, c);
                return;
            }

            // Unrecognized — treat as a block-keyed row with empty key so it stays out of matches.
            row.RowType = RowType.Block;
            row.Key = string.Empty;
        }

        private static string ResolveLinetypeName(Transaction tr, Curve c)
        {
            // Curve.Linetype may be "BYLAYER" / "BYBLOCK" — resolve through the layer using the
            // existing parser transaction (avoid nested transactions).
            string lt = c.Linetype;
            if (!string.IsNullOrEmpty(lt) &&
                !string.Equals(lt, "BYLAYER", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(lt, "BYBLOCK", StringComparison.OrdinalIgnoreCase))
            {
                return lt;
            }

            try
            {
                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(c.LayerId, OpenMode.ForRead);
                LinetypeTableRecord ltRec = (LinetypeTableRecord)tr.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead);
                return ltRec.Name;
            }
            catch
            {
                return null;
            }
        }

        private class EntityInfo
        {
            public ObjectId Id;
            public Entity Entity;
            public Extents3d Bounds;
            public double CentroidX;
            public double CentroidY;
        }
    }
}
