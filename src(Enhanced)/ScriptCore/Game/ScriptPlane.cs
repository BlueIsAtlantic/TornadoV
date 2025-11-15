using GTA;
using System;

namespace TornadoScript.ScriptCore.Game
{
    /// <summary>
    /// Represents a plane.
    /// </summary>
    public class ScriptPlane : ScriptEntity<Vehicle>
    {
        /// <summary>
        /// Fired when the vehicle is no longer drivable.
        /// </summary>
        public event ScriptEntityEventHandler Undrivable;

        /// <summary>
        /// State of the vehicle landing gear.
        /// </summary>
        public LandingGearState LandingGearState
        {
            get => Ref.LandingGearState switch
            {
                VehicleLandingGearState.Deploying => LandingGearState.Opening,
                VehicleLandingGearState.Deployed => LandingGearState.Deployed,
                VehicleLandingGearState.Retracting => LandingGearState.Closing,
                VehicleLandingGearState.Retracted => LandingGearState.Retracted,
                _ => LandingGearState.Retracted
            };
            set
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

        private int undrivableTicks = 0;

        public ScriptPlane(Vehicle baseRef) : base(baseRef)
        { }

        protected virtual void OnUndrivable(ScriptEntityEventArgs e)
        {
            Undrivable?.Invoke(this, e);
        }

        public override void OnUpdate(int gameTime)
        {
            if (!Ref.IsDriveable)
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
    }

    public enum LandingGearState
    {
        Deployed,
        Closing,
        Opening,
        Retracted
    }
}
