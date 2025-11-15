using System;
using System.Windows.Forms;
using GTA;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    /// <summary>
    /// Base class for a script thread.
    /// </summary>
    public abstract class ScriptThread : Script
    {
        private static ScriptExtensionPool _extensions;
        public static ScriptVarCollection Vars { get; private set; }

        protected ScriptThread()
        {
            try
            {
                _extensions = new ScriptExtensionPool();
                Vars = new ScriptVarCollection();

                Tick += (s, e) =>
                {
                    try { OnUpdate(GTA.Game.GameTime); }
                    catch (Exception ex) { CrashLogger.LogError(ex, "ScriptThread Tick OnUpdate failed"); }
                };

                KeyDown += (s, e) =>
                {
                    try { KeyPressedInternal(s, e); }
                    catch (Exception ex) { CrashLogger.LogError(ex, "ScriptThread Tick KeyDown failed"); }
                };
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptThread constructor failed");
            }
        }

        public static T Get<T>() where T : ScriptExtension
        {
            try { return _extensions.Get<T>(); }
            catch (Exception ex) { CrashLogger.LogError(ex, "ScriptThread.Get failed"); return null; }
        }

        public static void Add(ScriptExtension extension)
        {
            try
            {
                if (_extensions.Contains(extension)) return;

                extension.RegisterEvent("keydown");
                _extensions.Add(extension);
                extension.OnThreadAttached();
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptThread.Add failed");
            }
        }

        public static void Create<T>() where T : ScriptExtension, new()
        {
            try
            {
                var extension = Get<T>();
                if (extension != null) return;

                extension = new T();
                Add(extension);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptThread.Create failed");
            }
        }

        public static T GetOrCreate<T>() where T : ScriptExtension, new()
        {
            try
            {
                var extension = Get<T>();
                if (extension != null) return extension;

                extension = new T();
                Add(extension);
                return extension;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptThread.GetOrCreate failed");
                return null;
            }
        }

        internal static void Remove(ScriptExtension extension)
        {
            try
            {
                extension.OnThreadDetached();
                _extensions.Remove(extension);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptThread.Remove failed");
            }
        }

        public static void RegisterVar<T>(string name, T defaultValue, bool readOnly = false)
        {
            try { Vars.Add(name, new ScriptVar<T>(defaultValue, readOnly)); }
            catch (Exception ex) { CrashLogger.LogError(ex, $"RegisterVar<{typeof(T).Name}> failed"); }
        }

        public static ScriptVar<T> GetVar<T>(string name)
        {
            try { return Vars.Get<T>(name); }
            catch (Exception ex) { CrashLogger.LogError(ex, $"GetVar<{typeof(T).Name}> failed"); return null; }
        }

        public static bool SetVar<T>(string name, T value)
        {
            try
            {
                var foundVar = GetVar<T>(name);
                if (foundVar == null) return false;
                if (foundVar.ReadOnly) return false;
                foundVar.Value = value;
                return true;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"SetVar<{typeof(T).Name}> failed");
                return false;
            }
        }

        internal virtual void KeyPressedInternal(object sender, KeyEventArgs e)
        {
            try
            {
                for (int i = 0; i < _extensions.Count; i++)
                {
                    _extensions[i].NotifyEvent("keydown", new ScriptEventArgs(e));
                }
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "KeyPressedInternal failed");
            }
        }

        public virtual void OnUpdate(int gameTime)
        {
            try
            {
                for (int i = 0; i < _extensions.Count; i++)
                {
                    _extensions[i].OnUpdate(gameTime);
                }
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptThread.OnUpdate failed");
            }
        }

        public void DisposeScript()
        {
            try
            {
                for (int i = _extensions.Count - 1; i >= 0; i--)
                {
                    _extensions[i].Dispose();
                }
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "DisposeScript failed");
            }
        }
    }
}
