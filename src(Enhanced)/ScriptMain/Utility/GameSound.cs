using GTA;
using GTA.Math;
using GTA.Native;
using System;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain.Utility
{
    public class GameSound
    {
        private int _soundId;
        private readonly string _soundSet, _sound;

        public bool Active { get; private set; }

        public GameSound(string sound, string soundSet)
        {
            Active = false;
            _sound = sound ?? string.Empty;
            _soundSet = soundSet ?? string.Empty;
            _soundId = -1;
        }

        public static void Load(string audioBank)
        {
            if (string.IsNullOrEmpty(audioBank)) return;

            SafeRun(() =>
            {
                Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, audioBank, false);
            }, $"Load audio bank {audioBank}");
        }

        public static void Release(string audioBank)
        {
            if (string.IsNullOrEmpty(audioBank)) return;

            SafeRun(() =>
            {
                Function.Call(Hash.RELEASE_NAMED_SCRIPT_AUDIO_BANK, audioBank);
            }, $"Release audio bank {audioBank}");
        }

        public static void Load(GameSound sound)
        {
            if (sound == null || string.IsNullOrEmpty(sound._soundSet)) return;

            SafeRun(() =>
            {
                Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, sound._soundSet, false);
            }, $"Load sound {sound._sound}");
        }

        public void Play(Entity ent)
        {
            if (ent == null || !ent.Exists()) return;

            SafeRun(() =>
            {
                _soundId = Function.Call<int>(Hash.GET_SOUND_ID);
                if (_soundId == -1) return;

                Function.Call(Hash.PLAY_SOUND_FROM_ENTITY, _soundId, _sound, ent.Handle, 0, 0, 0);
                Active = true;
            }, $"Play sound {_sound} from entity");
        }

        public void Play(Vector3 position, int range)
        {
            SafeRun(() =>
            {
                _soundId = Function.Call<int>(Hash.GET_SOUND_ID);
                if (_soundId == -1) return;

                Function.Call(Hash.PLAY_SOUND_FROM_COORD, _soundId, _sound, position.X, position.Y, position.Z, 0, 1, Math.Max(0, range), 0);
                Active = true;
            }, $"Play sound {_sound} at position {position}");
        }

        public void Destroy()
        {
            SafeRun(() =>
            {
                if (_soundId == -1) return;

                Function.Call(Hash.STOP_SOUND, _soundId);
                Function.Call(Hash.RELEASE_SOUND_ID, _soundId);
                _soundId = -1;
                Active = false;
            }, $"Destroy sound {_sound}");
        }

        private static void SafeRun(Action action, string context)
        {
            try { action(); }
            catch (Exception ex) { CrashLogger.LogError(ex, context); }
        }
    }
}
