using HAtxLib.Utils;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HAtxLib.Core
{
    public class InputMethodManager
    {
        private readonly static HLog Log = HLog.Get<InputMethodManager>("Core");
        private readonly HAtx _hatx;

        public InputMethodManager(HAtx hatx)
        {
            _hatx = hatx;
        }

        /// <summary>
        /// Clear text in the current input field
        /// </summary>
        public void ImeClearText()
        {
            ImeWait();
            _hatx.ADB.Shell("am", "broadcast", "-a", "ADB_CLEAR_TEXT");
        }

        /// <summary>
        /// Input text using IME
        /// </summary>
        /// <param name="text">Text to input</param>
        /// <param name="clear">If true, clears existing text before input</param>
        public void ImeInputText(string text, bool clear = false)
        {
            ImeWait();
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            string type = "ADB_SET_TEXT";
            if (!clear)
            {
                type = "ADB_INPUT_TEXT";
            }
            _hatx.ADB.Shell("am", "broadcast", "-a", type, "--es", "text", data);
        }

        /// <summary>
        /// Wait for IME to be ready
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        public void ImeWait(int timeout = Configuration.AtxConstants.IME_WAIT_TIMEOUT)
        {
            long deadline = DateTimeOffset.Now.ToUnixTimeMilliseconds() + timeout;
            while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < deadline)
            {
                bool show = ImeCurrent(out string ime);
                if (!ime.StartsWith($"mCurMethodId={Configuration.AtxConstants.FAST_INPUT_IME_PACKAGE}"))
                {
                    ImeSet(true);
                    Thread.Sleep(500);
                    continue;
                }
                if (show)
                {
                    return;
                }
                Thread.Sleep(200);
            }
            Log.Warn($"IME was not ready after {timeout}ms timeout");
        }

        /// <summary>
        /// Enable or disable fast input IME
        /// </summary>
        /// <param name="fastime">True to enable, false to disable</param>
        public void ImeSet(bool fastime)
        {
            string fast_ime = Configuration.AtxConstants.FAST_INPUT_IME_PACKAGE;
            if (fastime)
            {
                _hatx.ADB.Shell("ime", "enable", fast_ime);
                _hatx.ADB.Shell("ime", "set", fast_ime);
            }
            else
            {
                _hatx.ADB.Shell("ime", "disable", fast_ime);
            }
        }

        /// <summary>
        /// Get current IME information
        /// </summary>
        /// <param name="ime">Output parameter for current IME method ID</param>
        /// <returns>True if input is shown, false otherwise</returns>
        public bool ImeCurrent(out string ime)
        {
            var result = _hatx.ADB.Shell("dumpsys", "input_method");
            Regex regex = new Regex("mCurMethodId=([-_./\\w]+)");
            Match match = regex.Match(result);
            if (match.Success)
            {
                ime = match.Groups[0].Value;
            }
            else
            {
                ime = "";
            }
            return result.Contains("mInputShown=true");
        }
    }
}
