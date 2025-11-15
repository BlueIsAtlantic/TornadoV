using System;

namespace TornadoScript.ScriptMain.CrashHandling
{
    public static class GlobalCrashHandler
    {
        // Static constructor hooks unhandled exceptions automatically
        static GlobalCrashHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                CrashLogger.LogError(ex, "Global UnhandledException");
            else
                CrashLogger.Log("Global UnhandledException: Non-Exception object thrown");
        }

        private static void OnUnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            CrashLogger.LogError(e.Exception, "Global UnobservedTaskException");
            e.SetObserved();
        }

        // Call this once from MainScript to trigger static constructor
        public static void Initialize()
        {
            // Static constructor runs automatically the first time this is accessed
        }
    }
}
