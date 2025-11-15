using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Drawing;
using TornadoScript.ScriptMain.CrashHandling;


namespace TornadoScript.ScriptMain.Utility
{
    public class LoopedParticle
    {
        private float _scale;
        private float _alpha;

        public string AssetName { get; }
        public string FxName { get; }

        public int Handle { get; private set; }

        public bool Exists =>
            Safe(() => Handle != -1 && Function.Call<bool>(Hash.DOES_PARTICLE_FX_LOOPED_EXIST, Handle), "Exists");

        public bool IsLoaded =>
            Safe(() => Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, AssetName), "IsLoaded");


        public float Alpha
        {
            get => _alpha;
            set => Safe(() =>
            {
                _alpha = value;
                Log("SET_PARTICLE_FX_LOOPED_ALPHA");
                Function.Call(Hash.SET_PARTICLE_FX_LOOPED_ALPHA, Handle, _alpha);
            }, "Alpha");
        }

        public float Scale
        {
            get => _scale;
            set => Safe(() =>
            {
                _scale = value;
                Log("SET_PARTICLE_FX_LOOPED_SCALE");
                Function.Call(Hash.SET_PARTICLE_FX_LOOPED_SCALE, Handle, _scale);
            }, "Scale");
        }

        public Color Colour
        {
            set => Safe(() =>
            {
                float r = value.R / 255f;
                float g = value.G / 255f;
                float b = value.B / 255f;

                Log("SET_PARTICLE_FX_LOOPED_COLOUR");
                Function.Call(Hash.SET_PARTICLE_FX_LOOPED_COLOUR, Handle, r, g, b);
            }, "Colour");
        }


        public LoopedParticle(string asset, string fx)
        {
            Handle = -1;
            AssetName = asset;
            FxName = fx;
        }


        // -------------------------------------------------------
        // PTFX LOADING
        // -------------------------------------------------------

        public bool EnsureLoaded()
        {
            return Safe(() =>
            {
                if (string.IsNullOrEmpty(AssetName)) return false;

                if (!IsLoaded)
                {
                    Log("REQUEST_NAMED_PTFX_ASSET");
                    Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, AssetName);

                    int timeout = Game.GameTime + 2000;

                    // Replace Script.Yield() with Thread.Sleep
                    while (!IsLoaded && Game.GameTime < timeout)
                        System.Threading.Thread.Sleep(0);

                    if (!IsLoaded)
                    {
                        CrashLogger.Log("PTFX asset failed to load: " + AssetName);
                        return false;
                    }
                }

                return true;
            }, "EnsureLoaded");
        }



        // -------------------------------------------------------
        // START — ENTITY
        // -------------------------------------------------------
        public void Start(Entity entity, float scale, Vector3 offset, Vector3 rotation, Bone? bone)
        {
            Safe(() =>
            {
                if (Handle != -1) return;
                if (entity == null || !entity.Exists())
                {
                    CrashLogger.Log("Start(Entity): Entity invalid");
                    return;
                }

                if (!EnsureLoaded()) return;

                _scale = scale;

                Log("USE_PARTICLE_FX_ASSET");
                Function.Call(Hash.USE_PARTICLE_FX_ASSET, AssetName);


                if (bone == null)
                {
                    Log("START_PARTICLE_FX_LOOPED_ON_ENTITY");
                    Handle = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY,
                        FxName, entity,
                        offset.X, offset.Y, offset.Z,
                        rotation.X, rotation.Y, rotation.Z,
                        scale, 0, 0, 1);
                }
                else
                {
                    Log("START_PARTICLE_FX_LOOPED_ON_ENTITY_BONE");
                    Handle = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY_BONE,
                        FxName, entity,
                        offset.X, offset.Y, offset.Z,
                        rotation.X, rotation.Y, rotation.Z,
                        (int)bone, scale, 0, 0, 0);
                }
            }, "Start(Entity)");
        }

        public void Start(Entity entity, float scale) =>
            Start(entity, scale, Vector3.Zero, Vector3.Zero, null);


        // -------------------------------------------------------
        // START — COORDS
        // -------------------------------------------------------
        public void Start(Vector3 position, float scale, Vector3 rotation)
        {
            Safe(() =>
            {
                if (Handle != -1) return;
                if (!EnsureLoaded()) return;

                _scale = scale;

                Log("USE_PARTICLE_FX_ASSET");
                Function.Call(Hash.USE_PARTICLE_FX_ASSET, AssetName);

                Log("START_PARTICLE_FX_LOOPED_AT_COORD");
                Handle = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_AT_COORD,
                    FxName, position.X, position.Y, position.Z,
                    rotation.X, rotation.Y, rotation.Z,
                    scale, 0, 0, 0, 0);
            }, "Start(Coords)");
        }

        public void Start(Vector3 position, float scale) =>
            Start(position, scale, Vector3.Zero);


        // -------------------------------------------------------
        // MODIFY
        // -------------------------------------------------------

        public void SetOffsets(Vector3 offset, Vector3 rotOffset) =>
            Safe(() =>
            {
                Log("SET_PARTICLE_FX_LOOPED_OFFSETS");
                Function.Call(Hash.SET_PARTICLE_FX_LOOPED_OFFSETS, Handle,
                    offset.X, offset.Y, offset.Z,
                    rotOffset.X, rotOffset.Y, rotOffset.Z);
            }, "SetOffsets");

        public void SetEvolution(string var, float val) =>
            Safe(() =>
            {
                Log("SET_PARTICLE_FX_LOOPED_EVOLUTION");
                Function.Call(Hash.SET_PARTICLE_FX_LOOPED_EVOLUTION, Handle, var, val, 0);
            }, "SetEvolution");


        // -------------------------------------------------------
        // REMOVE
        // -------------------------------------------------------

        public void Remove()
        {
            Safe(() =>
            {
                if (Handle == -1) return;

                Log("STOP_PARTICLE_FX_LOOPED");
                Function.Call(Hash.STOP_PARTICLE_FX_LOOPED, Handle, 0);

                Log("REMOVE_PARTICLE_FX");
                Function.Call(Hash.REMOVE_PARTICLE_FX, Handle, 0);

                Handle = -1;
            }, "Remove");
        }

        public void Remove(Vector3 pos, float radius)
        {
            Safe(() =>
            {
                if (Handle == -1) return;

                Log("REMOVE_PARTICLE_FX_IN_RANGE");
                Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE,
                    pos.X, pos.Y, pos.Z, radius);

                Handle = -1;
            }, "RemoveRange");
        }


        // -------------------------------------------------------
        // UNLOAD
        // -------------------------------------------------------
        public void Unload() =>
            Safe(() =>
            {
                if (IsLoaded)
                {
                    Log("REMOVE_NAMED_PTFX_ASSET");
                    Function.Call(Hash.REMOVE_NAMED_PTFX_ASSET, AssetName);
                }
            }, "Unload");


        // -------------------------------------------------------
        // SAFE WRAPPERS
        // -------------------------------------------------------

        private static void Safe(Action a, string ctx)
        {
            try { a(); }
            catch (Exception ex) { CrashLogger.LogError(ex, "LoopedParticle: " + ctx); }
        }

        private static T Safe<T>(Func<T> f, string ctx)
        {
            try { return f(); }
            catch (Exception ex) { CrashLogger.LogError(ex, "LoopedParticle: " + ctx); return default; }
        }

        private static void Log(string msg) =>
            CrashLogger.Log("Native call: " + msg);
    }
}
