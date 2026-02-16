using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HidSharp;

namespace ForceFeedbackSharpDx
{
    /// <summary>
    /// Rumble backend using raw HID output reports. Bypasses XInput/DirectInput so the controller
    /// can be shared with Elite Dangerous (which uses DirectInput). Uses HidSharp with non-exclusive
    /// open. Maps user index 0-3 to the Nth Xbox-compatible HID device in enumeration order.
    /// </summary>
    internal sealed class XInputHidBackend : IXInputRumbleBackend, IDisposable
    {
        private static readonly int[] XboxPids = { 0x02D1, 0x02DD, 0x02EA, 0x02FD, 0x028E, 0x028F, 0x0B00, 0x0B05, 0x0B13, 0x0B20, 0x0B22 };
        private const int MicrosoftVid = 0x045E;

        private readonly HidStream _stream;
        private readonly byte[] _rumbleReport;
        private readonly int _reportLength;
        private readonly bool _useReportId;

        private XInputHidBackend(HidStream stream, byte[] rumbleReport, int reportLength, bool useReportId)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _rumbleReport = rumbleReport ?? throw new ArgumentNullException(nameof(rumbleReport));
            _reportLength = reportLength;
            _useReportId = useReportId;
        }

        public bool IsConnected => _stream != null && _stream.CanWrite;

        public string BackendName => "Raw HID";

        public bool? ExclusiveAccessAcquired => null;

        public void SetVibration(ushort leftMotor, ushort rightMotor)
        {
            try
            {
                // Convert 0-65535 to 0-255 for HID
                byte left = (byte)(leftMotor >> 8);
                byte right = (byte)(rightMotor >> 8);

                if (_useReportId)
                {
                    // Xbox One S Bluetooth format: Report ID 0x03, mask 0x0F, left/right motors
                    // [0x03, 0x0F, 0x00, 0x00, 0x00, 0x00, left, right, 0xFF, 0x00, 0xEB]
                    if (_rumbleReport.Length >= 11)
                    {
                        _rumbleReport[0] = 0x03;
                        _rumbleReport[1] = 0x0F;
                        _rumbleReport[2] = 0x00;
                        _rumbleReport[3] = 0x00;
                        _rumbleReport[4] = 0x00;
                        _rumbleReport[5] = 0x00;
                        _rumbleReport[6] = left;
                        _rumbleReport[7] = right;
                        _rumbleReport[8] = 0xFF;
                        _rumbleReport[9] = 0x00;
                        _rumbleReport[10] = 0xEB;
                    }
                }
                else
                {
                    // No report ID: [0x00, left, right] or similar - pad to report length
                    if (_rumbleReport.Length >= 3)
                    {
                        _rumbleReport[0] = 0x00;
                        _rumbleReport[1] = left;
                        _rumbleReport[2] = right;
                    }
                }

                _stream.Write(_rumbleReport, 0, _reportLength);
            }
            catch (IOException) { /* Device may have disconnected */ }
            catch (ObjectDisposedException) { }
        }

        /// <summary>Tries to create a backend for the given user index (0-3). Returns null if no Xbox HID device at that index or open fails.</summary>
        public static IXInputRumbleBackend TryCreate(int userIndex)
        {
            if (userIndex < 0 || userIndex > 3) return null;

            try
            {
                var deviceList = DeviceList.Local;
                var xboxDevices = new List<HidDevice>();

                foreach (var device in deviceList.GetHidDevices(MicrosoftVid, null))
                {
                    if (device == null || !XboxPids.Contains(device.ProductID)) continue;
                    try
                    {
                        if (device.GetMaxOutputReportLength() <= 0) continue;
                    }
                    catch { continue; }
                    xboxDevices.Add(device);
                }

                if (userIndex >= xboxDevices.Count) return null;

                var hidDevice = xboxDevices[userIndex];
                var maxOut = hidDevice.GetMaxOutputReportLength();
                if (maxOut <= 0) return null;

                // Open with default config (Exclusive = false) to allow sharing with other apps
                var config = new OpenConfiguration();
                if (!hidDevice.TryOpen(config, out HidStream stream) || stream == null)
                    return null;

                // Xbox One S Bluetooth uses report ID 0x03, 11 bytes total
                bool useReportId = maxOut >= 11;
                var report = new byte[Math.Max(maxOut, 11)];
                int reportLength = useReportId ? 11 : Math.Min(maxOut, 4);

                return new XInputHidBackend(stream, report, reportLength, useReportId);
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            try { _stream?.Dispose(); } catch { }
        }
    }
}
