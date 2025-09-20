using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using TornadoScript.ScriptMain.Utility;
using GTA;
using GTA.Native;

namespace TornadoScript.ScriptMain.Config
{
    public static class IniHelper
    {
        public static readonly string IniPath;
        public static readonly IniFile IniFile;

        static IniHelper()
        {
            IniPath = string.Format("scripts\\{0}.ini",
                Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location));

            if (!File.Exists(IniPath))
                Create();

            IniFile = new IniFile(IniPath);
        }

        /// <summary>
        /// Write a string value to the config file at the specified section and key
        /// </summary>
        public static void WriteValue(string section, string key, string value)
        {
            IniFile.IniWriteValue(section, key, value);
        }

        /// <summary>
        /// Gets a config setting
        /// </summary>
        public static T GetValue<T>(string section, string key, T defaultValue = default(T))
        {
            Type type = typeof(T);
            if (!type.IsValueType && type != typeof(string))
                throw new ArgumentException("Not a known type.");

            var keyValue = IniFile.IniReadValue(section, key);
            var tConverter = TypeDescriptor.GetConverter(type);

            if (keyValue.Length > 0 && tConverter.CanConvertFrom(typeof(string)))
            {
                return (T)tConverter.ConvertFromString(null, CultureInfo.InvariantCulture, keyValue);
            }

            return defaultValue;
        }

        public static void Create()
        {
            try
            {
                if (File.Exists(IniPath)) File.Delete(IniPath);

                var list = Helpers.ReadEmbeddedResource(Properties.Resources.TornadoScript);
                Helpers.WriteListToFile(list, IniPath);

                ShowNotification("~g~TornadoScript INI created successfully!");
            }
            catch (AccessViolationException)
            {
                ShowNotification("~r~TornadoScript failed to write a new INI file. Access denied: " + IniPath);
            }
            catch (Exception e)
            {
                ShowNotification("~r~TornadoScript failed to write a new INI file. " + e.Message);
            }
        }

        /// <summary>
        /// Show a notification above the radar using SHVDN3 Function.Call
        /// </summary>
        public static void ShowNotification(string message)
        {
            Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, message);
            Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
        }
    }
}