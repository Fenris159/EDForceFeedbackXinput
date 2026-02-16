using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EDForceFeedbackSettingsEditor
{
    /// <summary>Checkbox that displays an X (cross) when checked instead of a checkmark.</summary>
    public class BindCheckBox : CheckBox
    {
        public BindCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
            Appearance = Appearance.Normal;
            FlatStyle = FlatStyle.Flat;
            Text = "";
            Size = new Size(20, 20);
            MinimumSize = new Size(20, 20);
            CheckedChanged += (s, _) => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            var rect = new Rectangle(1, 1, Width - 2, Height - 2);
            using (var borderPen = new Pen(Enabled ? SystemColors.WindowFrame : Color.Gray))
            {
                g.DrawRectangle(borderPen, rect);
            }
            if (Checked)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int m = 4;
                int w = Width - 2 * m;
                int h = Height - 2 * m;
                using (var pen = new Pen(Enabled ? Color.DarkGray : Color.Gray, 2f))
                {
                    g.DrawLine(pen, m, m, m + w, m + h);
                    g.DrawLine(pen, m + w, m, m, m + h);
                }
            }
        }
    }
}
