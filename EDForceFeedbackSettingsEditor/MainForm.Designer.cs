namespace EDForceFeedbackSettingsEditor
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FlowLayoutPanel flowLayout;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnRestore;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.flowLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnRestore = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // flowLayout
            //
            this.flowLayout = new System.Windows.Forms.FlowLayoutPanel
            {
                AutoScroll = true,
                Dock = System.Windows.Forms.DockStyle.Fill,
                FlowDirection = System.Windows.Forms.FlowDirection.TopDown,
                WrapContents = false,
                Padding = new System.Windows.Forms.Padding(8)
            };
            //
            // btnSave
            //
            this.btnSave = new System.Windows.Forms.Button
            {
                Text = "Save",
                Size = new System.Drawing.Size(90, 32)
            };
            this.btnSave.Click += Save_Click;
            //
            // btnRestore
            //
            this.btnRestore = new System.Windows.Forms.Button
            {
                Text = "Restore Defaults",
                Size = new System.Drawing.Size(120, 32)
            };
            this.btnRestore.Click += RestoreDefaults_Click;
            //
            // bottom panel
            //
            var bottomPanel = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Bottom,
                Height = 48,
                Padding = new System.Windows.Forms.Padding(12, 8, 12, 8)
            };
            this.btnSave.Location = new System.Drawing.Point(12, 8);
            this.btnRestore.Location = new System.Drawing.Point(108, 8);
            bottomPanel.Controls.Add(this.btnSave);
            bottomPanel.Controls.Add(this.btnRestore);
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(924, 501);
            this.Controls.Add(this.flowLayout);
            this.Controls.Add(bottomPanel);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ED Force Feedback - Rumble Settings Editor";
            this.MinimumSize = new System.Drawing.Size(800, 400);
            this.ResumeLayout(false);
        }
    }
}
