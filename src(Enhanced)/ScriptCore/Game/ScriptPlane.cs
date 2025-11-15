using GTA;
using System;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.Game
{
    public class ScriptPlane : ScriptEntity<Vehicle>
    {
        public event ScriptEntityEventHandler Undrivable;

        private int undrivableTicks = 0;

        public ScriptPlane(Vehicle baseRef) : base(baseRef) { }

        public LandingGearState LandingGearState
        {
            get
            {
                try
                {
                    if (Ref is not null && Ref.Exists())
                    {
                        return Ref.LandingGearState switch
                        {
                            VehicleLandingGearState.Deploying => LandingGearState.Opening,
                            VehicleLandingGearState.Deployed => LandingGearState.Deployed,
                            VehicleLandingGearState.Retracting => LandingGearState.Closing,
                            VehicleLandingGearState.Retracted => LandingGearState.Retracted,
                            _ => LandingGearState.Retracted
                        };
                    }
                }
                catch (Exception ex)
                {
                    CrashLogger.LogError(ex, "ScriptPlane.LandingGearState getter failed");
                }

                return LandingGearState.Retracted;
            }
            set
            {
                try
                {
                    if (Ref is not null && Ref.Exists())
                    {
                        Ref.LandingGearState = value switch
                        {
                            LandingGearState.Opening => VehicleLandingGearState.Deploying,
                            LandingGearState.Deployed => VehicleLandingGearState.Deployed,
                            LandingGearState.Closing => VehicleLandingGearState.Retracting,
                            LandingGearState.Retracted => VehicleLandingGearState.Retracted,
                            _ => VehicleLandingGearState.Retracted
                        };
                    }
                }
                catch (Exception ex)
                {
                    CrashLogger.LogError(ex, "ScriptPlane.LandingGearState setter failed");
                }
            }
        }

        protected virtual void OnUndrivable(ScriptEntityEventArgs e)
        {
            try
            {
                Undrivable?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptPlane.OnUndrivable failed");
            }
        }

        public override void OnUpdate(int gameTime)
        {
            try
            {
                if (Ref is not null && Ref.Exists() && !Ref.IsDriveable)
                {
                    if (undrivableTicks == 0)
                        OnUndrivable(new ScriptEntityEventArgs(gameTime));

                    undrivableTicks++;
                }
                else
                {
                    undrivableTicks = 0;
                }

                base.OnUpdate(gameTime);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ScriptPlane.OnUpdate failed");
            }
        }
    }

    public enum LandingGearState
    {
        Deployed,
        Closing,
        Opening,
        Retracted
    }
}
