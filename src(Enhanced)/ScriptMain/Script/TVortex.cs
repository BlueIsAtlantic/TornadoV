using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
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

        private readonly List<TornadoParticle> _particles = new List<TornadoParticle>(512);
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

        public const int MaxEntityCount = 200; // REDUCED from 300
        private readonly Dictionary<int, ActiveEntity> _pulledEntities = new Dictionary<int, ActiveEntity>(MaxEntityCount);
        private readonly List<int> _pendingRemovalEntities = new List<int>(32); // Pre-allocated
        private readonly List<KeyValuePair<int, ActiveEntity>> _entitySnapshot = new List<KeyValuePair<int, ActiveEntity>>(MaxEntityCount); // Reusable

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

        // OPTIMIZATION: Cache frequently used script vars
        private float _cachedVerticalForce;
        private float _cachedHorizontalForce;
        private float _cachedTopSpeed;
        private int _lastVarCacheTime;

        // OPTIMIZATION: Frame skipping for distant particles
        private int _updateFrameCounter;
        private const int PARTICLE_UPDATE_INTERVAL = 2; // Update every 2 frames

        public TornadoVortex(Vector3 initialPosition, bool neverDespawn)
        {
            _position = initialPosition;
            _createdTime = Game.GameTime;
            _lifeSpan = neverDespawn ? -1 : Probability.GetInteger(160000, 600000);
            MaxEntityDist = ScriptThread.GetVar<float>("vortexMaxEntityDist");

            // Cache initial values
            RefreshCachedVars();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RefreshCachedVars()
        {
            _cachedVerticalForce = ScriptThread.GetVar<float>("vortexVerticalPullForce");
            _cachedHorizontalForce = ScriptThread.GetVar<float>("vortexHorizontalPullForce");
            _cachedTopSpeed = ScriptThread.GetVar<float>("vortexTopEntitySpeed");
            _lastVarCacheTime = Game.GameTime;
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

            // OPTIMIZATION: Reduce particle count significantly for better performance
            maxLayers = Math.Min(maxLayers, 36); // Cap at 24 layers instead of 48
            particleCount = Math.Min(particleCount, 6); // Cap at 6 particles per layer

            var multiplier = 360 / particleCount;
            var particleSize = 3.0685f;
            maxLayers = enableClouds ? 8 : maxLayers; // Reduced from 12

            for (var layerIdx = 0; layerIdx < maxLayers; layerIdx++)
            {
                // OPTIMIZATION: Skip some layers for better performance

                int particlesThisLayer = (layerIdx > maxLayers - 4) ? particleCount + 2 : particleCount;

                for (var angle = 0; angle < particlesThisLayer; angle++)
                {
                    var position = _position;
                    position.Z += ScriptThread.GetVar<float>("vortexLayerSeperationScale") * layerIdx;
                    var rotation = new Vector3(angle * multiplier, 0, 0);
                    TornadoParticle particle;
                    bool bIsTopParticle = false;

                    try
                    {
                        // OPTIMIZATION: Only create bottom layer particles every other angle
                        if (layerIdx < 2 && angle % 2 == 0)
                        {
                            particle = new TornadoParticle(this, position, rotation, "scr_agencyheistb", "scr_env_agency3b_smoke", radius, layerIdx);
                            particle.StartFx(4.7f);
                            _particles.Add(particle);

                            try
                            {
                                if (particle?.Ref != null && particle.Ref.Exists())
                                    Function.Call(Hash.ADD_SHOCKING_EVENT_FOR_ENTITY, 86, particle.Ref.Handle, 0.0f);
                            }
                            catch { }
                        }
                    }
                    catch { }

                    try
                    {
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
                        catch { }
                    }
                    catch { }
                }
            }
        }

        private void ReleaseEntity(int entityIdx)
        {
            if (!_pendingRemovalEntities.Contains(entityIdx))
                _pendingRemovalEntities.Add(entityIdx);
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
            // OPTIMIZATION: Reduced frequency from 600ms to 1500ms
            if (gameTime < _nextUpdateTime)
                return;

            try
            {
                if (_pulledEntities.Count >= MaxEntityCount)
                {
                    _nextUpdateTime = gameTime + 1500;
                    return;
                }

                // OPTIMIZATION: Reduced search radius
                Entity[] allEntities = World.GetNearbyEntities(_position, Math.Min(maxDistanceDelta + 10f, 70f));

                int addedCount = 0;
                foreach (var ent in allEntities)
                {
                    try
                    {
                        if (ent == null || !ent.Exists()) continue;
                        if (_pulledEntities.ContainsKey(ent.Handle)) continue;
                        if (addedCount >= 20) break; // OPTIMIZATION: Limit entities added per update

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
                                catch { }
                            }
                        }

                        AddEntity(new ActiveEntity(ent, 3.0f * Probability.GetScalar(), 3.0f * Probability.GetScalar()));
                        addedCount++;
                    }
                    catch { continue; }
                }
            }
            catch { }

            _nextUpdateTime = gameTime + 1500; // OPTIMIZATION: Increased from 600ms
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePulledEntities(int gameTime, float maxDistanceDelta)
        {
            // OPTIMIZATION: Refresh cached vars every 5 seconds instead of reading every frame
            if (gameTime - _lastVarCacheTime > 5000)
            {
                RefreshCachedVars();
            }

            _pendingRemovalEntities.Clear();

            // OPTIMIZATION: Reuse snapshot list instead of creating new one
            _entitySnapshot.Clear();
            _entitySnapshot.AddRange(_pulledEntities);

            int processedCount = 0;
            const int MAX_ENTITIES_PER_FRAME = 100; // OPTIMIZATION: Limit entities processed per frame

            foreach (var kvp in _entitySnapshot)
            {
                if (processedCount >= MAX_ENTITIES_PER_FRAME) break;
                processedCount++;

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

                    float verticalForce = _cachedVerticalForce;
                    float horizontalForce = _cachedHorizontalForce;

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

                        Function.Call(Hash.SET_ENTITY_MAX_SPEED, entity.Handle, _cachedTopSpeed);
                    }
                    catch
                    {
                        ReleaseEntity(key);
                        continue;
                    }
                }
                catch
                {
                    ReleaseEntity(kvp.Key);
                    continue;
                }
            }

            foreach (var e in _pendingRemovalEntities)
            {
                try { _pulledEntities.Remove(e); } catch { }
            }
        }

        public override void OnUpdate(int gameTime)
        {
            try
            {
                if (_lifeSpan > 0 && gameTime - _createdTime > _lifeSpan)
                    _despawnRequested = true;

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
            catch { }
        }

        public override void Dispose()
        {
            try
            {
                _particles.ForEach(x => x.Dispose());
                _particles.Clear();
                _pulledEntities.Clear();
                _pendingRemovalEntities.Clear();
                _entitySnapshot.Clear();
            }
            catch { }
            base.Dispose();
        }
    }
}