using System;
using GTA;

namespace TornadoScript.ScriptMain.Frontend
{
    public class FrontendInput
    {
        private bool _active;
        private string _str = "";

        public void AddLine(string text) { _str = text; _active = true; }
        public void AddChar(char c) { _str += c; _active = true; }
        public string GetText() => _str;
        public void RemoveLastChar() { if (_str.Length > 0) _str = _str.Substring(0, _str.Length - 1); }
        public void Show() => _active = true;
        public void Hide() => _active = false;
        public void Clear() { _str = ""; _active = false; }

        public void Update(int gameTime)
        {
            if (!_active) return;
            // Do nothing — no text or rectangle drawn
        }
    }
}
