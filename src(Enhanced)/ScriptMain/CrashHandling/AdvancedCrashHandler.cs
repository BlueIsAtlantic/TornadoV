using GTA;
using System;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace TornadoScript.ScriptMain.CrashHandling
{
    public static class CrashLogger
    {
        [DllImport("TornadoVLogger.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void LogMessage(string message);

        [DllImport("TornadoVLogger.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void LogException(string exceptionMsg, string context);

        [DllImport("TornadoVLogger.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void ClearLog();

        public static void Log(string msg)
        {
            try { LogMessage(msg); } catch { /* fail silently */ }
        }

        public static void LogError(Exception ex, string context)
        {
            try { LogException(ex.ToString(), context); } catch { /* fail silently */ }
        }
    }
}
