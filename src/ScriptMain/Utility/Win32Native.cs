using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace TornadoScript.ScriptMain.Utility
{
    public sealed unsafe class Win32Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public IntPtr LpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        public enum ThreadAccess : int
        {
            TERMINATE = 0x0001,
            SUSPEND_RESUME = 0x0002,
            GET_CONTEXT = 0x0008,
            SET_CONTEXT = 0x0010,
            SET_INFORMATION = 0x0020,
            QUERY_INFORMATION = 0x0040,
            SET_THREAD_TOKEN = 0x0080,
            IMPERSONATE = 0x0100,
            DIRECT_IMPERSONATION = 0x0200
        }

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationThread(
            IntPtr threadHandle,
            int threadInformationClass,
            void* threadInformation,
            ulong threadInformationLength,
            ulong* returnLength);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(ThreadAccess desiredAccess, bool inheritHandle, int threadId);

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, uint cb);

        [DllImport("user32.dll", EntryPoint = "BlockInput")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

        public enum MapType : uint
        {
            MapvkVkToVsc = 0x0,
            MapvkVscToVk = 0x1,
            MapvkVkToChar = 0x2,
            MapvkVscToVkEx = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key, bool shift)
        {
            char ch = ' ';
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];

            if (shift)
                keyboardState[0x10] = 0x80;

            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MapvkVkToVsc);
            StringBuilder stringBuilder = new StringBuilder(2);
            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);

            if (result > 0)
                ch = stringBuilder[0];

            return ch;
        }

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int PlaySound(string szSound, IntPtr hModule, int flags);
    }
}
