using System;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;
using LegendBuilderWW.Config;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Locates the Vertical Legend BlockTableRecord. Looks in the current drawing first;
    /// if absent, side-loads from the configured source DWG and clones the block into the current DB.
    /// </summary>
    public class TemplateResolver
    {
        private readonly Settings _settings;
        private readonly TemplateReader _reader;

        public TemplateResolver(Settings settings)
        {
            _settings = settings;
            _reader = new TemplateReader();
        }

        /// <summary>
        /// Returns the ObjectId of the Vertical Legend BlockTableRecord inside <paramref name="db"/>,
        /// importing it from the source DWG if needed. Throws on failure with a user-friendly message.
        /// </summary>
        public ObjectId Resolve(Database db)
        {
            ObjectId existing = FindBlockInDatabase(db, _settings.SourceBlockName);
            if (!existing.IsNull)
            {
                return existing;
            }

            if (string.IsNullOrWhiteSpace(_settings.SourceDwgPath))
            {
                throw new InvalidOperationException(
                    "Vertical Legend block not found in drawing and no source DWG path is configured. " +
                    "Open Settings and set the source DWG path.");
            }

            if (!File.Exists(_settings.SourceDwgPath))
            {
                throw new FileNotFoundException(
                    string.Format(
                        "Vertical Legend block is not in this drawing and the configured source DWG was not found:\n  {0}\n" +
                        "Open Settings and update the source DWG path.",
                        _settings.SourceDwgPath));
            }

            _reader.ImportBlock(db, _settings.SourceDwgPath, _settings.SourceBlockName);

            ObjectId imported = FindBlockInDatabase(db, _settings.SourceBlockName);
            if (imported.IsNull)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Failed to import block '{0}' from '{1}'. Verify the source DWG contains a block with that exact name.",
                        _settings.SourceBlockName,
                        _settings.SourceDwgPath));
            }
            return imported;
        }

        private static ObjectId FindBlockInDatabase(Database db, string blockName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (bt.Has(blockName))
                {
                    ObjectId id = bt[blockName];
                    tr.Commit();
                    return id;
                }
                tr.Commit();
                return ObjectId.Null;
            }
        }
    }
}
