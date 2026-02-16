using System;
using System.Collections.Generic;
using System.Text;

namespace Journals
{
    public class Device
    {
        public string ProductGuid { get; set; }
        public string ProductName { get; set; }
        public bool? AutoCenter { get; set; } = true;
        public int? ForceFeedbackGain { get; set; } = 10000;
        public List<EventConfiguration> StatusEvents { get; set; } = new List<EventConfiguration>();
        /// <summary>When true, treat as XInput device. No DirectInput used. UserIndex from UserIndex property or auto-detect.</summary>
        public bool? XInput { get; set; }
        /// <summary>For XInput: UserIndex 0-3. Omit or -1 for auto-detect.</summary>
        public int? UserIndex { get; set; } = -1;
        /// <summary>For XInput: rumble intensity 0.0-1.0. Default 1.0.</summary>
        public double? RumbleGain { get; set; } = 1.0;
    }
}
