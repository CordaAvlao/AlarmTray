using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeepScheduler
{
    public enum ShutdownResult { None, Proceed, Postpone, Disable }

    public class ShutdownCountdownForm : Form
    {
        private Label _lblMessage;
        private Label _lblTimer;
        private Button _btnOk;
        private Button _btnPostpone;
        private Button _btnDisable;
        private System.Windows.Forms.Timer _uiTimer;
        private DateTime _targetTime;
        private bool _isDarkMode;

        public ShutdownResult Result { get; private set; } = ShutdownResult.None;
        public event EventHandler ResultSelected;

        public ShutdownCountdownForm(DateTime targetTime, bool isDarkMode)
        {
            _targetTime = targetTime;
            _isDarkMode = isDarkMode;
            InitializeComponent();
            ThemeHelper.ApplyTheme(this, isDarkMode);

            _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uiTimer.Tick += (s, e) => UpdateCountdown();
            _uiTimer.Start();
            UpdateCountdown();
        }

        private void InitializeComponent()
        {
            this.Text = "Alerte Extinction Windows";
            this.Size = new Size(400, 220);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = true;

            _lblMessage = new Label
            {
                Text = "Windows va s'éteindre bientôt !",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            _lblTimer = new Label
            {
                Text = "00:00",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60,
                ForeColor = Color.Red
            };

            _btnOk = new Button
            {
                Text = "OK (Continuer)",
                Size = new Size(110, 40),
                Location = new Point(15, 130),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += (s, e) => SelectResult(ShutdownResult.Proceed);

            _btnPostpone = new Button
            {
                Text = "Demain",
                Size = new Size(110, 40),
                Location = new Point(135, 130),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Orange,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _btnPostpone.FlatAppearance.BorderSize = 0;
            _btnPostpone.Click += (s, e) => SelectResult(ShutdownResult.Postpone);

            _btnDisable = new Button
            {
                Text = "Désactiver",
                Size = new Size(110, 40),
                Location = new Point(255, 130),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.SlateGray,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _btnDisable.FlatAppearance.BorderSize = 0;
            _btnDisable.Click += (s, e) => SelectResult(ShutdownResult.Disable);

            this.Controls.Add(_btnOk);
            this.Controls.Add(_btnPostpone);
            this.Controls.Add(_btnDisable);
            this.Controls.Add(_lblTimer);
            this.Controls.Add(_lblMessage);
        }

        private void SelectResult(ShutdownResult res)
        {
            Result = res;
            ResultSelected?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void UpdateCountdown()
        {
            var remaining = _targetTime - DateTime.Now;
            if (remaining.TotalSeconds <= 0)
            {
                _lblTimer.Text = "00:00";
                _uiTimer.Stop();
            }
            else
            {
                _lblTimer.Text = string.Format("{0:D2}:{1:D2}", remaining.Minutes, remaining.Seconds);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _uiTimer?.Stop();
            base.OnFormClosing(e);
        }
    }
}
