using System;
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
        /// \P is the MTEXT paragraph break; %%P is the +/- tolerance symbol.
        /// </summary>
        public string BuildLabelText(MeasureDownInput input)
        {
            // Top elevation: 2 decimal places; Bottom (flow line): 1 decimal place
            string top    = input.TopElevation.ToString("0.00");
            string bottom = input.BottomElevation.ToString("0.0");
            string size   = input.PipeSize.ToString("0.##");

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
                // Resolve INLET style; warn and continue with drawing default if missing
                ObjectId styleId = FindInletStyle(database, tr);
                if (styleId == ObjectId.Null)
                {
                    editor.WriteMessage(
                        string.Format("\n  [!] MLeader style '{0}' not found - using drawing default.", InletStyleName));
                    styleId = database.MLeaderStyleDictionaryId;
                }

                string content = BuildLabelText(input);

                // --- Build the MLeader ---
                MLeader mleader = new MLeader();
                mleader.SetDatabaseDefaults(database);

                if (!styleId.IsNull && styleId != database.MLeaderStyleDictionaryId)
                    mleader.MLeaderStyle = styleId;

                mleader.ContentType = ContentType.MTextContent;

                // Set MText content
                MText mtext = new MText();
                mtext.SetDatabaseDefaults(database);
                mtext.Contents = content;
                mtext.TextHeight = 0.06;
                mleader.MText = mtext;

                // IMPORTANT: set TextLocation BEFORE adding leader geometry.
                // The MLeader uses this to anchor the text and automatically computes
                // the dogleg from the last leader vertex to the text block.
                // Setting it after AddLeader/AddFirstVertex causes the text to drift
                // to the origin when the entity is reopened for editing.
                mleader.TextLocation = input.LeaderPoint;

                // Add leader line — only the arrowhead vertex is needed.
                // The MLeader connects the leader line to TextLocation automatically
                // via its internal dogleg, so AddLastVertex is not required.
                int leaderIdx     = mleader.AddLeader();
                int leaderLineIdx = mleader.AddLeaderLine(leaderIdx);
                mleader.AddFirstVertex(leaderLineIdx, input.InsertionPoint);

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
    }
}
