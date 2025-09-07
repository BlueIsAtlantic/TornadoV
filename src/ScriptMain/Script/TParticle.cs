using GTA;
using GTA.Math;
using GTA.Native;
using System;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.CrashHandling; // Use centralized CrashHandler
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
            }, "PostSetup");
        }

        private static Prop SafeSetup(Vector3 position)
        {
            Prop prop = null;
            SafeRun(() =>
            {
                var model = new Model("prop_beachball_02");
                if (!model.IsLoaded) model.Request(1000);
                prop = World.CreateProp(model, position, false, false);
                Function.Call(Hash.SET_ENTITY_COLLISION, prop.Handle, 0, 0);
                prop.IsVisible = false;
            }, "Setup");
            return prop;
        }

        public override void OnUpdate(int gameTime)
        {
            SafeRun(() =>
            {
                _centerPos = Parent.Position + _offset;

                if (Math.Abs(_angle) > Math.PI * 2.0f) _angle = 0.0f;

                if (Ref != null && Ref.Exists())
                {
                    Ref.Position = _centerPos +
                        MathEx.MultiplyVector(new Vector3(_radius * (float)Math.Cos(_angle), _radius * (float)Math.Sin(_angle), 0), _rotation);
                }

                if (IsCloud)
                    _angle -= ScriptThread.GetVar<float>("vortexRotationSpeed") * 0.16f * Game.LastFrameTime;
                else
                    _angle -= ScriptThread.GetVar<float>("vortexRotationSpeed") * _layerMask * Game.LastFrameTime;

                base.OnUpdate(gameTime);
            }, "OnUpdate");
        }

        public void StartFx(float scale)
        {
            SafeRun(() =>
            {
                if (!_ptfx.IsLoaded) _ptfx.Load();
                _ptfx.Start(this, scale);
            }, "StartFx");
        }

        public void RemoveFx()
        {
            SafeRun(() => _ptfx.Remove(), "RemoveFx");
        }

        public override void Dispose()
        {
            SafeRun(() =>
            {
                RemoveFx();
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
                // Use centralized CrashHandler
                CrashHandler.HandleCrash(ex, context);
            }
        }
    }
}
