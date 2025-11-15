using System;
using System.Collections.Generic;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    public class ScriptVarCollection : Dictionary<string, IScriptVar>
    {
        public ScriptVarCollection() : base(StringComparer.OrdinalIgnoreCase)
        { }

        /// <summary>
        /// Get a typed script variable from the collection safely.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public ScriptVar<T> Get<T>(string name)
        {
            try
            {
                if (TryGetValue(name, out var result))
                    return result as ScriptVar<T>;

                return null;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"ScriptVarCollection.Get<{typeof(T).Name}> failed for '{name}'");
                return null;
            }
        }
    }
}
