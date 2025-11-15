using GTA;
using GTA.Math;
using GTA.Native;
using System;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.CrashHandling;
using TornadoScript.ScriptMain.Script;
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
            SafeRun(() => { }, "TornadoFactory Constructor");
        }

        public TornadoVortex CreateVortex(Vector3 position)
        {
            return SafeRun(() =>
            {
                if (spawnInProgress) return null;

                // Validate position
                if (float.IsNaN(position.X) || float.IsNaN(position.Y) || float.IsNaN(position.Z))
                    return null;

                var player = Game.Player?.Character;
                if (player == null || !player.Exists()) return null;

                // Shift array safely
                for (int i = _activeVortexList.Length - 1; i > 0; i--)
                    _activeVortexList[i] = _activeVortexList[i - 1];

                // Safe ground height
                float groundZ = 0f;
                try { groundZ = World.GetGroundHeight(position); } catch { groundZ = position.Z; }
                position.Z = groundZ - 10f;

                var tVortex = new TornadoVortex(position, false);
                tVortex.Build();

                _activeVortexList[0] = tVortex;
                ActiveVortexCount = Math.Min(ActiveVortexCount + 1, _activeVortexList.Length);

                if (ScriptThread.GetVar<bool>("notifications"))
                {
                    try
                    {
                        Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Tornado spawned nearby.");
                        Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
                    }
                    catch { /* ignore feed errors */ }
                }

                spawnInProgress = true;
                return tVortex;
            }, "CreateVortex");
        }

        public override void OnUpdate(int gameTime)
        {
            SafeRun(() =>
            {
                var player = Game.Player?.Character;
                if (player == null || !player.Exists()) return;

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
                                try { Function.Call(Hash.SET_WIND_SPEED, 70.0f); } catch { }
                                spawnInProgress = true;
                                delaySpawn = true;
                            }
                            _lastSpawnAttempt = Game.GameTime;
                        }
                    }
                    else delaySpawn = false;

                    if (delaySpawn && Game.GameTime - _spawnDelayStartTime > (TornadoSpawnDelayBase + _spawnDelayAdditive))
                    {
                        spawnInProgress = false;
                        delaySpawn = false;

                        try
                        {
                            var position = player.Position + player.ForwardVector * 100f;
                            CreateVortex(position.Around(150.0f).Around(175.0f));
                        }
                        catch { /* ignore any errors in vortex spawn */ }
                    }
                }
                else
                {
                    if (_activeVortexList[0]?.DespawnRequested == true || (player.IsDead && SafeCall(() => Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT))))
                        RemoveAll();
                }

                base.OnUpdate(gameTime);
            }, "TornadoFactory OnUpdate");
        }

        // Helper for safe native calls returning bool
        private static bool SafeCall(Func<bool> func)
        {
            try { return func(); }
            catch { return false; }
        }


        public void RemoveAll()
        {
            SafeRun(() =>
            {
                spawnInProgress = false;
                for (var i = 0; i < ActiveVortexCount; i++)
                {
                    try
                    {
                        _activeVortexList[i]?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.LogError(ex, "RemoveAll: disposing vortex failed");
                    }
                    _activeVortexList[i] = null;
                }
                ActiveVortexCount = 0;
            }, "RemoveAll");
        }

        public override void Dispose()
        {
            SafeRun(() =>
            {
                for (var i = 0; i < ActiveVortexCount; i++)
                {
                    try
                    {
                        _activeVortexList[i]?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.LogError(ex, "Dispose: disposing vortex failed");
                    }
                    _activeVortexList[i] = null;
                }
                ActiveVortexCount = 0;

                try { base.Dispose(); }
                catch (Exception ex) { CrashLogger.LogError(ex, "Dispose: base.Dispose failed"); }
            }, "Dispose");
        }


        private static T SafeRun<T>(Func<T> func, string context)
        {
            try { return func(); }
            catch (Exception ex) { CrashLogger.LogError(ex, context); return default(T); }
        }

        private static void SafeRun(Action action, string context)
        {
            try { action(); }
            catch (Exception ex) { CrashLogger.LogError(ex, context); }
        }
    }
}
