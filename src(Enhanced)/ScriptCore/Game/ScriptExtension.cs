using System;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    public abstract class ScriptExtension : ScriptComponent, IScriptEventHandler
    {
        public ScriptExtensionEventPool Events { get; } = new ScriptExtensionEventPool();

        public ScriptExtension()
        {
            try
            {
                ScriptThread.Add(this);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptExtension Constructor");
            }
        }

        /// <summary>
        /// Raise an event with the specified name.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        public void NotifyEvent(string name)
        {
            NotifyEvent(name, new ScriptEventArgs());
        }

        /// <summary>
        /// Raise an event with the specified name and arguments.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="args">Event specific arguments.</param>
        public void NotifyEvent(string name, ScriptEventArgs args)
        {
            try
            {
                if (Events[name] != null)
                {
                    foreach (ScriptExtensionEventHandler handler in Events[name].GetInvocationList())
                    {
                        try
                        {
                            handler.Invoke(this, args);
                        }
                        catch (Exception ex)
                        {
                            CrashLogger.LogError(ex, $"Event '{name}' invocation failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"NotifyEvent failed for '{name}'");
            }
        }

        /// <summary>
        /// Register a script event for the underlying extension.
        /// </summary>
        /// <param name="name"></param>
        public void RegisterEvent(string name)
        {
            try
            {
                Events.Add(name, default(ScriptExtensionEventHandler));
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"RegisterEvent failed for '{name}'");
            }
        }

        internal virtual void OnThreadAttached()
        { }

        internal virtual void OnThreadDetached()
        { }

        public virtual void Dispose()
        {
            try
            {
                ScriptThread.Remove(this);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "Dispose failed in ScriptExtension");
            }
        }
    }
}
