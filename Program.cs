using System;
using System.Windows.Forms;

namespace Fotocopy
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Global exception handler to prevent silent crash and provide clear information
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show(
                    $"Terjadi error aplikasi:\n{e.Exception.Message}\n\nDetail:\n{e.Exception.StackTrace}",
                    "Error Aplikasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show(
                    $"Fatal Error:\n{ex?.Message ?? "Unknown error"}\n\nDetail:\n{ex?.StackTrace}",
                    "Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            Application.Run(new Form1());
        }
    }
}
