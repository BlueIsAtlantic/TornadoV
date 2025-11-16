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

        public TornadoFactory()
        {
            CrashLogger.Log("TornadoFactory: Constructor called");
            CrashHandler.Initialize();
        }

        public TornadoVortex CreateVortex(Vector3 position)
        {
            CrashLogger.Log("TornadoFactory.CreateVortex: METHOD CALLED");
            CrashLogger.Log($"TornadoFactory.CreateVortex: Position={position}");

            try
            {
                if (spawnInProgress)
                {
                    CrashLogger.Log("TornadoFactory.CreateVortex: Spawn already in progress, skipping");
                    return null;
                }

                CrashLogger.Log("TornadoFactory.CreateVortex: Validating position...");
                if (float.IsNaN(position.X) || float.IsNaN(position.Y) || float.IsNaN(position.Z))
                {
                    CrashLogger.Log("TornadoFactory.CreateVortex: Invalid position coordinates");
                    return null;
                }

                CrashLogger.Log("TornadoFactory.CreateVortex: Position valid, shifting vortex array...");
                for (var i = _activeVortexList.Length - 1; i > 0; i--)
                    _activeVortexList[i] = _activeVortexList[i - 1];

                CrashLogger.Log("TornadoFactory.CreateVortex: Getting ground height...");
                float groundZ = World.GetGroundHeight(position);

                if (groundZ < -1000f || float.IsNaN(groundZ))
                {
                    groundZ = position.Z;
                    CrashLogger.Log($"TornadoFactory.CreateVortex: Using fallback Z coordinate: {groundZ}");
                }

                position.Z = groundZ - 10.0f;
                CrashLogger.Log($"TornadoFactory.CreateVortex: Final position: {position}");

                CrashLogger.Log("TornadoFactory.CreateVortex: Creating TornadoVortex instance...");
                var tVortex = new TornadoVortex(position, false);
                CrashLogger.Log("TornadoFactory.CreateVortex: TornadoVortex instance created");

                try
                {
                    CrashLogger.Log("TornadoFactory.CreateVortex: Calling tVortex.Build()...");
                    tVortex.Build();
                    CrashLogger.Log("TornadoFactory.CreateVortex: Build() completed successfully");
                }
                catch (Exception ex)
                {
                    CrashLogger.Log($"TornadoFactory.CreateVortex: Build() EXCEPTION - {ex.Message}");
                    CrashLogger.LogError(ex, "CreateVortex.Build");

                    try
                    {
                        tVortex?.Dispose();
                    }
                    catch { }

                    spawnInProgress = false;
                    return null;
                }

                CrashLogger.Log("TornadoFactory.CreateVortex: Adding to active list...");
                _activeVortexList[0] = tVortex;
                ActiveVortexCount = Math.Min(ActiveVortexCount + 1, _activeVortexList.Length);

                if (ScriptThread.GetVar<bool>("notifications"))
                {
                    Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                    Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado spawned nearby.");
                    Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
                }

                spawnInProgress = true;

                CrashLogger.Log($"TornadoFactory.CreateVortex: SUCCESS - Tornado spawned at {position}");

                return tVortex;
            }
            catch (Exception ex)
            {
                CrashLogger.Log($"TornadoFactory.CreateVortex: FATAL EXCEPTION - {ex.Message}");
                CrashLogger.LogError(ex, "CreateVortex");
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
                                    CrashLogger.Log($"OnUpdate: Failed to set wind speed - {ex.Message}");
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
                            CrashLogger.LogError(ex, "OnUpdate: Failed to spawn storm tornado");
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
                                    CrashLogger.Log($"OnUpdate: Screen fade check failed - {ex.Message}");
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
                        CrashLogger.LogError(ex, "OnUpdate: Error checking vortex status");
                    }
                }

                base.OnUpdate(gameTime);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "TornadoFactory OnUpdate");
            }
        }

        public void RemoveAll()
        {
            try
            {
                spawnInProgress = false;

                CrashLogger.Log($"RemoveAll: Removing {ActiveVortexCount} active vortexes");

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
                        CrashLogger.LogError(ex, $"RemoveAll: Error disposing vortex at index {i}");
                    }
                }

                ActiveVortexCount = 0;

                try
                {
                    Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE,
                        Game.Player.Character.Position.X,
                        Game.Player.Character.Position.Y,
                        Game.Player.Character.Position.Z,
                        500.0f);
                }
                catch (Exception ex)
                {
                    CrashLogger.Log($"RemoveAll: Failed to remove particle effects - {ex.Message}");
                }

                CrashLogger.Log("RemoveAll: All vortexes removed successfully");
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "RemoveAll");
            }
        }

        public override void Dispose()
        {
            try
            {
                CrashLogger.Log("TornadoFactory: Disposing...");

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
                        CrashLogger.LogError(ex, $"Dispose: Error disposing vortex at index {i}");
                    }
                }

                base.Dispose();

                CrashLogger.Log("TornadoFactory: Disposed successfully");
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "TornadoFactory Dispose");
            }
        }
    }
}