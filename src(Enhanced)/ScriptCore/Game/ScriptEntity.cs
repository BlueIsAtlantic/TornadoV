using System;
using GTA;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    /// <summary>
    /// Represents a game entity.
    /// </summary>
    public abstract class ScriptEntity<T> : ScriptExtension, IScriptEntity where T : Entity
    {
        /// <summary>
        /// Base game entity reference.
        /// </summary>
        public T Ref { get; }

        /// <summary>
        /// Total entity ticks.
        /// </summary>
        public int TotalTicks { get; private set; }

        /// <summary>
        /// Total time entity has been available to the script.
        /// </summary>
        public TimeSpan TotalTime { get; private set; }

        /// <summary>
        /// Time at which the entity was made avilable to the script.
        /// </summary>
        public int CreatedTime { get; }

        /// <summary>
        /// Initialize the class.
        /// </summary>
        /// <param name="baseRef"></param>
        protected ScriptEntity(T baseRef)
        {
            try
            {
                Ref = baseRef;
                CreatedTime = GTA.Game.GameTime;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptEntity Constructor");
            }
        }

        /// <summary>
        /// Call this method each tick to update entity related information.
        /// </summary>
        public override void OnUpdate(int gameTime)
        {
            try
            {
                TotalTicks++;

                TotalTicks = TotalTicks % int.MaxValue;

                TotalTime = TimeSpan.FromMilliseconds(gameTime - CreatedTime);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptEntity OnUpdate");
            }
        }

        public void Remove()
        {
            try
            {
                Ref.Delete();
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptEntity Remove");
            }
        }

        public override void Dispose()
        {
            try
            {
                Remove();
                base.Dispose();
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptEntity Dispose");
            }
        }

        public static implicit operator Entity(ScriptEntity<T> e)
        {
            try
            {
                return e.Ref;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptEntity Implicit Operator");
                return null;
            }
        }
    }
}
