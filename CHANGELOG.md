# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-02-12

First release of **EDForceFeedbackXinput**. Changes since the original [EDForceFeedback](https://github.com/BobTheCoder/EDForceFeedback) by Bob (CMDR Axe_):

### Added

- **Xbox controller rumble (XInput)** – Native support for Xbox One, Elite Series 1/2, and Series X/S controllers. No custom drivers required.
- **Raw HID (HidSharp)** – Prefers raw HID output reports to bypass XInput/DirectInput; enables background rumble while Elite has the controller. Falls back to SharpDX XInput when HID is unavailable.
- **EDForceFeedbackSettingsEditor** – GUI editor for rumble strength, duration, and pulse settings. Edits `settings.json` in place.
- **TestForceFeedback** – Test harness to verify rumble without running Elite Dangerous. Simulates events via key presses.
- **Status.json handling** – Reads Elite's Status.json and emits events for flag changes (Gear, Hardpoints, Overheating, etc.).
- **Typed event handlers** – Docked, Undocked, Touchdown, Liftoff use Journal events for faster response.
- **ForceFileRumble** – Per-effect rumble strength (Left/Right 0.0–1.0) in settings.
- **Pulse mode** – Option to pulse vibration on/off for events (e.g. Overheating).
- **Event categories** – Settings grouped by Docking, Combat, Travel, etc.
- **EVENT_REFERENCE.md** – Documentation of all event keys and sources.

### Changed

- **EliteAPI v5.0.8** – Upgraded from v3. Journal and Status.json parsing updated for current Elite format.
- **Event keys** – Use `Status.Gear:True`, `Status.Hardpoints:False`, etc., instead of raw event names.
- **Force file naming** – Event-specific names (e.g. `Status_Gear_True.ffe`) for granular rumble control.
- **Console-only** – Removed WinForms GUI; main app is a console executable.
- **Centralized versioning** – Single `Version` in `Directory.Build.props` for all projects.

### Removed

- **EDForceFeedbackConsole** – Legacy project; replaced by EDForceFeedbackXinput (SDK-style).
- **Form1** – WinForms GUI; Settings Editor is the preferred way to configure.
- **Resume stack** – Disabled to prevent indefinite rumble when rapid events fire.
- **Duplicate event handling** – OnAllJson no longer double-fires Journal events.

### Fixed

- Rumble continuing indefinitely when TestForceFeedback fired rapid transitions (e.g. Hardpoints retracted).
- Duplicate rumble for Journal events (FSDJump, HullDamage, etc.) from redundant OnAllJson handlers.
- TestForceFeedback firing both True and False effects when simulating transitions; now uses single `TriggerEvent` per key.
- Emergency stop (key `s`) added to TestForceFeedback when rumble gets stuck.

### Technical

- XInput backends: `XInputWinGamingBackend` (Windows 10+), `XInputSharpDXBackend` (fallback).
- Event flow: Journal → typed handlers + OnAll; Status.json → EmitStatusEventsFromJson → EmitChangedStatusFlags.
- Suppressed events (Docked, Undocked, Touchdown, Liftoff, Status, HeatWarning, HeatDamage, ShieldState) avoid duplicate vibrations.
