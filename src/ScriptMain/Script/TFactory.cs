using GTA;
using GTA.Math;
using GTA.Native;
using System;
using TornadoScript.ScriptCore;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.Utility;

namespace TornadoScript.ScriptMain.Script
{
    /// <summary>
    /// Extension to manage the spawning of tornadoes.
    /// </summary>
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
            // No sound initialization
        }

        /// <summary>
        /// Create a vortex at the given position.
        /// </summary>
        public TornadoVortex CreateVortex(Vector3 position)
        {
            if (spawnInProgress)
                return null;

            // Shift existing tornadoes down the list
            for (var i = _activeVortexList.Length - 1; i > 0; i--)
                _activeVortexList[i] = _activeVortexList[i - 1];

            // Adjust Z so tornado spawns slightly below ground
            position.Z = World.GetGroundHeight(position) - 10.0f;

            // Create the tornado
            var tVortex = new TornadoVortex(position, false);
            tVortex.Build();
            _activeVortexList[0] = tVortex;

            ActiveVortexCount = Math.Min(ActiveVortexCount + 1, _activeVortexList.Length);

            // Show notification above minimap instead of subtitle
            if (ScriptThread.GetVar<bool>("notifications"))
            {
                Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado spawned nearby.");
                Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
            }

            spawnInProgress = true;
            return null;
        }


        public override void OnUpdate(int gameTime)
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
                            Function.Call(Hash.SET_WIND_SPEED, 70.0f); // suspense

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

        public void RemoveAll()
        {
            spawnInProgress = false;

            for (var i = 0; i < ActiveVortexCount; i++)
            {
                _activeVortexList[i].Dispose();
                _activeVortexList[i] = null;
            }

            ActiveVortexCount = 0;
        }

        public override void Dispose()
        {
            for (var i = 0; i < ActiveVortexCount; i++)
            {
                _activeVortexList[i].Dispose();
            }

            base.Dispose();
        }
    }
}
