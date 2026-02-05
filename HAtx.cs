using HAtxLib.ADB;
using HAtxLib.Catch;
using HAtxLib.Extend;
using HAtxLib.Script;
using HAtxLib.UIAutomator;
using HAtxLib.UIAutomator.Model;
using HAtxLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HAtxLib.Core;
using HAtxLib.Configuration;

namespace HAtxLib
{

    public partial class HAtx
    {
        private readonly static HLog Log = HLog.Get<HAtx>("Core");
        private readonly string _serial;
        private readonly ADBClient _client;
        private readonly InitHelper _initer;
        private int _port = -1;
        private string _url = null;
        private bool _debug = false;
        private ScreenOperations _screenOperations;
        private AppManager _appManager;
        private InputMethodManager _inputMethodManager;
        private DeviceManager _deviceManager;

        public int Port => _port;
        public string UDID => _serial;
        public ADBClient ADB => _client;
        public InitHelper Initer => _initer;
        
        internal string AtxAgentUrl => _url;
        
        public ScreenOperations Screen => _screenOperations ??= new ScreenOperations(this);
        public AppManager Apps => _appManager ??= new AppManager(this);
        public InputMethodManager IME => _inputMethodManager ??= new InputMethodManager(this);
        public DeviceManager Device => _deviceManager ??= new DeviceManager(this);

        #region Global Settings

        public int UINodeMaxWaitTime { get; set; } = Configuration.AtxConstants.MAX_WAIT_TIME;
        // Delay during detection
        public int UINodeClickExistDelay { get; set; } = Configuration.AtxConstants.UI_NODE_CLICK_EXIST_DELAY;
        // Click delay
        public int UINodeClickDelay { get; set; } = Configuration.AtxConstants.UI_NODE_CLICK_DELAY;

        #endregion

        public HAtx(string serial, bool init = true)
        {
            _serial = serial;
            _client = new ADBClient(_serial);
            if (init)
            {
                _initer = new InitHelper(_client);
                TryInitialize(Configuration.AtxConstants.MAX_INIT_RETRIES);
                Log.Info($"HAtx<{_serial}> Connect: {Connect()}");
                HRuntime.Run("Run UIAUTOMATOR", () => Log.Info($"HAtx<{_serial}> RunUiautomator: {RunUiautomator()}"));
            }
        }

