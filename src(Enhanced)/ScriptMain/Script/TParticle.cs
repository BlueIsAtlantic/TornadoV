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
                // ENHANCED: Ensure model is properly requested before creating prop
                var model = new Model("prop_beach_volball02");
                
                if (!model.IsLoaded)
                {
                    model.Request(2000); // Increased timeout for Enhanced
                    
                    // Wait for model to load
                    int timeout = 0;
                    while (!model.IsLoaded && timeout < 100)
                    {
                        GTA.Script.Wait(10);
                        timeout++;
                    }
                    
                    if (!model.IsLoaded)
                    {
                        ScriptCore.Logger.Log("TornadoParticle: Failed to load prop_beach_volball02 model");
                        return;
                    }
                }

                prop = World.CreateProp(model, position, false, false);
                
                if (prop != null && prop.Exists())
                {
                    Function.Call(Hash.SET_ENTITY_COLLISION, prop.Handle, 0, 0);
                    prop.IsVisible = false;
                    prop.IsPositionFrozen = false; // Ensure physics work
                }
                else
                {
                    ScriptCore.Logger.Log("TornadoParticle: Failed to create prop entity");
                }
            }, "SafeSetup");
            
            return prop;
        }

        public override void OnUpdate(int gameTime)
        {
            SafeRun(() =>
            {
                if (Ref == null || !Ref.Exists())
                {
                    // Entity was destroyed, clean up
                    RemoveFx();
                    return;
                }

                _centerPos = Parent.Position + _offset;

                if (Math.Abs(_angle) > Math.PI * 2.0f) 
                    _angle = 0.0f;

                Ref.Position = _centerPos + MathEx.MultiplyVector(
                    new Vector3(_radius * (float)Math.Cos(_angle), _radius * (float)Math.Sin(_angle), 0), 
                    _rotation
                );

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
                if (Ref == null || !Ref.Exists())
                {
                    ScriptCore.Logger.Log("TornadoParticle.StartFx: Ref entity is null or doesn't exist");
                    return;
                }

                // ENHANCED: Ensure particle asset is loaded before starting
                if (!_ptfx.IsLoaded)
                {
                    _ptfx.Load();
                    
                    // Wait for asset to load
                    int timeout = 0;
                    while (!_ptfx.IsLoaded && timeout < 100)
                    {
                        GTA.Script.Wait(10);
                        timeout++;
                    }
                    
                    if (!_ptfx.IsLoaded)
                    {
                        ScriptCore.Logger.Log($"TornadoParticle.StartFx: Failed to load particle asset {_ptfx.AssetName}");
                        return;
                    }
                }

                _ptfx.Start(this, scale);
                
                // Verify particle started successfully
                if (!_ptfx.Exists)
                {
                    ScriptCore.Logger.Log($"TornadoParticle.StartFx: Particle {_ptfx.FxName} failed to start");
                }
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
                
                // ENHANCED: Ensure prop is properly cleaned up
                if (Ref != null && Ref.Exists())
                {
                    try
                    {
                        Ref.Delete();
                    }
                    catch (Exception ex)
                    {
                        ScriptCore.Logger.Log($"TornadoParticle.Dispose: Error deleting prop - {ex.Message}");
                    }
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
                CrashHandler.HandleCrash(ex, $"TornadoParticle.{context}");
            }
        }
    }
}
