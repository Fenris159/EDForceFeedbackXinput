using System.Collections.Generic;

namespace Journals
{
    public class Settings
    {
        public List<Device> Devices { get; set; }

        /// <summary>
        /// Optional. Overrides default rumble (Left/Right 0.0-1.0) per force file for XInput devices.
        /// Key is force file name (e.g. "Dock.ffe", "HullDamage.ffe"). Applied when no per-event override.
        /// </summary>
        public Dictionary<string, ForceFileRumbleEntry> ForceFileRumble { get; set; }
    }

    public class ForceFileRumbleEntry
    {
        public double Left { get; set; } = 0.5;
        public double Right { get; set; } = 0.5;
    }
}
