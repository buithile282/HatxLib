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

namespace HAtxLib
{

    public class HAtx
    {
        private readonly static HLog Log = HLog.Get<HAtx>("Core");
        private readonly string _serial;
        private readonly ADBClient _client;
        private readonly InitHelper _initer;
        private int _port = -1;
        private string _url = null;
        private bool _debug = false;
        private UIAutomatorService _uiService = null; // Lazy singleton instance

        public int Port
        {
            get
            {
                return _port;
            }
        }
        public string UDID
        {
            get
            {
                return _serial;
            }
        }

        public ADBClient ADB
        {
            get
            {
                return _client;
            }
        }

        public InitHelper Initer
        {
            get
            {
                return _initer;
            }
        }

        #region Global Settings

        public int UINodeMaxWaitTime { get; set; } = AtxConstants.DEFAULT_UI_NODE_MAX_WAIT_TIME;
        /// <summary>
        /// Delay during UI node detection (milliseconds)
        /// </summary>
        public int UINodeClickExistDelay { get; set; } = AtxConstants.DEFAULT_UI_NODE_CLICK_EXIST_DELAY;
        /// <summary>
        /// Delay after UI node click (milliseconds)
        /// </summary>
        public int UINodeClickDelay { get; set; } = AtxConstants.DEFAULT_UI_NODE_CLICK_DELAY;

        #endregion

        /// <summary>
        /// Initializes a new instance of the HAtx class for controlling an Android device
        /// </summary>
        /// <param name="serial">Device serial number (UDID)</param>
        /// <param name="init">Whether to initialize the device (install ATX agent, start UIAutomator). Default is true.</param>
        /// <exception cref="ATXIniterException">Thrown when device initialization fails after maximum retry attempts</exception>
        public HAtx(string serial, bool init = true)
        {
            _serial = serial;
            _client = new ADBClient(_serial);
            if (init)
            {
                _initer = new InitHelper(_client);
                
                // Use RetryHelper to avoid infinite loop
                bool initSuccess = RetryHelper.ExecuteWithRetry(
                    action: () => _initer.Install(),
                    maxAttempts: AtxConstants.DEFAULT_MAX_INIT_RETRY_ATTEMPTS,
                    delayMs: AtxConstants.DEFAULT_RETRY_DELAY_MS,
                    operationName: $"Device Initialization ({_serial})"
                );

                if (!initSuccess)
                {
                    Log.Error($"HAtx<{_serial}> Failed to initialize device after {AtxConstants.DEFAULT_MAX_INIT_RETRY_ATTEMPTS} attempts");
                    throw new ATXIniterException($"Failed to initialize device {_serial}");
                }

                Log.Info($"HAtx<{_serial}> Connect: {Connect()}");
                HRuntime.Run("Run UIAUTOMATOR", () => Log.Info($"HAtx<{_serial}> RunUiautomator: {RunUiautomator()}"));
            }
        }

        /// <summary>
        /// Runs a script on the device
        /// </summary>
        /// <param name="script">Script to execute</param>
        /// <param name="notify">Notification callback when script completes</param>
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
        /// <summary>
        /// Gets the UIAutomator service instance (lazy singleton pattern)
        /// </summary>
        private UIAutomatorService UIService
        {
            get
            {
                if (_uiService == null)
                {
                    _uiService = new UIAutomatorService(this);
                }
                return _uiService;
            }
        }
        #endregion

        #region Set DEBUG
        /// <summary>
        /// Enables or disables debug logging
        /// </summary>
        /// <param name="debug">True to enable debug mode, false to disable</param>
        public void SetDebug(bool debug = true)
        {
            _debug = debug;
            HLog.InDebug = _debug;
        }
        #endregion

        #region Mobile display information page
        /// <summary>
        /// Displays device information on the mobile screen
        /// </summary>
        public void ShowInfo()
        {
            _client.Shell("am", "start", "-W", "-n", $"{AtxConstants.IDENTIFY_ACTIVITY}", "-e", "theme", "black");
        }
        #endregion

        #region DUMP Screen
        /// <summary>
        /// Dumps the UI hierarchy from the screen
        /// </summary>
        /// <returns>XML string representing the UI hierarchy, or null if failed</returns>
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
            var json = JsonRpc("deviceInfo");
            if (json == null)
            {
                return null;
            }
            if (json.Error != null)
            {
                Console.WriteLine($"DeviceInfo: {json.Error.ToString(Formatting.None)}");
                return null;
            }
            JObject data = (JObject)json.Data;
            return JsonConvert.DeserializeObject<UADeviceInfo>(data.ToString());
        }

