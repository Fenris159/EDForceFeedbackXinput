# EDForceFeedback

Elite Dangerous Force Feedback with support for Xbox controller rumble (XInput) and DirectInput joysticks (MSFFB2, Saitek Cyborg, etc.).

This fork adds **native Xbox controller rumble** via Windows XInput (no drivers required) and uses **EliteAPI v5.0.8** for journal and Status.json parsing.

## Description

EDForceFeedback.exe runs during an Elite Dangerous session. It reads the game's Journal and Status.json and triggers force feedback: rumble (Xbox) or .ffe playback (DirectInput joysticks).

**Supported devices:**

- [Microsoft Xbox One Controller](https://en.wikipedia.org/wiki/Xbox_One_controller) – Native XInput rumble (One, Elite 1/2, Series X/S). No drivers on Windows 10/11.
- [Microsoft Sidewinder Force Feedback 2](https://en.wikipedia.org/wiki/Microsoft_SideWinder#Force_Feedback_2) joystick.
- [Saitek Cyborg 3D Force Stick](https://www.yumpu.com/en/document/view/38049421/cyborg-force-manualqxd-saitekcom)

## Installation

1. Go to the [Releases](https://github.com/Fenris159/EDForceFeedbackXinput/releases) page on GitHub.
2. Download the latest release:
   - **EDForceFeedback** – main program (runs with Elite Dangerous).
   - **TestForceFeedback** – test harness for troubleshooting rumble without the game.
3. Extract the zip(s) to a folder of your choice (e.g. your Desktop or a dedicated game tools folder).
4. Run `EDForceFeedback.exe` or `TestForceFeedback.exe` from the extracted folder.

5. **Xbox controller rumble not working?** If your Xbox controller is connected but rumble doesn't respond, run `GameInputRedist.msi` from the `redist` folder (if included in the release). This installs or updates the Microsoft GameInput runtime required for Xbox rumble. You may need to run it once and then restart the program.

Both programs include `settings.json` and the Settings Editor.

## Usage

**EDForceFeedback (main program):**

1. Connect your joystick or Xbox controller.
2. Run EDForceFeedback.exe.
3. Start Elite Dangerous.
4. In-game events (hardpoints, landing gear, docking, etc.) trigger the configured feedback.

**TestForceFeedback (troubleshooting):** Run without Elite Dangerous to test rumble. Press 1–9 or a to fire events, s to stop. See [EVENT_REFERENCE.md](EVENT_REFERENCE.md).

## Settings Editor

Run **EDForceFeedbackSettingsEditor.exe** from the same folder as your `settings.json` (included in each release). It edits `settings.json` directly.

1. Run the editor from the extracted EDForceFeedback or TestForceFeedback folder.
2. Adjust per-event settings: Duration, Left/Right %, Pulse.
3. Use **Preview** to test an event on a connected Xbox controller.
4. **Save** writes changes to `settings.json`.

You can also edit `settings.json` manually. See [Configuration](#configuration) and [EVENT_REFERENCE.md](EVENT_REFERENCE.md).

## Xbox Controller Support (XInput)

Xbox controllers use Microsoft.GameInput (preferred), raw HID, or SharpDX XInput – no custom drivers needed.

**Microsoft.GameInput (preferred):** Uses the native GameInput API via a C++/CLI wrapper. Rumbles **all** connected gamepad devices. Requires the [GameInput Redistributable](https://aka.ms/gameinput) to be installed (see [Deployment](#deployment)).

**Raw HID (fallback):** When an Xbox controller exposes a HID interface, the program uses raw HID output reports via HidSharp. This **bypasses XInput and DirectInput entirely**, allowing background rumble injection while Elite Dangerous has the controller via DirectInput. The controller is not claimed through any high-level input stack.

**SharpDX XInput (last resort):** If GameInput and raw HID are unavailable, the program falls back to SharpDX XInput. XInput shares the controller with other applications but may be blocked when Elite has exclusive DirectInput access.

- **Auto-detection**: Controllers at UserIndex 0–3 are detected automatically.
- **Config**: Add `"XInput": true` and `"UserIndex": -1` (auto) or `0`–`3` in `settings.json`.
- **ForceFileRumble**: Per-effect rumble strength (e.g. `Status_Gear_True.ffe` → Left/Right 0.0–1.0).
- **Per-event overrides**: `LeftMotor`, `RightMotor`, `Pulse`, `Pulse_Amount` in StatusEvents.
- **Event naming**: `Status.Scooping:True` → `Status_Scooping_True.ffe` (replace `.` and `:` with `_`).

### Direct GameInput usage (C#)

If using the `GameInputWrapper` project directly:

```csharp
using GameInputWrapper;

var rumble = new GameInputRumbleManager();
if (rumble.Initialize())
{
    rumble.SetRumble(0.6f, 0.4f);  // low 60%, high 40% (indefinite until stopped)
    // ... after 300ms ...
    rumble.SetRumble(0f, 0f);      // stop
}
```

For timed rumble (e.g. 300ms), call `SetRumble(0.6f, 0.4f)` then `SetRumble(0, 0)` after the desired delay. The ForceFeedbackSharpDx integration handles timing automatically.

## Configuration

`settings.json` in each folder defines devices and events. Customize with the Settings Editor or by editing the file.

### Device types

**XInput (Xbox):**

```json
{
  "XInput": true,
  "UserIndex": -1,
  "RumbleGain": 1.0,
  "StatusEvents": [ ... ]
}
```

**DirectInput (MSFFB2, Saitek):**

```json
{
  "ProductGuid": "001b045e-0000-0000-0000-504944564944",
  "ProductName": "SideWinder Force Feedback 2 Joystick",
  "AutoCenter": true,
  "ForceFeedbackGain": 10000,
  "StatusEvents": [ ... ]
}
```

### StatusEvents

Each event maps to a force/rumble effect. Event keys use `Status.<Field>:<True|False>` or Journal names (e.g. `FSDJump`). See [EVENT_REFERENCE.md](EVENT_REFERENCE.md).

```json
{
  "Event": "Status.Gear:True",
  "ForceFile": "Status_Gear_True.ffe",
  "Duration": 1500,
  "Pulse": false,
  "Pulse_Amount": 0
}
```

- **Event** – Event key (e.g. `Status.Hardpoints:False`, `SupercruiseEntry`).
- **ForceFile** – For XInput: rumble mapping key. For DirectInput: .ffe file in `Forces\`.
- **Duration** – Length of effect in milliseconds.
- **Pulse** / **Pulse_Amount** – (XInput) Pulse vibration on/off `Pulse_Amount` times.

### ForceFileRumble (XInput)

Global rumble strength per effect. Keys match ForceFile names (e.g. `Status_Overheating_True.ffe`). Values 0.0–1.0 for Left and Right motors.

## Creating and Editing Forces (DirectInput)

DirectInput devices use .ffe files from the `Forces` folder. The EDForceFeedback release includes `FFUtils`:

- **fedit.exe** – Create and edit force effects. Save .ffe files into the `Forces` folder.
- **csFeedback.exe**, **FFConst.exe** – Additional configuration.

---

## Building from Source

For developers who want to build from the repository:

**Version:** Bump `Version` in `Directory.Build.props` for releases. See [CHANGELOG.md](CHANGELOG.md) for change history.

**Version check on startup:** EDForceFeedback and TestForceFeedback query the GitHub Releases API at startup. If a newer release exists, a message box suggests downloading it. The check compares the **release tag** (e.g. `v1.0.0`) against the built assembly version. **Zip filenames do not need to match** – use any names you like (e.g. `EDForceFeedback-v1.0.0.zip`). Only the tag on the GitHub release matters. When creating a release, set the tag to match `Version` in `Directory.Build.props` (e.g. `v1.0.0`).

1. Install [.NET SDK](https://dotnet.microsoft.com/download) 6.0 or later.
2. Restore and build:
   - Full solution: `dotnet build EDForceFeedback.sln -c Release`
   - Main app only: `.\build.ps1`
3. Outputs:
   - `EDForceFeedback\bin\Release\net48\EDForceFeedback.exe`
   - `EDForceFeedback\bin\Release\net48\EDForceFeedbackSettingsEditor.exe` (also copied here)
   - `TestForceFeedback\bin\Release\net48\TestForceFeedback.exe`

Packages are stored in `./packages` (see `nuget.config`).

**EliteAPI**: Uses EliteAPI v5.0.8. DLLs are in `lib/` from [EliteAPI releases](https://github.com/Somfic/EliteAPI/releases); no NuGet restore needed for EliteAPI.

**GameInput**: Xbox rumble via Microsoft.GameInput uses the native package `Microsoft.GameInput` (NuGet). The C++/CLI wrapper project `GameInputWrapper` targets x64 and .NET Framework 4.8. Build the solution with `Debug|x64` or `Release|x64` when using GameInput.

### Deployment

**GameInput Redistributable:** For Xbox rumble via GameInput, end users must have the Microsoft GameInput runtime installed. When building in **Release** configuration, the latest `GameInputRedist.msi` from the NuGet package is automatically copied to `EDForceFeedback\bin\Release\net48\redist\`.

**You may need to run this installer** to update to the latest GameInput runtime for GameInput rumble to work. If the runtime is missing or outdated, the app will fall back to raw HID or XInput for rumble.

**Options for end users:**

1. **Option A (recommended):** Include the `redist\GameInputRedist.msi` folder in your distribution. Instruct users to run `GameInputRedist.msi` once if Xbox rumble via GameInput does not work (e.g. controller not detected, no rumble response).
2. **Option B:** Direct users to install it from [aka.ms/gameinput](https://aka.ms/gameinput).

**Files to deploy (when using GameInput):**

- `EDForceFeedback.exe` (or `TestForceFeedback.exe`), `settings.json`, `Forces\`, etc.
- `GameInputWrapper.dll` (C++/CLI mixed assembly; built from the GameInputWrapper project)
- `redist\GameInputRedist.msi` – installer; users may need to run it to enable or update the GameInput runtime

---

## From Original Author

Enjoy! - Bob (CMDR Axe_)
