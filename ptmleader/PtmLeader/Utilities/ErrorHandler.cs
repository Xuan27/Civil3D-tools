using System;
using Autodesk.AutoCAD.EditorInput;

namespace PtmLeader.Utilities
{
    public static class ErrorHandler
    {
        public static void HandleException(Editor editor, Exception ex, string context)
        {
            editor.WriteMessage(string.Format("\n  [ERROR] {0}: {1}", context, ex.Message));
            System.Diagnostics.Debug.WriteLine(string.Format("{0} failed: {1}", context, ex));
        }

        public static void ShowMessage(Editor editor, string message)
        {
            editor.WriteMessage(string.Format("\n  {0}", message));
        }

        public static void ShowWarning(Editor editor, string message)
        {
            editor.WriteMessage(string.Format("\n  [!] {0}", message));
        }

        public static void ShowSuccess(Editor editor, string message)
        {
            editor.WriteMessage(string.Format("\n  {0}", message));
        }

        public static void ShowBanner(Editor editor, string title)
        {
            editor.WriteMessage(string.Format("\n{0}", new string('─', 60)));
            editor.WriteMessage(string.Format("\n  {0}", title));
            editor.WriteMessage(string.Format("\n{0}", new string('─', 60)));
        }
    }
}