        public AtxDeviceInfo Info()
        {
            using (HSocket socket = HSocket.Create(_url))
            {
                var result = socket.HttpGet("/info");
                if (result.Code == 200)
                {
                    return JsonConvert.DeserializeObject<AtxDeviceInfo>(result.Content);
                }
            }
            return null;
        }
        #endregion

        #region Is Online
        /// <summary>
        /// Check if online
        /// </summary>
        public bool IsAlive()
        {
            int size = 10;
            while (size-- > 0)
            {
                var device = DeviceInfo();
                if (device == null)
                {
                    Thread.Sleep(500);
                    continue;
                }
                return true;
            }
            return false;
        }
        #endregion

        #region Screen Related

        private readonly static Regex DumpsysDisplayScreenRegex = new Regex(".*DisplayViewport\\{.*?orientation=(?<orientation>.*?),.*?deviceWidth=(?<width>.*?),.*deviceHeight=(?<height>.*?)\\}");

        #region Get Screen Orientation

        /// <summary>
        /// Get Screen Orientation
        /// </summary>
        /// <returns></returns>
        public object[] GetOrientation()
        {
            string result = _client.Shell("dumpsys", "display");
            Match match = DumpsysDisplayScreenRegex.Match(result);
            int o;
            if (match.Success)
            {
                o = int.Parse(match.Groups["orientation"].Value);
            }
            else
            {
                o = DeviceInfo().DisplayRotation;
            }
            return OrientationDict[(Orientation)o];
        }

        #endregion

        #region Set Screen Orientation
        /// <summary>
        /// Set Screen Orientation
        /// </summary>
        public void SetOrientation(Orientation orientation)
        {
            JsonRpc("setOrientation", OrientationDict[orientation][1]);
        }
        #endregion

        #region Lock Screen Orientation
        /// <summary>
        /// Lock Screen Orientation 
        /// True = Auto, False = Locked
        /// </summary>
        public void FreezeRotation(bool freezed = true)
        {
            JsonRpc("freezeRotation", freezed);
        }
        #endregion

        #region Get Resolution
        /// <summary>
        /// Get Screen Resolution
        /// </summary>
        /// <returns></returns>
        public Size GetWindowSize()
        {
            var info = Info();
            return new Size(info.Display.Width, info.Display.Height);
        }
        #endregion

        #region Screen Off/On

        /// <summary>
        /// Screen On
        /// </summary>
        public void ScreenOn()
        {
            JsonRpc("wakeUp");
        }

        /// <summary>
        /// Screen Off
        /// </summary>
        public void ScreenOff()
        {
            JsonRpc("sleep");
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
            var pos = Rel2Abs(x, y);
            var result = JsonRpc("click", new int[] { pos.X, pos.Y });
            if (result == null)
            {
                return false;
            }
            if (result.Error != null)
            {
                throw new ATXNodeException("Click fail", result.Error);
            }
            return result.Data is bool s && s;
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
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(this, pos.X, pos.Y).Up(pos.X, pos.Y);
            Thread.Sleep(wait);
            Click(x, y);
            return false;
        }

        /// <summary>
        /// Long Press Screen
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="time"></param>
        public void LongClick(float x, float y, int time = 500)
        {
            Thread.Sleep(UINodeClickExistDelay);
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(this, pos.X, pos.Y).Wait(time).Up(pos.X, pos.Y);
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
            if (duration < 2)
            {
                duration = 2;
            }
            var fpos = Rel2Abs(fx, fy);
            var lpos = Rel2Abs(lx, ly);
            var result = JsonRpc("swipe", fpos.X, fpos.Y, lpos.X, lpos.Y, duration);
            if (result == null)
            {
                return false;
            }
            return result.Data is bool s && s;
        }

        #endregion

        #region Screen Operations

