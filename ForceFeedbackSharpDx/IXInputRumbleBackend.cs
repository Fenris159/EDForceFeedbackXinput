namespace ForceFeedbackSharpDx
{
    /// <summary>Low-level rumble backend. GameInput (preferred), Raw HID, or SharpDX XInput (fallback).</summary>
    public interface IXInputRumbleBackend
    {
        bool IsConnected { get; }
        /// <summary>Human-readable backend name for startup logging (e.g. "Microsoft.GameInput", "Raw HID", "SharpDX XInput").</summary>
        string BackendName { get; }
        /// <summary>For GameInput: whether AcquireExclusiveRawDeviceAccess succeeded. Null for other backends.</summary>
        bool? ExclusiveAccessAcquired { get; }
        void SetVibration(ushort leftMotor, ushort rightMotor);
    }
}
