using System;

namespace HAtxLib
{
    /// <summary>
    /// Constants used throughout the HAtxLib library
    /// </summary>
    public static class AtxConstants
    {
        #region Version Information
        /// <summary>
        /// Version of the ATX app (APK)
        /// </summary>
        public const string ATX_APP_VERSION = "2.3.3";

        /// <summary>
        /// Version of the ATX agent
        /// </summary>
        public const string ATX_AGENT_VERSION = "0.10.0";
        #endregion

        #region Network Configuration
        /// <summary>
        /// Default ATX agent listen address
        /// </summary>
        public const string ATX_LISTEN_ADDR = "127.0.0.1:7912";

        /// <summary>
        /// Default ATX agent port
        /// </summary>
        public const int ATX_AGENT_PORT = 7912;

        /// <summary>
        /// Base URL for GitHub downloads
        /// </summary>
        public const string GITHUB_BASEURL = "https://github.com/openatx";
        #endregion

        #region Download Paths
        /// <summary>
        /// GitHub path for downloading APK releases
        /// </summary>
        public const string GITHUB_DOWN_APK_PATH = "/android-uiautomator-server/releases/download/";

        /// <summary>
        /// GitHub path for downloading ATX agent releases
        /// </summary>
        public const string GITHUB_DOWN_AGENT_PATH = "/atx-agent/releases/download/";
        #endregion

        #region Android Paths
        /// <summary>
        /// Android temporary directory path
        /// </summary>
        public const string ANDROID_LOCAL_TMP_PATH = "/data/local/tmp/";

        /// <summary>
        /// ATX agent installation path on device
        /// </summary>
        public const string ATX_AGENT_PATH = "/data/local/tmp/atx-agent";
        #endregion

        #region Package Names
        /// <summary>
        /// Main UIAutomator package name
        /// </summary>
        public const string UIAUTOMATOR_PACKAGE = "com.github.uiautomator";

        /// <summary>
        /// UIAutomator test package name
        /// </summary>
        public const string UIAUTOMATOR_TEST_PACKAGE = "com.github.uiautomator.test";

        /// <summary>
        /// Fast input IME service
        /// </summary>
        public const string FAST_INPUT_IME = "com.github.uiautomator/.FastInputIME";

        /// <summary>
        /// UIAutomator toast activity
        /// </summary>
        public const string TOAST_ACTIVITY = "com.github.uiautomator/.ToastActivity";

        /// <summary>
        /// UIAutomator identify activity
        /// </summary>
        public const string IDENTIFY_ACTIVITY = "com.github.uiautomator/.IdentifyActivity";
        #endregion

        #region Default Timeouts and Delays
        /// <summary>
        /// Maximum wait time for UI node operations (milliseconds)
        /// </summary>
        public const int DEFAULT_UI_NODE_MAX_WAIT_TIME = 2000;

        /// <summary>
        /// Delay during UI node detection (milliseconds)
        /// </summary>
        public const int DEFAULT_UI_NODE_CLICK_EXIST_DELAY = 60;

        /// <summary>
        /// Delay after UI node click (milliseconds)
        /// </summary>
        public const int DEFAULT_UI_NODE_CLICK_DELAY = 100;

        /// <summary>
        /// Default timeout for UIAutomator start (seconds)
        /// </summary>
        public const int DEFAULT_UIAUTOMATOR_START_TIMEOUT = 20;

        /// <summary>
        /// Default maximum retry attempts for initialization
        /// </summary>
        public const int DEFAULT_MAX_INIT_RETRY_ATTEMPTS = 5;

        /// <summary>
        /// Default retry delay in milliseconds
        /// </summary>
        public const int DEFAULT_RETRY_DELAY_MS = 1000;
        #endregion

        #region Service Names
        /// <summary>
        /// UIAutomator service name
        /// </summary>
        public const string UIAUTOMATOR_SERVICE_NAME = "uiautomator";
        #endregion

        #region Permissions
        /// <summary>
        /// System alert window permission
        /// </summary>
        public const string PERMISSION_SYSTEM_ALERT_WINDOW = "android.permission.SYSTEM_ALERT_WINDOW";

        /// <summary>
        /// Fine location permission
        /// </summary>
        public const string PERMISSION_ACCESS_FINE_LOCATION = "android.permission.ACCESS_FINE_LOCATION";

        /// <summary>
        /// Read phone state permission
        /// </summary>
        public const string PERMISSION_READ_PHONE_STATE = "android.permission.READ_PHONE_STATE";
        #endregion
    }
}
