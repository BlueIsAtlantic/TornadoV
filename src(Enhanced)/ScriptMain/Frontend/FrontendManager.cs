using GTA;
using System;
using System.Windows.Forms;
using System.Windows.Input;
using TornadoScript.ScriptCore.Game;
using TornadoScript.ScriptMain.CrashHandling;
using TornadoScript.ScriptMain.Utility;
using Control = System.Windows.Forms.Control;

namespace TornadoScript.ScriptMain.Frontend
{
    public class FrontendManager : ScriptExtension
    {
        private readonly FrontendInput _input = new FrontendInput();
        private readonly FrontendOutput _output = new FrontendOutput();

        private bool _showingConsole;
        private bool _capsLock;

        public FrontendManager()
        {
            SafeRun(() => RegisterEvent("textadded"), "FrontendManager.Constructor");
        }

        internal override void OnThreadAttached()
        {
            SafeRun(() => Events["keydown"] += OnKeyDown, "FrontendManager.OnThreadAttached");
            base.OnThreadAttached();
        }

        private void OnKeyDown(object sender, ScriptEventArgs e)
        {
            SafeRun(() =>
            {
                if (e.Data is not KeyEventArgs keyArgs) return;

                var enableConsole = ScriptThread.GetVar<bool>("enableconsole");
                if (enableConsole == null || !enableConsole.Value) return;

                if (keyArgs.KeyCode == Keys.CapsLock)
                    _capsLock = !_capsLock;

                if (!_showingConsole)
                {
                    var toggleKey = ScriptThread.GetVar<Keys>("toggleconsole");
                    if (toggleKey != null && keyArgs.KeyCode == toggleKey.Value)
                        ShowConsole();
                }
                else
                {
                    GetConsoleInput(keyArgs);
                }
            }, "FrontendManager.OnKeyDown");
        }

        public void ShowConsole()
        {
            SafeRun(() =>
            {
                if (_showingConsole) return;
                _input?.Show();
                _output?.Show();
                _output?.DisableFadeOut();
                _showingConsole = true;
            }, "FrontendManager.ShowConsole");
        }

        public void HideConsole()
        {
            SafeRun(() =>
            {
                if (!_showingConsole) return;
                _input?.Clear();
                _input?.Hide();
                _output?.Hide();
                _output?.EnableFadeOut();
                _showingConsole = false;
            }, "FrontendManager.HideConsole");
        }

        public void WriteLine(string format, params object[] args)
        {
            SafeRun(() =>
            {
                if (_output == null) return;
                if (args == null || args.Length == 0)
                    _output.WriteLine(format);
                else
                    _output.WriteLine(format, args);
            }, "FrontendManager.WriteLine");
        }

        private void GetConsoleInput(KeyEventArgs e)
        {
            SafeRun(() =>
            {
                if (_input == null || _output == null) return;

                var key = KeyInterop.KeyFromVirtualKey((int)e.KeyCode);
                var keyChar = Win32Native.GetCharFromKey(key, (e.Modifiers & Keys.Shift) != 0);
                var capsLock = Control.IsKeyLocked(Keys.CapsLock);

                if (char.IsLetter(keyChar))
                    keyChar = (capsLock || e.Shift) ? char.ToUpper(keyChar) : char.ToLower(keyChar);
                else
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Back:
                            if (_input.GetText()?.Length < 1) HideConsole();
                            _input.RemoveLastChar();
                            return;

                        case Keys.Up: _output.ScrollUp(); return;
                        case Keys.Down: _output.ScrollDown(); return;
                        case Keys.Space: _input.AddChar(' '); return;
                        case Keys.Enter:
                            var text = _input.GetText() ?? "";
                            NotifyEvent("textadded", new ScriptEventArgs(text));
                            _output.WriteLine(text);
                            _input.Clear();
                            _output.ScrollToTop();
                            return;

                        case Keys.Escape: HideConsole(); return;
                    }
                }

                if (keyChar != ' ') _input.AddChar(keyChar);
            }, "FrontendManager.GetConsoleInput");
        }

        public override void OnUpdate(int gameTime)
        {
            SafeRun(() =>
            {
                _input?.Update(gameTime);
                _output?.Update(gameTime);

                if (_showingConsole)
                {
                    if (Game.IsControlJustPressed((GTA.Control)241) || Game.IsControlJustPressed((GTA.Control)188))
                        _output?.ScrollUp();
                    else if (Game.IsControlJustPressed((GTA.Control)242) || Game.IsControlJustPressed((GTA.Control)187))
                        _output?.ScrollDown();
                }

                base.OnUpdate(gameTime);
            }, "FrontendManager.OnUpdate");
        }

        private static void SafeRun(Action action, string context)
        {
            try { action(); }
            catch (Exception ex) { CrashLogger.LogError(ex, context); }
        }
    }
}
