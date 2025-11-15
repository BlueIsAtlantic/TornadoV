using System;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    public delegate void ScriptEntityEventHandler(IScriptEntity sender, ScriptEntityEventArgs args);

    /// <summary>
    /// Event args for a script entity event.
    /// </summary>
    public sealed class ScriptEntityEventArgs : EventArgs
    {
        public int GameTime { get; private set; }

        public ScriptEntityEventArgs(int gameTime)
        {
            try
            {
                GameTime = gameTime;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptEntityEventArgs Constructor");
            }
        }
    }
}
