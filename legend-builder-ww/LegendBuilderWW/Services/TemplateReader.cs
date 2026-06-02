using System;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Side-loads an external DWG and clones a single BlockTableRecord by name into the target database.
    /// </summary>
    public class TemplateReader
    {
        /// <summary>
        /// Imports the named block from <paramref name="sourceDwgPath"/> into <paramref name="targetDb"/>'s BlockTable.
        /// If the target DB already has a block by that name, this is a no-op.
        /// </summary>
        public void ImportBlock(Database targetDb, string sourceDwgPath, string blockName)
        {
            using (Transaction targetCheck = targetDb.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)targetCheck.GetObject(targetDb.BlockTableId, OpenMode.ForRead);
                if (bt.Has(blockName))
                {
                    targetCheck.Commit();
                    return;
                }
                targetCheck.Commit();
            }

            using (Database sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(sourceDwgPath, FileShare.Read, true, null);
                sourceDb.CloseInput(true);

                using (Transaction srcTr = sourceDb.TransactionManager.StartTransaction())
                {
                    BlockTable srcBt = (BlockTable)srcTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                    if (!srcBt.Has(blockName))
                    {
                        throw new InvalidOperationException(
                            string.Format("Block '{0}' was not found inside source DWG '{1}'.", blockName, sourceDwgPath));
                    }
                    srcTr.Commit();
                }

                // Database.Insert is the canonical API for cloning a block definition (BlockTableRecord)
                // from one database into another. It returns the ObjectId of the new BTR in targetDb.
                targetDb.Insert(blockName, sourceDb, true);
            }
        }
    }
}
