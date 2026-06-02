using System;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using LegendBuilderWW.Models;
using DBObject = Autodesk.AutoCAD.DatabaseServices.DBObject;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Walks the current drawing's model space and tallies which block names, linetypes,
    /// and hatch patterns are in use.
    ///
    /// For COGO points the marker block is not a BlockReference entity in model space — it
    /// is drawn from the point's PointStyle. We resolve each used PointStyle once and add
    /// its marker block name to the block tally, weighted by the number of points using it.
    /// </summary>
    public class DrawingScanner
    {
        public DrawingUsage Scan(Database db)
        {
            DrawingUsage usage = new DrawingUsage();
            Dictionary<ObjectId, int> styleUsage = new Dictionary<ObjectId, int>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in modelSpace)
                {
                    DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                    if (obj == null) continue;

                    CogoPoint point = obj as CogoPoint;
                    if (point != null)
                    {
                        TallyCogoPoint(point, styleUsage);
                        continue;
                    }

                    Entity ent = obj as Entity;
                    if (ent != null)
                    {
                        Tally(ent, tr, usage);
                    }
                }

                ResolveStylesToBlockCounts(tr, styleUsage, usage);

                tr.Commit();
            }

            return usage;
        }

        private static void TallyCogoPoint(CogoPoint point, Dictionary<ObjectId, int> styleUsage)
        {
            ObjectId styleId = point.StyleId;
            if (styleId.IsNull) return;
            int existing;
            styleUsage[styleId] = styleUsage.TryGetValue(styleId, out existing) ? existing + 1 : 1;
        }

        private static void ResolveStylesToBlockCounts(
            Transaction tr,
            Dictionary<ObjectId, int> styleUsage,
            DrawingUsage usage)
        {
            foreach (KeyValuePair<ObjectId, int> kvp in styleUsage)
            {
                string blockName = ResolveBlockFromPointStyle(tr, kvp.Key);
                if (string.IsNullOrEmpty(blockName)) continue;

                int existing;
                usage.BlockCounts[blockName] = usage.BlockCounts.TryGetValue(blockName, out existing)
                    ? existing + kvp.Value
                    : kvp.Value;
            }
        }

        /// <summary>
        /// Resolves the marker block name from a Civil 3D PointStyle. The Civil 3D API has
        /// varied across versions for which property holds the block name, so we probe a few
        /// candidate names via reflection on the PointStyle and its Marker sub-object.
        /// Returns null if no block-symbol marker is in use (e.g. the style uses a custom marker).
        /// </summary>
        private static string ResolveBlockFromPointStyle(Transaction tr, ObjectId styleId)
        {
            PointStyle style;
            try
            {
                style = tr.GetObject(styleId, OpenMode.ForRead) as PointStyle;
            }
            catch
            {
                return null;
            }
            if (style == null) return null;

            // Direct properties on PointStyle.
            string candidate = TryReadStringProperty(style,
                "SymbolBlockName", "AcadBlockName", "AcadBlockSymbolName", "BlockName", "SymbolName");
            if (!string.IsNullOrEmpty(candidate)) return candidate;

            // Some API versions expose the block via a nested Marker / SymbolStyle sub-object.
            object marker = TryReadAnyProperty(style, "Marker", "MarkerStyle", "Symbol");
            if (marker != null)
            {
                candidate = TryReadStringProperty(marker,
                    "SymbolBlockName", "AcadBlockName", "AcadBlockSymbolName", "BlockName", "SymbolName");
                if (!string.IsNullOrEmpty(candidate)) return candidate;
            }

            return null;
        }

        private static string TryReadStringProperty(object instance, params string[] propertyNames)
        {
            Type t = instance.GetType();
            foreach (string name in propertyNames)
            {
                PropertyInfo prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || prop.PropertyType != typeof(string)) continue;
                try
                {
                    string value = prop.GetValue(instance, null) as string;
                    if (!string.IsNullOrEmpty(value)) return value;
                }
                catch
                {
                    // Some property getters can throw — ignore and try the next name.
                }
            }
            return null;
        }

        private static object TryReadAnyProperty(object instance, params string[] propertyNames)
        {
            Type t = instance.GetType();
            foreach (string name in propertyNames)
            {
                PropertyInfo prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null) continue;
                try
                {
                    object value = prop.GetValue(instance, null);
                    if (value != null) return value;
                }
                catch
                {
                }
            }
            return null;
        }

        private static void Tally(Entity ent, Transaction tr, DrawingUsage usage)
        {
            BlockReference br = ent as BlockReference;
            if (br != null)
            {
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

        private static void Increment(Dictionary<string, int> dict, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            int count;
            dict[key] = dict.TryGetValue(key, out count) ? count + 1 : 1;
        }
    }
}
