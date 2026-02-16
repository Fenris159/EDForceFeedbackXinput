using System.Collections.Generic;
using Newtonsoft.Json;

namespace EDForceFeedbackSettingsEditor
{
    public class SettingsModel
    {
        [JsonProperty("KnownWorkingDevices")]
        public Dictionary<string, string> KnownWorkingDevices { get; set; }

        [JsonProperty("ForceFileRumble")]
        public Dictionary<string, ForceFileRumbleEntry> ForceFileRumble { get; set; }

        [JsonProperty("Devices")]
        public List<DeviceModel> Devices { get; set; }
    }

    public class ForceFileRumbleEntry
    {
        [JsonProperty("Left")]
        public double Left { get; set; }

        [JsonProperty("Right")]
        public double Right { get; set; }
    }

    public class DeviceModel
    {
        [JsonProperty("XInput")]
        public bool? XInput { get; set; }

        [JsonProperty("UserIndex")]
        public int? UserIndex { get; set; }

        [JsonProperty("RumbleGain")]
        public double? RumbleGain { get; set; }

        [JsonProperty("ProductGuid")]
        public string ProductGuid { get; set; }

        [JsonProperty("ProductName")]
        public string ProductName { get; set; }

        [JsonProperty("AutoCenter")]
        public bool? AutoCenter { get; set; }

        [JsonProperty("ForceFeedbackGain")]
        public int? ForceFeedbackGain { get; set; }

        [JsonProperty("StatusEvents")]
        public List<StatusEventModel> StatusEvents { get; set; }
    }

    public class StatusEventModel
    {
        [JsonProperty("Event")]
        public string Event { get; set; }

        [JsonProperty("ForceFile")]
        public string ForceFile { get; set; }

        [JsonProperty("Duration")]
        public int Duration { get; set; }

        [JsonProperty("Pulse")]
        public bool Pulse { get; set; }

        [JsonProperty("Pulse_Amount")]
        public int PulseAmount { get; set; }
    }
}
