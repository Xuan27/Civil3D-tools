using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using MeasureDownLabel.Models;

namespace MeasureDownLabel.Services
{
    /// <summary>
    /// Creates and places MLEADER objects in the drawing using the INLET multileader style
    /// </summary>
    public class MultiLeaderService
    {
        private const string InletStyleName = "INLET";

        /// <summary>
        /// Builds the multileader content string from the measure-down inputs.
        /// Format:  TOP = {top}'\P FL {size}" ({dir}) = {bottom}'±
        /// \P is the MTEXT paragraph break; %%P is the ± tolerance symbol.
        /// </summary>
        public string BuildLabelText(MeasureDownInput input)
        {
            // Format elevations to 2 decimal places
            string top = input.TopElevation.ToString("0.00");
            string bottom = input.BottomElevation.ToString("0.00");
            string size = input.PipeSize.ToString("0.##");

            // \P = MTEXT line break
            return string.Format(
                "TOP = {0}'\\PFL {1}\" ({2}) = {3}'%%P",
                top, size, input.PipeDirection, bottom);
        }

        /// <summary>
        /// Looks up the INLET MLeaderStyle ObjectId. Returns ObjectId.Null if not found.
        /// </summary>
        public ObjectId FindInletStyle(Database database, Transaction tr)
        {
            DBDictionary styleDict = tr.GetObject(
                database.MLeaderStyleDictionaryId, OpenMode.ForRead) as DBDictionary;

            if (styleDict == null)
                return ObjectId.Null;

            if (styleDict.Contains(InletStyleName))
                return styleDict.GetAt(InletStyleName);

            // Case-insensitive fallback
            foreach (DBDictionaryEntry entry in styleDict)
            {
                if (string.Equals(entry.Key, InletStyleName, StringComparison.OrdinalIgnoreCase))
                    return entry.Value;
            }

            return ObjectId.Null;
        }

        /// <summary>
        /// Places the MLEADER entity in model space and returns its ObjectId.
        /// </summary>
        public ObjectId PlaceMultiLeader(Database database, MeasureDownInput input, Editor editor)
        {
            ObjectId placedId = ObjectId.Null;

            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                // Resolve INLET style; warn but continue with default if missing
                ObjectId styleId = FindInletStyle(database, tr);
                if (styleId == ObjectId.Null)
                {
                    editor.WriteMessage(
                        string.Format("\n  [!] MLeader style '{0}' not found — using drawing default.", InletStyleName));
                    styleId = database.MLeaderStyleDictionaryId; // will fall back gracefully
                }

                // Build the label text
                string content = BuildLabelText(input);

                // Create the MLeader
                MLeader mleader = new MLeader();
                mleader.SetDatabaseDefaults(database);

                if (!styleId.IsNull && styleId != database.MLeaderStyleDictionaryId)
                    mleader.MLeaderStyle = styleId;

                mleader.ContentType = ContentType.MTextContent;

                // Set MTEXT content
                MText mtext = new MText();
                mtext.SetDatabaseDefaults(database);
                mtext.Contents = content;
                mtext.TextHeight = GetStyleTextHeight(database, tr, styleId);
                mleader.MText = mtext;

                // Build leader geometry: one leader, one segment
                int leaderIdx = mleader.AddLeader();
                mleader.AddLeaderLine(leaderIdx);

                // Arrow tip at the feature point (top of structure / invert point)
                mleader.SetFirstPoint(leaderIdx, 0, input.InsertionPoint);

                // Dogleg / landing end toward where the label sits
                mleader.SetLastPoint(leaderIdx, 0, input.LeaderPoint);

                // Add to model space
                BlockTableRecord modelSpace = tr.GetObject(
                    SymbolUtilityServices.GetBlockModelSpaceId(database),
                    OpenMode.ForWrite) as BlockTableRecord;

                placedId = modelSpace.AppendEntity(mleader);
                tr.AddNewlyCreatedDBObject(mleader, true);

                tr.Commit();
            }

            return placedId;
        }

        // ---------------------------------------------------------------------------
        // Helper: read the text height from the resolved MLeader style, or use a
        // sensible default so the label is readable regardless of style availability.
        // ---------------------------------------------------------------------------
        private double GetStyleTextHeight(Database database, Transaction tr, ObjectId styleId)
        {
            try
            {
                if (!styleId.IsNull && styleId != database.MLeaderStyleDictionaryId)
                {
                    MLeaderStyle style = tr.GetObject(styleId, OpenMode.ForRead) as MLeaderStyle;
                    if (style != null && style.TextHeight > 0)
                        return style.TextHeight;
                }
            }
            catch { /* ignore — fall through to default */ }

            return 0.1; // 0.1 drawing units; adjust per project standard
        }
    }
}
