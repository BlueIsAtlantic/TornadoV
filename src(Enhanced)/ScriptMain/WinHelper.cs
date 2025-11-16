using System;

namespace TornadoScript.ScriptMain
{
    /// <summary>
    /// Enhanced-compatible version - TLS manipulation removed
    /// </summary>
    public static class WinHelper
    {
        /// <summary>
        /// Returns a dummy thread ID for compatibility
        /// TLS manipulation is not needed in Enhanced
        /// </summary>
        public static int GetProcessMainThreadId()
        {
            // Return current thread ID as fallback
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// NO-OP for Enhanced compatibility
        /// TLS value copying is not needed and won't work in Enhanced
        /// </summary>
        public static void CopyTlsValues(IntPtr sourceThreadHandle, IntPtr targetThreadHandle, params int[] valuesOffsets)
        {
            // Enhanced: TLS manipulation removed
            // This was only needed for legacy memory access
        }

        /// <summary>
        /// NO-OP for Enhanced compatibility
        /// </summary>
        public static void CopyTlsValues(int sourceThreadId, int targetThreadId, params int[] valuesOffsets)
        {
            // Enhanced: TLS manipulation removed
        }
    }
}