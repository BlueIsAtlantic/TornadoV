using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using GTA;
using GTA.Native;
using TornadoScript.ScriptMain.CrashHandling;
using TornadoScript.ScriptMain.Utility;

namespace TornadoScript.ScriptMain.Config
{
    public static class IniHelper
    {
        public static readonly string IniPath;
        public static readonly IniFile IniFile;

        static IniHelper()
        {
            try
            {
                IniPath = $"scripts\\{Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)}.ini";

                if (!File.Exists(IniPath))
                    Create();

                IniFile = new IniFile(IniPath);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "IniHelper.StaticConstructor");
            }
        }

        public static void WriteValue(string section, string key, string value)
        {
            try
            {
                IniFile?.IniWriteValue(section, key, value);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"IniHelper.WriteValue ({section}:{key})");
            }
        }

        public static T GetValue<T>(string section, string key, T defaultValue = default)
        {
            try
            {
                Type type = typeof(T);
                if (!type.IsValueType && type != typeof(string))
                    throw new ArgumentException("IniHelper: Unsupported type.");

                string keyValue = IniFile?.IniReadValue(section, key) ?? "";
                var converter = TypeDescriptor.GetConverter(type);

                if (!string.IsNullOrEmpty(keyValue) && converter.CanConvertFrom(typeof(string)))
                    return (T)converter.ConvertFromString(null, CultureInfo.InvariantCulture, keyValue);

                return defaultValue;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"IniHelper.GetValue<{typeof(T).Name}> ({section}:{key})");
                return defaultValue;
            }
        }

        public static void Create()
        {
            try
            {
                if (File.Exists(IniPath))
                    File.Delete(IniPath);

                var list = Helpers.ReadEmbeddedResource(Properties.Resources.TornadoScript);
                Helpers.WriteListToFile(list, IniPath);

                ShowNotification("~g~TornadoScript INI created successfully!");
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "IniHelper.Create");
                ShowNotification($"~r~INI creation failed: {ex.Message}");
            }
        }

        public static void ShowNotification(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message)) return;
                Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, message);
                Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "IniHelper.ShowNotification");
            }
        }
    }
}
