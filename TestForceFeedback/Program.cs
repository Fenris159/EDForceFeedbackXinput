using Journals;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestForceFeedback
{
    internal static class Program
    {
        // Sample event JSON for simulating without the game (EliteAPI v5 format).
        private static readonly string JsonDocked = "{\"timestamp\":\"2025-01-01T12:00:00Z\",\"event\":\"Docked\",\"StationName\":\"Test\",\"StationType\":\"Coriolis Starport\",\"StarSystem\":\"Test\",\"SystemAddress\":0,\"MarketID\":0,\"StationFaction\":{\"Name\":\"\",\"FactionState\":\"\"},\"StationGovernment\":null,\"StationAllegiance\":\"\",\"StationServices\":[],\"StationEconomy\":null,\"StationEconomies\":[],\"DistFromStarLS\":0,\"ActiveFine\":false,\"Taxi\":false,\"Multicrew\":false,\"LandingPads\":{\"Small\":0,\"Medium\":0,\"Large\":0},\"Wanted\":false,\"CockpitBreach\":false,\"StationState\":\"\"}";
        private static readonly string JsonUndocked = "{\"timestamp\":\"2025-01-01T12:00:00Z\",\"event\":\"Undocked\",\"StationName\":\"Test\",\"MarketID\":0}";
        private static readonly string JsonTouchdown = "{\"timestamp\":\"2025-01-01T12:00:00Z\",\"event\":\"Touchdown\",\"PlayerControlled\":true,\"Latitude\":0,\"Longitude\":0,\"NearestDestination\":\"\"}";
        private static readonly string JsonLiftoff = "{\"timestamp\":\"2025-01-01T12:00:00Z\",\"event\":\"Liftoff\",\"PlayerControlled\":true,\"NearestDestination\":\"\"}";

        static void Main(string[] args)
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            var (isOutdated, latestVersion, releaseUrl) = VersionChecker.CheckForUpdateAsync(currentVersion).GetAwaiter().GetResult();
            if (isOutdated)
            {
                var result = MessageBox.Show(
                    $"A newer version ({latestVersion}) is available.\n\nClick Yes to open the download page and exit, or No to proceed with your current version.",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    try { Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true }); } catch { }
                    return;
                }
            }

            var fileName = $"{Directory.GetCurrentDirectory()}\\settings.json";

            if (args?.Length == 1)
            {
                if (args[0].CompareTo("-h") == 0 || args[0].CompareTo("help") == 0)
                {
                    Console.WriteLine("TestForceFeedback: Test harness for ED force feedback. Simulates Elite events without the game.");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  TestForceFeedback.exe -h                   Output this help.");
                    Console.WriteLine(@"  TestForceFeedback.exe <path>             Override the default settings file (e.g. C:\path\to\settings.json).");
                    Console.WriteLine($"  TestForceFeedback.exe                     Default settings: {Directory.GetCurrentDirectory()}\\settings.json");
                    return;
                }
                fileName = args[0];
            }

            Console.WriteLine($"Using settings file: {fileName}");

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"ERROR: Settings file not found: {fileName}");
                Console.WriteLine("Ensure settings.json exists next to the executable, or pass a path: TestForceFeedback.exe <path>");
                return;
            }

            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(fileName));
            if (settings == null || settings.Devices == null || settings.Devices.Count == 0)
            {
                Console.WriteLine("ERROR: Settings file invalid or has no Devices. Add at least one device (XInput for Xbox, or ProductGuid for joystick).");
                return;
            }

            // Prefer GameInput rumble, then HID, then XInput
            ForceFeedbackGameInput.XInputGameInputBackend.RegisterAsPreferred();

            // Use EliteAPI v5 with null dirs = no file watchers; we drive events via SimulateEvent.
            var testApi = new EliteAPI.EliteDangerousApi(null, null);
            var client = new Client();

            client.Initialize(settings, testApi).GetAwaiter().GetResult();

            void ReleaseDevices()
            {
                try { client?.Dispose(); } catch { }
            }
            AppDomain.CurrentDomain.ProcessExit += (s, e) => ReleaseDevices();
            Console.CancelKeyPress += (s, e) => { ReleaseDevices(); };

            Console.WriteLine("Event simulation (no game). Press a number key to fire an event, 0 to quit.");
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("0: Quit | s: Stop rumble (emergency)");
                Console.WriteLine("1: Docked (true)");
                Console.WriteLine("2: Undocked (false)");
                Console.WriteLine("3: Landed (true) - Touchdown");
                Console.WriteLine("4: Landed (false) - Liftoff");
                Console.WriteLine("5: Gear (deployed)");
                Console.WriteLine("6: Gear (retracted)");
                Console.WriteLine("7: Hardpoints (deployed)");
                Console.WriteLine("8: Hardpoints (retracted)");
                Console.WriteLine("9: Overheating (true)");
                Console.WriteLine("a: Overheating (false)");

                var key = Console.ReadKey();
                if (key.KeyChar == '0')
                {
                    ReleaseDevices();
                    break;
                }
                if (key.KeyChar == 's' || key.KeyChar == 'S')
                {
                    client.StopAllRumble();
                    Console.WriteLine(" Rumble stopped.");
                    continue;
                }

                switch (key.KeyChar)
                {
                    case '1':
                        client.SimulateEvent(JsonDocked);
                        break;
                    case '2':
                        client.SimulateEvent(JsonUndocked);
                        break;
                    case '3':
                        client.SimulateEvent(JsonTouchdown);
                        break;
                    case '4':
                        client.SimulateEvent(JsonLiftoff);
                        break;
                    case '5':
                        client.TriggerEvent("Status.Gear:True");
                        break;
                    case '6':
                        client.TriggerEvent("Status.Gear:False");
                        break;
                    case '7':
                        client.TriggerEvent("Status.Hardpoints:True");
                        break;
                    case '8':
                        client.TriggerEvent("Status.Hardpoints:False");
                        break;
                    case '9':
                        client.TriggerEvent("Status.Overheating:True");
                        break;
                    case 'a':
                    case 'A':
                        client.TriggerEvent("Status.Overheating:False");
                        break;
                }
            }
        }
    }
}
