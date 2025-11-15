using System;
using System.Collections.Generic;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    public delegate void ScriptExtensionEventHandler(ScriptExtension sender, ScriptEventArgs e);

    public class ScriptExtensionEventPool : Dictionary<string, ScriptExtensionEventHandler>
    {
        public ScriptExtensionEventPool() : base(StringComparer.OrdinalIgnoreCase)
        {
            try
            {
                // Nothing else needed here; Dictionary initialization is safe
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptExtensionEventPool Constructor");
            }
        }
    }
}
