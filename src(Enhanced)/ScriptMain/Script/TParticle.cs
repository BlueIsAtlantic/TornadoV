using GTA;
using GTA.Math;
using GTA.Native;
using System;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.CrashHandling;
using TornadoScript.ScriptMain.Utility;

namespace TornadoScript.ScriptMain.Script
{
    public sealed class TornadoParticle : ScriptProp
    {
        public int LayerIndex { get; }
        public TornadoVortex Parent { get; set; }
        public bool IsCloud { get; }

        private Vector3 _centerPos;
        private readonly Vector3 _offset;
        private readonly Quaternion _rotation;
        private readonly LoopedParticle _ptfx;
        private readonly float _radius;
        private float _angle, _layerMask;

        // OPTIMIZATION: Cache frequently accessed values
        private float _cachedRotationSpeed;
        private float _cachedLayerSeparation;
        private int _lastCacheTime;

        // OPTIMIZATION: Frame skipping for distant particles
        private int _updateSkipCounter;
        private const int UPDATE_SKIP_FREQUENCY = 2; // Update every 2 frames for better performance

        public TornadoParticle(TornadoVortex vortex, Vector3 position, Vector3 angle, string fxAsset, string fxName, float radius, int layerIdx, bool isCloud = false)
            : base(SafeSetup(position))
        {
            LayerIndex = layerIdx;
            _offset = new Vector3(0, 0, ScriptThread.GetVar<float>("vortexLayerSeperationScale") * layerIdx);
            _rotation = MathEx.Euler(angle);
            _radius = radius;
            Parent = vortex;
            _centerPos = position;
            IsCloud = isCloud;
            _ptfx = new LoopedParticle(fxAsset, fxName);
            _updateSkipCounter = layerIdx % UPDATE_SKIP_FREQUENCY; // Stagger updates

            SafeRun(PostSetup, "TornadoParticle Constructor");
        }

        private void PostSetup()
        {
            SafeRun(() =>
            {
                _layerMask = 1.0f - (float)LayerIndex / (ScriptThread.GetVar<int>("vortexMaxParticleLayers") * 4);
                _layerMask *= 0.1f * LayerIndex;
                _layerMask = 1.0f - _layerMask;
                if (_layerMask <= 0.3f) _layerMask = 0.3f;

                // OPTIMIZATION: Cache values at construction
                RefreshCache();
            }, "PostSetup");
        }

        private void RefreshCache()
        {
            _cachedRotationSpeed = ScriptThread.GetVar<float>("vortexRotationSpeed");
            _cachedLayerSeparation = ScriptThread.GetVar<float>("vortexLayerSeperationScale");
            _lastCacheTime = Game.GameTime;
        }

        private static Prop SafeSetup(Vector3 position)
        {
            Prop prop = null;
            SafeRun(() =>
            {
                var model = new Model("prop_beach_volball02");

                if (!model.IsLoaded)
                {
                    model.Request(2000);

                    int timeout = 0;
                    while (!model.IsLoaded && timeout < 100)
                    {
                        GTA.Script.Wait(10);
                        timeout++;
                    }

                    if (!model.IsLoaded)
                    {
                        return;
                    }
                }

                prop = World.CreateProp(model, position, false, false);

                if (prop != null && prop.Exists())
                {
                    Function.Call(Hash.SET_ENTITY_COLLISION, prop.Handle, 0, 0);
                    prop.IsVisible = false;
                    prop.IsPositionFrozen = false;
                }
            }, "SafeSetup");

            return prop;
        }

        public override void OnUpdate(int gameTime)
        {
            // OPTIMIZATION: Frame skipping - only update every Nth frame based on layer
            _updateSkipCounter++;
            if (_updateSkipCounter < UPDATE_SKIP_FREQUENCY)
                return;
            _updateSkipCounter = 0;

            // OPTIMIZATION: Refresh cache every 10 seconds instead of reading every frame
            if (gameTime - _lastCacheTime > 10000)
            {
                RefreshCache();
            }

            SafeRun(() =>
            {
                if (Ref == null || !Ref.Exists())
                {
                    RemoveFx();
                    return;
                }

                _centerPos = Parent.Position + _offset;

                // OPTIMIZATION: Use modulo to reset angle instead of checking abs
                if (_angle > 6.28318f) // 2*PI
                    _angle -= 6.28318f;
                else if (_angle < -6.28318f)
                    _angle += 6.28318f;

                // OPTIMIZATION: Pre-calculate sin/cos values
                float cosAngle = (float)Math.Cos(_angle);
                float sinAngle = (float)Math.Sin(_angle);

                Ref.Position = _centerPos + MathEx.MultiplyVector(
                    new Vector3(_radius * cosAngle, _radius * sinAngle, 0),
                    _rotation
                );

                // OPTIMIZATION: Use cached rotation speed
                if (IsCloud)
                    _angle -= _cachedRotationSpeed * 0.16f * Game.LastFrameTime;
                else
                    _angle -= _cachedRotationSpeed * _layerMask * Game.LastFrameTime;

                base.OnUpdate(gameTime);
            }, "OnUpdate");
        }

        public void StartFx(float scale)
        {
            SafeRun(() =>
            {
                if (Ref == null || !Ref.Exists())
                {
                    return;
                }

                if (!_ptfx.IsLoaded)
                {
                    _ptfx.Load();

                    int timeout = 0;
                    while (!_ptfx.IsLoaded && timeout < 100)
                    {
                        GTA.Script.Wait(10);
                        timeout++;
                    }

                    if (!_ptfx.IsLoaded)
                    {
                        return;
                    }
                }

                _ptfx.Start(this, scale);
            }, "StartFx");
        }

        public void RemoveFx()
        {
            SafeRun(() =>
            {
                if (_ptfx != null)
                {
                    _ptfx.Remove();
                }
            }, "RemoveFx");
        }

        public override void Dispose()
        {
            SafeRun(() =>
            {
                RemoveFx();

                if (Ref != null && Ref.Exists())
                {
                    try
                    {
                        Ref.Delete();
                    }
                    catch { }
                }

                base.Dispose();
            }, "Dispose");
        }

        private static void SafeRun(Action action, string context)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // Silent fail for performance
            }
        }
    }
}