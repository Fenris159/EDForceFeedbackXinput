using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using ForceFeedbackSharpDx;

namespace EDForceFeedbackSettingsEditor
{
    public partial class MainForm : Form
    {
        private readonly string _settingsPath;
        private SettingsModel _settings;
        private readonly List<EventRowControl> _rows = new List<EventRowControl>();
        private XInputRumbleDevice _rumbleDevice;
        private bool _previewUnavailable;

        private const int ContentWidth = 965;
        private const int HeaderHeight = 38;
        private const int RowHeight = 52;
        private const int CategorySectionHeight = 34;
        private const int FlowPadding = 16;
        private const int BottomPanelHeight = 48;
        private int _categorySectionCount;

        public MainForm(string settingsPath)
        {
            _settingsPath = settingsPath;
            InitializeComponent();
            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;
            LoadSettings();
            InitRumbleDevice();
            BuildEventRows();
        }

        private void InitRumbleDevice()
        {
            try
            {
                for (int i = 0; i <= 3; i++)
                {
                    var dev = new XInputRumbleDevice(i);
                    if (dev.IsConnected)
                    {
                        _rumbleDevice = dev;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                _rumbleDevice = null;
                _previewUnavailable = true;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _rumbleDevice?.Dispose();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            SizeToFitContent();
        }

        private void SizeToFitContent()
        {
            int rowCount = _rows.Count;
            int preferredWidth = ContentWidth + FlowPadding + SystemInformation.VerticalScrollBarWidth;
            int preferredHeight = HeaderHeight + (_categorySectionCount * CategorySectionHeight) + (rowCount > 0 ? rowCount * RowHeight : RowHeight) + FlowPadding + BottomPanelHeight;

            var screen = Screen.FromControl(this).WorkingArea;
            int maxHeight = (int)(screen.Height * 0.5);
            int maxWidth = (int)(screen.Width * 0.95);

            int width = Math.Min(Math.Max(preferredWidth, MinimumSize.Width), maxWidth);
            int height = Math.Min(Math.Max(preferredHeight, MinimumSize.Height), maxHeight);

            ClientSize = new Size(width, height);
        }

        private void LoadSettings()
        {
            string json = File.ReadAllText(_settingsPath);
            _settings = JsonConvert.DeserializeObject<SettingsModel>(json);
            if (_settings?.Devices == null || _settings.Devices.Count == 0)
            {
                MessageBox.Show("No devices or events found in settings.", "Invalid Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
            }
        }

        private void BuildEventRows()
        {
            var firstDevice = _settings.Devices.FirstOrDefault(d => d.StatusEvents?.Count > 0);
            if (firstDevice?.StatusEvents == null) return;

            flowLayout.Controls.Clear();
            _rows.Clear();

            flowLayout.Controls.Add(new HeaderRowControl());

            var grouped = EventCategories.GroupEvents(firstDevice.StatusEvents);
            _categorySectionCount = grouped.Count;
            foreach (var (categoryName, categoryEvents) in grouped)
            {
                flowLayout.Controls.Add(new CategorySectionControl(categoryName));
                foreach (var evt in categoryEvents)
                {
                    string forceFile = evt.ForceFile ?? "";
                    if (!_settings.ForceFileRumble.TryGetValue(forceFile, out var rumble))
                        rumble = new ForceFileRumbleEntry { Left = 0.5, Right = 0.5 };

                    var row = new EventRowControl(
                        evt.Event,
                        evt.ForceFile,
                        evt.Duration,
                        (int)(rumble.Left * 100),
                        (int)(rumble.Right * 100),
                        evt.Pulse,
                        evt.PulseAmount);
                    row.PreviewClicked += Row_PreviewClicked;
                    _rows.Add(row);
                    flowLayout.Controls.Add(row);
                }
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (var row in _rows)
                {
                    string forceFile = row.ForceFile;
                    if (!_settings.ForceFileRumble.ContainsKey(forceFile))
                        _settings.ForceFileRumble[forceFile] = new ForceFileRumbleEntry();
                    _settings.ForceFileRumble[forceFile].Left = row.LeftValue / 100.0;
                    _settings.ForceFileRumble[forceFile].Right = row.RightValue / 100.0;

                    foreach (var device in _settings.Devices)
                    {
                        var evt = device.StatusEvents?.FirstOrDefault(s => s.Event == row.EventKey);
                        if (evt != null)
                        {
                            evt.Duration = row.Duration;
                            evt.Pulse = row.Pulse;
                            evt.PulseAmount = row.PulseAmount;
                        }
                    }
                }

                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);
                MessageBox.Show("Settings saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Row_PreviewClicked(object sender, EventArgs e)
        {
            if (_previewUnavailable)
            {
                MessageBox.Show("Preview is unavailable. Run the editor from EDForceFeedbackSettingsEditor\\bin\\Debug\\net48 (or ensure all dependencies are present).", "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_rumbleDevice == null || !_rumbleDevice.IsConnected)
            {
                MessageBox.Show("No Xbox controller detected. Connect an Xbox controller (UserIndex 0-3) to preview vibration.", "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var row = (EventRowControl)sender;
            double left = row.LeftValue / 100.0;
            double right = row.RightValue / 100.0;
            int duration = row.Duration > 0 ? row.Duration : 250;
            _rumbleDevice.PlayFileEffect(row.ForceFile, duration, left, right, row.Pulse, row.PulseAmount);
        }

        private void RestoreDefaults_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reload settings from file? Any unsaved changes will be lost.", "Restore Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            LoadSettings();
            BuildEventRows();
            MessageBox.Show("Settings reloaded from file.", "Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
