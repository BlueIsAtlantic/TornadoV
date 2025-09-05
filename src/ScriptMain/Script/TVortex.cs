using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.Memory;
using TornadoScript.ScriptMain.Utility;

namespace TornadoScript.ScriptMain.Script
{
    public class TornadoVortex : ScriptExtension
    {
        public float ForceScale { get; } = 3.0f;
        public float InternalForcesDist { get; } = 5.0f;

        private readonly List<TornadoParticle> _particles = new List<TornadoParticle>();
        private int _aliveTime, _createdTime, _nextUpdateTime;
        private int _lastDebrisSpawnTime = 0;
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
        private int lastParticleShapeTestTime = 0;

        private Color particleColorPrev, particleColorGoal;
        private Color particleColor = Color.Black;
        private float particleLerpTime = 0.0f;
        private const float ColorLerpDuration = 200.0f;

        private bool _useInternalEntityArray = false;

        public TornadoVortex(Vector3 initialPosition, bool neverDespawn)
        {
            _position = initialPosition;
            _createdTime = Game.GameTime;
            _lifeSpan = neverDespawn ? -1 : Probability.GetInteger(160000, 600000);
            _useInternalEntityArray = ScriptThread.GetVar<bool>("vortexUseEntityPool");
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
            bool enableDebris = ScriptThread.GetVar<bool>("vortexEnableCloudTopParticleDebris");

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

                    if (layerIdx < 2)
                    {
                        particle = new TornadoParticle(this, position, rotation, "scr_agencyheistb", "scr_env_agency3b_smoke", radius, layerIdx);
                        particle.StartFx(4.7f);
                        _particles.Add(particle);
                        Function.Call(Hash.ADD_SHOCKING_EVENT_FOR_ENTITY, 86, particle.Ref.Handle, 0.0f);
                    }

                    if (enableClouds && layerIdx > maxLayers - 3)
                    {
                        if (enableDebris)
                        {
                            particle = new TornadoParticle(this, position, rotation, "scr_agencyheistb", "scr_env_agency3b_smoke", radius * 2.2f, layerIdx);
                            particle.StartFx(12.7f);
                            _particles.Add(particle);
                        }

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
                }
            }
        }

        private void ReleaseEntity(int entityIdx)
        {
            pendingRemovalEntities.Add(entityIdx);
        }

        private void AddEntity(ActiveEntity entity)
        {
            _pulledEntities[entity.Entity.Handle] = entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CollectNearbyEntities(int gameTime, float maxDistanceDelta)
        {
            if (gameTime < _nextUpdateTime)
                return;

            foreach (var ent in MemoryAccess.CollectEntitiesFull())
            {
                if (_pulledEntities.Count >= MaxEntityCount) break;
                if (_pulledEntities.ContainsKey(ent.Handle) ||
                    ent.Position.DistanceTo2D(_position) > maxDistanceDelta + 4.0f ||
                    ent.HeightAboveGround > 300.0f) continue;

                if (ent is Ped ped && !ped.IsRagdoll)
                    Function.Call(Hash.SET_PED_TO_RAGDOLL, ped.Handle, 800, 1500, 2, 1, 1, 0);

                AddEntity(new ActiveEntity(ent, 3.0f * Probability.GetScalar(), 3.0f * Probability.GetScalar()));
            }

            _nextUpdateTime = gameTime + 600;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePulledEntities(int gameTime, float maxDistanceDelta)
        {
            float verticalForce = ScriptThread.GetVar<float>("vortexVerticalPullForce");
            float horizontalForce = ScriptThread.GetVar<float>("vortexHorizontalPullForce");
            float topSpeed = ScriptThread.GetVar<float>("vortexTopEntitySpeed");

            pendingRemovalEntities.Clear();

            foreach (var e in _pulledEntities)
            {
                var entity = e.Value.Entity;
                var dist = Vector2.Distance(entity.Position.Vec2(), _position.Vec2());
                if (dist > maxDistanceDelta - 13f || entity.HeightAboveGround > 300.0f)
                {
                    ReleaseEntity(e.Key);
                    continue;
                }

                var targetPos = new Vector3(_position.X + e.Value.XBias, _position.Y + e.Value.YBias, entity.Position.Z);
                var direction = Vector3.Normalize(targetPos - entity.Position);
                var forceBias = Probability.NextFloat();
                var force = ForceScale * (forceBias + forceBias / dist);

                if (e.Value.IsPlayer)
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

                if (entity.Model.IsPlane)
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

            foreach (var e in pendingRemovalEntities)
            {
                _pulledEntities.Remove(e);
            }
        }

        private void UpdateSurfaceDetection(int gameTime)
        {
            if (gameTime - lastParticleShapeTestTime > 1200)
            {
                particleColorPrev = particleColor;
                particleColorGoal = Color.Black; // fallback
                particleLerpTime = 0.0f;
                lastParticleShapeTestTime = gameTime;
            }

            if (particleLerpTime < 1.0f)
            {
                particleLerpTime += Game.LastFrameTime / ColorLerpDuration;
                particleColor = particleColor.Lerp(particleColorGoal, particleLerpTime);
            }

            MemoryAccess.SetPtfxColor("core", "ent_amb_smoke_foundry", 0, particleColor);
            MemoryAccess.SetPtfxColor("core", "ent_amb_smoke_foundry", 1, particleColor);
            MemoryAccess.SetPtfxColor("core", "ent_amb_smoke_foundry", 2, particleColor);
        }

        private void UpdateDebrisLayer()
        {
            if (Game.GameTime - _lastDebrisSpawnTime > 3000 + Probability.GetInteger(0, 5000))
            {
                new TDebris(this, _position, ScriptThread.GetVar<float>("vortexRadius"));
            }
        }

        public override void OnUpdate(int gameTime)
        {
            if (gameTime - _createdTime > _lifeSpan)
                _despawnRequested = true;

            if (ScriptThread.GetVar<bool>("vortexEnableSurfaceDetection"))
                UpdateSurfaceDetection(gameTime);

            if (ScriptThread.GetVar<bool>("vortexMovementEnabled"))
            {
                if (_destination == Vector3.Zero || _position.DistanceTo(_destination) < 15.0f)
                    ChangeDestination(false);

                if (_position.DistanceTo(_player.Position) > 200.0f)
                    ChangeDestination(true);

                var vTarget = MathEx.MoveTowards(_position, _destination, ScriptThread.GetVar<float>("vortexMoveSpeedScale") * 0.287f);
                _position = Vector3.Lerp(_position, vTarget, Game.LastFrameTime * 20.0f);
            }

            float maxEntityDist = ScriptThread.GetVar<float>("vortexMaxEntityDist");
            CollectNearbyEntities(gameTime, maxEntityDist);
            UpdatePulledEntities(gameTime, maxEntityDist);
            UpdateDebrisLayer();
        }

        public override void Dispose()
        {
            _particles.ForEach(x => x.Dispose());
            base.Dispose();
        }
    }
}
