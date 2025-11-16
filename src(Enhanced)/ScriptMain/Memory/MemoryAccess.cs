using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace TornadoScript.ScriptMain.Memory
{
    /// <summary>
    /// Enhanced-compatible version - uses native calls instead of memory manipulation
    /// </summary>
    public static class MemoryAccess
    {
        private static bool bInitialized = false;

        // Particle color tracking (memory modification removed for Enhanced)
        private static Dictionary<string, Color> particleColors = new Dictionary<string, Color>();

        public static void Initialize()
        {
            // No memory patterns needed for Enhanced - using native calls only
            bInitialized = true;
        }

        /// <summary>
        /// Collect entities using native World functions instead of memory pools
        /// </summary>
        public static IList<Entity> CollectEntitiesFull()
        {
            if (!bInitialized)
                Initialize();

            List<Entity> entities = new List<Entity>();

            try
            {
                // Use native entity collection - much safer for Enhanced
                Entity[] nearbyEntities = World.GetNearbyEntities(Game.Player.Character.Position, 300f);

                foreach (var entity in nearbyEntities)
                {
                    if (entity != null && entity.Exists())
                    {
                        entities.Add(entity);
                    }
                }
            }
            catch (Exception ex)
            {
                ScriptCore.Logger.Log("CollectEntitiesFull error: " + ex.Message);
            }

            return entities;
        }

        /// <summary>
        /// Legacy method - now uses native collection
        /// </summary>
        public static IEnumerable<Entity> GetAllEntitiesInternal()
        {
            if (!bInitialized)
                Initialize();

            Entity[] entities = World.GetNearbyEntities(Game.Player.Character.Position, 400f);

            foreach (var entity in entities)
            {
                if (entity != null && entity.Exists())
                    yield return entity;
            }
        }

        /// <summary>
        /// Particle color modification - Enhanced doesn't support direct memory modification
        /// This is now a NO-OP but kept for compatibility
        /// </summary>
        public static void SetPtfxLOD(string baseAsset, string particleName)
        {
            // Enhanced: Cannot modify particle LOD via memory
            // Keeping method signature for compatibility
        }

        /// <summary>
        /// Particle color setting - Enhanced doesn't support this via memory
        /// Stores color preference but cannot actually modify particles
        /// </summary>
        public static void SetPtfxColor(string baseAsset, string particleName, int emitterIndex, Color newColor)
        {
            if (!bInitialized)
                Initialize();

            string key = $"{baseAsset}:{particleName}:{emitterIndex}";
            particleColors[key] = newColor;

            // NOTE: In Enhanced, we cannot directly modify particle colors via memory
            // Alternative: Use different particle effects that have the desired color
            // or accept the default particle colors
        }
    }
}