        public void TouchDown(float x, float y)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(this, pos.X, pos.Y);
        }

        public void TouchMove(float x, float y)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Move(this, pos.X, pos.Y);
        }

        public void TouchUp(float x, float y)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Up(this, pos.X, pos.Y);
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
            if (duration < 2)
            {
                duration = 2;
            }
            duration *= 200;
            var fpos = Rel2Abs(fx, fy);
            var lpos = Rel2Abs(lx, ly);
            var result = JsonRpc("drag", fpos.X, fpos.Y, lpos.X, lpos.Y, duration);
            if (result == null)
            {
                return false;
            }
            return result.Data is bool s && s;
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

        public AppInfo GetAppInfo(string package)
        {
            using (HSocket socket = HSocket.Create(_url))
            {
                var result = socket.HttpGet($"/packages/{package}/info");
                if (result == null)
                {
                    return new AppInfo();
                }
                var info = JsonConvert.DeserializeObject<AppInfo>(result.Content);
                return info;
            }
        }

        public void AppStart(string package, bool monkey = false, bool stop = false, bool wait = false, string activity = null)
        {
            if (stop)
            {
                AppStop(package);
            }
            if (monkey)
            {
                _client.Shell("monkey", "-p", package, "-c", "android.intent.category.LAUNCHER", "1");
                if (wait)
                {
                    AppWait(package);
                }
                return;
            }
            if (string.IsNullOrWhiteSpace(activity))
            {
                var info = GetAppInfo(package);
                if (info.Success)
                {
                    activity = info.Data.MainActivity;
                    if (activity.IndexOf('.') == -1)
                    {
                        activity = "." + activity;
                    }
                }
            }
            Log.Debug($"Start APP: {package}/{activity}");
            _client.Shell("am", "start", "-a", "android.intent.action.MAIN", "-c", "android.intent.category.LAUNCHER", "-n", $"{package}/{activity}");
            if (wait)
            {
                AppWait(package);
            }
        }

        public void AppStop(string package)
        {
            _client.Shell("am", "force-stop", package);
        }

        public void AppClear(string package)
        {
            _client.Shell("pm", "clear", package);
        }

        public void AppUninstall(string package)
        {
            _client.Shell("pm", "uninstall", package);
        }

        /// <summary>
        /// Uninstalls all user-installed apps except specified exclusions
        /// </summary>
        /// <param name="excludes">Package names to exclude from uninstallation</param>
        public void AppUninstallAll(params string[] excludes)
        {
            List<string> list = new List<string>() {
                AtxConstants.UIAUTOMATOR_PACKAGE,
                AtxConstants.UIAUTOMATOR_TEST_PACKAGE
            };
            list.AddRange(excludes);
            var apps = _client.AppList("-3");
            foreach (string app in apps)
            {
                if (list.Contains(app))
                {
                    continue;
                }
                AppUninstall(app);
            }
        }

        /// <summary>
        /// Stops all running apps except specified exclusions
        /// </summary>
        /// <param name="excludes">Package names to exclude from stopping</param>
        public void AppStopAll(params string[] excludes)
        {
            List<string> list = new List<string>() {
                AtxConstants.UIAUTOMATOR_PACKAGE,
                AtxConstants.UIAUTOMATOR_TEST_PACKAGE
            };
            list.AddRange(excludes);
            List<string> apps = _client.AppRunningList();
            foreach (string app in apps)
            {
                if (list.Contains(app))
                {
                    continue;
                }
                AppStop(app);
            }

        }

        public int AppPidOf(string package)
        {
            using (HSocket socket = HSocket.Create(_url))
            {
                var result = socket.HttpGet($"/pidof/{package}");
                if (result == null)
                {
                    return -1;
                }
                if (int.TryParse(result.Content, out int pid))
                {
                    return pid;
                }
            }
            return -1;
        }

        public AppCurrentInfo AppCurrent()
        {
            AppCurrentInfo info = new AppCurrentInfo();
            var result = _client.Shell("dumpsys", "window", "windows");
            Regex focus = new Regex("mCurrentFocus=Window\\{.*?\\s+(?<package>[^\\s]+)/(?<activity>[^\\s]+)\\}");
            Match match = focus.Match(result);
            if (match.Success)
            {
                info.Package = match.Groups["package"].Value;
                info.Activity = match.Groups["activity"].Value;
                return info;
            }
            result = _client.Shell("dumpsys", "activity", "activities");
            Regex record = new Regex("mResumedActivity: ActivityRecord\\{.*?\\s+(?<package>[^\\s]+)/(?<activity>[^\\s]+)\\s.*?\\}");
            match = record.Match(result);
            if (match.Success)
            {
                info.Package = match.Groups["package"].Value;
                result = _client.Shell("dumpsys", "activity", "top");
                Regex activity = new Regex("ACTIVITY (?<package>[^\\s]+)/(?<activity>[^/\\s]+) \\w+ pid=(?<pid>\\d+)");
                var matchs = activity.Matches(result);
                if (matchs.Count > 0)
                {
                    for (int i = 0; i < matchs.Count; i++)
                    {
                        if (matchs[i].Groups["package"].Value == info.Package)
                        {
                            info.Activity = matchs[i].Groups["activity"].Value;
                            info.Pid = int.TryParse(matchs[i].Groups["pid"].Value, out int pid) ? pid : 0;
                            return info;
                        }
                    }
                }
            }
            return null;
        }

        public bool AppWait(string package, int timeout = 20000, string activity = null, bool front = false)
        {
            long deadline = DateTimeOffset.Now.ToUnixTimeMilliseconds() + timeout;
            while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < deadline)
            {
                try
                {
                    if (front)
                    {
                        var info = AppCurrent();
                        if (info == null)
                        {
                            continue;
                        }
                        if (info.Package == package)
                        {
                            if (!string.IsNullOrWhiteSpace(activity))
                            {
                                if (activity == info.Activity)
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        var list = _client.AppRunningList();
                        if (list.Contains(package))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"AppWait iteration error for package '{package}': {ex.Message}");
                }
                finally
                {
                    Thread.Sleep(1000);
                }
            }
            return false;
        }

        #endregion

        #region Input Method

        public void ImeClearText()
        {
            ImeWait();
            ADB.Shell("am", "broadcast", "-a", "ADB_CLEAR_TEXT");
        }

        public void ImeInputText(string text, bool clear = false)
        {
            ImeWait();
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            string type = "ADB_SET_TEXT";
            if (!clear)
            {
                type = "ADB_INPUT_TEXT";
            }
            ADB.Shell("am", "broadcast", "-a", type, "--es", "text", data);
        }

        public void ImeWait(int timeout = 5000)
        {
            long deadline = DateTimeOffset.Now.ToUnixTimeMilliseconds() + timeout;
            while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < deadline)
            {
                bool show = ImeCurrent(out string ime);
                if (!ime.StartsWith("mCurMethodId=com.github.uiautomator/.FastInputIME"))
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
        }

        public void ImeSet(bool fastime)
        {
            string fast_ime = "com.github.uiautomator/.FastInputIME";
            if (fastime)
            {
                ADB.Shell("ime", "enable", fast_ime);
                ADB.Shell("ime", "set", fast_ime);
            }
            else
            {
                ADB.Shell("ime", "disable", fast_ime);
            }
        }

        public bool ImeCurrent(out string ime)
        {
            var result = ADB.Shell("dumpsys", "input_method");
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

        #endregion

        #region Coordinate System Conversion
        /// <summary>
        /// Convert Coordinate System
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        internal Point Rel2Abs(float x, float y)
        {
            Point pos = new Point();
            Size size = GetWindowSize();
            if (x > 1)
            {
                pos.X = (int)x;
            }
            else
            {
                pos.X = (int)(x * size.Width);
            }
            if (y > 1)
            {
                pos.Y = (int)y;
            }
            else
            {
                pos.Y = (int)(y * size.Height);
            }
            return pos;
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
                catch (Exception ex)
                {
                    Log.Error($"JsonRpc failed for method '{method}': {ex.Message}");
                    Log.Debug($"Stack trace: {ex.StackTrace}");
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
                AtxConstants.UIAUTOMATOR_PACKAGE,
                AtxConstants.PERMISSION_SYSTEM_ALERT_WINDOW,
                AtxConstants.PERMISSION_ACCESS_FINE_LOCATION,
                AtxConstants.PERMISSION_READ_PHONE_STATE
            };
            _client.Shell(argv);
        }

        private bool RunUiautomator(int timeout = AtxConstants.DEFAULT_UIAUTOMATOR_START_TIMEOUT)
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
                AtxConstants.TOAST_ACTIVITY,
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
            string result = _client.Shell($"am instrument -w -r -e debug false -e class com.github.uiautomator.stub.Stub {AtxConstants.UIAUTOMATOR_TEST_PACKAGE}/android.support.test.runner.AndroidJUnitRunner");
            if (result.Contains("does not have a signature matching the target"))
            {
                InitHelper initer = new InitHelper(_client);
                initer.SetupAtxApp();
            }
            return false;
        }

        private void ShowFloatWindow(bool show = true)
        {
            _client.Shell("am", "start", "-n", AtxConstants.TOAST_ACTIVITY, "-e", "showFloatWindow", show.ToString().ToLower());
        }
        #endregion

        #region Installation Helper Class

        public class InitHelper
        {
            private readonly static HLog Log = HLog.Get<InitHelper>("Installation Assistant");
            private readonly ADBClient _client;
            private readonly string _abi;
            private readonly string _sdk;

            // Use constants from AtxConstants
            private readonly static string ATX_APP_VERSION = AtxConstants.ATX_APP_VERSION;
            private readonly static string ATX_AGENT_VERSION = AtxConstants.ATX_AGENT_VERSION;

            private readonly static ReaderWriterLockSlim DownLock = new ReaderWriterLockSlim();
            private readonly static string CACHE_PATH = $"{AppDomain.CurrentDomain.BaseDirectory}/{Properties.Resources.CACHE_PATH}";
            private readonly static string ATX_LISTEN_ADDR = AtxConstants.ATX_LISTEN_ADDR;
            private readonly static string GITHUB_BASEURL = AtxConstants.GITHUB_BASEURL;
            private readonly static string GITHUB_DOWN_APK_PATH = AtxConstants.GITHUB_DOWN_APK_PATH;
            private readonly static string GITHUB_DOWN_AGENT_PATH = AtxConstants.GITHUB_DOWN_AGENT_PATH;
            private readonly static string ANDROID_LOCAL_TMP_PATH = AtxConstants.ANDROID_LOCAL_TMP_PATH;
            private readonly static string ATX_AGENT_PATH = AtxConstants.ATX_AGENT_PATH;
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
                    Console.WriteLine($"AtxAgent version: {version}");
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
                catch (Exception ex)
                {
                    Log.Warn($"IsAtxAgentOutdated version parsing failed: {ex.Message}");
                    Log.Debug($"Stack trace: {ex.StackTrace}");
                    // Return true to trigger reinstall if version check fails
                    return true;
                }
            }
            #endregion

            #region atx-app
            public void SetupAtxApp()
            {
                if (IsAtxAppOutdated())
                {
                    _client.Shell("pm", "uninstall", AtxConstants.UIAUTOMATOR_PACKAGE);
                    _client.Shell("pm", "uninstall", AtxConstants.UIAUTOMATOR_TEST_PACKAGE);
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
                var apk_debug = _client.AppInfo(AtxConstants.UIAUTOMATOR_PACKAGE);
                var apk_debug_test = _client.AppInfo(AtxConstants.UIAUTOMATOR_TEST_PACKAGE);
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
                _client.Shell("pm", "uninstall", AtxConstants.UIAUTOMATOR_PACKAGE);
                _client.Shell("pm", "uninstall", AtxConstants.UIAUTOMATOR_TEST_PACKAGE);
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

        #region Screen Orientation
        private readonly static Dictionary<Orientation, object[]> OrientationDict = new Dictionary<Orientation, object[]>() {
            { Orientation.Natural, new object[] { 0, "natural", "n", 0 } },
            { Orientation.Left, new object[] { 1, "left", "l", 90 } },
            { Orientation.Upsidedown, new object[] { 2, "upsidedown", "u", 180 } },
            { Orientation.Right, new object[] { 3, "right", "r", 270 } }
        };

        public enum Orientation
        {
            Natural = 0,
            Left,
            Upsidedown,
            Right
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

        #region Device Information
        public class AtxDeviceInfo
        {
            [JsonProperty("udid")]
            public string udid { get; set; }
            [JsonProperty("version")]
            public string Version { get; set; }
            [JsonProperty("serial")]
            public string Serial { get; set; }
            [JsonProperty("brand")]
            public string Brand { get; set; }
            [JsonProperty("model")]
            public string Model { get; set; }
            [JsonProperty("hwaddr")]
            public string Hwaddr { get; set; }
            [JsonProperty("sdk")]
            public int Sdk { get; set; }
            [JsonProperty("agentVersion")]
            public string AgentVersion { get; set; }
            [JsonProperty("display")]
            public DisplayInfo Display { get; set; }
            [JsonProperty("battery")]
            public BatteryInfo Battery { get; set; }
            [JsonProperty("memory")]
            public MemoryInfo Memory { get; set; }
            [JsonProperty("cpu")]
            public CpuInfo Cpu { get; set; }
            [JsonProperty("arch")]
            public object Arch { get; set; }
            [JsonProperty("owner")]
            public object Owner { get; set; }
            [JsonProperty("presenceChangedAt")]
            public object PresenceChangedAt { get; set; }
            [JsonProperty("usingBeganAt")]
            public object UsingBeganAt { get; set; }
            [JsonProperty("product")]
            public object Product { get; set; }
            [JsonProperty("provider")]
            public object Provider { get; set; }

            public class DisplayInfo
            {
                [JsonProperty("width")]
                public int Width { get; set; }
                [JsonProperty("height")]
                public int Height { get; set; }
            }
            public class BatteryInfo
            {
                [JsonProperty("acPowered")]
                public bool AcPowered { get; set; }
                [JsonProperty("usbPowered")]
                public bool UsbPowered { get; set; }
                [JsonProperty("wirelessPowered")]
                public bool WirelessPowered { get; set; }
                [JsonProperty("present")]
                public bool Present { get; set; }
                [JsonProperty("status")]
                public int Status { get; set; }
                [JsonProperty("health")]
                public int Health { get; set; }
                [JsonProperty("level")]
                public int Level { get; set; }
                [JsonProperty("scale")]
                public int Scale { get; set; }
                [JsonProperty("voltage")]
                public int Voltage { get; set; }
                [JsonProperty("temperature")]
                public int Temperature { get; set; }
                [JsonProperty("technology")]
                public string Technology { get; set; }
            }
            public class MemoryInfo
            {
                [JsonProperty("total")]
                public long Total { get; set; }

                [JsonProperty("around")]
                public string Around { get; set; }
            }
            public class CpuInfo
            {
                [JsonProperty("cores")]
                public int Cores { get; set; }
                [JsonProperty("hardware")]
                public string Hardware { get; set; }
            }
        }
        #endregion

        #region APP Information
        public class AppInfo
        {
            [JsonProperty("data")]
            public DataInfo Data { get; set; }
            [JsonProperty("success")]
            public bool Success { get; set; } = false;
            [JsonProperty("description")]
            public string Description { get; set; }

            public class DataInfo
            {
                [JsonProperty("packageName")]
                public string PackageName { get; set; }
                [JsonProperty("mainActivity")]
                public string MainActivity { get; set; }
                [JsonProperty("label")]
                public string Label { get; set; }
                [JsonProperty("versionName")]
                public string VersionName { get; set; }
                [JsonProperty("versionCode")]
                public long VersionCode { get; set; }
                [JsonProperty("size")]
                public long Size { get; set; }
            }
        }
        #endregion

        #region AppCurrent
        public class AppCurrentInfo
        {
            [JsonProperty("package")]
            public string Package { get; set; }
            [JsonProperty("activity")]
            public string Activity { get; set; }
            [JsonProperty("pid")]
            public int Pid { get; set; } = -1;
        }
        #endregion

        #region Touch

        internal class AtxTouch
        {
            private readonly HAtx _atx;
            internal AtxTouch(HAtx atx)
            {
                _atx = atx;
            }

            public AtxTouch Down(int x, int y)
            {
                Event(0, x, y);
                return this;
            }

            public AtxTouch Up(int x, int y)
            {
                Event(1, x, y);
                return this;
            }

            public AtxTouch Move(int x, int y)
            {
                Event(2, x, y);
                return this;
            }

            public AtxTouch Wait(int wait)
            {
                Thread.Sleep(wait);
                return this;
            }

            private void Event(int @event, int x, int y)
            {
                _ = _atx.JsonRpc("injectInputEvent", @event, x, y, 0) ?? throw new ATXException("AtxTouch.Move fail");
            }

            public static AtxTouch Down(HAtx atx, int x, int y)
            {
                return new AtxTouch(atx).Down(x, y);
            }

            public static AtxTouch Up(HAtx atx, int x, int y)
            {
                return new AtxTouch(atx).Up(x, y);
            }

            public static AtxTouch Move(HAtx atx, int x, int y)
            {
                return new AtxTouch(atx).Move(x, y);
            }
        }

        #endregion
    }
}