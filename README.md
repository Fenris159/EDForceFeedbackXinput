# EDForceFeedback

Elite Dangerous Force Feedback with a Microsoft Force Feedback 2 Joystick (MSFFB2)

This fork adds **native support for Xbox controller rumble** via Windows XInput (no drivers required) and updates the project for **EliteAPI v5.0.8**, which includes improved journal parsing, Status.json handling, and event support.

## Description

EDForceFeedback.exe is a console program that runs during an Elite Dangerous session. It watches the ED log files and responds to game events by playing a force feedback editor (.ffe) file.

It has been tested with the following devices:

- [Microsoft Xbox One Controller](https://en.wikipedia.org/wiki/Xbox_One_controller) – **Native XInput rumble** (Xbox One, Xbox Elite Series 1/2, Xbox Series X/S) – **No drivers required** on Windows 10/11.
- [Microsoft Sidewinder Force Feedback 2](https://en.wikipedia.org/wiki/Microsoft_SideWinder#Force_Feedback_2) joystick.
- [Saitek Cyborg 3D Force Stick](https://www.yumpu.com/en/document/view/38049421/cyborg-force-manualqxd-saitekcom)

## Xbox Controller Support (Native XInput)

Xbox controllers (Xbox One, Xbox Elite Series 1/2, Xbox Series X/S) are supported via **native Windows XInput** – no custom drivers or XInput modification tools needed.

- **Auto-detection**: Connected Xbox controllers at UserIndex 0–3 are detected automatically; no `settings.json` entry is required.
- **Rumble mapping**: .ffe effect names are mapped to left/right motor rumble patterns (e.g., `Dock.ffe` → strong symmetric, `VibrateSide.ffe` → right-heavy).
- **Optional config**: Add an XInput device in `settings.json` with `"XInput": true`, `"UserIndex": 0`, and `"RumbleGain": 1.0` for custom mappings.
- **Per-event overrides**: Use `"LeftMotor": 0.9`, `"RightMotor": 0.7` in StatusEvents for custom rumble intensity per event.

## Usage

1. Connect your joystick/gamepad and run the EDForceFeedback.exe program.
2. Start Elite Dangerous.
3. In-game events like deploy/retract hardpoints and deploy/retract landing gear will send the configured forces to the joystick.

## Configuration

The `settings.json` file contains the device and the forces that will be played when an event occurs. Multiple different devices can be configured with different effects for each device.

Included in the package are several different examples of different configurations.

### Devices

```json
"Devices": [
    {
      "ProductGuid": "02dd045e-0000-0000-0000-504944564944",
      "ProductName": "Controller (Xbox One For Windows)",
      "StatusEvents": [
        ...
      ]
    }
]
```

Each device and the configured effects for the device has its own section. If the device guid or product name are unknown, you may attempt to use the values the program outputs for the connected devices while it is initializing.

### ProductGuid & ProductName

Both values are searched against the list during initialization. If either is found the device is selected.

### Autocenter

The Microsoft Force Feedback device sometimes turns off the autocenter value after playing an effect. Setting this value to true resets the centering spring.

### ForceFeedbackGain

This value is the strength of the effects. The valid range is 0–10000.

### StatusEvents

Each event can have a different force played for the on or off state. The on/off state for the "Docked" event is the following: `Status.Docked:True` happens when the ship is docked and `Status.Docked:False` happens when the ship takes off.

```json
{
    "Event": "Status.Docked:True",
    "ForceFile": "Dock.ffe",
    "Duration": 2000
}
```

### Event

The `"Event"` field – the [EliteAPI](https://github.com/Somfic/EliteAPI) StatusEvent name and state to respond to.

The format is: `Status.<StatusEventName>:<True or False>`

### ForceFile

The `"ForceFile"` field – the name of the force file (.ffe) to play when this event is detected. The force files can be found under the `.\Forces` folder. There is a force file editor included under the `.\FFUtils` folder. See the *Creating and Editing Forces* section for more information.

### Duration

The `"Duration"` field – this is how long the force will be played. The value is in milliseconds (1 second = 1000 milliseconds). The forces will be stopped after this amount of time even if the .ffe file is configured to play longer.

### Additional Events

The `settings.json` [JSON](https://www.json.org/) file only has a few of the status events defined. Additional status events are provided by the [EliteAPI](https://github.com/Somfic/EliteAPI) and can be added to the `StatusEvents` array in the settings file. During game play, the console window will output the names of the events that were detected. You can use these names to add additional forces.

**EliteAPI dependency**: This project uses EliteAPI v5.0.8 for journal and Status.json parsing. The required DLLs (`EliteAPI.dll`, `Newtonsoft.Json.dll`) are included in the `lib/` folder (from the [EliteAPI GitHub releases](https://github.com/Somfic/EliteAPI/releases)); no NuGet package is required.

## Creating and Editing Forces

On startup all .ffe files in the `.\Forces` folder will be loaded. To create new forces use `fedit.exe` and save the file in the `.\Forces` folder.

### .\FFUtils

These are Microsoft utilities to edit and configure force feedback devices. They may get removed from this location. They can be found in various locations on the internet. They were part of the DirectX DirectInput developer packages.

- csFeedback.exe
- fedit.exe
- FFConst.exe

### fedit.exe

This utility allows you to build your own custom forces. For an example, open the Cargo.ffe file. The forces can overlay and be played before, after, and simultaneously to create complex movement and effects.

## Build

1. Ensure [.NET SDK](https://dotnet.microsoft.com/download) (6.0+) is installed.
2. Restore packages: `dotnet restore EDForceFeedback.sln --packages .\packages`
3. Build main app: `dotnet build EDForceFeedback\EDForceFeedback.csproj -c Release`
4. Or use the build script: `.\build.ps1`

Output: `EDForceFeedback\bin\Release\net48\EDForceFeedback.exe`

Packages are stored in the workspace `./packages` folder (see `nuget.config`).

**TestForceFeedback**: A test harness (`TestForceFeedback.exe`) simulates Elite events without the game. Build with the solution or run `dotnet build TestForceFeedback\TestForceFeedback.csproj -c Release`.

## From Original Author

Enjoy! - Bob (CMDR Axe_)
