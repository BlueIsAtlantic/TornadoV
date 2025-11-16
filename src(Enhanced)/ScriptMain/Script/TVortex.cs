using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.CrashHandling;
using TornadoScript.ScriptMain.Memory;
using TornadoScript.ScriptMain.Utility;

namespace TornadoScript.ScriptMain.Script
{
    public class TornadoVortex : ScriptExtension
    {
        public float ForceScale { get; } = 3.0f;
        public float InternalForcesDist { get; } = 5.0f;
        public float MaxEntityDist { get; set; } = 57.0f;

        private readonly List<TornadoParticle> _particles = new List<TornadoParticle>();
        private int _aliveTime, _createdTime, _nextUpdateTime;
        private int _lastFullUpdateTime;
        private int _lifeSpan;

        private struct ActiveEntity
        {
            public ActiveEntity(Entity entity, float xBias, float yBias)
            {
                Entity = entity;
                XBias = xBias;
                YBias = yBias;
                IsPlayer = entity == Helpers.GetLocalPed();
            }

            public Entity Entity { get; }
            public float XBias { get; }
            public float YBias { get; }
            public bool IsPlayer { get; }
        }

        public const int MaxEntityCount = 300;
        private readonly Dictionary<int, ActiveEntity> _pulledEntities = new Dictionary<int, ActiveEntity>();
        private readonly List<int> pendingRemovalEntities = new List<int>();

        private Vector3 _position, _destination;
        private bool _despawnRequested;

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public bool DespawnRequested
        {
            get { return _despawnRequested; }
            set { _despawnRequested = value; }
        }

        private readonly Ped _player = Helpers.GetLocalPed();
        private int _lastPlayerShapeTestTime;
        private bool _lastRaycastResultFailed;

        // Particle color system removed for Enhanced compatibility
        // Enhanced doesn't support direct particle color modification

        private bool _useNativeEntityCollection = true;

        public TornadoVortex(Vector3 initialPosition, bool neverDespawn)
        {
            _position = initialPosition;
            _createdTime = Game.GameTime;
            _lifeSpan = neverDespawn ? -1 : Probability.GetInteger(160000, 600000);
            MaxEntityDist = ScriptThread.GetVar<float>("vortexMaxEntityDist");
        }

        public void ChangeDestination(bool trackToPlayer)
        {
            for (int i = 0; i < 50; i++)
            {
                _destination = trackToPlayer ? _player.Position.Around(130.0f) : Helpers.GetRandomPositionFromCoords(_destination, 100.0f);
                _destination.Z = World.GetGroundHeight(_destination) - 10.0f;
                var nearestRoadPos = World.GetNextPositionOnStreet(_destination);

                if (_destination.DistanceTo(nearestRoadPos) < 40.0f && Math.Abs(nearestRoadPos.Z - _destination.Z) < 10.0f)
                    break;
            }
        }

