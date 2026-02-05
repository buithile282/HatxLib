using HAtxLib.ADB;
using HAtxLib.UIAutomator.Model;
using HAtxLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace HAtxLib.Core
{
    public class DeviceManager
    {
        private readonly static HLog Log = HLog.Get<DeviceManager>("Core");
        private readonly HAtx _atx;

        private readonly static Regex DumpsysDisplayScreenRegex = new Regex(".*DisplayViewport\\{.*?orientation=(?<orientation>.*?),.*?deviceWidth=(?<width>.*?),.*deviceHeight=(?<height>.*?)\\}");

        private readonly static Dictionary<Orientation, object[]> OrientationDict = new Dictionary<Orientation, object[]>() {
            { Orientation.Natural, new object[] { 0, "natural", "n", 0 } },
            { Orientation.Left, new object[] { 1, "left", "l", 90 } },
            { Orientation.Upsidedown, new object[] { 2, "upsidedown", "u", 180 } },
            { Orientation.Right, new object[] { 3, "right", "r", 270 } }
        };

        public DeviceManager(HAtx atx)
        {
            _atx = atx;
        }

        /// <summary>
        /// Device Information
        /// </summary>
        public UADeviceInfo DeviceInfo()
        {
            var json = _atx.JsonRpc("deviceInfo");
            if (json == null)
            {
                return null;
            }
            if (json.Error != null)
            {
                Log.Error($"DeviceInfo error: {json.Error.ToString(Formatting.None)}");
                return null;
            }
            JObject data = (JObject)json.Data;
            return JsonConvert.DeserializeObject<UADeviceInfo>(data.ToString());
        }

        public AtxDeviceInfo Info()
        {
            using (HSocket socket = HSocket.Create(_atx.AtxAgentUrl))
            {
                var result = socket.HttpGet("/info");
                if (result.Code == 200)
                {
                    return JsonConvert.DeserializeObject<AtxDeviceInfo>(result.Content);
                }
            }
            return null;
        }

        /// <summary>
        /// Check if device is online/alive
        /// </summary>
        public bool IsAlive()
        {
            return RetryHelper.ExecuteWithRetry(
                operation: () => DeviceInfo(),
                successCondition: device => device != null,
                maxRetries: Configuration.AtxConstants.DEFAULT_RETRY_COUNT,
                delayMs: Configuration.AtxConstants.DEFAULT_RETRY_DELAY
            ) != null;
        }

        /// <summary>
        /// Get Screen Orientation
        /// </summary>
        /// <returns></returns>
        public object[] GetOrientation()
        {
            string result = _atx.ADB.Shell("dumpsys", "display");
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

        /// <summary>
        /// Set Screen Orientation
        /// </summary>
        public void SetOrientation(Orientation orientation)
        {
            _atx.JsonRpc("setOrientation", OrientationDict[orientation][1]);
        }

        /// <summary>
        /// Lock Screen Orientation 
        /// True = Auto, False = Locked
        /// </summary>
        public void FreezeRotation(bool freezed = true)
        {
            _atx.JsonRpc("freezeRotation", freezed);
        }

        /// <summary>
        /// Get Screen Resolution
        /// </summary>
        /// <returns></returns>
        public Size GetWindowSize()
        {
            var info = Info();
            return new Size(info.Display.Width, info.Display.Height);
        }

        /// <summary>
        /// Screen On
        /// </summary>
        public void ScreenOn()
        {
            _atx.JsonRpc("wakeUp");
        }

        /// <summary>
        /// Screen Off
        /// </summary>
        public void ScreenOff()
        {
            _atx.JsonRpc("sleep");
        }

        public void ShowInfo()
        {
            _atx.ADB.Shell("am", "start", "-W", "-n", "com.github.uiautomator/.IdentifyActivity", "-e", "theme", "black");
        }

        /// <summary>
        /// DUMP Screen
        /// </summary>
        public string DumpHierarchy()
        {
            return HRuntime.Run("Screen DUMP", () => {
                using (HSocket socket = HSocket.Create(_atx.AtxAgentUrl))
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
            var result = _atx.JsonRpc("dumpWindowHierarchy", compressed, null);
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

        public enum Orientation
        {
            Natural = 0,
            Left,
            Upsidedown,
            Right
        }

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
    }
}
