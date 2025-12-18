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
        private ShutdownCountdownForm? _shutdownForm;
        private bool _shutdownCancelledToday = false;
        private bool _shutdownWarned5m = false;
        private bool _shutdownWarned1m = false;
        private DateTime _lastShutdownCheckDay = DateTime.MinValue;

        public BeepAppContext()
        {
            configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
            LoadConfig();
            
            // Initialize sounds and ensure built-in files exist
            SoundHelper.InitializeIntegratedSounds();

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
                Text = "Beep Scheduler"
            };
            UpdateNextAlarmTooltip();

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
            var oldEnabled = config.ShutdownEnabled;
            var oldTime = config.ShutdownTime;

            using (var form = new SettingsForm(config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    config = form.UpdatedConfig;
                    
                    // Reset shutdown logic if settings were changed
                    if (config.ShutdownEnabled != oldEnabled || config.ShutdownTime != oldTime)
                    {
                        _shutdownCancelledToday = false;
                        _shutdownWarned5m = false;
                        _shutdownWarned1m = false;
                    }

                    SaveConfig();
                    UpdateNextAlarmTooltip();
                    new ToastForm("Paramètres sauvegardés !").Show();
                }
            }
        }

        private void CheckTime(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            
            // Handle Shutdown sequence
            HandleShutdownSequence(now);

            // Update tooltip at the start of every minute
            if (now.Second == 0) UpdateNextAlarmTooltip();

            if (now.Second != 0) return; // Alarms only trigger at :00 seconds

            foreach (var entry in config.Schedules)
            {
                if (!entry.IsEnabled) continue; // Skip disabled alarms
                
                if (!entry.Days.Contains(now.DayOfWeek)) continue;
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

        private void UpdateNextAlarmTooltip()
        {
            DateTime now = DateTime.Now;
            DateTime? nextAlarm = null;

            for (int dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                DateTime day = now.Date.AddDays(dayOffset);
                foreach (var entry in config.Schedules)
                {
                    if (!entry.IsEnabled || !entry.Days.Contains(day.DayOfWeek)) continue;

                    var times = entry.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var timeStr in times)
                    {
                        if (TimeSpan.TryParse(timeStr.Trim(), out var scheduledTime))
                        {
                            DateTime actualTime = day.Add(scheduledTime);
                            if (actualTime > now)
                            {
                                if (nextAlarm == null || actualTime < nextAlarm.Value)
                                {
                                    nextAlarm = actualTime;
                                }
                            }
                        }
                    }
                }
                if (nextAlarm != null) break;
            }

            string info = nextAlarm.HasValue 
                ? $"Prochaine : {nextAlarm.Value:HH:mm} ({GetDayName(nextAlarm.Value.DayOfWeek)})" 
                : "Aucune alarme active";

            string text = $"Beep Scheduler\n{info}\nG: Arrêt | D: Options";
            if (text.Length > 63) text = text.Substring(0, 60) + "..."; // Legacy NotifyIcon limit is often 64
            trayIcon.Text = text;
        }

        private string GetDayName(DayOfWeek d)
        {
            return d switch {
                DayOfWeek.Monday => "Lun",
                DayOfWeek.Tuesday => "Mar",
                DayOfWeek.Wednesday => "Mer",
                DayOfWeek.Thursday => "Jeu",
                DayOfWeek.Friday => "Ven",
                DayOfWeek.Saturday => "Sam",
                DayOfWeek.Sunday => "Dim",
                _ => ""
            };
        }

        private void HandleShutdownSequence(DateTime now)
        {
            if (!config.ShutdownEnabled)
            {
                if (_shutdownForm != null) { _shutdownForm.Close(); _shutdownForm = null; }
                return;
            }

            // Reset flags every new day
            if (now.Date > _lastShutdownCheckDay)
            {
                _shutdownCancelledToday = false;
                _shutdownWarned5m = false;
                _shutdownWarned1m = false;
                _lastShutdownCheckDay = now.Date;
            }

            if (_shutdownCancelledToday) return;

            if (TimeSpan.TryParse(config.ShutdownTime, out var shutdownTime))
            {
                var targetDateTime = now.Date.Add(shutdownTime);
                var diff = targetDateTime - now;
                double secondsLeft = diff.TotalSeconds;

                // 5 minutes (300s) mark
                if (secondsLeft > 60 && secondsLeft <= 300)
                {
                    if (!_shutdownWarned5m && (_shutdownForm == null || _shutdownForm.IsDisposed))
                    {
                        ShowShutdownAlert(targetDateTime);
                    }
                }
                // 1 minute (60s) mark
                else if (secondsLeft > 0 && secondsLeft <= 60)
                {
                    if (!_shutdownWarned1m && (_shutdownForm == null || _shutdownForm.IsDisposed))
                    {
                        ShowShutdownAlert(targetDateTime);
                    }
                    else if (_shutdownForm != null && !_shutdownForm.Focused)
                    {
                        _shutdownForm.Activate(); 
                    }
                }
                else if (secondsLeft <= 0 && secondsLeft > -5)
                {
                    if (!_shutdownCancelledToday) ExecuteShutdown();
                }
                else if (secondsLeft > 300)
                {
                    // Too early, reset flags if they were set (e.g. if user changed clock)
                    _shutdownWarned5m = false;
                    _shutdownWarned1m = false;
                    if (_shutdownForm != null) { _shutdownForm.Close(); _shutdownForm = null; }
                }
            }
        }

        private void ShowShutdownAlert(DateTime target)
        {
            _shutdownForm = new ShutdownCountdownForm(target, config.IsDarkMode);
            _shutdownForm.ResultSelected += (s, e) => {
                var res = ((ShutdownCountdownForm)s!).Result;
                if (res == ShutdownResult.Postpone) 
                {
                    _shutdownCancelledToday = true;
                }
                else if (res == ShutdownResult.Disable)
                {
                    config.ShutdownEnabled = false;
                    _shutdownCancelledToday = true;
                    SaveConfig();
                }
                else if (res == ShutdownResult.Proceed)
                {
                    // User acknowledged but wants to continue
                    var diff = target - DateTime.Now;
                    if (diff.TotalSeconds > 60) _shutdownWarned5m = true;
                    else _shutdownWarned1m = true;
                }
            };
            _shutdownForm.Show();
        }

        private void ExecuteShutdown()
        {
            try
            {
                System.Diagnostics.Process.Start("shutdown", "/s /t 0");
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'extinction : " + ex.Message);
            }
        }

        private void PlaySound(AudioSettings audio)
        {
            try 
            {
                string soundPath = audio.UseCustomSound ? audio.CustomSoundPath : SoundHelper.GetSoundPath(SoundType.StandardBeep);
                
                if (File.Exists(soundPath))
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Open(new Uri(soundPath));
                    _mediaPlayer.Volume = audio.Volume / 100.0;
                    _mediaPlayer.Play();
                    _isMp3Playing = true;
                }
                else
                {
                    // Ultimate fallback
                    new Thread(() => Console.Beep(1000, 500)).Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error playing sound: " + ex.Message);
            }
        }

        private void Exit(object? sender, EventArgs e)
        {
            trayIcon.Visible = true;
            UpdateNextAlarmTooltip();

            // Create Timer
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
