using System;
using GTA;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    /// <summary>
    /// Represents a ped.
    /// </summary>
    public class ScriptPed : ScriptEntity<Ped>
    {
        /// <summary>
        /// Fired when the ped has entered a vehicle.
        /// </summary>
        public event ScriptEntityEventHandler EnterVehicle;

        /// <summary>
        /// Fired when the ped has exited a vehicle.
        /// </summary>
        public event ScriptEntityEventHandler ExitVehicle;

        private int vehicleTicks = 0;

        /// <summary>
        /// If the ped is a local player/ human.
        /// </summary>
        public bool IsHuman
        {
            get
            {
                try
                {
                    return Ref == GTA.Game.Player.Character;
                }
                catch (Exception ex)
                {
                    CrashLogger.LogError(ex, "ScriptPed.IsHuman getter failed");
                    return false;
                }
            }
        }

        public ScriptPed(Ped baseRef) : base(baseRef) { }

        protected virtual void OnEnterVehicle(ScriptEntityEventArgs e)
        {
            try
            {
                EnterVehicle?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptPed.OnEnterVehicle failed");
            }
        }

        protected virtual void OnExitVehicle(ScriptEntityEventArgs e)
        {
            try
            {
                ExitVehicle?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptPed.OnExitVehicle failed");
            }
        }

        public override void OnUpdate(int gameTime)
        {
            try
            {
                if (Ref.IsInVehicle())
                {
                    if (vehicleTicks == 0)
                        OnEnterVehicle(new ScriptEntityEventArgs(gameTime));
                    vehicleTicks++;
                }
                else
                {
                    if (vehicleTicks > 0)
                    {
                        OnExitVehicle(new ScriptEntityEventArgs(gameTime));
                        vehicleTicks = 0;
                    }
                }

                base.OnUpdate(gameTime);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptPed.OnUpdate failed");
            }
        }
    }
}
