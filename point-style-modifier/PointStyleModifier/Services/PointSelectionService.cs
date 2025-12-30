using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace PointStyleModifier.Services
{
    /// <summary>
    /// Service for handling COGO point selection operations
    /// </summary>
    public class PointSelectionService
    {
        /// <summary>
        /// Creates a selection filter that only allows COGO points to be selected
        /// </summary>
        /// <returns>SelectionFilter configured for COGO points</returns>
        public SelectionFilter CreateCogoPointFilter()
        {
            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "AeccDbCogoPoint")
            };
            return new SelectionFilter(filterList);
        }

        /// <summary>
        /// Validates the selection result and provides an error message if invalid
        /// </summary>
        /// <param name="result">The prompt selection result to validate</param>
        /// <param name="errorMessage">Output parameter containing error message if validation fails</param>
        /// <returns>True if selection is valid, false otherwise</returns>
        public bool ValidateSelection(PromptSelectionResult result, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (result.Status == PromptStatus.Error)
            {
                errorMessage = "An error occurred during selection.";
                return false;
            }

            if (result.Status == PromptStatus.Cancel)
            {
                errorMessage = "Selection was cancelled by user.";
                return false;
            }

            if (result.Status != PromptStatus.OK)
            {
                errorMessage = "Invalid selection.";
                return false;
            }

            if (result.Value == null || result.Value.Count == 0)
            {
                errorMessage = "No points were selected.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts ObjectIds from a prompt selection result
        /// </summary>
        /// <param name="result">The prompt selection result</param>
        /// <returns>ObjectIdCollection containing the selected object IDs</returns>
        public ObjectIdCollection GetObjectIds(PromptSelectionResult result)
        {
            ObjectIdCollection objectIds = new ObjectIdCollection();

            if (result.Value != null)
            {
                foreach (SelectedObject selectedObj in result.Value)
                {
                    if (selectedObj != null)
                    {
                        objectIds.Add(selectedObj.ObjectId);
                    }
                }
            }

            return objectIds;
        }
    }
}
