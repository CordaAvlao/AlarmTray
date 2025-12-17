using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace BeepScheduler
{
    // Custom ApplicationContext to run without a main form
    public class BeepAppContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private System.Windows.Forms.Timer timer;
        private AppConfig config;
        private string configPath;
        private System.Windows.Media.MediaPlayer _mediaPlayer;
        private bool _isMp3Playing = false;

        public BeepAppContext()
        {
            configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
            LoadConfig();
            
            // Initialize MediaPlayer on Main Thread
            _mediaPlayer = new System.Windows.Media.MediaPlayer();
            _mediaPlayer.MediaEnded += (s, e) => _isMp3Playing = false;

            // Initialize Tray Icon
            Icon appIcon;
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "bell_icon.png");
                if (File.Exists(iconPath))
                {
                    using (Bitmap bmp = new Bitmap(iconPath))
                    {
                        appIcon = Icon.FromHandle(bmp.GetHicon());
                    }
                }
                else
                {
                    appIcon = SystemIcons.Application;
                }
            }
            catch
            {
                appIcon = SystemIcons.Application;
            }

            trayIcon = new NotifyIcon()
            {
                Icon = appIcon,
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "Beep Scheduler\nClic G: Arrêt\nClic D: Paramètres"
            };

            // Add Mouse Click Handler
            trayIcon.MouseClick += TrayIcon_MouseClick;

            // Add menu items
            trayIcon.ContextMenuStrip.Items.Add("Paramètres", null, OpenSettings);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("Quitter", null, Exit);

            // Start timer (check every second) - Uses UI Thread
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += CheckTime;
            timer.Start();
        }

        private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_isMp3Playing)
                {
                    _mediaPlayer.Stop();
                    _isMp3Playing = false;
                    // Optional: Show feedback
                    // trayIcon.ShowBalloonTip(1000, "Beep Scheduler", "Alarme arrêtée.", ToolTipIcon.Info);
                }
            }
        }

        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch
                {
                    config = new AppConfig();
                }
            }
            else
            {
                config = new AppConfig();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la sauvegarde de la configuration : " + ex.Message);
            }
        }

        private void OpenSettings(object? sender, EventArgs e)
        {
            using (var form = new SettingsForm(config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    config = form.UpdatedConfig;
                    SaveConfig();
                    new ToastForm("Paramètres sauvegardés !").Show();
                }
            }
        }

        private void CheckTime(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            if (now.Second != 0) return; // Only trigger at :00 seconds

            foreach (var entry in config.Schedules)
            {
                // Check if today is in the selected days
                if (!entry.Days.Contains(now.DayOfWeek)) continue;

                // Split times by semicolon
                var times = entry.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var timeStr in times)
                {
                    if (TimeSpan.TryParse(timeStr.Trim(), out var scheduledTime))
                    {
                        if (now.Hour == scheduledTime.Hours && now.Minute == scheduledTime.Minutes)
                        {
                            PlaySound(entry.Audio);
                        }
                    }
                }
            }
        }

        private void PlaySound(AudioSettings audio)
        {
            if (audio.UseCustomSound && File.Exists(audio.CustomSoundPath))
            {
                try 
                {
                    _mediaPlayer.Stop(); // Stop any previous sound
                    _mediaPlayer.Open(new Uri(audio.CustomSoundPath));
                    _mediaPlayer.Volume = audio.Volume / 100.0;
                    _mediaPlayer.Play();
                    _isMp3Playing = true;
                }
                catch (Exception ex)
                {
                    // Fallback or log
                    Console.WriteLine("Error playing sound: " + ex.Message);
                }
            }
            else
            {
                // Beep is blocking, run in thread to avoid freezing UI
                new Thread(() =>
                {
                    Console.Beep(5000, 500);
                    Thread.Sleep(50);
                    Console.Beep(3000, 500);
                }).Start();
            }
        }

        private void Exit(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BeepAppContext());
        }
    }
}
