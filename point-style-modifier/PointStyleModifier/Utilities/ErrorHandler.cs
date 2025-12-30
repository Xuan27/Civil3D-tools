using System;
using Autodesk.AutoCAD.EditorInput;

namespace PointStyleModifier.Utilities
{
    public static class ErrorHandler
    {
        public static void HandleException(Editor editor, Exception ex, string context)
        {
            string errorMessage = string.Format("\nError in {0}: {1}", context, ex.Message);
            editor.WriteMessage(errorMessage);
            System.Diagnostics.Debug.WriteLine(string.Format("{0} failed with exception: {1}", context, ex));
        }

        public static void ShowMessage(Editor editor, string message)
        {
            editor.WriteMessage(string.Format("\n{0}", message));
        }

        public static void ShowWarning(Editor editor, string message)
        {
            editor.WriteMessage(string.Format("\nWarning: {0}", message));
        }

        public static void ShowSuccess(Editor editor, string message)
        {
            editor.WriteMessage(string.Format("\n{0}", message));
        }
    }
}
