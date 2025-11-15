using GTA;
using System;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    public class ScriptProp : ScriptEntity<Prop>
    {
        public ScriptProp(Prop baseRef) : base(baseRef)
        {
            try
            {
                // Additional constructor logic can go here
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptProp constructor failed");
            }
        }

        public override void OnUpdate(int gameTime)
        {
            try
            {
                if (Ref is not null && Ref.Exists())
                    base.OnUpdate(gameTime);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptProp.OnUpdate failed");
            }
        }

        public new void Remove()
        {
            try
            {
                if (Ref is not null && Ref.Exists())
                    base.Remove();
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptProp.Remove failed");
            }
        }

        public override void Dispose()
        {
            try
            {
                if (Ref is not null && Ref.Exists())
                    base.Dispose();
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptProp.Dispose failed");
            }
        }
    }
}
