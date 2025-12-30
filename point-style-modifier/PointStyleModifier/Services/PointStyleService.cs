using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using PointStyleModifier.Models;

namespace PointStyleModifier.Services
{
    /// <summary>
    /// Service for point style operations in Civil 3D
    /// </summary>
    public class PointStyleService
    {
        /// <summary>
        /// Retrieves all available point styles from the current Civil 3D document
        /// </summary>
        /// <param name="database">The AutoCAD database</param>
        /// <returns>List of PointStyleInfo objects representing available styles</returns>
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
                throw new Exception($"Failed to retrieve point styles: {ex.Message}", ex);
            }

            return pointStyles;
        }

        /// <summary>
        /// Applies a point style to a collection of COGO points
        /// </summary>
        /// <param name="database">The AutoCAD database</param>
        /// <param name="pointIds">Collection of COGO point ObjectIds</param>
        /// <param name="styleId">The ObjectId of the style to apply</param>
        /// <returns>Number of points successfully modified</returns>
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
                throw new Exception($"Failed to apply style to points: {ex.Message}", ex);
            }

            return modifiedCount;
        }
    }
}
