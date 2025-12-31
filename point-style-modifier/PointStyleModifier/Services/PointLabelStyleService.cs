using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using PointStyleModifier.Models;

namespace PointStyleModifier.Services
{
    /// <summary>
    /// Service for point label style operations in Civil 3D
    /// </summary>
    public class PointLabelStyleService
    {
        /// <summary>
        /// Retrieves all available point label styles from the current Civil 3D document
        /// </summary>
        /// <param name="database">The AutoCAD database</param>
        /// <returns>List of LabelStyleInfo objects representing available label styles</returns>
        public List<LabelStyleInfo> GetPointLabelStyles(Database database)
        {
            List<LabelStyleInfo> labelStyles = new List<LabelStyleInfo>();

            try
            {
                CivilDocument civilDoc = CivilApplication.ActiveDocument;

                using (Transaction tr = database.TransactionManager.StartTransaction())
                {
                    var pointLabelStyles = civilDoc.Styles.LabelStyles.PointLabelStyles.LabelStyles;

                    foreach (ObjectId styleId in pointLabelStyles)
                    {
                        LabelStyle style = tr.GetObject(styleId, OpenMode.ForRead) as LabelStyle;
                        if (style != null)
                        {
                            labelStyles.Add(new LabelStyleInfo
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
                throw new Exception(string.Format("Failed to retrieve point label styles: {0}", ex.Message), ex);
            }

            return labelStyles;
        }

        /// <summary>
        /// Applies a label style to a collection of COGO points
        /// </summary>
        /// <param name="database">The AutoCAD database</param>
        /// <param name="pointIds">Collection of COGO point ObjectIds</param>
        /// <param name="styleId">The ObjectId of the label style to apply</param>
        /// <returns>Number of points successfully modified</returns>
        public int ApplyLabelStyleToPoints(Database database, ObjectIdCollection pointIds, ObjectId styleId)
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
                            point.LabelStyleId = styleId;
                            modifiedCount++;
                        }
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to apply label style to points: {0}", ex.Message), ex);
            }

            return modifiedCount;
        }
    }
}
