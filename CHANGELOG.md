# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2026-06-14

### Added

- Optional `JournalDirectory` setting for users with redirected or non-standard Elite Dangerous journal folders.
- Optional `BindingsDirectory` setting for users with redirected or non-standard Elite Dangerous bindings folders.
- Current release notes file for the v1.0.1 journal-path fix release.
- GitHub Actions CI workflow for Windows release builds.
- GitHub Actions release workflow that builds release zips and uses `CurrentReleaseNotes.md` as the GitHub Release description.

### Changed

- Bumped project version from `1.0.0` to `1.0.1`.
- EDForceFeedback now passes explicit journal and bindings directories to EliteAPI instead of relying on EliteAPI's default path discovery.
- README configuration documentation now explains journal and bindings path overrides.
- README now includes repository badges and release workflow instructions.
- Bundled EDForceFeedback and TestForceFeedback `settings.json` files now include `JournalDirectory` and `BindingsDirectory` placeholders.
- Settings Editor now preserves `JournalDirectory` and `BindingsDirectory` when saving `settings.json`.

### Fixed

- Fixed immediate startup crash on Windows 10/11 when EliteAPI attempted to create file watchers for missing or incorrectly resolved Elite Dangerous directories.
- Fixed journal discovery for setups where Elite Dangerous is installed on a non-system drive while journals remain under the current user's Saved Games folder.

## [1.0.0] - 2025-02-12

First release of **EDForceFeedbackXinput**. Changes since the original [EDForceFeedback](https://github.com/BobTheCoder/EDForceFeedback) by Bob (CMDR Axe_):

### Added

- **Microsoft.GameInput (preferred backend)** – C++/CLI wrapper (`GameInputWrapper`) for native GameInput API. Rumbles all connected gamepad devices. Supports Xbox and PlayStation (DualSense, DualShock 4). Requires Microsoft GameInput Redistributable.
- **Xbox controller rumble** – Native support for Xbox One, Elite Series 1/2, and Series X/S. No custom drivers required. Backend order: GameInput → Raw HID → SharpDX XInput.
- **PlayStation controller support** – DualSense and DualShock 4 via GameInput (native) or XInput emulation (DS4Windows, DualSenseX, Steam Input).
- **Raw HID (HidSharp)** – Fallback when GameInput unavailable. Bypasses XInput/DirectInput for Xbox controllers; enables background rumble while Elite has the controller via DirectInput.
- **SharpDX XInput** – Last-resort fallback for Xbox or PlayStation (when emulated as XInput).
- **AcquireExclusiveRawDeviceAccess** – Attempted at startup per device (v0 IGameInputDevice). Returns false on Windows—not yet implemented by Microsoft; future runtime updates may enable it.
- **Console diagnostics** – Startup log shows which backend is used (Microsoft.GameInput, Raw HID, or SharpDX XInput) and AcquireExclusiveRawDeviceAccess result for GameInput.
- **EDForceFeedbackSettingsEditor** – GUI editor for rumble strength, duration, and pulse settings. Edits `settings.json` in place.
- **TestForceFeedback** – Test harness to verify rumble without running Elite Dangerous. Simulates events via key presses.
- **Status.json handling** – Reads Elite's Status.json and emits events for flag changes (Gear, Hardpoints, Overheating, etc.).
- **Typed event handlers** – Docked, Undocked, Touchdown, Liftoff use Journal events for faster response.
- **ForceFileRumble** – Per-effect rumble strength (Left/Right 0.0–1.0) in settings.
- **Pulse mode** – Option to pulse vibration on/off for events (e.g. Overheating).
- **Event categories** – Settings grouped by Docking, Combat, Travel, etc.
- **EVENT_REFERENCE.md** – Documentation of all event keys and sources.
- **Connection scenarios documentation** – Wired/Bluetooth vs Xbox Wireless Adapter; ReWASD + HidHide workaround for wireless adapter users.
- **GameInput Redistributable** – Automatically copied to output `redist\` folder in Release builds for distribution.

### Changed

- **EliteAPI v5.0.8** – Upgraded from v3. Journal and Status.json parsing updated for current Elite format.
- **Event keys** – Use `Status.Gear:True`, `Status.Hardpoints:False`, etc., instead of raw event names.
- **Force file naming** – Event-specific names (e.g. `Status_Gear_True.ffe`) for granular rumble control.
- **Console-only** – Removed WinForms GUI; main app is a console executable.
- **Centralized versioning** – Single `Version` in `Directory.Build.props` for all projects.
- **Project branding** – EDForceFeedbackXinput as the fork name across documentation.

### Removed

- **EDForceFeedbackConsole** – Legacy project; replaced by EDForceFeedback (main app, SDK-style).
- **Form1** – WinForms GUI; Settings Editor is the preferred way to configure.
- **Resume stack** – Disabled to prevent indefinite rumble when rapid events fire.
- **Duplicate event handling** – OnAllJson no longer double-fires Journal events.

### Fixed

- Rumble continuing indefinitely when TestForceFeedback fired rapid transitions (e.g. Hardpoints retracted).
- Duplicate rumble for Journal events (FSDJump, HullDamage, etc.) from redundant OnAllJson handlers.
- TestForceFeedback firing both True and False effects when simulating transitions; now uses single `TriggerEvent` per key.
- Emergency stop (key `s`) added to TestForceFeedback when rumble gets stuck.
- MSB3270 processor architecture warning – ForceFeedbackGameInput targets x64 to match GameInputWrapper.
- Debug build failure – BasicRuntimeChecks set to Default for C++/CLI (avoids /RTC1 and /clr incompatibility).

### Technical

- Rumble backends: `Microsoft.GameInput` (C++/CLI, preferred), `XInputHidBackend` (Raw HID, Xbox), `XInputSharpDXBackend` (fallback).
- Event flow: Journal → typed handlers + OnAll; Status.json → EmitStatusEventsFromJson → EmitChangedStatusFlags.
- Suppressed events (Docked, Undocked, Touchdown, Liftoff, Status, HeatWarning, HeatDamage, ShieldState) avoid duplicate vibrations.
- Build: x64 platform; Visual Studio with C++ desktop workload required for GameInputWrapper.
