using System;
using System.IO;
using System.Windows.Forms;

namespace EDForceFeedbackSettingsEditor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string settingsPath = Path.Combine(baseDir, "settings.json");

            if (!File.Exists(settingsPath))
            {
                MessageBox.Show(
                    "settings.json was not found in the same folder as this program.\n\n" +
                    "Please run EDForceFeedbackSettingsEditor.exe from the same folder as your settings.json " +
                    "(e.g. next to EDForceFeedback.exe or TestForceFeedback.exe).",
                    "Settings Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Application.Run(new MainForm(settingsPath));
        }
    }
}
