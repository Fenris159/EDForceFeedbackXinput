using SharpDX.XInput;

namespace ForceFeedbackSharpDx
{
    internal sealed class XInputSharpDXBackend : IXInputRumbleBackend
    {
        private readonly Controller _controller;

        public XInputSharpDXBackend(int userIndex)
        {
            _controller = new Controller((UserIndex)userIndex);
        }

        public bool IsConnected => _controller.IsConnected;

        public void SetVibration(ushort leftMotor, ushort rightMotor)
        {
            _controller.SetVibration(new Vibration { LeftMotorSpeed = leftMotor, RightMotorSpeed = rightMotor });
        }
    }
}
