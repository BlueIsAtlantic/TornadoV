using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Windows.Forms;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.Commands;
using TornadoScript.ScriptMain.Config;
using TornadoScript.ScriptMain.CrashHandling;
using TornadoScript.ScriptMain.Memory;
using TornadoScript.ScriptMain.Utility;

namespace TornadoScript.ScriptMain.Script
{
    public class MainScript : ScriptThread
    {
        public static TornadoFactory Factory;
        public readonly TornadoFactory _factory;

        public MainScript()
        {

            _factory = GetOrCreate<TornadoFactory>();
            Factory = _factory;
            CrashHandler.Initialize();
            RegisterVars();
            SetupAssets();
            GetOrCreate<CommandManager>();
            KeyDown += KeyPressed;
        }

        private static void SetupAssets()
        {
            SafeRun(() =>
            {
                MemoryAccess.Initialize();
            }, "SetupAssets");
        }

        private static void RegisterVars()
        {
            SafeRun(() =>
            {
                RegisterVar("toggleconsole", Keys.T, false);
                RegisterVar("enableconsole", IniHelper.GetValue("Other", "EnableConsole", false));
                RegisterVar("notifications", IniHelper.GetValue("Other", "Notifications", true));
                RegisterVar("spawninstorm", IniHelper.GetValue("Other", "SpawnInStorm", true));
                RegisterVar("soundenabled", IniHelper.GetValue("Other", "SoundEnabled", false));
                RegisterVar("sirenenabled", IniHelper.GetValue("Other", "SirenEnabled", false));
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
                RegisterVar("vortexParticleMod", IniHelper.GetValue("VortexAdvanced", "ParticleMod", false));
                RegisterVar("vortexEnableCloudTopParticle", IniHelper.GetValue("VortexAdvanced", "CloudTopEnabled", true));
                RegisterVar("vortexEnableCloudTopParticleDebris", IniHelper.GetValue("VortexAdvanced", "CloudTopDebrisEnabled", true));
                RegisterVar("vortexEnableSurfaceDetection", IniHelper.GetValue("VortexAdvanced", "EnableSurfaceDetection", false));
                RegisterVar("vortexUseEntityPool", IniHelper.GetValue("VortexAdvanced", "UseInternalPool", false));
            }, "RegisterVars");
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            try
            {
                if (!GetVar<bool>("enablekeybinds"))
                    return;

                var toggleKeyVar = GetVar<Keys>("togglescript");
                if (toggleKeyVar == null)
                    return;

                var toggleKey = toggleKeyVar.Value;

                if (e.KeyCode != toggleKey)
                    return;


                // Check if despawning
                if (_factory != null && _factory.ActiveVortexCount > 0 && !GetVar<bool>("multiVortex"))
                {
                    _factory.RemoveAll();
                    if (GetVar<bool>("notifications"))
                    {
                        Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado despawned!");
                        Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
                    }
                    return;
                }

                // ENHANCED FIX: Get player using DIRECT natives instead of Game.Player.Character
                int playerId = Function.Call<int>(Hash.PLAYER_ID);

                int playerPedId = Function.Call<int>(Hash.PLAYER_PED_ID);;

                // Check if ped exists
                bool pedExists = Function.Call<bool>(Hash.DOES_ENTITY_EXIST, playerPedId);
                if (!pedExists)
                {
                    return;
                }

                // Get player position using native
                Vector3 playerPos = Function.Call<Vector3>(Hash.GET_ENTITY_COORDS, playerPedId, true);

                // Calculate random spawn position (NO ForwardVector!)
                double randomAngle = Probability.GetInteger(0, 360) * (Math.PI / 180.0);
                float distance = 180f;

                var offset = new Vector3(
                    (float)Math.Cos(randomAngle) * distance,
                    (float)Math.Sin(randomAngle) * distance,
                    0f
                );

                var spawnPos = playerPos + offset;

                // Create vortex
                var vortex = _factory?.CreateVortex(spawnPos);

                if (vortex != null)
                {
                    if (GetVar<bool>("notifications"))
                    {
                       // Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                       // Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado spawned!");
                       // Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
                    }
                }
                else
                {

                }

            }
            catch (Exception ex)
            {

            }
        }

        public override void OnUpdate(int gameTime)
        {
            base.OnUpdate(gameTime);
        }

        public void Cleanup()
        {
            SafeRun(() =>
            {
                _factory?.RemoveAll();
                Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE, 0f, 0f, 0f, 1000000.0f);
            }, "Cleanup");
        }

        private static void SafeRun(Action action, string context)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ScriptCore.Logger.Log($"MainScript.SafeRun: Exception in context {context}: {ex}");
            }
        }
    }
}