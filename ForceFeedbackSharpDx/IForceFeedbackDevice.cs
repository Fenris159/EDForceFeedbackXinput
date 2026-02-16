using System;

namespace ForceFeedbackSharpDx
{
    /// <summary>
    /// Common interface for force feedback devices (DirectInput .ffe playback and XInput rumble).
    /// </summary>
    public interface IForceFeedbackDevice : IDisposable
    {
        /// <summary>
        /// Gets the display name of the device.
        /// </summary>
        string GetName();

        /// <summary>
        /// Plays an effect. For DirectInput devices this loads and plays an .ffe file.
        /// For XInput devices this maps the force file name to a rumble pattern.
        /// </summary>
        /// <param name="forceFileName">Name of the force file (e.g. "Dock.ffe") or effect to approximate.</param>
        /// <param name="duration">Duration in milliseconds. Zero or below plays until stopped.</param>
        /// <param name="leftMotorOverride">Optional. For XInput: left motor 0.0-1.0 override.</param>
        /// <param name="rightMotorOverride">Optional. For XInput: right motor 0.0-1.0 override.</param>
        /// <param name="pulse">Optional. For XInput: pulse vibration on/off multiple times.</param>
        /// <param name="pulseAmount">Optional. For XInput: number of pulses when pulse is true. Each pulse lasts duration ms.</param>
        void PlayFileEffect(string forceFileName, int duration, double? leftMotorOverride = null, double? rightMotorOverride = null, bool pulse = false, int pulseAmount = 0);
    }
}
