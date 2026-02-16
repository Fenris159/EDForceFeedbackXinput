using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Journals
{
    public class EventConfiguration
    {
        public string Event { get; set; }
        public string ForceFile { get; set; }
        public int Duration { get; set; } = 250;

        /// <summary>Converts an event name to a unique .ffe filename (e.g. "Status.Scooping:True" → "Status_Scooping_True.ffe").</summary>
        public static string EventToFfeName(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName)) return "Unknown.ffe";
            var name = eventName.Trim().Replace(":", "_").Replace(".", "_");
            if (!name.EndsWith(".ffe", StringComparison.OrdinalIgnoreCase))
                name += ".ffe";
            return name;
        }

        /// <summary>Optional. For XInput rumble override: left motor 0.0-1.0.</summary>
        public double? LeftMotor { get; set; }
        /// <summary>Optional. For XInput rumble override: right motor 0.0-1.0.</summary>
        public double? RightMotor { get; set; }
        /// <summary>Optional. For XInput: pulse vibration on/off multiple times. Default false.</summary>
        public bool Pulse { get; set; }
        /// <summary>Optional. For XInput: number of pulses when Pulse is true. Each pulse lasts Duration ms. Default 0.</summary>
        [JsonProperty("Pulse_Amount")]
        public int PulseAmount { get; set; }
    }
}
