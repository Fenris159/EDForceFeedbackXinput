using System.Drawing;
using System.Windows.Forms;

namespace EDForceFeedbackSettingsEditor
{
    /// <summary>Header row that uses the same column layout as EventRowControl, so headers align with columns and scroll together.</summary>
    public class HeaderRowControl : UserControl
    {
        public HeaderRowControl()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 11,
                RowCount = 1,
                Height = 36,
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var font = new Font(Font.FontFamily, 9F, FontStyle.Bold);

            var lblEvent = new Label { Text = "Event", Font = font, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var lblDuration = new Label { Text = "Duration (ms)", Font = font, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var lblDurationSpacer = new Label { Text = "", Dock = DockStyle.Fill };
            var lblLeft = new Label { Text = "Left %", Font = font, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var lblBindLR = new Label { Text = "Bind L/R", Font = font, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var lblRight = new Label { Text = "Right %", Font = font, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var lblRightSpacer = new Label { Text = "", Dock = DockStyle.Fill };
            var lblPulseOn = new Label { Text = "Pulse On", Font = font, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var lblPulseCount = new Label { Text = "Pulse Count", Font = font, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var lblPreview = new Label { Text = "Preview", Font = font, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

            layout.Controls.Add(lblEvent, 0, 0);
            layout.Controls.Add(lblDuration, 1, 0);
            layout.Controls.Add(lblDurationSpacer, 2, 0);
            layout.Controls.Add(lblLeft, 3, 0);
            layout.Controls.Add(lblBindLR, 4, 0);
            layout.Controls.Add(lblRight, 5, 0);
            layout.Controls.Add(lblRightSpacer, 6, 0);
            layout.Controls.Add(lblPulseOn, 7, 0);
            layout.Controls.Add(lblPulseCount, 8, 0);
            layout.Controls.Add(lblPreview, 9, 0);

            layout.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            BackColor = SystemColors.ControlLight;
            Controls.Add(layout);
            Height = 38;
            Margin = new Padding(2);
            MinimumSize = new Size(945, 38);
            Width = 965;
        }
    }
}
