using System;
using Autodesk.AutoCAD.EditorInput;

namespace PointStyleModifier.Utilities
{
    /// <summary>
    /// Centralized error handling and user messaging utility
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Handles exceptions and displays user-friendly error messages
        /// </summary>
        /// <param name="editor">The AutoCAD editor for displaying messages</param>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="context">Context string describing what operation failed</param>
        public static void HandleException(Editor editor, Exception ex, string context)
        {
            string errorMessage = $"\nError in {context}: {ex.Message}";
            editor.WriteMessage(errorMessage);

            // Log to command line for debugging
            System.Diagnostics.Debug.WriteLine($"{context} failed with exception: {ex}");
        }

        /// <summary>
        /// Displays an informational message to the user
        /// </summary>
        /// <param name="editor">The AutoCAD editor for displaying messages</param>
        /// <param name="message">The message to display</param>
        public static void ShowMessage(Editor editor, string message)
        {
            editor.WriteMessage($"\n{message}");
        }

        /// <summary>
        /// Displays a warning message to the user
        /// </summary>
        /// <param name="editor">The AutoCAD editor for displaying messages</param>
        /// <param name="message">The warning message to display</param>
        public static void ShowWarning(Editor editor, string message)
        {
            editor.WriteMessage($"\nWarning: {message}");
        }

        /// <summary>
        /// Displays a success message to the user
        /// </summary>
        /// <param name="editor">The AutoCAD editor for displaying messages</param>
        /// <param name="message">The success message to display</param>
        public static void ShowSuccess(Editor editor, string message)
        {
            editor.WriteMessage($"\n{message}");
        }
    }
}
