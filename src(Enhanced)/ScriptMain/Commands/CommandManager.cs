using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.Frontend;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptMain.Commands
{
    public class CommandManager : ScriptExtension
    {
        private readonly Dictionary<string, Func<string[], string>> _commands =
            new Dictionary<string, Func<string[], string>>();

        private FrontendManager _frontendMgr;

        public CommandManager()
        {
            SafeRun(() =>
            {
                AddCommand("spawn", Commands.SpawnVortex);
                AddCommand("summon", Commands.SummonVortex);
                AddCommand("set", SetVar);
                AddCommand("reset", ResetVar);
                AddCommand("ls", ListVars);
                AddCommand("list", ListVars);
                AddCommand("help", ShowHelp);
                AddCommand("?", ShowHelp);
            }, "CommandManager Constructor");
        }

        internal override void OnThreadAttached()
        {
            SafeRun(() =>
            {
                _frontendMgr = ScriptThread.GetOrCreate<FrontendManager>();
                _frontendMgr.Events["textadded"] += OnInputEvent;
                base.OnThreadAttached();
            }, "CommandManager.OnThreadAttached");
        }

        public void OnInputEvent(object sender, ScriptEventArgs e)
        {
            SafeRun(() =>
            {
                if (_frontendMgr == null) return; // <--- safety check

                var cmd = (string)e.Data;
                if (string.IsNullOrEmpty(cmd)) return;

                var stringArray = cmd.Split(' ');
                var command = stringArray[0].ToLower();

                if (!_commands.TryGetValue(command, out var func) || func == null) return; // <--- check func

                var args = stringArray.Skip(1).ToArray();
                var text = func.Invoke(args);
                if (!string.IsNullOrEmpty(text))
                    _frontendMgr.WriteLine(text);
            }, "CommandManager.OnInputEvent");
        }

        private static string SetVar(params string[] args)
        {
            return SafeFunc(() =>
            {
                if (args.Length < 2) return "SetVar: Invalid format.";

                var varName = args[0];

                var foundVar = ScriptThread.GetVar<int>(varName) ??
                               ScriptThread.GetVar<float>(varName) as dynamic ??
                               ScriptThread.GetVar<bool>(varName) as dynamic;

                if (foundVar == null) return $"Variable '{varName}' not found.";

                if (int.TryParse(args[1], out var i) && foundVar is ScriptVar<int> intVar)
                    return !ScriptThread.SetVar(varName, i) ? "Failed to set the (integer) variable. Is it readonly?" : null;

                if (float.TryParse(args[1], out var f) && foundVar is ScriptVar<float> floatVar)
                    return !ScriptThread.SetVar(varName, f) ? "Failed to set the (float) variable. Is it readonly?" : null;

                if (bool.TryParse(args[1], out var b) && foundVar is ScriptVar<bool> boolVar)
                    return !ScriptThread.SetVar(varName, b) ? "Failed to set the (bool) variable. Is it readonly?" : null;

                return $"Variable '{varName}' not found.";
            }, "CommandManager.SetVar");
        }


        private static string ResetVar(params string[] args)
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

                return $"Variable '{args[0]}' not found.";
            }, "CommandManager.ResetVar");
        }

        private static string ListVars(params string[] args)
        {
            return SafeFunc(() =>
            {
                var foundCount = 0;

                var frontend = ScriptThread.Get<FrontendManager>();

                foreach (var var in ScriptThread.Vars)
                {
                    frontend.WriteLine(var.Key + (var.Value.ReadOnly ? " (read-only)" : ""));
                    foundCount++;
                }

                return $"Found {foundCount} vars.";
            }, "CommandManager.ListVars");
        }

        private static string ShowHelp(params string[] args)
        {
            return SafeFunc(() =>
            {
                var frontend = ScriptThread.Get<FrontendManager>();
                frontend.WriteLine("~r~set~w~: Set a variable\t\t~r~reset~w~: Reset a variable\t\t~r~ls~w~: List all vars");
                return "Commands:";
            }, "CommandManager.ShowHelp");
        }

        public void AddCommand(string name, Func<string[], string> command)
        {
            SafeRun(() => _commands.Add(name, command), $"CommandManager.AddCommand:{name}");
        }

        // Helpers for wrapping code in try-catch
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
