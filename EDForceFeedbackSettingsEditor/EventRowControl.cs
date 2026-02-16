using System;
using System.Drawing;
using System.Windows.Forms;

namespace EDForceFeedbackSettingsEditor
{
    public class EventRowControl : UserControl
    {
        private readonly string _eventKey;
        private readonly Label _lblEvent;
        private readonly TrackBar _tbDuration;
        private readonly TextBox _txtDuration;
        private readonly TrackBar _tbLeft;
        private readonly TextBox _txtLeft;
        private readonly BindCheckBox _chkBind;
        private readonly TrackBar _tbRight;
        private readonly TextBox _txtRight;
        private readonly CheckBox _chkPulse;
        private bool _updatingMirror;
        private readonly NumericUpDown _numPulse;
        private readonly Button _btnPreview;

        public event EventHandler PreviewClicked;

        public string EventKey => _eventKey;
        public string ForceFile { get; }
        public int Duration => int.TryParse(_txtDuration.Text, out var v) && v >= 0 ? v : _tbDuration.Value;
        public int LeftValue => _tbLeft.Value;
        public int RightValue => _tbRight.Value;
        public bool Pulse => _chkPulse.Checked;
        public int PulseAmount => (int)_numPulse.Value;

        public EventRowControl(string eventKey, string forceFile, int duration, int left, int right, bool pulse, int pulseAmount)
        {
            _eventKey = eventKey;
            ForceFile = forceFile ?? "";

            _lblEvent = new Label
            {
                Text = EventFriendlyNames.GetFriendlyName(eventKey),
                AutoSize = false,
                Width = 200,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left
            };

            _tbDuration = new TrackBar { Minimum = 0, Maximum = 5000, TickFrequency = 250, Width = 120, Height = 45 };
            _txtDuration = new TextBox { Width = 55, TextAlign = HorizontalAlignment.Right };
            _tbDuration.Value = Math.Max(0, Math.Min(5000, duration));
            _txtDuration.Text = duration.ToString();
            _tbDuration.ValueChanged += (s, _) => { _txtDuration.Text = _tbDuration.Value.ToString(); };
            _txtDuration.TextChanged += (s, _) =>
            {
                if (int.TryParse(_txtDuration.Text, out var v) && v >= 0 && v <= 5000)
                    _tbDuration.Value = v;
            };

            _tbLeft = new TrackBar { Minimum = 0, Maximum = 100, TickFrequency = 10, Width = 120, Height = 45 };
            _txtLeft = new TextBox { Width = 45, TextAlign = HorizontalAlignment.Right };
            _chkBind = new BindCheckBox();
            _tbLeft.Value = Math.Max(0, Math.Min(100, left));
            _txtLeft.Text = _tbLeft.Value.ToString();
            _tbLeft.ValueChanged += (s, _) =>
            {
                _txtLeft.Text = _tbLeft.Value.ToString();
                if (_chkBind.Checked && !_updatingMirror) MirrorLeftToRight();
            };
            _txtLeft.TextChanged += (s, _) =>
            {
                if (int.TryParse(_txtLeft.Text, out var v))
                {
                    _tbLeft.Value = Math.Max(0, Math.Min(100, v));
                    if (_chkBind.Checked && !_updatingMirror) MirrorLeftToRight();
                }
            };
            _chkBind.CheckedChanged += (s, _) =>
            {
                if (_chkBind.Checked && !_updatingMirror) MirrorLeftToRight();
            };

            _tbRight = new TrackBar { Minimum = 0, Maximum = 100, TickFrequency = 10, Width = 120, Height = 45 };
            _txtRight = new TextBox { Width = 45, TextAlign = HorizontalAlignment.Right };
            _tbRight.Value = Math.Max(0, Math.Min(100, right));
            _txtRight.Text = _tbRight.Value.ToString();
            _tbRight.ValueChanged += (s, _) =>
            {
                _txtRight.Text = _tbRight.Value.ToString();
                if (_chkBind.Checked && !_updatingMirror) MirrorRightToLeft();
            };
            _txtRight.TextChanged += (s, _) =>
            {
                if (int.TryParse(_txtRight.Text, out var v))
                {
                    _tbRight.Value = Math.Max(0, Math.Min(100, v));
                    if (_chkBind.Checked && !_updatingMirror) MirrorRightToLeft();
                }
            };

            _chkPulse = new CheckBox { Text = "", AutoSize = false, Checked = pulse, Width = 20, Height = 20 };
            _numPulse = new NumericUpDown { Minimum = 0, Maximum = 20, Width = 50, Value = Math.Max(0, Math.Min(20, pulseAmount)) };
            _btnPreview = new Button { Text = "Preview", Width = 60, Height = 28 };
            _btnPreview.Click += (s, _) => PreviewClicked?.Invoke(this, EventArgs.Empty);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 11,
                RowCount = 1,
                Height = 50,
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

            var leftCellPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };
            leftCellPanel.Controls.Add(_txtLeft);
            leftCellPanel.Controls.Add(_chkBind);

            layout.Controls.Add(_lblEvent, 0, 0);
            layout.Controls.Add(_tbDuration, 1, 0);
            layout.Controls.Add(_txtDuration, 2, 0);
            layout.Controls.Add(_tbLeft, 3, 0);
            layout.Controls.Add(leftCellPanel, 4, 0);
            layout.Controls.Add(_tbRight, 5, 0);
            layout.Controls.Add(_txtRight, 6, 0);
            layout.Controls.Add(_chkPulse, 7, 0);
            layout.Controls.Add(_numPulse, 8, 0);
            layout.Controls.Add(_btnPreview, 9, 0);

            layout.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            Controls.Add(layout);
            Height = 52;
            Margin = new Padding(2);
            MinimumSize = new Size(945, 52);
            Width = 965;
        }

        private void MirrorLeftToRight()
        {
            _updatingMirror = true;
            try
            {
                int v = _tbLeft.Value;
                _tbRight.Value = v;
                _txtRight.Text = v.ToString();
            }
            finally { _updatingMirror = false; }
        }

        private void MirrorRightToLeft()
        {
            _updatingMirror = true;
            try
            {
                int v = _tbRight.Value;
                _tbLeft.Value = v;
                _txtLeft.Text = v.ToString();
            }
            finally { _updatingMirror = false; }
        }
    }
}
