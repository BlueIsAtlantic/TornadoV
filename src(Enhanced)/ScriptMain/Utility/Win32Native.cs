using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain.Utility
{
    public static class Win32Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public IntPtr LpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, uint cb);

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
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public static char GetCharFromKey(Key key, bool shift)
        {
            try
            {
                char ch = ' ';
                int virtualKey = KeyInterop.VirtualKeyFromKey(key);
                byte[] keyboardState = new byte[256];

                if (shift)
                    keyboardState[0x10] = 0x80;

                GetKeyboardState(keyboardState);

                uint scanCode = MapVirtualKey((uint)virtualKey, 0 /*MapvkVkToVsc*/);
                StringBuilder stringBuilder = new StringBuilder(2);
                int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);

                if (result > 0)
                    ch = stringBuilder[0];

                return ch;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "Win32Native.GetCharFromKey");
                return ' ';
            }
        }

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int PlaySound(string szSound, IntPtr hModule, int flags);

        public static int SafePlaySound(string szSound, IntPtr hModule, int flags)
        {
            try
            {
                return PlaySound(szSound, hModule, flags);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "Win32Native.SafePlaySound");
                return 0;
            }
        }
    }
}
