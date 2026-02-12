using Microsoft.Extensions.Logging;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ForceFeedbackSharpDx
{
    /// <summary>
    /// XInput rumble device for Xbox controllers (Xbox One, Xbox Elite Series 1/2, etc.).
    /// Uses native Windows XInput - no drivers required. Maps .ffe effect names to rumble patterns.
    /// </summary>
    public class XInputRumbleDevice : IForceFeedbackDevice
    {
        private readonly Controller _controller;
        private readonly UserIndex _userIndex;
        private readonly int _userIndexInt;
        private readonly double _rumbleGain; // 0.0 to 1.0
        private readonly Dictionary<string, RumbleEventConfig> _customRumbleEvents;
        private readonly ILogger _logger;
        private bool _disposed;
        private string _cachedName;

        private const ushort MaxMotorSpeed = 65535;

        public XInputRumbleDevice(int userIndex, double rumbleGain = 1.0, Dictionary<string, RumbleEventConfig> customRumbleEvents = null)
        {
            _userIndexInt = userIndex;
            _userIndex = (UserIndex)userIndex;
            _controller = new Controller(_userIndex);
            _rumbleGain = Math.Max(0, Math.Min(1.0, rumbleGain));
            _customRumbleEvents = customRumbleEvents ?? new Dictionary<string, RumbleEventConfig>();
            _logger = null;
        }

        public XInputRumbleDevice(int userIndex, ILogger logger, double rumbleGain = 1.0, Dictionary<string, RumbleEventConfig> customRumbleEvents = null)
        {
            _userIndexInt = userIndex;
            _userIndex = (UserIndex)userIndex;
            _controller = new Controller(_userIndex);
            _rumbleGain = Math.Max(0, Math.Min(1.0, rumbleGain));
            _customRumbleEvents = customRumbleEvents ?? new Dictionary<string, RumbleEventConfig>();
            _logger = logger;
        }

        public bool IsConnected => _controller.IsConnected;

        public string GetName()
        {
            if (_cachedName != null)
                return _cachedName;

            try
            {
                if (_controller.IsConnected)
                {
                    var caps = _controller.GetCapabilities(DeviceQueryType.Any);
                    _cachedName = $"Xbox Controller (Index {_userIndexInt})";
                    return _cachedName;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("XInputRumbleDevice GetName: {0}", ex.Message);
            }

            _cachedName = $"Xbox Controller (Index {_userIndexInt})";
            return _cachedName;
        }

        public void PlayFileEffect(string forceFileName, int duration, double? leftMotorOverride = null, double? rightMotorOverride = null)
        {
            if (!_controller.IsConnected)
            {
                _logger?.LogDebug("XInputRumbleDevice: Controller {0} not connected", _userIndexInt);
                return;
            }

            // Per-event override from settings.json (LeftMotor, RightMotor)
            if (leftMotorOverride.HasValue && rightMotorOverride.HasValue)
            {
                PlayRumble(leftMotorOverride.Value, rightMotorOverride.Value, duration > 0 ? duration : 250);
                return;
            }

            var effectKey = (forceFileName ?? "").Trim().ToLowerInvariant();
            RumbleEventConfig customConfig;
            if (_customRumbleEvents.TryGetValue(effectKey, out customConfig))
            {
                PlayRumble(customConfig.LeftMotor, customConfig.RightMotor, customConfig.Duration > 0 ? customConfig.Duration : duration);
                return;
            }

            // Map common .ffe effect names to rumble patterns
            var (leftMotor, rightMotor, patternDuration) = MapForceFileToRumble(forceFileName, duration);
            PlayRumble(leftMotor, rightMotor, patternDuration);
        }

        /// <summary>
        /// Maps .ffe effect file names to (leftMotor, rightMotor, duration) where motors are 0.0-1.0.
        /// </summary>
        private (double leftMotor, double rightMotor, int duration) MapForceFileToRumble(string forceFileName, int duration)
        {
            var name = (forceFileName ?? "").Trim().ToLowerInvariant().Replace(".ffe", "");
            int d = duration > 0 ? duration : 250;

            switch (name)
            {
                case "dock":
                    return (0.85, 0.85, Math.Max(d, 1500));  // Strong symmetric - docking thud
                case "gear":
                    return (0.9, 0.9, Math.Max(d, 2000));    // Heavy symmetric - gear deploy
                case "hardpoints":
                    return (0.8, 0.8, Math.Max(d, 1500));    // Strong - hardpoints
                case "landed":
                    return (0.75, 0.75, Math.Max(d, 1200));  // Medium-strong - landing
                case "cargo":
                    return (0.7, 0.7, Math.Max(d, 1500));    // Medium - cargo scoop
                case "vibrate":
                    return (0.5, 0.5, Math.Min(d, 300));     // Light symmetric pulse
                case "vibrateside":
                    return (0.3, 0.7, Math.Min(d, 500));     // Right-heavy (low fuel, overheating)
                case "damper":
                    return (0.4, 0.4, Math.Min(d, 400));     // Light sustained
                case "centerspringxy":
                    return (0.2, 0.2, Math.Min(d, 200));     // Very light
                case "supercruise":
                    return (0.6, 0.6, Math.Max(d, 1000));    // Medium - supercruise engage
                default:
                    return (0.6, 0.6, Math.Max(d, 250));     // Default: medium symmetric
            }
        }

        private void PlayRumble(double leftMotor, double rightMotor, int durationMs)
        {
            if (durationMs <= 0)
                durationMs = 250;

            leftMotor = Math.Max(0, Math.Min(1.0, leftMotor)) * _rumbleGain;
            rightMotor = Math.Max(0, Math.Min(1.0, rightMotor)) * _rumbleGain;

            ushort left = (ushort)(leftMotor * MaxMotorSpeed);
            ushort right = (ushort)(rightMotor * MaxMotorSpeed);

            _ = Task.Run(() =>
            {
                try
                {
                    var vibe = new Vibration { LeftMotorSpeed = left, RightMotorSpeed = right };
                    _controller.SetVibration(vibe);
                    Thread.Sleep(Math.Min(durationMs, 5000)); // Cap at 5 seconds
                    _controller.SetVibration(new Vibration { LeftMotorSpeed = 0, RightMotorSpeed = 0 });
                    _logger?.LogDebug("XInputRumbleDevice effect complete: {0}ms L={1} R={2}", durationMs, left, right);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("XInputRumbleDevice PlayRumble: {0}", ex.Message);
                    try { _controller.SetVibration(new Vibration { LeftMotorSpeed = 0, RightMotorSpeed = 0 }); } catch { }
                }
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger?.LogDebug("XInputRumbleDevice Task faulted: {0}", t.Exception?.InnerException?.Message);
            });
        }

        /// <summary>
        /// Optional per-event rumble override (LeftMotor, RightMotor 0.0-1.0, Duration ms).
        /// </summary>
        public class RumbleEventConfig
        {
            public double LeftMotor { get; set; } = 0.8;
            public double RightMotor { get; set; } = 0.8;
            public int Duration { get; set; } = 250;
        }

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                _controller?.SetVibration(new Vibration { LeftMotorSpeed = 0, RightMotorSpeed = 0 });
            }
            catch { }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
