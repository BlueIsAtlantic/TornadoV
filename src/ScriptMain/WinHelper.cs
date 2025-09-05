using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TornadoScript.ScriptMain.Utility;

namespace TornadoScript.ScriptMain
{
    public static unsafe class WinHelper
    {
        private static int mainThreadId = -1;
        private static Dictionary<int, IntPtr> threadHandleDictionary = new Dictionary<int, IntPtr>();
        private static Dictionary<IntPtr, THREAD_BASIC_INFORMATION> threadInformationDictionary = new Dictionary<IntPtr, THREAD_BASIC_INFORMATION>();

        [StructLayout(LayoutKind.Sequential)]
        public struct THREAD_BASIC_INFORMATION
        {
            public int ExitStatus;
            public IntPtr TebBaseAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TEB
        {
            public IntPtr ThreadLocalStoragePointer;
        }

        public static int GetProcessMainThreadId()
        {
            if (mainThreadId == -1)
            {
                long lowestStartTime = long.MaxValue;
                ProcessThread lowestStartTimeThread = null;

                foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
                {
                    long startTime = thread.StartTime.Ticks;
                    if (startTime < lowestStartTime)
                    {
                        lowestStartTime = startTime;
                        lowestStartTimeThread = thread;
                    }
                }

                mainThreadId = lowestStartTimeThread == null ? -1 : lowestStartTimeThread.Id;
            }

            return mainThreadId;
        }

        public static void CopyTlsValues(IntPtr sourceThreadHandle, IntPtr targetThreadHandle, params int[] valuesOffsets)
        {
            THREAD_BASIC_INFORMATION sourceThreadInfo, targetThreadInfo;

            if (!threadInformationDictionary.TryGetValue(sourceThreadHandle, out sourceThreadInfo))
            {
                sourceThreadInfo = new THREAD_BASIC_INFORMATION();
                int sourceStatus = Win32Native.NtQueryInformationThread(sourceThreadHandle, 0, &sourceThreadInfo, (ulong)sizeof(THREAD_BASIC_INFORMATION), null);
                if (sourceStatus != 0)
                {
                    ScriptCore.Logger.Log($"Source Thread Invalid Query Status: {sourceStatus}");
                    return;
                }
                threadInformationDictionary[sourceThreadHandle] = sourceThreadInfo;
            }

            if (!threadInformationDictionary.TryGetValue(targetThreadHandle, out targetThreadInfo))
            {
                targetThreadInfo = new THREAD_BASIC_INFORMATION();
                int targetStatus = Win32Native.NtQueryInformationThread(targetThreadHandle, 0, &targetThreadInfo, (ulong)sizeof(THREAD_BASIC_INFORMATION), null);
                if (targetStatus != 0)
                {
                    ScriptCore.Logger.Log($"Target Thread Invalid Query Status: {targetStatus}");
                    return;
                }
                threadInformationDictionary[targetThreadHandle] = targetThreadInfo;
            }

            TEB* sourceTeb = (TEB*)sourceThreadInfo.TebBaseAddress;
            TEB* targetTeb = (TEB*)targetThreadInfo.TebBaseAddress;

            foreach (int offset in valuesOffsets)
            {
                *(long*)(*(byte**)(targetTeb->ThreadLocalStoragePointer) + offset) = *(long*)(*(byte**)(sourceTeb->ThreadLocalStoragePointer) + offset);
            }
        }

        public static void CopyTlsValues(int sourceThreadId, int targetThreadId, params int[] valuesOffsets)
        {
            IntPtr sourceThreadHandle = IntPtr.Zero, targetThreadHandle = IntPtr.Zero;

            if (!threadHandleDictionary.TryGetValue(sourceThreadId, out sourceThreadHandle))
            {
                try
                {
                    sourceThreadHandle = Win32Native.OpenThread(Win32Native.ThreadAccess.QUERY_INFORMATION, false, sourceThreadId);
                    threadHandleDictionary[sourceThreadId] = sourceThreadHandle;
                }
                catch { }
            }

            if (!threadHandleDictionary.TryGetValue(targetThreadId, out targetThreadHandle))
            {
                try
                {
                    targetThreadHandle = Win32Native.OpenThread(Win32Native.ThreadAccess.QUERY_INFORMATION, false, targetThreadId);
                    threadHandleDictionary[targetThreadId] = targetThreadHandle;
                }
                catch { }
            }

            CopyTlsValues(sourceThreadHandle, targetThreadHandle, valuesOffsets);
        }
    }
}
