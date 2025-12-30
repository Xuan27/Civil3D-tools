using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using PointStyleModifier.Models;

namespace PointStyleModifier.Services
{
    public class PointStyleService
    {
        public List<PointStyleInfo> GetPointStyles(Database database)
        {
            List<PointStyleInfo> pointStyles = new List<PointStyleInfo>();

            try
            {
                CivilDocument civilDoc = CivilApplication.ActiveDocument;

                using (Transaction tr = database.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId styleId in civilDoc.Styles.PointStyles)
                    {
                        PointStyle style = tr.GetObject(styleId, OpenMode.ForRead) as PointStyle;
                        if (style != null)
                        {
                            pointStyles.Add(new PointStyleInfo
                            {
                                StyleId = styleId,
                                Name = style.Name,
                                Description = style.Description
                            });
                        }
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to retrieve point styles: {0}", ex.Message), ex);
            }

            return pointStyles;
        }

        public int ApplyStyleToPoints(Database database, ObjectIdCollection pointIds, ObjectId styleId)
        {
            int modifiedCount = 0;

            try
            {
                using (Transaction tr = database.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId pointId in pointIds)
                    {
                        CogoPoint point = tr.GetObject(pointId, OpenMode.ForWrite) as CogoPoint;
                        if (point != null)
                        {
                            point.StyleId = styleId;
                            modifiedCount++;
                        }
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to apply style to points: {0}", ex.Message), ex);
            }

            return modifiedCount;
        }
    }
}
