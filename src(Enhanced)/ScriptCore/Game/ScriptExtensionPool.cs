using System;
using System.Collections.Generic;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    public class ScriptExtensionPool : List<ScriptExtension>
    {
        /// <summary>
        /// Get an extension from the pool by its type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>() where T : ScriptComponent
        {
            try
            {
                for (int i = 0; i < Count; i++)
                {
                    if (this[i] is T item)
                    {
                        return item;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"ScriptExtensionPool.Get<{typeof(T).Name}> failed");
            }

            return null;
        }
    }
}
