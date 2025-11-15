using GTA;
using GTA.Native;
using System;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.Frontend;
using TornadoScript.ScriptMain.Script;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain.Commands
{
    public static class Commands
    {
        public static string SetVar(params string[] args)
        {
            return SafeFunc(() =>
            {
                if (args.Length < 2) return "SetVar: Invalid format.";
                var varName = args[0];

                var foundInt = ScriptThread.GetVar<int>(varName);
                if (foundInt != null && int.TryParse(args[1], out var i))
                    return !ScriptThread.SetVar(varName, i) ? "Failed to set the (integer) variable. Is it readonly?" : null;

                var foundFloat = ScriptThread.GetVar<float>(varName);
                if (foundFloat != null && float.TryParse(args[1], out var f))
                    return !ScriptThread.SetVar(varName, f) ? "Failed to set the (float) variable. Is it readonly?" : null;

                var foundBool = ScriptThread.GetVar<bool>(varName);
                if (foundBool != null && bool.TryParse(args[1], out var b))
                    return !ScriptThread.SetVar(varName, b) ? "Failed to set the (bool) variable. Is it readonly?" : null;

                return $"Variable '{varName}' not found.";
            }, "Commands.SetVar");
        }

        public static string ResetVar(params string[] args)
        {
            return SafeFunc(() =>
            {
                if (args.Length < 1) return "ResetVar: Invalid format.";
                var varName = args[0];

                var foundInt = ScriptThread.GetVar<int>(varName);
                if (foundInt != null) { foundInt.Value = foundInt.Default; return null; }

                var foundFloat = ScriptThread.GetVar<float>(varName);
                if (foundFloat != null) { foundFloat.Value = foundFloat.Default; return null; }

                var foundBool = ScriptThread.GetVar<bool>(varName);
                if (foundBool != null) { foundBool.Value = foundBool.Default; return null; }

                return $"Variable '{varName}' not found.";
            }, "Commands.ResetVar");
        }

        public static string ListVars(params string[] args)
        {
            return SafeFunc(() =>
            {
                var frontend = ScriptThread.Get<FrontendManager>();
                if (frontend == null) return "FrontendManager not available.";

                var foundCount = 0;
                foreach (var kvp in ScriptThread.Vars)
                {
                    if (kvp.Value != null)
                    {
                        frontend.WriteLine(kvp.Key + (kvp.Value.ReadOnly ? " (read-only)" : ""));
                        foundCount++;
                    }
                }

                return $"Found {foundCount} vars.";
            }, "Commands.ListVars");
        }

        public static string SummonVortex(params string[] args)
        {
            return SafeFunc(() =>
            {
                var vtxmgr = ScriptThread.Get<TornadoFactory>();
                if (vtxmgr == null || vtxmgr.ActiveVortexCount == 0) return "No active vortex to summon.";

                var player = Game.Player?.Character;
                if (player == null || !player.Exists()) return "Player not available.";

                vtxmgr.ActiveVortexList[0].Position = player.Position;
                return "Vortex summoned";
            }, "Commands.SummonVortex");
        }

        public static string SpawnVortex(params string[] args)
        {
            return SafeFunc(() =>
            {
                var vtxmgr = ScriptThread.Get<TornadoFactory>();
                var player = Game.Player?.Character;
                if (vtxmgr == null || player == null || !player.Exists()) return "Cannot spawn vortex.";

                Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE, 0f, 0f, 0f, 1000000.0f);
                Function.Call(Hash.SET_WIND, 70.0f);

                var position = player.Position + player.ForwardVector * 180f;
                vtxmgr.CreateVortex(position);

                return $"Vortex spawned ({position})";
            }, "Commands.SpawnVortex");
        }

        public static string ShowHelp(params string[] args)
        {
            return SafeFunc(() =>
            {
                var frontend = ScriptThread.Get<FrontendManager>();
                if (frontend != null)
                {
                    frontend.WriteLine(
                        "~r~set~w~: Set a variable\t\t" +
                        "~r~reset~w~: Reset a variable\t\t" +
                        "~r~ls~w~: List all vars\t\t" +
                        "~r~spawn~w~: Spawn a tornado vortex\t\t" +
                        "~r~summon~w~: Summon the vortex to your current position"
                    );
                }
                return "Commands:";
            }, "Commands.ShowHelp");
        }

        private static void SafeRun(Action action, string context)
        {
            try { action(); }
            catch (Exception ex) { CrashLogger.LogError(ex, context); }
        }

        private static T SafeFunc<T>(Func<T> func, string context)
        {
            try { return func(); }
            catch (Exception ex) { CrashLogger.LogError(ex, context); return default; }
        }
    }
}
