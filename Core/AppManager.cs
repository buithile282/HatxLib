using HAtxLib.ADB;
using HAtxLib.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace HAtxLib.Core
{
    public class AppManager
    {
        private readonly static HLog Log = HLog.Get<AppManager>("Core");
        private readonly HAtx _atx;

        public AppManager(HAtx atx)
        {
            _atx = atx;
        }

        public AppInfo GetAppInfo(string package)
        {
            using (HSocket socket = HSocket.Create(_atx.AtxAgentUrl))
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
                _atx.ADB.Shell("monkey", "-p", package, "-c", "android.intent.category.LAUNCHER", "1");
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
            _atx.ADB.Shell("am", "start", "-a", "android.intent.action.MAIN", "-c", "android.intent.category.LAUNCHER", "-n", $"{package}/{activity}");
            if (wait)
            {
                AppWait(package);
            }
        }

        public void AppStop(string package)
        {
            _atx.ADB.Shell("am", "force-stop", package);
        }

        public void AppClear(string package)
        {
            _atx.ADB.Shell("pm", "clear", package);
        }

        public void AppUninstall(string package)
        {
            _atx.ADB.Shell("pm", "uninstall", package);
        }

        public void AppUninstallAll(params string[] excludes)
        {
            List<string> list = new List<string>() {
                "com.github.uiautomator",
                "com.github.uiautomator.test"
            };
            list.AddRange(excludes);
            var apps = _atx.ADB.AppList("-3");
            foreach (string app in apps)
            {
                if (list.Contains(app))
                {
                    continue;
                }
                AppUninstall(app);
            }
        }

        public void AppStopAll(params string[] excludes)
        {
            List<string> list = new List<string>() {
                "com.github.uiautomator",
                "com.github.uiautomator.test"
            };
            list.AddRange(excludes);
            List<string> apps = _atx.ADB.AppRunningList();
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
            using (HSocket socket = HSocket.Create(_atx.AtxAgentUrl))
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
            var result = _atx.ADB.Shell("dumpsys", "window", "windows");
            Regex focus = new Regex("mCurrentFocus=Window\\{.*?\\s+(?<package>[^\\s]+)/(?<activity>[^\\s]+)\\}");
            Match match = focus.Match(result);
            if (match.Success)
            {
                info.Package = match.Groups["package"].Value;
                info.Activity = match.Groups["activity"].Value;
                return info;
            }
            result = _atx.ADB.Shell("dumpsys", "activity", "activities");
            Regex record = new Regex("mResumedActivity: ActivityRecord\\{.*?\\s+(?<package>[^\\s]+)/(?<activity>[^\\s]+)\\s.*?\\}");
            match = record.Match(result);
            if (match.Success)
            {
                info.Package = match.Groups["package"].Value;
                result = _atx.ADB.Shell("dumpsys", "activity", "top");
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

        public bool AppWait(string package, int timeout = Configuration.AtxConstants.DEFAULT_TIMEOUT, string activity = null, bool front = false)
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
                        var list = _atx.ADB.AppRunningList();
                        if (list.Contains(package))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"AppWait check failed: {ex.Message}");
                }
                finally
                {
                    Thread.Sleep(1000);
                }
            }
            return false;
        }

        #region Data Classes

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
    }
}
