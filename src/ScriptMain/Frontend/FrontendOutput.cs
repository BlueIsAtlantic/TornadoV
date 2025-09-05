using System;
using System.Drawing;

namespace TornadoScript.ScriptMain.Frontend
{
    /// <summary>
    /// Dummy FrontendOutput stub to prevent errors in other scripts.
    /// </summary>
    public class FrontendOutput
    {
        // Keep the same method signatures so other scripts calling it won't break

        public void WriteLine(string format, params object[] args)
        {
            // Do nothing
        }

        public void WriteLine(string text)
        {
            // Do nothing
        }

        public void Show()
        {
            // Do nothing
        }

        public void Hide()
        {
            // Do nothing
        }

        public void ScrollUp()
        {
            // Do nothing
        }

        public void ScrollToTop()
        {
            // Do nothing
        }

        public void ScrollDown()
        {
            // Do nothing
        }

        public void DisableFadeOut()
        {
            // Do nothing
        }

        public void EnableFadeOut()
        {
            // Do nothing
        }

        // Keep Update method signature so Tick-based calls still work
        public void Update(int gameTime)
        {
            // Do nothing
        }
    }
}
