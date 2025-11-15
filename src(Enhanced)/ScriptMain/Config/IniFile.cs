using System;
using System.Runtime.InteropServices;
using System.Text;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain.Config
{
    /// <summary>
    /// Safe INI file reader/writer
    /// </summary>
    public class IniFile
    {
        public string Path;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public IniFile(string iniPath)
        {
            Path = iniPath ?? throw new ArgumentNullException(nameof(iniPath));
        }

        public void IniWriteValue(string section, string key, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(Path))
                    return;

                WritePrivateProfileString(section ?? "", key ?? "", value ?? "", Path);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"IniFile.IniWriteValue ({section}:{key})");
            }
        }

        public string IniReadValue(string section, string key)
        {
            try
            {
                if (string.IsNullOrEmpty(Path))
                    return string.Empty;

                var temp = new StringBuilder(255);
                GetPrivateProfileString(section ?? "", key ?? "", "", temp, temp.Capacity, Path);
                return temp.ToString();
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"IniFile.IniReadValue ({section}:{key})");
                return string.Empty;
            }
        }
    }
}
