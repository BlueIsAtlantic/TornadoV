using System;

namespace TornadoScript.ScriptCore.Game
{
    public delegate void ScriptComponentEventHandler(ScriptThread sender, ScriptComponentEventArgs args);

    /// <summary>
    /// Event args for a script extension event.
    /// </summary>
    public sealed class ScriptComponentEventArgs : EventArgs
    {
        public ScriptComponentEventArgs(ScriptComponent extension)
        {
            try
            {
                Extension = extension ?? throw new ArgumentNullException(nameof(extension));
            }
            catch (Exception ex)
            {
                TornadoScript.ScriptMain.CrashHandling.CrashLogger.LogError(ex, "ScriptComponentEventArgs Constructor");
            }
        }

        public ScriptComponent Extension { get; private set; }
    }
}
