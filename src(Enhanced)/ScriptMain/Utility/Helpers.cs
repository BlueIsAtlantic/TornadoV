using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using GTA;
using GTA.Math;
using GTA.Native;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain.Utility
{
    public static class Helpers
    {
        public static Ped GetLocalPed()
        {
            return SafeRun(() => Game.Player?.Character, "GetLocalPed");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Vec2(this Vector3 v)
        {
            return SafeRun(() => new Vector2(v.X, v.Y), "Vec2");
        }

        public static void ApplyForceToCenterOfMass(this Entity entity, Vector3 force)
        {
            if (entity == null || !entity.Exists()) return;

            SafeRun(() =>
            {
                Function.Call(Hash.APPLY_FORCE_TO_ENTITY_CENTER_OF_MASS, entity.Handle, 1, force.X, force.Y, force.Z, 0, 0, 1, 1);
            }, "ApplyForceToCenterOfMass");
        }

        public static Vector3 GetRandomPositionFromCoords(Vector3 position, float multiplier)
        {
            return SafeRun(() =>
            {
                int v1 = Function.Call<int>(Hash.GET_RANDOM_INT_IN_RANGE, 0, 4); // 0-3
                float randX = 0, randY = 0;

                switch (v1)
                {
                    case 0:
                        randX = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 50f, 200f) * multiplier;
                        randY = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, -50f, 50f) * multiplier;
                        break;
                    case 1:
                        randX = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, -200f, -50f) * multiplier;
                        randY = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, -50f, 50f) * multiplier;
                        break;
                    case 2:
                        randX = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, -200f, -50f) * multiplier;
                        randY = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 50f, 200f) * multiplier;
                        break;
                    default:
                        randX = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 50f, 200f) * multiplier;
                        randY = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 50f, 200f) * multiplier;
                        break;
                }

                // Clamp extremely large values to prevent engine crashes
                randX = Clamp(randX, -10000f, 10000f);
                randY = Clamp(randY, -10000f, 10000f);

                return new Vector3(position.X + randX, position.Y + randY, position.Z);
            }, "GetRandomPositionFromCoords");
        }

        public static string[] GetLines(this string s)
        {
            if (string.IsNullOrEmpty(s)) return Array.Empty<string>();
            return SafeRun(() => s.Split(new[] { Environment.NewLine }, StringSplitOptions.None), "GetLines");
        }

        public static IList<string> ReadEmbeddedResource(string resource)
        {
            if (string.IsNullOrEmpty(resource)) return new List<string>();
            return SafeRun(() =>
            {
                string[] text = resource.GetLines();
                return new List<string>(text);
            }, "ReadEmbeddedResource");
        }

        public static float Lerp(this float a, float b, float f)
        {
            return SafeRun(() => a * (1.0f - f) + b * f, "Lerp");
        }

        public static Color Lerp(this Color source, Color target, double percent)
        {
            return SafeRun(() =>
            {
                float p = (float)percent; // cast to float
                p = Clamp(p, 0f, 1f);     // if you also add the Clamp helper
                var r = (byte)(source.R + (target.R - source.R) * p);
                var g = (byte)(source.G + (target.G - source.G) * p);
                var b = (byte)(source.B + (target.B - source.B) * p);
                return Color.FromArgb(source.A, r, g, b);
            }, "Color Lerp");
        }


        public static void WriteListToFile(IList<string> list, string filepath)
        {
            if (list == null || string.IsNullOrEmpty(filepath)) return;

            SafeRun(() =>
            {
                if (File.Exists(filepath)) File.Delete(filepath);
                using (StreamWriter stream = new StreamWriter(filepath))
                {
                    foreach (string line in list)
                        stream.WriteLine(line);
                }
            }, "WriteListToFile");
        }

        public static void NotifyWithIcon(string title, string text, string icon = "")
        {
            if (string.IsNullOrEmpty(text)) return;

            SafeRun(() =>
            {
                Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
                Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
            }, "NotifyWithIcon");
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

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

    }
}
