using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ForceFeedbackSharpDx
{
    /// <summary>
    /// XInput rumble device for Xbox controllers (Xbox One, Xbox Elite Series 1/2, etc.).
    /// Uses Windows.Gaming.Input on Windows 10+ when available; otherwise SharpDX XInput.
    /// Maps .ffe effect names to rumble patterns.
    /// </summary>
    public class XInputRumbleDevice : IForceFeedbackDevice
    {
        private readonly IXInputRumbleBackend _backend;
        private readonly int _userIndexInt;
        private readonly double _rumbleGain; // 0.0 to 1.0
        private readonly Dictionary<string, RumbleEventConfig> _customRumbleEvents;
        private readonly ILogger _logger;
        private bool _disposed;
        private string _cachedName;
        private int _currentRumbleGeneration; // Incremented on each new effect; only the active effect may call SetVibration(0)
        private readonly object _effectLock = new object();
        private readonly Stack<(double Left, double Right, int EndTimeTicks)> _resumeStack = new Stack<(double, double, int)>(); // EndTimeTicks = when effect should stop (absolute)
        private double _currentEffectLeft;
        private double _currentEffectRight;
        private int _currentEffectDurationMs;
        private long _currentEffectStartTicks; // 0 = no continuous effect to resume

        private const ushort MaxMotorSpeed = 65535;
        private const int MinResumeMs = 50; // Don't push to resume stack if less than this remains
        private const int MaxDurationMs = 300000; // 5 min safety cap in case something goes wrong

        public XInputRumbleDevice(int userIndex, double rumbleGain = 1.0, Dictionary<string, RumbleEventConfig> customRumbleEvents = null)
        {
            _userIndexInt = userIndex;
            _backend = CreateBackend(userIndex);
            _rumbleGain = Math.Max(0, Math.Min(1.0, rumbleGain));
            _customRumbleEvents = customRumbleEvents ?? new Dictionary<string, RumbleEventConfig>();
            _logger = null;
        }

        public XInputRumbleDevice(int userIndex, ILogger logger, double rumbleGain = 1.0, Dictionary<string, RumbleEventConfig> customRumbleEvents = null)
        {
            _userIndexInt = userIndex;
            _backend = CreateBackend(userIndex);
            _rumbleGain = Math.Max(0, Math.Min(1.0, rumbleGain));
            _customRumbleEvents = customRumbleEvents ?? new Dictionary<string, RumbleEventConfig>();
            _logger = logger;
        }

        /// <summary>Creates backend: Windows.Gaming.Input on Windows 10+ when available, else SharpDX XInput.</summary>
        private static IXInputRumbleBackend CreateBackend(int userIndex)
        {
            try
            {
                var winBackend = XInputWinGamingBackend.TryCreate(userIndex);
                if (winBackend != null && winBackend.IsConnected)
                    return winBackend;
            }
            catch
            {
                // Windows.Gaming.Input unavailable (older Windows, WinRT load failure, etc.) - use SharpDX
            }
            return new XInputSharpDXBackend(userIndex);
        }

        public bool IsConnected => _backend.IsConnected;

        public string GetName()
        {
            if (_cachedName != null)
                return _cachedName;
            _cachedName = $"Xbox Controller (Index {_userIndexInt})";
            return _cachedName;
        }

        private const int PulseGapMs = 80;

        /// <summary>When starting a new effect, cancel the current one. Resume stack disabled - it caused indefinite rumble when rapid events fire (e.g. TestForceFeedback key 8).</summary>
        private void CancelCurrentEffect()
        {
            lock (_effectLock)
            {
                _resumeStack.Clear(); // Don't resume - new effect replaces current
                _currentEffectStartTicks = 0;
            }
        }

        /// <summary>Try to resume - disabled; always returns false.</summary>
        private bool TryResumeNext()
        {
            lock (_effectLock) { _currentEffectStartTicks = 0; }
            return false;
        }

        public void PlayFileEffect(string forceFileName, int duration, double? leftMotorOverride = null, double? rightMotorOverride = null, bool pulse = false, int pulseAmount = 0)
        {
            if (!_backend.IsConnected)
            {
                _logger?.LogDebug("XInputRumbleDevice: Controller {0} not connected", _userIndexInt);
                return;
            }

            double leftMotor;
            double rightMotor;
            int pulseDuration = duration > 0 ? duration : 250;

            // Per-event override from settings.json (LeftMotor, RightMotor)
            if (leftMotorOverride.HasValue && rightMotorOverride.HasValue)
            {
                leftMotor = leftMotorOverride.Value;
                rightMotor = rightMotorOverride.Value;
            }
            else
            {
                var effectKey = (forceFileName ?? "").Trim().ToLowerInvariant();
                RumbleEventConfig customConfig;
                if (_customRumbleEvents.TryGetValue(effectKey, out customConfig))
                {
                    leftMotor = customConfig.LeftMotor;
                    rightMotor = customConfig.RightMotor;
                    pulseDuration = customConfig.Duration > 0 ? customConfig.Duration : pulseDuration;
                }
                else
                {
                    var mapped = MapForceFileToRumble(forceFileName, duration);
                    leftMotor = mapped.leftMotor;
                    rightMotor = mapped.rightMotor;
                    pulseDuration = mapped.duration;
                }
            }

            if (pulse && pulseAmount > 0)
            {
                PlayRumblePulse(leftMotor, rightMotor, pulseDuration, pulseAmount);
            }
            else
            {
                PlayRumble(leftMotor, rightMotor, pulseDuration);
            }
        }

        /// <summary>
        /// Maps .ffe effect file names to (leftMotor, rightMotor, duration) where motors are 0.0-1.0.
        /// Can be overridden by ForceFileRumble in settings.json. New/unknown force files default to 0.5.
        /// </summary>
        private (double leftMotor, double rightMotor, int duration) MapForceFileToRumble(string forceFileName, int duration)
        {
            var name = (forceFileName ?? "").Trim().ToLowerInvariant().Replace(".ffe", "");
            int d = duration > 0 ? duration : 250;

            switch (name)
            {
                // Existing effects - keep current defaults
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
                // New effects - default 0.5 per user request
                case "hulldamage":
                case "underattack":
                case "shieldstate":
                case "heatdamage":
                case "heatwarning":
                case "interdicted":
                case "interdiction":
                case "escapeinterdiction":
                case "died":
                case "cockpitbreached":
                case "launchsrv":
                case "docksrv":
                case "launchfighter":
                case "dockfighter":
                case "fuelscoop":
                case "approachsettlement":
                case "leavebody":
                case "approachbody":
                case "fsdtarget":
                case "dockingrequested":
                case "dockinggranted":
                case "dockingdenied":
                case "dockingcancelled":
                case "dockingtimeout":
                case "loadgame":
                case "fileheader":
                default:
                    return (0.5, 0.5, Math.Max(d, 250));     // New events: default 0.5
            }
        }

        /// <summary>Plays multiple pulses of vibration. Each pulse: on for durationMs, off for PulseGapMs. Does not register as resumable; when it ends, any interrupted continuous effect will resume.</summary>
        private void PlayRumblePulse(double leftMotor, double rightMotor, int durationMs, int pulseCount)
        {
            if (durationMs <= 0) durationMs = 250;
            if (pulseCount <= 0) return;

            leftMotor = Math.Max(0, Math.Min(1.0, leftMotor)) * _rumbleGain;
            rightMotor = Math.Max(0, Math.Min(1.0, rightMotor)) * _rumbleGain;
            ushort left = (ushort)(leftMotor * MaxMotorSpeed);
            ushort right = (ushort)(rightMotor * MaxMotorSpeed);
            CancelCurrentEffect();
            lock (_effectLock) { _currentEffectStartTicks = 0; } // Pulse doesn't register as resumable
            int myGeneration = Interlocked.Increment(ref _currentRumbleGeneration);

            _ = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < pulseCount; i++)
                    {
                        if (Volatile.Read(ref _currentRumbleGeneration) != myGeneration) return;
                        _backend.SetVibration(left, right);
                        Thread.Sleep(Math.Min(durationMs, MaxDurationMs));
                        if (Volatile.Read(ref _currentRumbleGeneration) != myGeneration) return;
                        _backend.SetVibration(0, 0);
                        if (i < pulseCount - 1)
                            Thread.Sleep(PulseGapMs);
                    }
                    if (Volatile.Read(ref _currentRumbleGeneration) == myGeneration)
                    {
                        if (!TryResumeNext())
                            _backend.SetVibration(0, 0);
                    }
                    _logger?.LogDebug("XInputRumbleDevice pulse complete: {0} pulses x {1}ms", pulseCount, durationMs);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("XInputRumbleDevice PlayRumblePulse: {0}", ex.Message);
                    if (Volatile.Read(ref _currentRumbleGeneration) == myGeneration)
                    {
                        if (!TryResumeNext())
                        { try { _backend.SetVibration(0, 0); } catch { } }
                    }
                }
            });
        }

        private void PlayRumble(double leftMotor, double rightMotor, int durationMs)
        {
            if (durationMs <= 0)
                durationMs = 250;

            leftMotor = Math.Max(0, Math.Min(1.0, leftMotor)) * _rumbleGain;
            rightMotor = Math.Max(0, Math.Min(1.0, rightMotor)) * _rumbleGain;

            CancelCurrentEffect();
            lock (_effectLock)
            {
                _currentEffectLeft = leftMotor;
                _currentEffectRight = rightMotor;
                _currentEffectDurationMs = durationMs;
                _currentEffectStartTicks = Environment.TickCount;
            }

            ushort left = (ushort)(leftMotor * MaxMotorSpeed);
            ushort right = (ushort)(rightMotor * MaxMotorSpeed);
            int myGeneration = Interlocked.Increment(ref _currentRumbleGeneration);

            _ = Task.Run(() =>
            {
                try
                {
                    _backend.SetVibration(left, right);
                    Thread.Sleep(Math.Min(durationMs, MaxDurationMs));
                    if (Volatile.Read(ref _currentRumbleGeneration) == myGeneration)
                    {
                        if (!TryResumeNext())
                            _backend.SetVibration(0, 0);
                    }
                    _logger?.LogDebug("XInputRumbleDevice effect complete: {0}ms L={1} R={2}", durationMs, left, right);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("XInputRumbleDevice PlayRumble: {0}", ex.Message);
                    if (Volatile.Read(ref _currentRumbleGeneration) == myGeneration)
                    {
                        if (!TryResumeNext())
                        { try { _backend.SetVibration(0, 0); } catch { } }
                    }
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

        /// <summary>Stop all rumble immediately and clear the resume stack. Use when rumble gets stuck.</summary>
        public void StopRumble()
        {
            lock (_effectLock) { _resumeStack.Clear(); _currentEffectStartTicks = 0; }
            Interlocked.Increment(ref _currentRumbleGeneration);
            try { _backend?.SetVibration(0, 0); } catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            lock (_effectLock) { _resumeStack.Clear(); _currentEffectStartTicks = 0; }
            try
            {
                _backend?.SetVibration(0, 0);
            }
            catch { }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
