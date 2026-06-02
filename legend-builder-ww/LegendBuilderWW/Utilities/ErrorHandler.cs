using System;
using Autodesk.AutoCAD.EditorInput;

namespace LegendBuilderWW.Utilities
{
    public static class ErrorHandler
    {
        public static void HandleException(Editor editor, Exception ex, string context)
        {
            // Dump full exception details to the command line so NREs are debuggable from the AutoCAD console.
            editor.WriteMessage(string.Format("\nError in {0}: {1}", context, ex.Message));
            editor.WriteMessage(string.Format("\n  Type: {0}", ex.GetType().FullName));
            editor.WriteMessage(string.Format("\n  Source: {0}", ex.Source ?? "(null)"));
            editor.WriteMessage(string.Format("\n  Stack:\n{0}", ex.StackTrace ?? "(no stack trace)"));

            Exception inner = ex.InnerException;
            int depth = 0;
            while (inner != null && depth < 5)
            {
                editor.WriteMessage(string.Format("\n  Inner [{0}] {1}: {2}", depth, inner.GetType().FullName, inner.Message));
                editor.WriteMessage(string.Format("\n  Inner stack:\n{0}", inner.StackTrace ?? "(no stack trace)"));
                inner = inner.InnerException;
                depth++;
            }

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
