using GTA;
using GTA.Math;
using GTA.Native;
using System;
using TornadoScript.ScriptCore;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.CrashHandling; // <-- CrashHandler
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
            // Optional: initialize CrashHandler if not already
            CrashHandler.Initialize();
        }

        public TornadoVortex CreateVortex(Vector3 position)
        {
            try
            {
                if (spawnInProgress)
                    return null;

                for (var i = _activeVortexList.Length - 1; i > 0; i--)
                    _activeVortexList[i] = _activeVortexList[i - 1];

                position.Z = World.GetGroundHeight(position) - 10.0f;

                var tVortex = new TornadoVortex(position, false);

                try
                {
                    tVortex.Build();
                }
                catch (Exception ex)
                {
                    CrashHandler.HandleCrash(ex, "Failed to build tornado vortex in TornadoFactory.");
                }

                _activeVortexList[0] = tVortex;
                ActiveVortexCount = Math.Min(ActiveVortexCount + 1, _activeVortexList.Length);

                if (ScriptThread.GetVar<bool>("notifications"))
                {
                    Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                    Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado spawned nearby.");
                    Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
                }

                spawnInProgress = true;
                return tVortex;
            }
            catch (Exception ex)
            {
                CrashHandler.HandleCrash(ex, "Error in CreateVortex.");
                return null;
            }
        }

        public override void OnUpdate(int gameTime)
        {
            try
            {
                if (ActiveVortexCount < 1)
                {
                    if (World.Weather == Weather.ThunderStorm && ScriptThread.GetVar<bool>("spawnInStorm"))
                    {
                        if (!spawnInProgress && Game.GameTime - _lastSpawnAttempt > 1000)
                        {
                            if (Probability.GetBoolean(0.05f))
                            {
                                _spawnDelayStartTime = Game.GameTime;
                                _spawnDelayAdditive = Probability.GetInteger(0, 40);
                                Function.Call(Hash.SET_WIND_SPEED, 70.0f);
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

                        var position = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 100f;
                        CreateVortex(position.Around(150.0f).Around(175.0f));
                    }
                }
                else
                {
                    if (_activeVortexList[0].DespawnRequested || Game.Player.IsDead && Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT))
                    {
                        RemoveAll();
                    }
                }

                base.OnUpdate(gameTime);
            }
            catch (Exception ex)
            {
                CrashHandler.HandleCrash(ex, "Error in TornadoFactory OnUpdate.");
            }
        }

        public void RemoveAll()
        {
            try
            {
                spawnInProgress = false;
                for (var i = 0; i < ActiveVortexCount; i++)
                {
                    try { _activeVortexList[i]?.Dispose(); }
                    catch (Exception ex)
                    {
                        CrashHandler.HandleCrash(ex, "Error disposing active vortex in RemoveAll.");
                    }
                    _activeVortexList[i] = null;
                }
                ActiveVortexCount = 0;
            }
            catch (Exception ex)
            {
                CrashHandler.HandleCrash(ex, "Error in RemoveAll.");
            }
        }

        public override void Dispose()
        {
            try
            {
                for (var i = 0; i < ActiveVortexCount; i++)
                {
                    try { _activeVortexList[i]?.Dispose(); }
                    catch (Exception ex)
                    {
                        CrashHandler.HandleCrash(ex, "Error disposing active vortex in Dispose.");
                    }
                }

                base.Dispose();
            }
            catch (Exception ex)
            {
                CrashHandler.HandleCrash(ex, "Error in TornadoFactory Dispose.");
            }
        }
    }
}
