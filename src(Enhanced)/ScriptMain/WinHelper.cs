using System;
using System.Diagnostics;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain
{
    public static class WinHelper
    {
        // Previously used to locate the engine's main thread and copy TLS.
        // On Enhanced we cannot and must not perform that operation.
        // Provide safe fallbacks.

        public static int GetProcessMainThreadId()
        {
            try
            {
                int mainThreadId = -1;
                long lowestStart = long.MaxValue;
                foreach (ProcessThread t in Process.GetCurrentProcess().Threads)
                {
                    try
                    {
                        if (t.StartTime.Ticks < lowestStart)
                        {
                            lowestStart = t.StartTime.Ticks;
                            mainThreadId = t.Id;
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.LogError(ex, "WinHelper.GetProcessMainThreadId(Thread iteration)");
                    }
                }
                return mainThreadId;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "WinHelper.GetProcessMainThreadId");
                return -1;
            }
        }

        // No-op: copying TLS is not supported in Enhanced-compatible mode.
        public static void CopyTlsValues(IntPtr sourceThreadHandle, IntPtr targetThreadHandle, params int[] valuesOffsets)
        {
            try
            {
                // intentionally left blank for Enhanced compatibility
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "WinHelper.CopyTlsValues(IntPtr)");
            }
        }

        public static void CopyTlsValues(int sourceThreadId, int targetThreadId, params int[] valuesOffsets)
        {
            try
            {
                // intentionally left blank for Enhanced compatibility
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "WinHelper.CopyTlsValues(int)");
            }
        }
    }
}
