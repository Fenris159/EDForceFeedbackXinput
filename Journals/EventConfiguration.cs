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
        /// <summary>Optional. For XInput rumble override: left motor 0.0-1.0.</summary>
        public double? LeftMotor { get; set; }
        /// <summary>Optional. For XInput rumble override: right motor 0.0-1.0.</summary>
        public double? RightMotor { get; set; }
    }
}
