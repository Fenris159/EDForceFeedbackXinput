using Journals;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestForceFeedback
{
    internal static class Program
    {
        // Sample event JSON for simulating without the game (EliteAPI v5 format).
        private static readonly string JsonDocked = "{\"timestamp\":\"2025-01-01T12:00:00Z\",\"event\":\"Docked\",\"StationName\":\"Test\",\"StationType\":\"Coriolis Starport\",\"StarSystem\":\"Test\",\"SystemAddress\":0,\"MarketID\":0,\"StationFaction\":{\"Name\":\"\",\"FactionState\":\"\"},\"StationGovernment\":null,\"StationAllegiance\":\"\",\"StationServices\":[],\"StationEconomy\":null,\"StationEconomies\":[],\"DistFromStarLS\":0,\"ActiveFine\":false,\"Taxi\":false,\"Multicrew\":false,\"LandingPads\":{\"Small\":0,\"Medium\":0,\"Large\":0},\"Wanted\":false,\"CockpitBreach\":false,\"StationState\":\"\"}";
        private static readonly string JsonUndocked = "{\"timestamp\":\"2025-01-01T12:00:00Z\",\"event\":\"Undocked\",\"StationName\":\"Test\",\"MarketID\":0}";
        private static readonly string JsonTouchdown = "{\"timestamp\":\"2025-01-01T12:00:00Z\",\"event\":\"Touchdown\",\"PlayerControlled\":true,\"Latitude\":0,\"Longitude\":0,\"NearestDestination\":\"\"}";
        private static readonly string JsonLiftoff = "{\"timestamp\":\"2025-01-01T12:00:00Z\",\"event\":\"Liftoff\",\"PlayerControlled\":true,\"NearestDestination\":\"\"}";

        private static string StatusJson(long flags)
        {
            return $"{{\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",\"event\":\"Status\",\"Flags\":{flags},\"Pips\":[4,4,2],\"FireGroup\":0,\"GuiFocus\":0}}";
        }

        static void Main(string[] args)
        {
            var fileName = $"{Directory.GetCurrentDirectory()}\\settings.json";

            if (args?.Length == 1)
            {
                if (args[0].CompareTo("-h") == 0 || args[0].CompareTo("help") == 0)
                {
                    Console.WriteLine("TestForceFeedback: Test harness for ED force feedback. Simulates Elite events without the game.");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  TestForceFeedback.exe -h                   Output this help.");
                    Console.WriteLine(@"  TestForceFeedback.exe c:\settings.json    Override the default settings file.");
                    Console.WriteLine($"  TestForceFeedback.exe                     Default settings: {Directory.GetCurrentDirectory()}\\settings.json");
                    return;
                }
                fileName = args[0];
            }

            Console.WriteLine($"Using settings file: {fileName}");

            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(fileName));
            if (settings == null)
            {
                Console.WriteLine("Failed to load settings.");
                return;
            }

            // Use EliteAPI v5 with null dirs = no file watchers; we drive events via SimulateEvent.
            var testApi = new EliteAPI.EliteDangerousApi(null, null);
            var client = new Client();

            client.Initialize(settings, testApi).GetAwaiter().GetResult();

            Console.WriteLine("Event simulation (no game). Press a number key to fire an event, 0 to quit.");
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("0: Quit");
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
                if (key.KeyChar == '0') break;

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
                        client.SimulateEvent(StatusJson(0));
                        client.SimulateEvent(StatusJson(4)); // Gear
                        break;
                    case '6':
                        client.SimulateEvent(StatusJson(4));
                        client.SimulateEvent(StatusJson(0));
                        break;
                    case '7':
                        client.SimulateEvent(StatusJson(0));
                        client.SimulateEvent(StatusJson(64)); // Hardpoints
                        break;
                    case '8':
                        client.SimulateEvent(StatusJson(64));
                        client.SimulateEvent(StatusJson(0));
                        break;
                    case '9':
                        client.SimulateEvent(StatusJson(0));
                        client.SimulateEvent(StatusJson(1048576)); // Overheating
                        break;
                    case 'a':
                    case 'A':
                        client.SimulateEvent(StatusJson(1048576));
                        client.SimulateEvent(StatusJson(0));
                        break;
                }
            }
        }
    }
}
