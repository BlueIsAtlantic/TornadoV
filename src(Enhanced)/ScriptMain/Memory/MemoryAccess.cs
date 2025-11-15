using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Drawing;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain.Memory
{
    public static class MemoryAccess
    {
        private static bool bInitialized = true;

        public static void Initialize()
        {
            try
            {
                bInitialized = true;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "MemoryAccess.Initialize");
            }
        }

        public static Vector3 GetEntityPosition(Entity entity)
        {
            try
            {
                if (entity != null && entity.Exists())
                    return entity.Position;

                return Vector3.Zero;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "MemoryAccess.GetEntityPosition");
                return Vector3.Zero;
            }
        }

        public static IList<Entity> CollectEntitiesFull()
        {
            try
            {
                var list = new List<Entity>();

                foreach (var v in World.GetAllVehicles())
                    if (v != null && v.Exists())
                        list.Add(v);

                foreach (var p in World.GetAllPeds())
                    if (p != null && p.Exists())
                        list.Add(p);

                foreach (var prop in World.GetAllProps())
                    if (prop != null && prop.Exists())
                        list.Add(prop);

                return list;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "MemoryAccess.CollectEntitiesFull");
                return new List<Entity>();
            }
        }

        public static IEnumerable<Entity> GetAllEntitiesInternal()
        {
            List<Entity> entities;
            try
            {
                entities = new List<Entity>(CollectEntitiesFull());
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "MemoryAccess.GetAllEntitiesInternal");
                entities = new List<Entity>();
            }

            foreach (var e in entities)
            {
                if (e != null && e.Exists())
                    yield return e;
            }
        }


        public static void SetPtfxColor(string baseAsset, string particleName, int emitterIndex, Color newColor)
        {
            try
            {
                if (string.IsNullOrEmpty(baseAsset) || string.IsNullOrEmpty(particleName))
                    return;
                // Enhanced mode: no memory access required
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "MemoryAccess.SetPtfxColor");
            }
        }

        public static void SetLoopedPtfxColor(int particleHandle, Color color)
        {
            try
            {
                if (particleHandle <= 0) return;

                float r = color.R / 255f;
                float g = color.G / 255f;
                float b = color.B / 255f;

                try
                {
                    GTA.Native.Function.Call(GTA.Native.Hash.SET_PARTICLE_FX_LOOPED_COLOUR, particleHandle, r, g, b);
                }
                catch
                {
                    // silently ignore invalid handles
                }
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "MemoryAccess.SetLoopedPtfxColor");
            }
        }

        // Defensive wrapper to safely use any entity
        public static void SafeEntityAction(Entity entity, Action<Entity> action)
        {
            try
            {
                if (entity != null && entity.Exists())
                    action(entity);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "MemoryAccess.SafeEntityAction");
            }
        }
    }
}
