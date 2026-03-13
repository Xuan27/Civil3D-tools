using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MeasureDownLabel.Models;

namespace MeasureDownLabel.Services
{
    /// <summary>
    /// Updates the content of existing MLeader entities with formatted measure-down labels.
    /// Leader geometry, style, and placement are preserved exactly as drawn.
    /// </summary>
    public class MultiLeaderService
    {
        /// <summary>
        /// Builds the MText content string from the measure-down inputs.
        /// Format:  TOP = {top}'\P FL {size}" ({dir}) = {bottom}'±[\P FL ...]
        /// \P   = MTEXT paragraph break
        /// %%P  = ± symbol
        /// </summary>
        public string BuildLabelText(MeasureDownInput input)
        {
            string top = input.TopElevation.ToString("0.00");

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendFormat("{0} = {1}'", input.StructureType, top);

            foreach (FlowLineEntry fl in input.FlowLines)
            {
                string size   = fl.PipeSize.ToString("0.##");
                string bottom = fl.Elevation.ToString("0.0");
                sb.AppendFormat("\\PFL {0}\" ({1}) = {2}'%%P", size, fl.PipeDirection, bottom);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Updates the MText content of an existing MLeader in the drawing.
        /// Only the text content is changed — geometry, style, and position are untouched.
        /// </summary>
        public bool UpdateExistingMLeader(Database database, ObjectId mleaderId,
            MeasureDownInput input, Editor editor)
        {
            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                MLeader mleader = tr.GetObject(mleaderId, OpenMode.ForWrite) as MLeader;
                if (mleader == null)
                {
                    editor.WriteMessage("\n  [!] Could not open selected entity as an MLeader.");
                    return false;
                }

                if (mleader.ContentType != ContentType.MTextContent)
                {
                    editor.WriteMessage("\n  [!] Selected multileader does not use MText content.");
                    return false;
                }

                MText mtext = mleader.MText;
                mtext.Contents = BuildLabelText(input);
                mleader.MText = mtext;

                tr.Commit();
            }

            return true;
        }
    }
}
