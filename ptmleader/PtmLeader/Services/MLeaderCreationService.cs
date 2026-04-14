using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace PtmLeader.Services
{
    /// <summary>
    /// Creates a new MLeader entity with MText content at the specified locations.
    /// </summary>
    public class MLeaderCreationService
    {
        /// <summary>
        /// Adds a new MLeader to model space.
        /// </summary>
        /// <param name="database">The active drawing database.</param>
        /// <param name="arrowPoint">The tip of the leader arrow (on or near the points).</param>
        /// <param name="landingPoint">Where the dogleg ends and the text is attached.</param>
        /// <param name="content">The MText content string (e.g. "1-5").</param>
        /// <param name="editor">Editor used to report any errors.</param>
        /// <returns>True if the MLeader was created successfully.</returns>
        public bool CreateMLeader(Database database, Point3d arrowPoint,
            Point3d landingPoint, string content, Editor editor)
        {
            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(database.BlockTableId, OpenMode.ForRead)
                    as BlockTable;
                BlockTableRecord modelSpace = tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite)
                    as BlockTableRecord;

                MLeader mleader = new MLeader();
                mleader.SetDatabaseDefaults(database);

                mleader.ContentType = ContentType.MTextContent;

                // Build the MText block
                MText mtext = new MText();
                mtext.SetDatabaseDefaults(database);
                mtext.TextStyleId = GetTextStyleId(database, tr, "Simplex");
                mtext.TextHeight  = 0.06;
                mtext.Contents    = "\\H0.06;" + content;
                mtext.Location    = landingPoint;
                mleader.MText     = mtext;

                // Add the leader line
                int leaderIndex = mleader.AddLeader();
                int lineIndex   = mleader.AddLeaderLine(leaderIndex);
                mleader.AddFirstVertex(lineIndex, arrowPoint);
                mleader.AddLastVertex(lineIndex, landingPoint);

                modelSpace.AppendEntity(mleader);
                tr.AddNewlyCreatedDBObject(mleader, true);

                tr.Commit();
            }

            return true;
        }

        private ObjectId GetTextStyleId(Database database, Transaction tr, string styleName)
        {
            TextStyleTable tst = tr.GetObject(database.TextStyleTableId, OpenMode.ForRead)
                as TextStyleTable;

            if (tst != null && tst.Has(styleName))
                return tst[styleName];

            return database.Textstyle; // fall back to drawing default
        }
    }
}
