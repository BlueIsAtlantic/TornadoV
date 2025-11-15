using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace TornadoScript.ScriptMain.CrashHandling
{
    public static class CrashHandler
    {
        private static string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TornadoV_Error.log");

        public static void Initialize()
        {
            // Redirect Console output
            var consoleWriter = new StringWriter();
            Console.SetOut(consoleWriter);

            // Capture unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Exception ex = e.ExceptionObject as Exception;
                HandleCrash(ex, consoleWriter.ToString());
            };

            // Capture unobserved task exceptions
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                HandleCrash(e.Exception, consoleWriter.ToString());
                e.SetObserved();
            };
        }

        // Made public so other scripts can call it
        public static void HandleCrash(Exception ex, string consoleOutput)
        {
            try
            {
                string errorMsg = "TORNADOV ERROR\n\n";
                errorMsg += "Exception: " + ex?.Message + "\n";
                errorMsg += "Stack Trace:\n" + ex?.StackTrace + "\n\n";
                errorMsg += "Console Output:\n" + consoleOutput;

                // Write to log file
                File.WriteAllText(logPath, errorMsg);

                // Show error window
                MessageBox.Show(errorMsg, "TORNADOV ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // End GTA V process (for testing only!)
                Process.GetCurrentProcess().Kill();
            }
            catch
            {
                // Prevent recursive crash
            }
        }
    }
}