        /// <summary>
        /// Attempts to initialize the device with retry logic
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>True if initialization succeeded</returns>
        private bool TryInitialize(int maxRetries = Configuration.AtxConstants.MAX_INIT_RETRIES)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    _initer.Install();
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Initialization attempt {i + 1}/{maxRetries} failed: {ex.Message}");
                    if (i < maxRetries - 1)
                    {
                        Thread.Sleep(Configuration.AtxConstants.INIT_RETRY_DELAY);
                    }
                }
            }
            throw new ATXException($"Failed to initialize device after {maxRetries} attempts");
        }

        public void RunScript(IScript script, Action notify)
        {
            HTry.Run(() => {
                script.RunScript(this);
            }, () => {
                script.TaskFailure("RunScript Error");
            }, () => {
                notify?.Invoke();
            });
        }

        #region Connection
        public string AtxAgentUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_url))
                {
                    if (!Connect())
                    {
                        throw new ATXException("Connect Error");
                    }
                }
                return _url;
            }
        }
        public string AtxAgentWs
        {
            get
            {
                if (_port == -1)
                {
                    if (!Connect())
                    {
                        throw new ATXException("Connect Error");
                    }
                }
                return $"ws://127.0.0.1:{_port}";
            }
        }

        private bool Connect()
        {
            int port = _client.ForwardPort(7912);
            if (port == -1)
            {
                return false;
            }
            _port = port;
            _url = $"http://127.0.0.1:{port}";
            return true;
        }
        #endregion

        #region UI Service
        private UIAutomatorService _uiService;
        private UIAutomatorService UIService => _uiService ??= new UIAutomatorService(this);
        #endregion

        #region Set DEBUG
        public void SetDebug(bool debug = true)
        {
            _debug = debug;
            HLog.InDebug = _debug;
        }
        #endregion

        #region Mobile display information page
        /// <summary>
        /// Mobile display information page
        /// </summary>
        public void ShowInfo()
        {
            _client.Shell("am", "start", "-W", "-n", "com.github.uiautomator/.IdentifyActivity", "-e", "theme", "black");
        }
        #endregion

        #region DUMP Screen
        /// <summary>
        /// DUMP Screen
        /// </summary>
        public string DumpHierarchy()
        {
            return HRuntime.Run("Screen DUMP", () => {
                using (HSocket socket = HSocket.Create(_url))
                {
                    var result = socket.HttpGet("/dump/hierarchy");
                    if (result == null || result.Code != 200)
                    {
                        return null;
                    }
                    JObject json = JObject.Parse(result.Content);
                    return json.Value<string>("result");
                }
            });
        }

        /// <summary>
        /// DUMP Screen (DumpWindowHierarchy)
        /// </summary>
        public string DumpWindowHierarchy(bool compressed = false)
        {
            var result = JsonRpc("dumpWindowHierarchy", compressed, null);
            if (result == null || result.Data == null)
            {
                return null;
            }
            if (result.Data is string xml)
            {
                return xml;
            }
            return null;
        }

        #endregion

        #region Device Information
        /// <summary>
        /// Device Information
        /// </summary>
        public UADeviceInfo DeviceInfo()
        {
            return Device.DeviceInfo();
        }

        public DeviceManager.AtxDeviceInfo Info()
        {
            return Device.Info();
        }
        #endregion

        #region Is Online
        /// <summary>
        /// Check if device is online/alive
        /// </summary>
        public bool IsAlive()
        {
            return Device.IsAlive();
        }
        #endregion

        #region Screen Related

        #region Get Screen Orientation

        /// <summary>
        /// Get Screen Orientation
        /// </summary>
        /// <returns></returns>
        public object[] GetOrientation()
        {
            return Device.GetOrientation();
        }

        #endregion

        #region Set Screen Orientation
        /// <summary>
        /// Set Screen Orientation
        /// </summary>
        public void SetOrientation(DeviceManager.Orientation orientation)
        {
            Device.SetOrientation(orientation);
        }
        #endregion

        #region Lock Screen Orientation
        /// <summary>
        /// Lock Screen Orientation 
        /// True = Auto, False = Locked
        /// </summary>
        public void FreezeRotation(bool freezed = true)
        {
            Device.FreezeRotation(freezed);
        }
        #endregion

        #region Get Resolution
        /// <summary>
        /// Get Screen Resolution
        /// </summary>
        /// <returns></returns>
        public Size GetWindowSize()
        {
            return Device.GetWindowSize();
        }
        #endregion

        #region Screen Off/On

        /// <summary>
        /// Screen On
        /// </summary>
        public void ScreenOn()
        {
            Device.ScreenOn();
        }

        /// <summary>
        /// Screen Off
        /// </summary>
        public void ScreenOff()
        {
            Device.ScreenOff();
        }

        #endregion

        #endregion

        #region Screen Click
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        /// <exception cref="ATXNodeException"></exception>
        public bool Click(float x, float y)
        {
            return Screen.Click(x, y);
        }

        /// <summary>
        /// Screen Double Click
        /// </summary>
        /// <param name="x">Coordinate X</param>
        /// <param name="y">Coordinate Y</param>
        /// <param name="wait">Interval</param>
        /// <returns></returns>
        public bool DoubleClick(float x, float y, int wait = 60)
        {
            return Screen.DoubleClick(x, y, wait);
        }

        /// <summary>
        /// Long Press Screen
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="time"></param>
        public void LongClick(float x, float y, int time = 500)
        {
            Screen.LongClick(x, y, time);
        }
        #endregion

        #region Screen Swipe
        /// <summary>
        /// Swipe Screen
        /// </summary>
        /// <param name="fx">Start X</param>
        /// <param name="fy">Start Y</param>
        /// <param name="lx">End X</param>
        /// <param name="ly">End Y</param>
        /// <param name="duration">Duration</param>
        /// <returns></returns>
        public bool Swipe(float fx, float fy, float lx, float ly, int duration = 55)
        {
            return Screen.Swipe(fx, fy, lx, ly, duration);
        }

        #endregion

        #region Screen Operations

        public void TouchDown(float x, float y)
        {
            Screen.TouchDown(x, y);
        }

        public void TouchMove(float x, float y)
        {
            Screen.TouchMove(x, y);
        }

        public void TouchUp(float x, float y)
        {
            Screen.TouchUp(x, y);
        }
        #endregion

        #region Drag
        /// <summary>
        /// Drag
        /// </summary>
        /// <param name="fx"></param>
        /// <param name="fy"></param>
        /// <param name="lx"></param>
        /// <param name="ly"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public bool Drag(float fx, float fy, float lx, float ly, int duration = 55)
        {
            return Screen.Drag(fx, fy, lx, ly, duration);
        }
        #endregion

        #region Press Button
        /// <summary>
        /// Press Button
        /// </summary>
        public void Press(string key)
        {
            JsonRpc("pressKey", key);
        }

        /// <summary>
        /// Press Button
        /// </summary>
        public void Press(PressKey key)
        {
            JsonRpc("pressKey", PressKeyDict[key]);
        }
        #endregion

        #region Clipboard



        #endregion

        #region App Related

        public AppManager.AppInfo GetAppInfo(string package)
        {
            return Apps.GetAppInfo(package);
        }

        public void AppStart(string package, bool monkey = false, bool stop = false, bool wait = false, string activity = null)
        {
            Apps.AppStart(package, monkey, stop, wait, activity);
        }

        public void AppStop(string package)
        {
            Apps.AppStop(package);
        }

        public void AppClear(string package)
        {
            Apps.AppClear(package);
        }

        public void AppUninstall(string package)
        {
            Apps.AppUninstall(package);
        }

        public void AppUninstallAll(params string[] excludes)
        {
            Apps.AppUninstallAll(excludes);
        }

        public void AppStopAll(params string[] excludes)
        {
            Apps.AppStopAll(excludes);
        }

        public int AppPidOf(string package)
        {
            return Apps.AppPidOf(package);
        }

        public AppManager.AppCurrentInfo AppCurrent()
        {
            return Apps.AppCurrent();
        }

        public bool AppWait(string package, int timeout = Configuration.AtxConstants.DEFAULT_TIMEOUT, string activity = null, bool front = false)
        {
            return Apps.AppWait(package, timeout, activity, front);
        }

        #endregion

        #region Input Method

        public void ImeClearText()
        {
            IME.ImeClearText();
        }

        public void ImeInputText(string text, bool clear = false)
        {
            IME.ImeInputText(text, clear);
        }

        public void ImeWait(int timeout = Configuration.AtxConstants.IME_WAIT_TIMEOUT)
        {
            IME.ImeWait(timeout);
        }

        public void ImeSet(bool fastime)
        {
            IME.ImeSet(fastime);
        }

        public bool ImeCurrent(out string ime)
        {
            return IME.ImeCurrent(out ime);
        }

        #endregion

        #region JSONRPC Base Function
        /// <summary>
        /// JSONRPC Base Function
        /// </summary>
        public JsonRpcResponse JsonRpc(string method, params object[] argv)
        {
            return HRuntime.Run($"JSONRPC<{method}>", () => {
                string url = $"{_url}/jsonrpc/0";
                JArray array = new JArray();
                foreach (var obj in argv)
                {
                    if (obj is By)
                    {
                        array.Add((obj as By).ToJson());
                        continue;
                    }
                    array.Add(obj);
                }
                string id = Guid.NewGuid().ToString().Replace("-", "");
                JObject json = new JObject {
                    { "jsonrpc", "2.0" },
                    { "id", id },
                    { "method", method },
                    { "params", array }
                };
                if (_debug)
                {
                    Log.Debug($"JsonRpc >>> {json.ToString(Formatting.None)}");
                }
                try
                {
                    using (var socket = HSocket.Create(_url))
                    {
                        var result = socket.HttpPost("/jsonrpc/0", json);
                        if (_debug)
                        {
                            string temp = result?.Content.Strip();
                            Log.Debug($"JsonRpc <<< {(temp.Length > 100 ? $"{temp.Substring(0, 100)} ... {temp.Substring(temp.Length - 51, 50)}" : temp)}");
                        }
                        if (result == null || result.Code != 200)
                        {
                            throw new ATXException($"JsonRpc Fail {result?.Code}");
                        }
                        if (string.IsNullOrWhiteSpace(result.Content))
                        {
                            return null;
                        }
                        return JsonConvert.DeserializeObject<JsonRpcResponse>(result.Content);
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }
        #endregion

        #region Element Operations

        /// <summary>
        /// Element Operations Entry
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        public UINode FindNode(By by)
        {
            return new UINode(this, by);
        }

        #endregion

        #region Start UIAutomator

        private void GrantAppPermissions()
        {
            var argv = new string[] {
                "pm",
                "grant",
                "com.github.uiautomator",
                "android.permission.SYSTEM_ALERT_WINDOW",
                "android.permission.ACCESS_FINE_LOCATION",
                "android.permission.READ_PHONE_STATE"
            };
            _client.Shell(argv);
        }

        private bool RunUiautomator(int timeout = 20)
        {
            bool service = UIService.Running();
            if (IsAlive() && service)
            {
                return true;
            }
            if (service)
            {
                Log.Debug($"Uiautomator Service Stop: {UIService.Stop()}");
            }
            Thread.Sleep(1000);
            GrantAppPermissions();
            var argv = new string[] {
                "am",
                "start",
                "-a",
                "android.intent.action.MAIN",
                "-c",
                "android.intent.category.LAUNCHER",
                "-n",
                "com.github.uiautomator/.ToastActivity",
            };
            Log.Debug($"RunUiautomator: {_client.Shell(argv)}");
            Log.Debug($"Uiautomator Service Start: {UIService.Start()}");
            Thread.Sleep(500);
            Log.Debug($"Uiautomator Running: {UIService.Running()}");
            while (timeout-- > 0)
            {
                if (!UIService.Running())
                {
                    continue;
                }
                if (IsAlive())
                {
                    ShowFloatWindow();
                    return true;
                }
                Thread.Sleep(1000);
            }
            UIService.Stop();
            string result = _client.Shell("am instrument -w -r -e debug false -e class com.github.uiautomator.stub.Stub com.github.uiautomator.test/android.support.test.runner.AndroidJUnitRunner");
            if (result.Contains("does not have a signature matching the target"))
            {
                InitHelper initer = new InitHelper(_client);
                initer.SetupAtxApp();
            }
            return false;
        }

        private void ShowFloatWindow(bool show = true)
        {
            _client.Shell("am", "start", "-n", "com.github.uiautomator/.ToastActivity", "-e", "showFloatWindow", show.ToString().ToLower());
        }
        #endregion

        #region Installation Helper Class

        public class InitHelper
        {
            private readonly static HLog Log = HLog.Get<InitHelper>("Installation Assistant");
            private readonly ADBClient _client;
            private readonly string _abi;
            private readonly string _sdk;

            private readonly static string ATX_APP_VERSION = "2.3.3";
            private readonly static string ATX_AGENT_VERSION = "0.10.0";

            private readonly static ReaderWriterLockSlim DownLock = new ReaderWriterLockSlim();
            private readonly static string CACHE_PATH = $"{AppDomain.CurrentDomain.BaseDirectory}/{Properties.Resources.CACHE_PATH}";
            private readonly static string ATX_LISTEN_ADDR = "127.0.0.1:7912";
            private readonly static string GITHUB_BASEURL = "https://github.com/openatx";
            private readonly static string GITHUB_DOWN_APK_PATH = "/android-uiautomator-server/releases/download/";
            private readonly static string GITHUB_DOWN_AGENT_PATH = "/atx-agent/releases/download/";
            private readonly static string ANDROID_LOCAL_TMP_PATH = "/data/local/tmp/";
            private readonly static string ATX_AGENT_PATH = "/data/local/tmp/atx-agent";
            private readonly static string[] ATX_APKS = new string[2] { "app-uiautomator", "app-uiautomator-test" };
            private readonly static Dictionary<string, string> ATX_AGENT_FILE_DICT = new Dictionary<string, string>() {
                { "armeabi-v7a", "atx-agent_{0}_linux_armv7.tar.gz" },
                { "arm64-v8a", "atx-agent_{0}_linux_arm64.tar.gz" },
                { "armeabi", "atx-agent_{0}_linux_armv6.tar.gz" },
                { "x86", "atx-agent_{0}_linux_386.tar.gz" },
                { "x86_64", "atx-agent_{0}_linux_386.tar.gz" },
            };
            private string ATX_AGENT_DOWN_URL
            {
                get
                {
                    if (_abi == null)
                    {
                        throw new ATXIniterException("CPU not exists");
                    }
                    if (!ATX_AGENT_FILE_DICT.ContainsKey(_abi))
                    {
                        throw new ATXIniterException("CPU not support");
                    }
                    string file = ATX_AGENT_FILE_DICT[_abi];
                    return $"{GITHUB_BASEURL}{GITHUB_DOWN_AGENT_PATH}{ATX_AGENT_VERSION}/{string.Format(file, ATX_AGENT_VERSION)}";
                }
            }
            private string ATX_AGENT_CAHCE_FILE
            {
                get
                {
                    if (_abi == null)
                    {
                        throw new ATXIniterException("CPU not exists");
                    }
                    if (!ATX_AGENT_FILE_DICT.ContainsKey(_abi))
                    {
                        throw new ATXIniterException("CPU not support");
                    }
                    string file = string.Format(ATX_AGENT_FILE_DICT[_abi], ATX_AGENT_VERSION);
                    return $"{CACHE_PATH}atx_agent/{ATX_AGENT_VERSION}/{file}";
                }
            }

            public InitHelper(ADBClient client)
            {
                _client = client;
                _abi = _client.GetProp("ro.product.cpu.abi");
                _sdk = _client.GetProp("ro.build.version.sdk");
            }

            #region atx-agent
            public void SetupAtxAgent(bool restart = false)
            {
                if (CheckAtxAgentVersion() && !restart)
                {
                    return;
                }
                _client.KillProcessByName("atx-agent");
                _client.Shell(ATX_AGENT_PATH, "server", "--stop");
                Thread.Sleep(500);
                if (IsAtxAgentOutdated())
                {
                    GithubDown(ATX_AGENT_DOWN_URL, ATX_AGENT_CAHCE_FILE);
                    string file = Path.GetDirectoryName(ATX_AGENT_CAHCE_FILE) + $"\\{_abi}\\atx-agent";
                    if (!File.Exists(file))
                    {
                        HZip.UnzipTgz(ATX_AGENT_CAHCE_FILE, Path.GetDirectoryName(file));
                    }
                    Log.Debug($"PUSH {file}: {_client.Push(file, ATX_AGENT_PATH)}");
                }
                _client.Shell(ATX_AGENT_PATH, "server", "--nouia", "-d", "--addr", ATX_LISTEN_ADDR);
                int size = 10;
                while (!CheckAtxAgentVersion())
                {
                    size--;
                    if (size <= 0)
                    {
                        throw new ATXIniterException("Init atx-agent fail");
                    }
                    Thread.Sleep(500);
                }
                Log.Info($"SetupAtxAgent: True");
            }

            public bool CheckAtxAgentVersion()
            {
                int port = _client.ForwardPort(7912);
                if (port == -1)
                {
                    return false;
                }
                using (HSocket socket = HSocket.Create("127.0.0.1", port))
                {
                    var result = socket.HttpGet("/version");
                    if (result == null || result.Code != 200)
                    {
                        return false;
                    }
                    return true;
                }
            }

            private bool IsAtxAgentOutdated()
            {
                try
                {
                    string version = _client.Shell(ATX_AGENT_PATH, "version");
                    Log.Info($"AtxAgent version: {version}");
                    if (version == "dev")
                    {
                        return false;
                    }
                    var nv = ATX_AGENT_VERSION.Split('.');
                    var ov = version.Split('.');
                    if (nv[1] != ov[1])
                    {
                        return true;
                    }
                    return int.Parse(ov[2]) < int.Parse(nv[2]);
                }
                catch (Exception)
                {
                    return true;
                }
            }
            #endregion

            #region atx-app
            public void SetupAtxApp()
            {
                if (IsAtxAppOutdated())
                {
                    _client.Shell("pm", "uninstall", "com.github.uiautomator");
                    _client.Shell("pm", "uninstall", "com.github.uiautomator.test");
                    foreach (string app in ATX_APKS)
                    {
                        string tmp = $"{ANDROID_LOCAL_TMP_PATH}{app}.apk";
                        _client.Shell("rm", tmp);
                        string url = $"{GITHUB_BASEURL}{GITHUB_DOWN_APK_PATH}{ATX_APP_VERSION}/{app}.apk";
                        string file = $"{CACHE_PATH}apk/{ATX_APP_VERSION}/{app}.apk";
                        GithubDown(url, file);
                        Log.Debug($"PUSH {tmp}: {_client.Push(file, tmp, 420)}");
                        Log.Debug($"INSTALL {tmp}: {_client.Shell("pm", "install", "-r", "-t", tmp)}");
                    }
                }
            }

            public bool IsAtxAppOutdated()
            {
                var apk_debug = _client.AppInfo("com.github.uiautomator");
                var apk_debug_test = _client.AppInfo("com.github.uiautomator.test");
                if (apk_debug == null || apk_debug_test == null)
                {
                    return true;
                }
                if (apk_debug.VersionName != ATX_APP_VERSION)
                {
                    return true;
                }
                if (apk_debug.Signature != apk_debug_test.Signature)
                {
                    return true;
                }
                return false;
            }
            #endregion

            #region minicap
            public void SetupMinicap()
            {
                if (_abi == "x86")
                {
                    Log.Warn("abi:x86 not supported well, skip install minicap");
                    return;
                }
                if (int.Parse(_sdk) > 30)
                {
                    Log.Warn("Android R (sdk:30) has no minicap resource");
                    return;
                }
                string base_url = $"{GITHUB_BASEURL}/stf-binaries/raw/0.3.0/node_modules/@devicefarmer/minicap-prebuilt/prebuilt/";
                string result = _client.Shell("ls", "-a", "/data/local/tmp");
                var list = new List<string>(result.Split(' '));
                if (!list.Contains("minicap.so"))
                {
                    string so_url = $"{base_url}{_abi}/lib/android-{_sdk}/minicap.so";
                    string so_file = $"{CACHE_PATH}minicap/{_abi}/minicap.so";
                    GithubDown(so_url, so_file);
                    Log.Debug($"PUSH {ANDROID_LOCAL_TMP_PATH}minicap.so: {_client.Push(so_file, $"{ANDROID_LOCAL_TMP_PATH}minicap.so")}");
                }
                if (!list.Contains("minicap"))
                {
                    string minicap_url = $"{base_url}{_abi}/bin/minicap";
                    string minicap_file = $"{CACHE_PATH}minicap/{_abi}/minicap";
                    GithubDown(minicap_url, minicap_file);
                    Log.Debug($"PUSH {ANDROID_LOCAL_TMP_PATH}minicap: {_client.Push(minicap_file, $"{ANDROID_LOCAL_TMP_PATH}minicap")}");
                }
            }
            #endregion

            #region minitouch
            public void SetupMinitouch()
            {
                string result = _client.Shell("ls", "-a", "/data/local/tmp");
                var list = new List<string>(result.Split(' '));
                if (!list.Contains("minitouch"))
                {
                    string base_url = $"{GITHUB_BASEURL}/stf-binaries/raw/0.3.0/node_modules/@devicefarmer/minitouch-prebuilt/prebuilt/{_abi}/bin/minitouch";
                    string minitouch_file = $"{CACHE_PATH}minitouch/{_abi}/minitouch";
                    GithubDown(base_url, minitouch_file);
                    Log.Debug($"PUSH {ANDROID_LOCAL_TMP_PATH}minitouch: {_client.Push(minitouch_file, $"{ANDROID_LOCAL_TMP_PATH}minitouch")}");
                }
            }
            #endregion

            #region Install/Uninstall/Reinstall
            public void Install()
            {
                SetupMinitouch();
                SetupMinicap();
                SetupAtxApp();
                SetupAtxAgent();
            }

            public void Reinstall(bool clear = false)
            {
                if (clear)
                {
                    if (Directory.Exists(CACHE_PATH))
                    {
                        try
                        {
                            Directory.Delete(CACHE_PATH, true);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Clear cache path exception: {ex}");
                        }
                    }
                }
                Uninstall();
                Install();
            }

            public void Uninstall()
            {
                _client.KillProcessByName("atx-agent");
                _client.Shell(ATX_AGENT_PATH, "server", "--stop");
                Thread.Sleep(1000);
                _client.Shell("rm", ATX_AGENT_PATH);
                _client.Shell("rm", $"{ANDROID_LOCAL_TMP_PATH}minicap");
                _client.Shell("rm", $"{ANDROID_LOCAL_TMP_PATH}minicap.so");
                _client.Shell("rm", $"{ANDROID_LOCAL_TMP_PATH}minitouch");
                foreach (string app in ATX_APKS)
                {
                    _client.Shell("rm", $"{ANDROID_LOCAL_TMP_PATH}{app}.apk");
                }
                _client.Shell("pm", "uninstall", "com.github.uiautomator");
                _client.Shell("pm", "uninstall", "com.github.uiautomator.test");
            }
            #endregion

            #region Download
            private void GithubDown(string url, string file)
            {
                DownLock.EnterWriteLock();
                try
                {
                    string path = Path.GetDirectoryName(file);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    if (File.Exists(file))
                    {
                        return;
                    }
                    HRuntime.Run("Download Task", () => {
                        using (var client = new WebClient())
                        {
                            client.Headers.Add("user-agent", "Hell");
                            client.DownloadFile(url, file);
                        }
                    });
                    Log.Info($"DOWN {file}: Complete");
                }
                catch (Exception)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                    Log.Error($"DOWN {file}: Failed");
                }
                finally
                {
                    DownLock.ExitWriteLock();
                }
            }
            #endregion
        }

        #endregion

        #region PressKey (Buttons)
        private readonly static Dictionary<PressKey, string> PressKeyDict = new Dictionary<PressKey, string>() {
            { PressKey.Home, "home" },
            { PressKey.Back, "back" },
            { PressKey.Left, "left" },
            { PressKey.Right, "right" },
            { PressKey.Up, "up" },
            { PressKey.Down, "down" },
            { PressKey.Center, "center" },
            { PressKey.Menu, "menu" },
            { PressKey.Search, "search" },
            { PressKey.Enter, "enter" },
            { PressKey.Delete, "delete" },
            { PressKey.Recent, "recent" },
            { PressKey.VolumeUp, "volume_up" },
            { PressKey.VolumeDown, "volume_down" },
            { PressKey.VolumeMute, "volume_mute" },
            { PressKey.Camera, "camera" },
            { PressKey.Power, "power" },
        };

        public enum PressKey
        {
            Home,
            Back,
            Left,
            Right,
            Up,
            Down,
            Center,
            Menu,
            Search,
            Enter,
            Delete,
            Recent,
            VolumeUp,
            VolumeDown,
            VolumeMute,
            Camera,
            Power
        }
        #endregion
    }
}