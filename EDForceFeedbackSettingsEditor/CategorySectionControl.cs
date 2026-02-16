using System.Drawing;
using System.Windows.Forms;

namespace EDForceFeedbackSettingsEditor
{
    /// <summary>Section header for event categories in the settings grid.</summary>
    public class CategorySectionControl : UserControl
    {
        public CategorySectionControl(string categoryName)
        {
            var lbl = new Label
            {
                Text = categoryName,
                Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
                ForeColor = SystemColors.Highlight,
                AutoSize = false,
                Height = 26,
                Width = 965,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };
            lbl.Dock = DockStyle.Fill;
            BackColor = SystemColors.Control;
            Padding = new Padding(2, 6, 2, 2);
            Controls.Add(lbl);
            Height = 34;
            Margin = new Padding(2, 8, 2, 2);
            MinimumSize = new Size(945, 34);
        }
    }
}
