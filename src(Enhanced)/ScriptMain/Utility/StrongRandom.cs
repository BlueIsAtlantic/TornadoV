using System;
using System.Security.Cryptography;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain.Utility
{
    public static class StrongRandom
    {
        [ThreadStatic]
        private static Random _random;

        public static int Next(int inclusiveLowerBound, int inclusiveUpperBound)
        {
            try
            {
                if (inclusiveUpperBound < inclusiveLowerBound) return inclusiveLowerBound;

                if (_random == null)
                {
                    int seed;
                    try
                    {
                        // Try crypto-seeded RNG
                        var cryptoBytes = new byte[4];
                        using (var rng = RandomNumberGenerator.Create())
                        {
                            rng.GetBytes(cryptoBytes);
                        }
                        seed = BitConverter.ToInt32(cryptoBytes, 0);
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.LogError(ex, "StrongRandom: Crypto seed failed, using Environment.TickCount");
                        seed = Environment.TickCount;
                    }

                    _random = new Random(seed);
                }

                return _random.Next(inclusiveLowerBound, inclusiveUpperBound + 1);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "StrongRandom: Random generation failed, returning fallback");
                return inclusiveLowerBound; // fallback safe value
            }
        }
    }
}
