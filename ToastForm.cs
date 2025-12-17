using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeepScheduler
{
    public class ToastForm : Form
    {
        private System.Windows.Forms.Timer _timer;

        public ToastForm(string message)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(64, 64, 64); // Dark Gray
            this.Size = new Size(250, 50);
            this.StartPosition = FormStartPosition.Manual;
            this.Opacity = 0.9;

            Label lbl = new Label
            {
                Text = message,
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            lbl.Click += (s, e) => this.Close();
            this.Controls.Add(lbl);

            this.Click += (s, e) => this.Close();

            _timer = new System.Windows.Forms.Timer { Interval = 2000 };
            _timer.Tick += (s, e) => this.Close();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(workingArea.Right - this.Width - 10, workingArea.Bottom - this.Height - 10);
            _timer.Start();
        }
    }
}
