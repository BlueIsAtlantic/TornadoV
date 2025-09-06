using GTA;
using GTA.Native;
using System.Windows.Forms;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.Commands;
using TornadoScript.ScriptMain.Config;
using TornadoScript.ScriptMain.Memory;
using TornadoScript.ScriptMain.Utility;

namespace TornadoScript.ScriptMain.Script
{
    public class MainScript : ScriptThread
    {
        public static TornadoFactory Factory; // ← static for menu access
        public readonly TornadoFactory _factory;
        private bool didInitTlsAlloc = false;

        private TornadoMenu tornadoMenu;
        public MainScript()
        {
            RegisterVars();
            SetupAssets();
            _factory = GetOrCreate<TornadoFactory>();
            Factory = _factory; // ← assign static
            GetOrCreate<CommandManager>();
            KeyDown += KeyPressed;
        }

        private static void SetupAssets()
        {
            MemoryAccess.Initialize();

            if (GetVar<bool>("vortexParticleMod"))
            {
                Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "core");
                MemoryAccess.SetPtfxColor("core", "ent_amb_smoke_foundry", 1, System.Drawing.Color.Black);
                MemoryAccess.SetPtfxColor("core", "ent_amb_smoke_foundry", 2, System.Drawing.Color.Black);
            }
        }

        private static void RegisterVars()
        {
            RegisterVar("toggleconsole", Keys.T, false);
            RegisterVar("enableconsole", IniHelper.GetValue("Other", "EnableConsole", false));
            RegisterVar("notifications", IniHelper.GetValue("Other", "Notifications", true));
            RegisterVar("spawninstorm", IniHelper.GetValue("Other", "SpawnInStorm", true));
            RegisterVar("soundenabled", IniHelper.GetValue("Other", "SoundEnabled", true));
            RegisterVar("sirenenabled", IniHelper.GetValue("Other", "SirenEnabled", true));
            RegisterVar("togglescript", IniHelper.GetValue("KeyBinds", "ToggleScript", Keys.F6), true);
            RegisterVar("enablekeybinds", IniHelper.GetValue("KeyBinds", "KeybindsEnabled", true));
            RegisterVar("multiVortex", IniHelper.GetValue("VortexAdvanced", "MultiVortexEnabled", true));
            RegisterVar("vortexMovementEnabled", IniHelper.GetValue("Vortex", "MovementEnabled", true));
            RegisterVar("vortexMoveSpeedScale", IniHelper.GetValue("Vortex", "MoveSpeedScale", 1.0f));
            RegisterVar("vortexTopEntitySpeed", IniHelper.GetValue("Vortex", "MaxEntitySpeed", 40.0f));
            RegisterVar("vortexMaxEntityDist", IniHelper.GetValue("Vortex", "MaxEntityDistance", 57.0f));
            RegisterVar("vortexHorizontalPullForce", IniHelper.GetValue("Vortex", "HorizontalForceScale", 1.7f));
            RegisterVar("vortexVerticalPullForce", IniHelper.GetValue("Vortex", "VerticalForceScale", 2.29f));
            RegisterVar("vortexRotationSpeed", IniHelper.GetValue("Vortex", "RotationSpeed", 2.4f));
            RegisterVar("vortexRadius", IniHelper.GetValue("Vortex", "VortexRadius", 9.40f));
            RegisterVar("vortexReverseRotation", IniHelper.GetValue("Vortex", "ReverseRotation", false));
            RegisterVar("vortexMaxParticleLayers", IniHelper.GetValue("VortexAdvanced", "MaxParticleLayers", 48));
            RegisterVar("vortexParticleCount", IniHelper.GetValue("VortexAdvanced", "ParticlesPerLayer", 9));
            RegisterVar("vortexLayerSeperationScale", IniHelper.GetValue("VortexAdvanced", "LayerSeperationAmount", 22.0f));
            RegisterVar("vortexParticleName", IniHelper.GetValue("VortexAdvanced", "ParticleName", "ent_amb_smoke_foundry"));
            RegisterVar("vortexParticleAsset", IniHelper.GetValue("VortexAdvanced", "ParticleAsset", "core"));
            RegisterVar("vortexParticleMod", IniHelper.GetValue("VortexAdvanced", "ParticleMod", true));
            RegisterVar("vortexEnableCloudTopParticle", IniHelper.GetValue("VortexAdvanced", "CloudTopEnabled", true));
            RegisterVar("vortexEnableCloudTopParticleDebris", IniHelper.GetValue("VortexAdvanced", "CloudTopDebrisEnabled", true));
            RegisterVar("vortexEnableSurfaceDetection", IniHelper.GetValue("VortexAdvanced", "EnableSurfaceDetection", true));
            RegisterVar("vortexUseEntityPool", IniHelper.GetValue("VortexAdvanced", "UseInternalPool", true));
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (!GetVar<bool>("enablekeybinds")) return;
            if (e.KeyCode != GetVar<Keys>("togglescript")) return;

            // If there's already a tornado and multiVortex is off, despawn it
            if (_factory.ActiveVortexCount > 0 && !GetVar<bool>("multiVortex"))
            {
                _factory.RemoveAll();
                // Optional: notify about despawn instead of spawn
                if (GetVar<bool>("notifications"))
                {
                    Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                    Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado despawned!");
                    Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
                }
                return; // exit early, don't spawn a new tornado
            }

            // Spawn a new tornado
            Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE, 0f, 0f, 0f, 1000000.0f);
            Function.Call(Hash.SET_WIND, 70.0f);

            var position = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 180f;
            var vortex = _factory.CreateVortex(position);

            // Only notify if a new tornado was actually spawned
            if (vortex != null && GetVar<bool>("notifications"))
            {
                Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado spawned!");
                Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
            }
        }


        public override void OnUpdate(int gameTime)
        {
            if (!didInitTlsAlloc)
            {
                WinHelper.CopyTlsValues(WinHelper.GetProcessMainThreadId(), Win32Native.GetCurrentThreadId(), 0xC8, 0xC0, 0xB8);
                didInitTlsAlloc = true;
            }

            base.OnUpdate(gameTime);
        }

        /// <summary>
        /// Manual cleanup method to call on mod unload or script reload.
        /// </summary>
        public void Cleanup()
        {
            _factory?.RemoveAll();
            Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE, 0f, 0f, 0f, 1000000.0f);
            ReleaseAssets();
        }

        private static void ReleaseAssets()
        {
            // Placeholder for any additional asset cleanup
        }
    }
}
