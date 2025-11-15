using System;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    public class ScriptEventArgs : EventArgs
    {
        public object Data { get; private set; }

        public ScriptEventArgs() : this(null) { }

        public ScriptEventArgs(object data)
        {
            try
            {
                Data = data;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptEventArgs Constructor");
            }
        }
    }

    public class ScriptEventArgs<T> : EventArgs
    {
        public T Data { get; private set; }

        public ScriptEventArgs(T data)
        {
            try
            {
                Data = data;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"ScriptEventArgs<{typeof(T).Name}> Constructor");
            }
        }
    }
}
