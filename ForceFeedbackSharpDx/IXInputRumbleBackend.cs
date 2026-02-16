namespace ForceFeedbackSharpDx
{
    /// <summary>Low-level rumble backend. Abstracts XInput (SharpDX) vs Windows.Gaming.Input.</summary>
    internal interface IXInputRumbleBackend
    {
        bool IsConnected { get; }
        void SetVibration(ushort leftMotor, ushort rightMotor);
    }
}
