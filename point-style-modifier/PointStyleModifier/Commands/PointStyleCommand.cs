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
    /// AutoCAD command class for modifying COGO point styles
    /// </summary>
    public class PointStyleCommand
    {
        /// <summary>
        /// Command to modify the style of selected COGO points
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

                // Step 4: Retrieve available point styles
                PointStyleService styleService = new PointStyleService();
                List<PointStyleInfo> availableStyles = styleService.GetPointStyles(database);

                if (availableStyles == null || availableStyles.Count == 0)
                {
                    ErrorHandler.ShowWarning(editor, "No point styles found in the current drawing.");
                    return;
                }

                // Step 5: Show dialog for style selection
                PointStyleInfo selectedStyle = ShowStyleSelectionDialog(availableStyles);

                if (selectedStyle == null)
                {
                    ErrorHandler.ShowMessage(editor, "Command cancelled.");
                    return;
                }

                // Step 6: Apply selected style to points
                int modifiedCount = styleService.ApplyStyleToPoints(
                    database,
                    selectedPointIds,
                    selectedStyle.StyleId);

                // Step 7: Display success message
                ErrorHandler.ShowSuccess(
                    editor,
                    $"Successfully applied style '{selectedStyle.Name}' to {modifiedCount} point(s).");
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

        /// <summary>
        /// Shows the point style selection dialog
        /// </summary>
        /// <param name="availableStyles">List of available point styles</param>
        /// <returns>Selected PointStyleInfo or null if cancelled</returns>
        private PointStyleInfo ShowStyleSelectionDialog(List<PointStyleInfo> availableStyles)
        {
            using (PointStyleSelectorForm form = new PointStyleSelectorForm())
            {
                form.PopulatePointStyles(availableStyles);

                if (Application.ShowModalDialog(form) == System.Windows.Forms.DialogResult.OK)
                {
                    return form.SelectedPointStyle;
                }
            }

            return null;
        }
    }
}
