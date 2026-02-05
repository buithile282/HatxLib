using System;

namespace HAtxLib.Configuration
{
    /// <summary>
    /// Contains all constant values used throughout the HAtxLib library
    /// </summary>
    public static class AtxConstants
    {
        #region Timeouts
        /// <summary>
        /// Default timeout for operations (milliseconds)
        /// </summary>
        public const int DEFAULT_TIMEOUT = 20000;

        /// <summary>
        /// Default retry count for operations
        /// </summary>
        public const int DEFAULT_RETRY_COUNT = 10;

        /// <summary>
        /// Default delay between retries (milliseconds)
        /// </summary>
        public const int DEFAULT_RETRY_DELAY = 500;

        /// <summary>
        /// Maximum wait time for UI operations (milliseconds)
        /// </summary>
        public const int MAX_WAIT_TIME = 2000;

        /// <summary>
        /// Default IME wait timeout (milliseconds)
        /// </summary>
        public const int IME_WAIT_TIMEOUT = 5000;

        /// <summary>
        /// Maximum initialization retries
        /// </summary>
        public const int MAX_INIT_RETRIES = 5;

        /// <summary>
        /// Initialization retry delay (milliseconds)
        /// </summary>
        public const int INIT_RETRY_DELAY = 1000;
        #endregion

        #region Packages
        /// <summary>
        /// UIAutomator package name
        /// </summary>
        public const string UIAUTOMATOR_PACKAGE = "com.github.uiautomator";

        /// <summary>
        /// UIAutomator test package name
        /// </summary>
        public const string UIAUTOMATOR_TEST_PACKAGE = "com.github.uiautomator.test";

        /// <summary>
        /// ADB IME package name
        /// </summary>
        public const string ADB_IME_PACKAGE = "com.github.uiautomator/.AdbKeyboard";

        /// <summary>
        /// Fast Input IME package name
        /// </summary>
        public const string FAST_INPUT_IME_PACKAGE = "com.github.uiautomator/.FastInputIME";
        #endregion

        #region Versions
        /// <summary>
        /// ATX App version
        /// </summary>
        public const string ATX_APP_VERSION = "2.3.3";

        /// <summary>
        /// ATX Agent version
        /// </summary>
        public const string ATX_AGENT_VERSION = "0.10.0";
        #endregion

        #region URLs
        /// <summary>
        /// GitHub base URL for openatx
        /// </summary>
        public const string GITHUB_BASEURL = "https://github.com/openatx";
        #endregion

        #region Delays
        /// <summary>
        /// Default delay during UI node click detection (milliseconds)
        /// </summary>
        public const int UI_NODE_CLICK_EXIST_DELAY = 60;

        /// <summary>
        /// Default delay after UI node click (milliseconds)
        /// </summary>
        public const int UI_NODE_CLICK_DELAY = 100;

        /// <summary>
        /// Default long click duration (milliseconds)
        /// </summary>
        public const int DEFAULT_LONG_CLICK_DURATION = 500;

        /// <summary>
        /// Minimum swipe/drag duration (milliseconds)
        /// </summary>
        public const int MIN_SWIPE_DURATION = 2;

        /// <summary>
        /// Default swipe duration (milliseconds)
        /// </summary>
        public const int DEFAULT_SWIPE_DURATION = 55;

        /// <summary>
        /// Drag duration multiplier
        /// </summary>
        public const int DRAG_DURATION_MULTIPLIER = 200;
        #endregion
    }
}
