using System;
using GTA;
using GTA.Math;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain.Utility
{
    public class ShapeTestResult
    {
        public bool DidHit { get; private set; }
        public int HitEntity { get; private set; }
        public Vector3 HitPosition { get; private set; }
        public Vector3 HitNormal { get; private set; }

        public ShapeTestResult(bool didHit, int hitEntity, Vector3 hitPosition, Vector3 hitNormal)
        {
            DidHit = didHit;
            HitEntity = hitEntity;
            HitPosition = hitPosition;
            HitNormal = hitNormal;
        }
    }

    public static class ShapeTestEx
    {
        public static ShapeTestResult RunShapeTest(Vector3 start, Vector3 end, Entity ignoreEntity, IntersectFlags flags)
        {
            try
            {
                RaycastResult ray = World.Raycast(start, end, flags, ignoreEntity);

                if (!ray.DidHit)
                    return new ShapeTestResult(false, 0, Vector3.Zero, Vector3.Zero);

                Vector3 hitNormal = (ray.HitPosition - start).Normalized;

                int hitEntityHandle = 0;
                try
                {
                    hitEntityHandle = ray.HitEntity?.Handle ?? 0;
                }
                catch (Exception ex)
                {
                    CrashLogger.LogError(ex, "ShapeTestEx: failed to get HitEntity handle");
                }

                return new ShapeTestResult(true, hitEntityHandle, ray.HitPosition, hitNormal);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "ShapeTestEx: RunShapeTest failed");
                return new ShapeTestResult(false, 0, Vector3.Zero, Vector3.Zero);
            }
        }
    }
}