        public void Build()
        {
            float radius = ScriptThread.GetVar<float>("vortexRadius");
            int particleCount = ScriptThread.GetVar<int>("vortexParticleCount");
            int maxLayers = ScriptThread.GetVar<int>("vortexMaxParticleLayers");
            string particleAsset = ScriptThread.GetVar<string>("vortexParticleAsset");
            string particleName = ScriptThread.GetVar<string>("vortexParticleName");
            bool enableClouds = ScriptThread.GetVar<bool>("vortexEnableCloudTopParticle");

            var multiplier = 360 / particleCount;
            var particleSize = 3.0685f;
            maxLayers = enableClouds ? 12 : maxLayers;

            for (var layerIdx = 0; layerIdx < maxLayers; layerIdx++)
            {
                for (var angle = 0; angle < (layerIdx > maxLayers - 4 ? particleCount + 5 : particleCount); angle++)
                {
                    var position = _position;
                    position.Z += ScriptThread.GetVar<float>("vortexLayerSeperationScale") * layerIdx;
                    var rotation = new Vector3(angle * multiplier, 0, 0);
                    TornadoParticle particle;
                    bool bIsTopParticle = false;

                    try
                    {
                        // Bottom layers use different particle effect
                        if (layerIdx < 2)
                        {
                            particle = new TornadoParticle(this, position, rotation, "scr_agencyheistb", "scr_env_agency3b_smoke", radius, layerIdx);
                            particle.StartFx(4.7f);
                            _particles.Add(particle);

                            try
                            {
                                if (particle?.Ref != null && particle.Ref.Exists())
                                    Function.Call(Hash.ADD_SHOCKING_EVENT_FOR_ENTITY, 86, particle.Ref.Handle, 0.0f);
                            }
                            catch (Exception exInner)
                            {
                                CrashHandler.HandleCrash(exInner, $"Build: Add shocking event failed for small particle at layer {layerIdx}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashHandler.HandleCrash(ex, $"TVortex Build() small particle spawn failed at layer {layerIdx}");
                    }

                    try
                    {
                        // Top cloud particles
                        if (enableClouds && layerIdx > maxLayers - 3)
                        {
                            position.Z += 12f;
                            particleSize += 6.0f;
                            radius += 7f;
                            bIsTopParticle = true;
                        }

                        particle = new TornadoParticle(this, position, rotation, particleAsset, particleName, radius, layerIdx, bIsTopParticle);
                        particle.StartFx(particleSize);
                        radius += 0.08f * (0.72f * layerIdx);
                        particleSize += 0.01f * (0.12f * layerIdx);
                        _particles.Add(particle);

                        try
                        {
                            if (particle?.Ref != null && particle.Ref.Exists())
                                Function.Call(Hash.ADD_SHOCKING_EVENT_FOR_ENTITY, 86, particle.Ref.Handle, 0.0f);
                        }
                        catch (Exception exInner)
                        {
                            CrashHandler.HandleCrash(exInner, $"Build: Add shocking event failed for main particle at layer {layerIdx}");
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashHandler.HandleCrash(ex, $"TVortex Build() main particle spawn failed at layer {layerIdx}");
                    }
                }
            }
        }

        private void ReleaseEntity(int entityIdx)
        {
            if (!pendingRemovalEntities.Contains(entityIdx))
                pendingRemovalEntities.Add(entityIdx);
        }

        private void AddEntity(ActiveEntity entity)
        {
            if (entity.Entity != null && entity.Entity.Exists())
            {
                _pulledEntities[entity.Entity.Handle] = entity;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CollectNearbyEntities(int gameTime, float maxDistanceDelta)
        {
            if (gameTime < _nextUpdateTime)
                return;

            try
            {
                // ENHANCED: Use native entity collection instead of memory pool
                Entity[] allEntities = World.GetNearbyEntities(_position, maxDistanceDelta + 20f);

                foreach (var ent in allEntities)
                {
                    try
                    {
                        if (ent == null || !ent.Exists()) continue;
                        if (_pulledEntities.Count >= MaxEntityCount) break;
                        if (_pulledEntities.ContainsKey(ent.Handle)) continue;

                        var dist2d = ent.Position.DistanceTo2D(_position);
                        if (dist2d > maxDistanceDelta + 4.0f) continue;
                        if (ent.HeightAboveGround > 300.0f) continue;

                        if (ent is Ped ped && !ped.IsRagdoll)
                        {
                            if (ped.Exists())
                            {
                                try
                                {
                                    Function.Call(Hash.SET_PED_TO_RAGDOLL, ped.Handle, 800, 1500, 2, 1, 1, 0);
                                }
                                catch (Exception exNative)
                                {
                                    CrashHandler.HandleCrash(exNative, "CollectNearbyEntities: SET_PED_TO_RAGDOLL failed");
                                }
                            }
                        }

                        AddEntity(new ActiveEntity(ent, 3.0f * Probability.GetScalar(), 3.0f * Probability.GetScalar()));
                    }
                    catch (Exception exEnt)
                    {
                        CrashHandler.HandleCrash(exEnt, "CollectNearbyEntities: entity loop failed");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashHandler.HandleCrash(ex, "CollectNearbyEntities: collection failed");
            }

            _nextUpdateTime = gameTime + 600;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePulledEntities(int gameTime, float maxDistanceDelta)
        {
            float globalVerticalForce = ScriptThread.GetVar<float>("vortexVerticalPullForce");
            float globalHorizontalForce = ScriptThread.GetVar<float>("vortexHorizontalPullForce");
            float topSpeed = ScriptThread.GetVar<float>("vortexTopEntitySpeed");

            pendingRemovalEntities.Clear();

            var snapshot = new List<KeyValuePair<int, ActiveEntity>>(_pulledEntities);

            foreach (var kvp in snapshot)
            {
                int key = kvp.Key;
                var value = kvp.Value;

                try
                {
                    var entity = value.Entity;

                    if (entity == null || !entity.Exists())
                    {
                        ReleaseEntity(key);
                        continue;
                    }

                    var pos = entity.Position;
                    var dist = Vector2.Distance(pos.Vec2(), _position.Vec2());
                    if (dist > maxDistanceDelta - 13f || entity.HeightAboveGround > 300.0f)
                    {
                        ReleaseEntity(key);
                        continue;
                    }

                    var targetPos = new Vector3(_position.X + value.XBias, _position.Y + value.YBias, pos.Z);
                    var dirVec = targetPos - pos;
                    if (dirVec.Length() < 0.0001f)
                        continue;

                    var direction = Vector3.Normalize(dirVec);
                    var forceBias = Probability.NextFloat();
                    var force = ForceScale * (forceBias + forceBias / Math.Max(dist, 1.0f));

                    float verticalForce = globalVerticalForce;
                    float horizontalForce = globalHorizontalForce;

                    if (value.IsPlayer)
                    {
                        verticalForce *= 1.62f;
                        horizontalForce *= 1.2f;

                        if (gameTime - _lastPlayerShapeTestTime > 1000)
                        {
                            var raycast = World.Raycast(entity.Position, targetPos, IntersectFlags.Map);
                            _lastRaycastResultFailed = raycast.DidHit;
                            _lastPlayerShapeTestTime = gameTime;
                        }

                        if (_lastRaycastResultFailed) continue;

                        try
                        {
                            entity.ApplyForce(direction * horizontalForce, new Vector3(Probability.NextFloat(), 0, Probability.GetScalar()));
                            var upDir = Vector3.Normalize(new Vector3(_position.X, _position.Y, _position.Z + 1000.0f) - entity.Position);
                            entity.ApplyForceToCenterOfMass(upDir * verticalForce);
                            var cross = Vector3.Cross(direction, Vector3.WorldUp);
                            entity.ApplyForceToCenterOfMass(Vector3.Normalize(cross) * force * horizontalForce);
                            Function.Call(Hash.SET_ENTITY_MAX_SPEED, entity.Handle, topSpeed);
                        }
                        catch (Exception exNative)
                        {
                            CrashHandler.HandleCrash(exNative, $"UpdatePulledEntities: player native calls failed for entity {key}");
                            ReleaseEntity(key);
                            continue;
                        }

                        continue;
                    }

                    try
                    {
                        var model = entity.Model;
                        if (model != null && model.IsValid && model.IsPlane)
                        {
                            force *= 6.0f;
                            verticalForce *= 6.0f;
                        }

                        entity.ApplyForce(direction * horizontalForce, new Vector3(Probability.NextFloat(), 0, Probability.GetScalar()));
                        var upDir = Vector3.Normalize(new Vector3(_position.X, _position.Y, _position.Z + 1000.0f) - entity.Position);
                        entity.ApplyForceToCenterOfMass(upDir * verticalForce);
                        var cross = Vector3.Cross(direction, Vector3.WorldUp);
                        entity.ApplyForceToCenterOfMass(Vector3.Normalize(cross) * force * horizontalForce);

                        Function.Call(Hash.SET_ENTITY_MAX_SPEED, entity.Handle, topSpeed);
                    }
                    catch (Exception exNative)
                    {
                        CrashHandler.HandleCrash(exNative, $"UpdatePulledEntities: native calls failed for entity {key}");
                        ReleaseEntity(key);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    CrashHandler.HandleCrash(ex, $"TVortex UpdatePulledEntities failed for entity {kvp.Key}");
                    ReleaseEntity(kvp.Key);
                    continue;
                }
            }

            foreach (var e in pendingRemovalEntities)
            {
                try { _pulledEntities.Remove(e); } catch { /* ignore */ }
            }
        }

        public override void OnUpdate(int gameTime)
        {
            try
            {
                if (_lifeSpan > 0 && gameTime - _createdTime > _lifeSpan)
                    _despawnRequested = true;

                // ENHANCED: Surface detection removed (required memory modification)
                // Particle colors cannot be changed dynamically in Enhanced

                if (ScriptThread.GetVar<bool>("vortexMovementEnabled"))
                {
                    if (_destination == Vector3.Zero || _position.DistanceTo(_destination) < 15.0f)
                        ChangeDestination(false);

                    if (_position.DistanceTo(_player.Position) > 200.0f)
                        ChangeDestination(true);

                    var vTarget = MathEx.MoveTowards(_position, _destination, ScriptThread.GetVar<float>("vortexMoveSpeedScale") * 0.287f);
                    _position = Vector3.Lerp(_position, vTarget, Game.LastFrameTime * 20.0f);
                }

                CollectNearbyEntities(gameTime, MaxEntityDist);
                UpdatePulledEntities(gameTime, MaxEntityDist);
            }
            catch (Exception ex)
            {
                CrashHandler.HandleCrash(ex, "TVortex OnUpdate failed");
            }
        }

        public override void Dispose()
        {
            try
            {
                _particles.ForEach(x => x.Dispose());
            }
            catch (Exception ex)
            {
                CrashHandler.HandleCrash(ex, "TVortex Dispose failed");
            }
            base.Dispose();
        }
    }
}
