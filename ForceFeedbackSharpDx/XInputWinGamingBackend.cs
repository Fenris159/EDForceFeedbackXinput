using System;
using Windows.Gaming.Input;

namespace ForceFeedbackSharpDx
{
    /// <summary>Rumble backend using Windows.Gaming.Input (WinRT). Requires Windows 10+.</summary>
    internal sealed class XInputWinGamingBackend : IXInputRumbleBackend
    {
        private readonly Gamepad _gamepad;

        private XInputWinGamingBackend(Gamepad gamepad)
        {
            _gamepad = gamepad ?? throw new ArgumentNullException(nameof(gamepad));
        }

        public bool IsConnected => _gamepad != null;

        public void SetVibration(ushort leftMotor, ushort rightMotor)
        {
            double left = leftMotor / 65535.0;
            double right = rightMotor / 65535.0;
            _gamepad.Vibration = new GamepadVibration
            {
                LeftMotor = left,
                RightMotor = right,
                LeftTrigger = 0,
                RightTrigger = 0
            };
        }

        /// <summary>Tries to create a backend for the given user index (0-3). Returns null if Windows.Gaming.Input is unavailable or no gamepad at that index.</summary>
        public static IXInputRumbleBackend TryCreate(int userIndex)
        {
            try
            {
                var gamepads = Gamepad.Gamepads;
                if (gamepads == null || userIndex < 0 || userIndex >= gamepads.Count)
                    return null;
                var gamepad = gamepads[userIndex];
                if (gamepad == null)
                    return null;
                return new XInputWinGamingBackend(gamepad);
            }
            catch
            {
                return null;
            }
        }
    }
}
