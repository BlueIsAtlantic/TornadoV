using System;
using System.IO;
using System.Runtime.InteropServices;
using TornadoScript.ScriptCore.Game;

namespace TornadoScript.ScriptMain.Audio
{
    /// <summary>
    /// Manages audio playback for tornado sounds using Windows MCI (Media Control Interface)
    /// </summary>
    public class AudioManager : IDisposable
    {
        [DllImport("winmm.dll")]
        private static extern int mciSendString(string command, System.Text.StringBuilder returnValue, int returnLength, IntPtr winHandle);

        private readonly string soundsFolderPath;
        private bool isTornadoSoundPlaying = false;
        private bool isSirenSoundPlaying = false;

        private string tornadoSoundPath;
        private string sirenSoundPath;

        // Settings
        private bool soundEnabled;
        private bool sirenEnabled;

        public bool IsTornadoSoundPlaying => isTornadoSoundPlaying;
        public bool IsSirenSoundPlaying => isSirenSoundPlaying;

        public AudioManager()
        {
            // Path to sounds folder: scripts/TornadoV_sounds/
            soundsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "TornadoV_sounds");

            tornadoSoundPath = Path.Combine(soundsFolderPath, "tornado.wav");
            sirenSoundPath = Path.Combine(soundsFolderPath, "siren.wav");

            // Load settings from script vars
            soundEnabled = ScriptThread.GetVar<bool>("soundenabled");
            sirenEnabled = ScriptThread.GetVar<bool>("sirenenabled");
        }

        /// <summary>
        /// Start playing the looping tornado sound
        /// </summary>
        public void PlayTornadoSound()
        {
            if (!soundEnabled || isTornadoSoundPlaying) return;

            try
            {
                if (!File.Exists(tornadoSoundPath))
                {
                    ScriptCore.Logger.Log($"Tornado sound file not found: {tornadoSoundPath}");
                    return;
                }

                // Stop any previous tornado sound
                mciSendString($"close tornado", null, 0, IntPtr.Zero);

                // Open and play the sound with repeat
                mciSendString($"open \"{tornadoSoundPath}\" type waveaudio alias tornado", null, 0, IntPtr.Zero);
                mciSendString("play tornado repeat", null, 0, IntPtr.Zero);

                isTornadoSoundPlaying = true;
                ScriptCore.Logger.Log("Tornado sound started (looping)");
            }
            catch (Exception ex)
            {
                ScriptCore.Logger.Log($"Error playing tornado sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop the tornado sound
        /// </summary>
        public void StopTornadoSound()
        {
            if (!isTornadoSoundPlaying) return;

            try
            {
                mciSendString("stop tornado", null, 0, IntPtr.Zero);
                mciSendString("close tornado", null, 0, IntPtr.Zero);
                isTornadoSoundPlaying = false;
                ScriptCore.Logger.Log("Tornado sound stopped");
            }
            catch (Exception ex)
            {
                ScriptCore.Logger.Log($"Error stopping tornado sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Play the tornado siren sound (one-shot, not looping)
        /// </summary>
        public void PlaySirenSound()
        {
            if (!sirenEnabled || isSirenSoundPlaying) return;

            try
            {
                if (!File.Exists(sirenSoundPath))
                {
                    ScriptCore.Logger.Log($"Siren sound file not found: {sirenSoundPath}");
                    return;
                }

                // Stop any previous siren sound
                mciSendString($"close siren", null, 0, IntPtr.Zero);

                // Open and play the sound once (no repeat)
                mciSendString($"open \"{sirenSoundPath}\" type waveaudio alias siren", null, 0, IntPtr.Zero);
                mciSendString("play siren", null, 0, IntPtr.Zero);

                isSirenSoundPlaying = true;
                ScriptCore.Logger.Log("Siren sound started (one-shot)");

                // Note: We don't set isSirenSoundPlaying to false here because we want to prevent
                // multiple sirens playing. It will be reset when RemoveAll() is called or on next spawn attempt.
            }
            catch (Exception ex)
            {
                ScriptCore.Logger.Log($"Error playing siren sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop the siren sound if playing
        /// </summary>
        public void StopSirenSound()
        {
            if (!isSirenSoundPlaying) return;

            try
            {
                mciSendString("stop siren", null, 0, IntPtr.Zero);
                mciSendString("close siren", null, 0, IntPtr.Zero);
                isSirenSoundPlaying = false;
                ScriptCore.Logger.Log("Siren sound stopped");
            }
            catch (Exception ex)
            {
                ScriptCore.Logger.Log($"Error stopping siren sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Update settings from script variables
        /// </summary>
        public void UpdateSettings()
        {
            soundEnabled = ScriptThread.GetVar<bool>("soundenabled");
            sirenEnabled = ScriptThread.GetVar<bool>("sirenenabled");
        }

        /// <summary>
        /// Check if siren is still playing (for tracking purposes)
        /// </summary>
        public bool IsSirenStillPlaying()
        {
            if (!isSirenSoundPlaying) return false;

            try
            {
                var status = new System.Text.StringBuilder(128);
                mciSendString("status siren mode", status, 128, IntPtr.Zero);

                // If status is "playing", siren is still playing
                bool stillPlaying = status.ToString().Trim().ToLower() == "playing";

                if (!stillPlaying)
                {
                    isSirenSoundPlaying = false;
                }

                return stillPlaying;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            StopTornadoSound();
            StopSirenSound();
        }
    }
}