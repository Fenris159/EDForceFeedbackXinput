# Current Release Notes

## EDForceFeedbackXinput v1.0.1

This update builds on the v1.0.0 release and fixes startup crashes caused by EliteAPI journal path discovery on Windows 10/11 systems where Elite Dangerous is installed outside the system drive.

### Fixed

- Fixed an immediate startup crash when EDForceFeedback attempted to start EliteAPI file watchers and EliteAPI could not resolve a valid journal directory.
- Journal discovery no longer depends on the Elite Dangerous install location.
- EDForceFeedback now resolves the standard Windows Saved Games known folder first, then falls back to `%USERPROFILE%\Saved Games\Frontier Developments\Elite Dangerous`.
- Added support for explicit `JournalDirectory` and `BindingsDirectory` settings for users with redirected or non-standard folders.
- Settings Editor now preserves `JournalDirectory` and `BindingsDirectory` when saving `settings.json`.

### Configuration

The new path settings are optional. Existing users can leave them as `null`.

```json
{
  "JournalDirectory": null,
  "BindingsDirectory": null
}
```

If automatic detection fails, set them explicitly:

```json
{
  "JournalDirectory": "C:\\Users\\senre\\Saved Games\\Frontier Developments\\Elite Dangerous",
  "BindingsDirectory": "C:\\Users\\senre\\AppData\\Local\\Frontier Developments\\Elite Dangerous\\Options\\Bindings"
}
```

## EDForceFeedbackXinput v1.0.0

Elite Dangerous force feedback and rumble support for Xbox and PlayStation controllers.

EDForceFeedbackXinput reads Elite Dangerous Journal files and `Status.json`, then triggers gamepad rumble or DirectInput force feedback effects for events such as hardpoints, landing gear, docking, and other ship or SRV state changes.

### Highlights

- Xbox controller support for Xbox One, Elite 1/2, and Series X/S controllers.
- PlayStation controller support for DualSense and DualShock 4 through GameInput or XInput emulation tools such as DS4Windows, DualSenseX, or Steam Input.
- DirectInput joystick support for devices such as Microsoft SideWinder Force Feedback 2 and Saitek Cyborg 3D Force Stick.
- No custom drivers required. Rumble uses GameInput, raw HID, or XInput.
- Settings Editor for per-event rumble strength, duration, and pulse configuration.
- TestForceFeedback utility for testing rumble without running Elite Dangerous.

### Downloads

- `EDForceFeedback`: Main app to run alongside Elite Dangerous.
- `TestForceFeedback`: Rumble test harness for controller troubleshooting.

### Requirements

- Windows 10/11, 64-bit.
- .NET Framework 4.8.
- Microsoft GameInput runtime. Install `redist\GameInputRedist.msi` if rumble is not detected.
- Xbox Wireless Adapter users should follow the ReWASD + HidHide workaround documented in the README.

### Installation

1. Download and extract the release zip.
2. Run `EDForceFeedback.exe` or `TestForceFeedback.exe`.
3. Run `redist\GameInputRedist.msi` if rumble is not detected.
4. Start Elite Dangerous and keep EDForceFeedback running.
5. See `README.md` for configuration, connection notes, and troubleshooting.

### Project Lineage

This fork is based on EDForceFeedback by Bob (CMDR Axe_), with Xbox and PlayStation controller rumble support added.
