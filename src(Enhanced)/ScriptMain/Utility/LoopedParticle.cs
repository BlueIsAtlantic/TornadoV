using GTA;
using GTA.Math;
using GTA.Native;
using Color = System.Drawing.Color;

namespace TornadoScript.ScriptMain.Utility
{
    public class LoopedParticle
    {
        private float _scale;
        private float _alpha;

        public string AssetName { get; }
        public string FxName { get; }
        public int Handle { get; private set; }

        public bool Exists => Handle != -1 && Function.Call<bool>(Hash.DOES_PARTICLE_FX_LOOPED_EXIST, Handle);
        public bool IsLoaded => Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, AssetName);

        public float Alpha { get { return _alpha; } set { Function.Call(Hash.SET_PARTICLE_FX_LOOPED_ALPHA, Handle, _alpha = value); } }
        public float Scale { get { return _scale; } set { Function.Call(Hash.SET_PARTICLE_FX_LOOPED_SCALE, Handle, _scale = value); } }
        public Color Colour { set { Function.Call(Hash.SET_PARTICLE_FX_LOOPED_COLOUR, Handle, value.R, value.G, value.B, 0); } }

        public LoopedParticle(string assetName, string fxName)
        {
            Handle = -1;
            AssetName = assetName;
            FxName = fxName;
        }

        public void Load()
        {
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, AssetName);
        }

        public void Start(Entity entity, float scale, Vector3 offset, Vector3 rotation, Bone? bone)
        {
            if (Handle != -1) return;

            _scale = scale;

            Function.Call(Hash.USE_PARTICLE_FX_ASSET, AssetName);

            Handle = bone == null ?
                Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY, FxName,
                    entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, scale, 0, 0, 1) :
                Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY_BONE, FxName,
                    entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, (int)bone, scale, 0, 0, 0);
        }

        public void Start(Entity entity, float scale)
        {
            Start(entity, scale, Vector3.Zero, Vector3.Zero, null);
        }

        public void Start(Vector3 position, float scale, Vector3 rotation)
        {
            if (Handle != -1) return;

            _scale = scale;

            Function.Call(Hash.USE_PARTICLE_FX_ASSET, AssetName);

            Handle = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_AT_COORD, FxName,
                position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, scale, 0, 0, 0, 0);
        }

        public void Start(Vector3 position, float scale)
        {
            Start(position, scale, Vector3.Zero);
        }

        public void SetOffsets(Vector3 offset, Vector3 rotOffset)
        {
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_OFFSETS, Handle, offset.X, offset.Y, offset.Z, rotOffset.X, rotOffset.Y, rotOffset.Z);
        }

        public void SetEvolution(string variableName, float value)
        {
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_EVOLUTION, Handle, variableName, value, 0);
        }

        public void Remove()
        {
            if (Handle == -1) return;

            Function.Call(Hash.STOP_PARTICLE_FX_LOOPED, Handle, 0);
            Function.Call(Hash.REMOVE_PARTICLE_FX, Handle, 0);
            Handle = -1;
        }

        public void Remove(Vector3 position, float radius)
        {
            if (Handle == -1) return;

            Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE, position.X, position.Y, position.Z, radius);
            Handle = -1;
        }

        public void Unload()
        {
            if (IsLoaded)
                Function.Call(Hash.REMOVE_NAMED_PTFX_ASSET, AssetName);
        }
    }
}
