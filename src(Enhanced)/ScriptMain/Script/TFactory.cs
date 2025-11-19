using GTA;
using GTA.Math;
using GTA.Native;
using System;
using TornadoScript.ScriptCore;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.CrashHandling;
using TornadoScript.ScriptMain.Utility;

namespace TornadoScript.ScriptMain.Script
{
    public class TornadoFactory : ScriptExtension
    {
        private const int VortexLimit = 30;
        private const int TornadoSpawnDelayBase = 20000;
        private int _spawnDelayAdditive = 0;
        private int _spawnDelayStartTime = 0;
        private int _lastSpawnAttempt;

        public int ActiveVortexCount { get; private set; }
        private readonly TornadoVortex[] _activeVortexList = new TornadoVortex[VortexLimit];
        public TornadoVortex[] ActiveVortexList => _activeVortexList;

        private bool spawnInProgress = false;
        private bool delaySpawn = false;

        // OPTIMIZATION: Reduce spawn cooldown to prevent stacking operations
        private int _lastSpawnCompleteTime = 0;
        private const int SPAWN_COOLDOWN = 2000; // 2 second cooldown between spawns

        public TornadoFactory()
        {
            CrashHandler.Initialize();
        }

        public TornadoVortex CreateVortex(Vector3 position)
        {
            try
            {
                // OPTIMIZATION: Enforce cooldown between spawns
                if (Game.GameTime - _lastSpawnCompleteTime < SPAWN_COOLDOWN)
                {
                    return null;
                }

                if (spawnInProgress)
                {
                    return null;
                }

                if (float.IsNaN(position.X) || float.IsNaN(position.Y) || float.IsNaN(position.Z))
                {
                    return null;
                }

                spawnInProgress = true; // Set BEFORE any operations

                for (var i = _activeVortexList.Length - 1; i > 0; i--)
                    _activeVortexList[i] = _activeVortexList[i - 1];

                float groundZ = World.GetGroundHeight(position);

                if (groundZ < -1000f || float.IsNaN(groundZ))
                {
                    groundZ = position.Z;
                }

                position.Z = groundZ - 10.0f;

                var tVortex = new TornadoVortex(position, false);

                try
                {
                    // OPTIMIZATION: Clear old particles before building new ones
                    Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE,
                        position.X, position.Y, position.Z, 200.0f);

                    // Wait one frame to let particles clear
                    GTA.Script.Wait(0);

                    tVortex.Build();

                    // OPTIMIZATION: Wait another frame after building
                    GTA.Script.Wait(0);
                }
                catch (Exception ex)
                {
                    try
                    {
                        tVortex?.Dispose();
                    }
                    catch { }

                    spawnInProgress = false;
                    return null;
                }

                _activeVortexList[0] = tVortex;
                ActiveVortexCount = Math.Min(ActiveVortexCount + 1, _activeVortexList.Length);

                if (ScriptThread.GetVar<bool>("notifications"))
                {
                    Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                    Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado spawned nearby.");
                    Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
                }

                _lastSpawnCompleteTime = Game.GameTime; // OPTIMIZATION: Track completion time
                spawnInProgress = false; // Reset flag

                return tVortex;
            }
            catch (Exception ex)
            {
                spawnInProgress = false;
                return null;
            }
        }

        public override void OnUpdate(int gameTime)
        {
            try
            {
                if (ActiveVortexCount < 1)
                {
                    if (ScriptThread.GetVar<bool>("spawnInStorm"))
                    {
                        Weather currentWeather = World.Weather;
                        bool isStorm = currentWeather == Weather.ThunderStorm || currentWeather == Weather.Raining;

                        if (isStorm && !spawnInProgress && Game.GameTime - _lastSpawnAttempt > 1000)
                        {
                            if (Probability.GetBoolean(0.05f))
                            {
                                _spawnDelayStartTime = Game.GameTime;
                                _spawnDelayAdditive = Probability.GetInteger(0, 40);

                                try
                                {
                                    Function.Call(Hash.SET_WIND_SPEED, 70.0f);
                                }
                                catch (Exception ex)
                                {
                                    return;
                                }

                                spawnInProgress = true;
                                delaySpawn = true;
                            }

                            _lastSpawnAttempt = Game.GameTime;
                        }
                    }
                    else
                    {
                        delaySpawn = false;
                    }

                    if (delaySpawn && Game.GameTime - _spawnDelayStartTime > (TornadoSpawnDelayBase + _spawnDelayAdditive))
                    {
                        spawnInProgress = false;
                        delaySpawn = false;

                        try
                        {
                            var position = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 100f;
                            position = position.Around(150.0f).Around(175.0f);

                            CreateVortex(position);
                        }
                        catch (Exception ex)
                        {
                            spawnInProgress = false;
                        }
                    }
                }
                else
                {
                    try
                    {
                        if (_activeVortexList[0] != null)
                        {
                            bool shouldRemove = _activeVortexList[0].DespawnRequested;

                            if (Game.Player.IsDead)
                            {
                                try
                                {
                                    if (Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT))
                                    {
                                        shouldRemove = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                }
                            }

                            if (shouldRemove)
                            {
                                RemoveAll();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                base.OnUpdate(gameTime);
            }
            catch (Exception ex)
            {
            }
        }

        public void RemoveAll()
        {
            try
            {
                spawnInProgress = false;

                for (var i = 0; i < ActiveVortexCount; i++)
                {
                    try
                    {
                        if (_activeVortexList[i] != null)
                        {
                            _activeVortexList[i].Dispose();
                            _activeVortexList[i] = null;
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                ActiveVortexCount = 0;

                try
                {
                    // OPTIMIZATION: Expanded particle cleanup radius
                    Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE,
                        Game.Player.Character.Position.X,
                        Game.Player.Character.Position.Y,
                        Game.Player.Character.Position.Z,
                        1000.0f); // Increased from 500
                }
                catch (Exception ex)
                {
                }
            }
            catch (Exception ex)
            {

            }
        }

        public override void Dispose()
        {
            try
            {
                for (var i = 0; i < ActiveVortexCount; i++)
                {
                    try
                    {
                        if (_activeVortexList[i] != null)
                        {
                            _activeVortexList[i].Dispose();
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }

                base.Dispose();
            }
            catch (Exception ex)
            {

            }
        }
    }
}