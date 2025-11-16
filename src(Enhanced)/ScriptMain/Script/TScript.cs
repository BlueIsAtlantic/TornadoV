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
            CrashLogger.Log("MainScript: Constructor START");

            _factory = GetOrCreate<TornadoFactory>();
            Factory = _factory;

            CrashLogger.Log("MainScript: Factory created");

            CrashHandler.Initialize();
            CrashLogger.Log("MainScript: CrashHandler initialized");

            RegisterVars();
            CrashLogger.Log("MainScript: Vars registered");

            SetupAssets();
            CrashLogger.Log("MainScript: Assets setup");

            GetOrCreate<CommandManager>();
            CrashLogger.Log("MainScript: CommandManager created");

            KeyDown += KeyPressed;
            CrashLogger.Log("MainScript: KeyDown event attached");

            CrashLogger.Log("MainScript: Constructor END");
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

                CrashLogger.Log("===== F6 PRESSED - SPAWN SEQUENCE START =====");

                // Check if despawning
                if (_factory != null && _factory.ActiveVortexCount > 0 && !GetVar<bool>("multiVortex"))
                {
                    CrashLogger.Log("KeyPressed: Despawning existing tornado");
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
                CrashLogger.Log("KeyPressed: Getting player ID via native...");
                int playerId = Function.Call<int>(Hash.PLAYER_ID);
                CrashLogger.Log($"KeyPressed: Player ID = {playerId}");

                CrashLogger.Log("KeyPressed: Getting player ped ID via native...");
                int playerPedId = Function.Call<int>(Hash.PLAYER_PED_ID);
                CrashLogger.Log($"KeyPressed: Player ped ID = {playerPedId}");

                // Check if ped exists
                CrashLogger.Log("KeyPressed: Checking if ped exists...");
                bool pedExists = Function.Call<bool>(Hash.DOES_ENTITY_EXIST, playerPedId);
                if (!pedExists)
                {
                    CrashLogger.Log("KeyPressed: Player ped doesn't exist!");
                    return;
                }
                CrashLogger.Log("KeyPressed: Player ped exists");

                // Get player position using native
                CrashLogger.Log("KeyPressed: Getting player coordinates via native...");
                Vector3 playerPos = Function.Call<Vector3>(Hash.GET_ENTITY_COORDS, playerPedId, true);
                CrashLogger.Log($"KeyPressed: Player position: {playerPos}");

                // Calculate random spawn position (NO ForwardVector!)
                CrashLogger.Log("KeyPressed: Calculating RANDOM spawn position...");
                double randomAngle = Probability.GetInteger(0, 360) * (Math.PI / 180.0);
                float distance = 180f;

                var offset = new Vector3(
                    (float)Math.Cos(randomAngle) * distance,
                    (float)Math.Sin(randomAngle) * distance,
                    0f
                );

                var spawnPos = playerPos + offset;
                CrashLogger.Log($"KeyPressed: Spawn position calculated: {spawnPos}");

                // Create vortex
                CrashLogger.Log("KeyPressed: Calling CreateVortex...");
                var vortex = _factory?.CreateVortex(spawnPos);

                if (vortex != null)
                {
                    CrashLogger.Log("KeyPressed: SUCCESS - Tornado spawned!");
                    if (GetVar<bool>("notifications"))
                    {
                        Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado spawned!");
                        Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
                    }
                }
                else
                {
                    CrashLogger.Log("KeyPressed: CreateVortex returned null");
                }

                CrashLogger.Log("===== SPAWN SEQUENCE COMPLETE =====");
            }
            catch (Exception ex)
            {
                CrashLogger.Log($"KeyPressed: EXCEPTION - {ex.Message}");
                CrashLogger.LogError(ex, "KeyPressed");
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
                CrashLogger.LogError(ex, context);
            }
        }
    }
}