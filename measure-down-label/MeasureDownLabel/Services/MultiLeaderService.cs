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

            // \H0.06; = force text height to 0.06 (overrides any style override)
            // \P      = MTEXT paragraph break
            // %%P     = +/- symbol
            return string.Format(
                "\\H0.06;TOP = {0}'\\PFL {1}\" ({2}) = {3}'%%P",
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
                // Note: TextHeight here is the baseline; \H0.06; in the content string
                // is the reliable override since the INLET style may override TextHeight.
                MText mtext = new MText();
                mtext.SetDatabaseDefaults(database);
                mtext.Contents = content;
                mtext.TextHeight = 0.06;
                mleader.MText = mtext;

                // Per the ObjectARX API:
                //   AddFirstVertex  = content/text side of the leader line
                //   AddLastVertex   = arrowhead side of the leader line
                // TextLocation must be set AFTER all vertices so the MLeader can
                // determine which side of the text block to attach the leader to.
                int leaderIdx     = mleader.AddLeader();
                int leaderLineIdx = mleader.AddLeaderLine(leaderIdx);

                // Arrowhead at the structure (last vertex = arrowhead side)
                mleader.AddLastVertex(leaderLineIdx, input.InsertionPoint);

                // Text location — set after geometry so attachment side is computed correctly
                mleader.TextLocation = input.LeaderPoint;

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
