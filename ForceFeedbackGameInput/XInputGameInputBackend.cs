using System;
using ForceFeedbackSharpDx;
using GameInputWrapper;

namespace ForceFeedbackGameInput
{
    /// <summary>
    /// Rumble backend using Microsoft.GameInput via C++/CLI wrapper.
    /// Rumbles all detected gamepad devices. Preferred over HID and XInput.
    /// </summary>
    public sealed class XInputGameInputBackend : IXInputRumbleBackend, IDisposable
    {
        private static readonly object s_lock = new object();
        private static XInputGameInputBackend s_instance;
        private static bool s_initialized;

        private readonly GameInputRumbleManager _manager;

        private XInputGameInputBackend(GameInputRumbleManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public bool IsConnected => _manager != null && _manager.DeviceCount > 0;

        public string BackendName => "Microsoft.GameInput";

        public bool? ExclusiveAccessAcquired => _manager?.ExclusiveAccessAcquired;

        public void SetVibration(ushort leftMotor, ushort rightMotor)
        {
            if (_manager == null) return;
            float low = leftMotor / 65535f;
            float high = rightMotor / 65535f;
            _manager.SetRumble(low, high);
        }

        public void Dispose()
        {
            // Shared instance - don't dispose _manager; it lives for the app lifetime
        }

        /// <summary>
        /// Tries to create the GameInput backend. Returns a shared instance if GameInput has devices, else null.
        /// </summary>
        public static IXInputRumbleBackend TryCreate(int userIndex)
        {
            lock (s_lock)
            {
                if (s_instance != null)
                    return s_instance;

                if (!s_initialized)
                {
                    s_initialized = true;
                    try
                    {
                        var manager = new GameInputRumbleManager();
                        if (manager.Initialize() && manager.DeviceCount > 0)
                        {
                            s_instance = new XInputGameInputBackend(manager);
                        }
                    }
                    catch (Exception)
                    {
                        // GameInput runtime not installed or no devices
                    }
                }
            }
            return s_instance;
        }

        /// <summary>
        /// Registers the GameInput backend as the preferred resolver. Call before creating XInputRumbleDevice.
        /// </summary>
        public static void RegisterAsPreferred()
        {
            XInputRumbleDevice.BackendResolver = userIndex => TryCreate(userIndex);
        }
    }
}
