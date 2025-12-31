using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using PointStyleModifier.Models;
using PointStyleModifier.Services;
using PointStyleModifier.UI;
using PointStyleModifier.Utilities;

namespace PointStyleModifier.Commands
{
    /// <summary>
    /// AutoCAD command class for modifying COGO point and label styles
    /// </summary>
    public class PointStyleCommand
    {
        /// <summary>
        /// Command to modify the style and/or label style of selected COGO points
        /// Usage: Type MODIFYPOINTSTYLE at the AutoCAD command line
        /// </summary>
        [CommandMethod("MODIFYPOINTSTYLE")]
        public void ModifyPointStyle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            Editor editor = doc.Editor;
            Database database = doc.Database;

            try
            {
                // Step 1: Prompt user to select COGO points
                PromptSelectionResult selectionResult = SelectCogoPoints(editor);

                PointSelectionService selectionService = new PointSelectionService();

                // Step 2: Validate selection
                string errorMessage;
                if (!selectionService.ValidateSelection(selectionResult, out errorMessage))
                {
                    if (selectionResult.Status != PromptStatus.Cancel)
                    {
                        ErrorHandler.ShowMessage(editor, errorMessage);
                    }
                    return;
                }

                // Step 3: Get ObjectIds from selection
                ObjectIdCollection selectedPointIds = selectionService.GetObjectIds(selectionResult);

                if (selectedPointIds.Count == 0)
                {
                    ErrorHandler.ShowMessage(editor, "No COGO points were selected.");
                    return;
                }

                // Step 4: Retrieve available styles
                PointStyleService pointStyleService = new PointStyleService();
                PointLabelStyleService labelStyleService = new PointLabelStyleService();

                List<PointStyleInfo> availablePointStyles = pointStyleService.GetPointStyles(database);
                List<LabelStyleInfo> availableLabelStyles = labelStyleService.GetPointLabelStyles(database);

                if ((availablePointStyles == null || availablePointStyles.Count == 0) &&
                    (availableLabelStyles == null || availableLabelStyles.Count == 0))
                {
                    ErrorHandler.ShowWarning(editor, "No point styles or label styles found in the current drawing.");
                    return;
                }

                // Step 5: Show dialog for style selection
                using (PointStyleSelectorForm form = new PointStyleSelectorForm())
                {
                    form.PopulateStyles(availablePointStyles, availableLabelStyles);

                    if (Application.ShowModalDialog(form) != System.Windows.Forms.DialogResult.OK)
                    {
                        ErrorHandler.ShowMessage(editor, "Command cancelled.");
                        return;
                    }

                    // Step 6: Apply selected styles to points
                    int pointStyleModifiedCount = 0;
                    int labelStyleModifiedCount = 0;
                    List<string> messages = new List<string>();

                    if (form.ModifyPointStyle && form.SelectedPointStyle != null)
                    {
                        pointStyleModifiedCount = pointStyleService.ApplyStyleToPoints(
                            database,
                            selectedPointIds,
                            form.SelectedPointStyle.StyleId);
                        messages.Add(string.Format("Point style '{0}' applied to {1} point(s)",
                            form.SelectedPointStyle.Name, pointStyleModifiedCount));
                    }

                    if (form.ModifyLabelStyle && form.SelectedLabelStyle != null)
                    {
                        labelStyleModifiedCount = labelStyleService.ApplyLabelStyleToPoints(
                            database,
                            selectedPointIds,
                            form.SelectedLabelStyle.StyleId);
                        messages.Add(string.Format("Label style '{0}' applied to {1} point(s)",
                            form.SelectedLabelStyle.Name, labelStyleModifiedCount));
                    }

                    // Step 7: Display success message
                    if (messages.Count > 0)
                    {
                        ErrorHandler.ShowSuccess(editor, string.Format("Successfully modified styles:\n{0}",
                            string.Join("\n", messages.ToArray())));
                    }
                }
            }
            catch (System.Exception ex)
            {
                ErrorHandler.HandleException(editor, ex, "MODIFYPOINTSTYLE");
            }
        }

        /// <summary>
        /// Prompts the user to select COGO points from the drawing
        /// </summary>
        /// <param name="editor">The AutoCAD editor</param>
        /// <returns>PromptSelectionResult containing the selected objects</returns>
        private PromptSelectionResult SelectCogoPoints(Editor editor)
        {
            PointSelectionService selectionService = new PointSelectionService();
            SelectionFilter filter = selectionService.CreateCogoPointFilter();

            PromptSelectionOptions options = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect COGO points: ",
                MessageForRemoval = "\nRemove COGO points: "
            };

            return editor.GetSelection(options, filter);
        }
    }
}
