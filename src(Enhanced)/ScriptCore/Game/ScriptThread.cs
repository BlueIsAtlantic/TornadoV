using System.Windows.Forms;
using GTA;

namespace TornadoScript.ScriptCore.Game
{
    /// <summary>
    /// Base class for a script thread - OPTIMIZED VERSION
    /// </summary>
    public abstract class ScriptThread : Script
    {
        private static ScriptExtensionPool _extensions;
        public static ScriptVarCollection Vars { get; private set; }

        // OPTIMIZATION: Cache extension count to avoid Count property access
        private static int _extensionCount;

        protected ScriptThread()
        {
            _extensions = new ScriptExtensionPool();
            Vars = new ScriptVarCollection();
            Tick += (s, e) => OnUpdate(GTA.Game.GameTime);
            KeyDown += KeyPressedInternal;
        }

        public static T Get<T>() where T : ScriptExtension
        {
            return _extensions.Get<T>();
        }

        public static void Add(ScriptExtension extension)
        {
            if (_extensions.Contains(extension)) return;

            extension.RegisterEvent("keydown");

            _extensions.Add(extension);
            _extensionCount = _extensions.Count; // OPTIMIZATION: Update cached count

            extension.OnThreadAttached();
        }

        public static void Create<T>() where T : ScriptExtension, new()
        {
            var extension = Get<T>();

            if (extension != null) return;

            extension = new T();

            Add(extension);
        }

        public static T GetOrCreate<T>() where T : ScriptExtension, new()
        {
            var extension = Get<T>();

            if (extension != null)
                return extension;

            extension = new T();

            Add(extension);

            return extension;
        }

        internal static void Remove(ScriptExtension extension)
        {
            extension.OnThreadDetached();

            _extensions.Remove(extension);
            _extensionCount = _extensions.Count; // OPTIMIZATION: Update cached count
        }

        public static void RegisterVar<T>(string name, T defaultValue, bool readOnly = false)
        {
            Vars.Add(name, new ScriptVar<T>(defaultValue, readOnly));
        }

        public static ScriptVar<T> GetVar<T>(string name)
        {
            return Vars.Get<T>(name);
        }

        public static bool SetVar<T>(string name, T value)
        {
            var foundVar = GetVar<T>(name);

            if (foundVar == null || foundVar.ReadOnly)
                return false;

            foundVar.Value = value;

            return true;
        }

        internal virtual void KeyPressedInternal(object sender, KeyEventArgs e)
        {
            // OPTIMIZATION: Use cached count and direct indexing
            for (int i = 0; i < _extensionCount; i++)
            {
                _extensions[i].NotifyEvent("keydown", new ScriptEventArgs(e));
            }
        }

        /// <summary>
        /// Updates the thread - OPTIMIZED VERSION
        /// </summary>
        public virtual void OnUpdate(int gameTime)
        {
            // OPTIMIZATION: Use cached count and avoid bounds checking
            for (int i = 0; i < _extensionCount; i++)
            {
                try
                {
                    _extensions[i].OnUpdate(gameTime);
                }
                catch
                {
                    // Silent fail to prevent one extension from breaking others
                }
            }
        }

        /// <summary>
        /// Removes the thread and all extensions - OPTIMIZED VERSION
        /// </summary>
        public void DisposeScript()
        {
            // OPTIMIZATION: Dispose in reverse order for better cleanup
            for (int i = _extensionCount - 1; i >= 0; i--)
            {
                try
                {
                    _extensions[i].Dispose();
                }
                catch
                {
                    // Continue disposing others even if one fails
                }
            }

            _extensionCount = 0; // Reset cached count
        }
    }
}