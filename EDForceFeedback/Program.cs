using Journals;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EDForceFeedback
{
    static public class Program
    {
        static private async Task Main(string[] args)
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            var (isOutdated, latestVersion, releaseUrl) = await VersionChecker.CheckForUpdateAsync(currentVersion).ConfigureAwait(false);
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

            // Check if a settings file was specified
            if (args?.Length == 1)
            {
                if (args[0].CompareTo("-h") == 0 || args[0].CompareTo("help") == 0)
                {
                    Console.WriteLine("EDForceFeedBack: EDForceFeedback.exe is a console program that runs during a Elite Dangerous session.");
                    Console.WriteLine("It watches the ED log files and responds to game events by playing a force feedback editor (.ffe) file.");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("EDForceFeedback.exe -h                   Output this help.");
                    Console.WriteLine(@"EDForceFeedback.exe c:\settings.json    Override the default settings file and use this instead.");
                    Console.WriteLine($"EDForceFeedback.exe                     Will default to the settings file {Directory.GetCurrentDirectory()}\\settings.json");
                    return;
                }
                else
                {
                    fileName = args[0];
                }
            }

            Console.WriteLine($"Using settings file: {fileName}");

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"ERROR: Settings file not found: {fileName}");
                Console.WriteLine("Ensure settings.json exists next to the executable, or pass a path: EDForceFeedback.exe <path>");
                return;
            }

            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(fileName));
            if (settings?.Devices == null || settings.Devices.Count == 0)
            {
                Console.WriteLine("ERROR: Settings file has no Devices. Add at least one device (XInput for Xbox, or ProductGuid for joystick).");
                return;
            }

            var client = new Client();

            await client.Initialize(settings).ConfigureAwait(false);

            while (true)
                await Task.Delay(5000).ConfigureAwait(false);
        }
    }
}
