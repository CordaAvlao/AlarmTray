using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

namespace BeepScheduler
{
    public class SettingsForm : Form
    {
        private AppConfig _config;
        private DataGridView _gridSchedule;
        private Button _btnSave;
        private Button _btnCancel;
        private Button _btnAdd;
        private Button _btnExport;
        private Button _btnImport;
        private Button _btnAbout;
        private CheckBox _chkStartWithWindows;
        private CheckBox _chkDarkMode;
        private CheckBox _chkShutdown;
        private TextBox _txtShutdownTime;

        public AppConfig UpdatedConfig => _config;

        public SettingsForm(AppConfig config)
        {
            _config = config;
            InitializeComponent();
            LoadConfigData();
            ApplyCurrentTheme();
        }

        private void ApplyCurrentTheme()
        {
            ThemeHelper.ApplyTheme(this, _config.IsDarkMode);
            if (_chkDarkMode != null) _chkDarkMode.Checked = _config.IsDarkMode;
        }

        private void InitializeComponent()
        {
            // --- Form Styling ---
            this.Text = "Paramètres Beep Scheduler";
            this.Size = new Size(820, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.WhiteSmoke;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // --- Main Layout Panel ---
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(15)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // Middle bar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // Shutdown bar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));

            // --- Grid Styling ---
            _gridSchedule = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ShowCellToolTips = true
            };

            // Grid Visuals
            _gridSchedule.EnableHeadersVisualStyles = false;
            _gridSchedule.ColumnHeadersHeight = 45;
            _gridSchedule.RowTemplate.Height = 50;

            // --- Columns ---
            _gridSchedule.Columns.Clear();
            
            var colEnabled = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "IsEnabled",
                HeaderText = "Actif",
                Width = 45,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                FlatStyle = FlatStyle.Flat
            };

            var colDelete = new DataGridViewButtonColumn
            {
                HeaderText = "",
                Text = "X",
                UseColumnTextForButtonValue = true,
                Width = 25,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                FlatStyle = FlatStyle.Flat
            };
            colDelete.DefaultCellStyle.ForeColor = Color.Red;
            colDelete.DefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            colDelete.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            var colDays = new DataGridViewTextBoxColumn
            {
                HeaderText = "Jours Actifs",
                Width = 320,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly = true
            };

            var colTimes = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Times",
                HeaderText = "Heures (HH:mm; ...)",
                Width = 200
            };

            var colAudio = new DataGridViewButtonColumn
            {
                HeaderText = "Son",
                Text = "Configurer...",
                UseColumnTextForButtonValue = true,
                Width = 100
            };

            _gridSchedule.Columns.AddRange(new DataGridViewColumn[] { colEnabled, colDelete, colDays, colTimes, colAudio });
            
            _gridSchedule.CellContentClick += Grid_CellContentClick;
            _gridSchedule.CellPainting += Grid_CellPainting;
            _gridSchedule.CellMouseClick += Grid_CellMouseClick;
            _gridSchedule.CellValidating += Grid_CellValidating;
            _gridSchedule.CellToolTipTextNeeded += (s, e) => {
                if (e.RowIndex >= 0)
                {
                    if (e.ColumnIndex == 0) e.ToolTipText = "Activer/Désactiver l'alarme";
                    else if (e.ColumnIndex == 1) e.ToolTipText = "Supprimer cette alarme";
                }
            };

            // --- Middle Panel (Export/Import/Theme) ---
            var pnlMiddle = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 5, 0, 0) };
            
            _btnExport = new Button { Text = "Exporter", Width = 100, Height = 30 };
            _btnExport.Click += (s, e) => ExportConfig();
            
            _btnImport = new Button { Text = "Importer", Width = 100, Height = 30 };
            _btnImport.Click += (s, e) => ImportConfig();

            _chkDarkMode = new CheckBox { Text = "Thème Sombre", AutoSize = true, Padding = new Padding(10, 5, 0, 0) };
            _chkDarkMode.CheckedChanged += (s, e) => {
                _config.IsDarkMode = _chkDarkMode.Checked;
                ApplyCurrentTheme();
            };

            _btnAbout = new Button { Text = "?", Width = 30, Height = 30 };
            _btnAbout.Click += (s, e) => new AboutForm(_config.IsDarkMode).ShowDialog();

            pnlMiddle.Controls.AddRange(new Control[] { _btnExport, _btnImport, _chkDarkMode, _btnAbout });

            // --- Shutdown Panel ---
            var pnlShutdown = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            
            _chkShutdown = new CheckBox { Text = "Activer l'extinction automatique à :", AutoSize = true, Padding = new Padding(0, 5, 0, 0) };
            _chkShutdown.Checked = _config.ShutdownEnabled;
            
            _txtShutdownTime = new TextBox { Text = _config.ShutdownTime, Width = 60, Height = 25 };
            
            var lblShutdownNote = new Label { Text = "(Prévient 5 et 1 minute avant)", AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Italic), Padding = new Padding(5, 8, 0, 0) };
            
            pnlShutdown.Controls.AddRange(new Control[] { _chkShutdown, _txtShutdownTime, lblShutdownNote });

            // --- Bottom Panel ---
            var pnlBottom = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };

            _btnAdd = new Button
            {
                Text = "+",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(0, 5),
                BackColor = Color.MediumSeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAdd.FlatAppearance.BorderSize = 0;
            _btnAdd.Click += (s, e) => {
                var list = (System.ComponentModel.BindingList<DaySchedule>)_gridSchedule.DataSource;
                list.Add(new DaySchedule { Times = "08:00", Days = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, Audio = new AudioSettings() });
            };

            _chkStartWithWindows = new CheckBox { Text = "Démarrer avec Windows", Location = new Point(55, 15), AutoSize = true, Font = new Font("Segoe UI", 9.5F) };

            _btnSave = new Button { Text = "Sauvegarder", DialogResult = DialogResult.OK, Location = new Point(530, 10), Size = new Size(110, 35), BackColor = Color.DodgerBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += SaveChanges;

            _btnCancel = new Button { Text = "Annuler", DialogResult = DialogResult.Cancel, Location = new Point(650, 10), Size = new Size(110, 35), Cursor = Cursors.Hand };

            pnlBottom.Controls.AddRange(new Control[] { _btnAdd, _chkStartWithWindows, _btnSave, _btnCancel });

            mainLayout.Controls.Add(_gridSchedule, 0, 0);
            mainLayout.Controls.Add(pnlMiddle, 0, 1);
            mainLayout.Controls.Add(pnlShutdown, 0, 2);
            mainLayout.Controls.Add(pnlBottom, 0, 3);

            this.Controls.Add(mainLayout);
        }

        private void ExportConfig()
        {
            using (var sfd = new System.Windows.Forms.SaveFileDialog { Filter = "JSON Files|*.json", FileName = "BeepConfig_Export.json" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                        string json = System.Text.Json.JsonSerializer.Serialize(_config, options);
                        System.IO.File.WriteAllText(sfd.FileName, json);
                        MessageBox.Show("Configuration exportée !");
                    }
                    catch (Exception ex) { MessageBox.Show("Erreur export : " + ex.Message); }
                }
            }
        }

        private void ImportConfig()
        {
            using (var ofd = new System.Windows.Forms.OpenFileDialog { Filter = "JSON Files|*.json" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText(ofd.FileName);
                        var imported = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);
                        if (imported != null)
                        {
                            _config = imported;
                            LoadConfigData();
                            ApplyCurrentTheme();
                            MessageBox.Show("Configuration importée !");
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Erreur import : " + ex.Message); }
                }
            }
        }

        private void LoadConfigData()
        {
            var bindingList = new System.ComponentModel.BindingList<DaySchedule>(_config.Schedules.ToList());
            _gridSchedule.DataSource = bindingList;

            // Check Registry for Auto-Start
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("BeepScheduler");
                        if (val != null && val.ToString() == Application.ExecutablePath)
                        {
                            _chkStartWithWindows.Checked = true;
                        }
                    }
                }
            }
            catch { }
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == 1) // Delete
            {
                if (_gridSchedule.Rows[e.RowIndex].DataBoundItem is DaySchedule schedule)
                {
                    var list = (System.ComponentModel.BindingList<DaySchedule>)_gridSchedule.DataSource;
                    list.Remove(schedule);
                }
                return;
            }
            if (e.ColumnIndex == 4) // Audio
            {
                if (_gridSchedule.Rows[e.RowIndex].DataBoundItem is DaySchedule schedule)
                {
                    using (var audioForm = new AudioConfigForm(schedule.Audio, _config.IsDarkMode))
                    {
                        if (audioForm.ShowDialog() == DialogResult.OK)
                        {
                            schedule.Audio = audioForm.Settings;
                        }
                    }
                }
            }
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 2) // Days circles column
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                if (!(_gridSchedule.Rows[e.RowIndex].DataBoundItem is DaySchedule schedule)) return;

                string[] letters = { "L", "M", "M", "J", "V", "S", "D" };
                DayOfWeek[] days = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
                int circleSize = 24, spacing = 6;
                int totalWidth = (circleSize * 7) + (spacing * 6);
                int startX = e.CellBounds.X + (e.CellBounds.Width - totalWidth) / 2;
                int startY = e.CellBounds.Y + (e.CellBounds.Height - circleSize) / 2;

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var brushActive = new SolidBrush(Color.DodgerBlue))
                using (var brushInactive = new SolidBrush(_config.IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.WhiteSmoke))
                using (var penInactive = new Pen(_config.IsDarkMode ? Color.FromArgb(80, 80, 80) : Color.Silver))
                using (var textBrushActive = new SolidBrush(Color.White))
                using (var textBrushInactive = new SolidBrush(_config.IsDarkMode ? Color.Gray : Color.Gray))
                {
                    for (int i = 0; i < 7; i++)
                    {
                        var rect = new Rectangle(startX + (circleSize + spacing) * i, startY, circleSize, circleSize);
                        bool isSelected = schedule.Days.Contains(days[i]);

                        if (isSelected) e.Graphics.FillEllipse(brushActive, rect);
                        else { e.Graphics.FillEllipse(brushInactive, rect); e.Graphics.DrawEllipse(penInactive, rect); }

                        var size = e.Graphics.MeasureString(letters[i], this.Font);
                        e.Graphics.DrawString(letters[i], this.Font, isSelected ? textBrushActive : textBrushInactive, rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);
                    }
                }
                e.Handled = true;
            }
        }

        private void Grid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 2) // Days circles column
            {
                if (!(_gridSchedule.Rows[e.RowIndex].DataBoundItem is DaySchedule schedule)) return;

                int circleSize = 24, spacing = 6;
                int totalWidth = (circleSize * 7) + (spacing * 6);
                var cellRect = _gridSchedule.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                int startX = (cellRect.Width - totalWidth) / 2;
                
                DayOfWeek[] days = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

                for (int i = 0; i < 7; i++)
                {
                    int x = startX + (circleSize + spacing) * i;
                    if (e.X >= x && e.X <= x + circleSize)
                    {
                        if (schedule.Days.Contains(days[i])) schedule.Days.Remove(days[i]);
                        else schedule.Days.Add(days[i]);
                        _gridSchedule.InvalidateCell(e.ColumnIndex, e.RowIndex);
                        break;
                    }
                }
            }
        }

        private void Grid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == 3) // Times column
            {
                string input = e.FormattedValue?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(input)) return;

                var times = input.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var t in times)
                {
                    if (!TimeSpan.TryParse(t.Trim(), out _))
                    {
                        MessageBox.Show($"Format d'heure invalide : '{t.Trim()}'.\nUtilisez HH:mm (ex: 08:30) et séparez par des points-virgules.", "Erreur de saisie", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        e.Cancel = true;
                        return;
                    }
                }
            }
        }

        private void SaveChanges(object sender, EventArgs e)
        {
            _config.Schedules = ((System.ComponentModel.BindingList<DaySchedule>)_gridSchedule.DataSource).ToList();
            _config.ShutdownEnabled = _chkShutdown.Checked;
            _config.ShutdownTime = _txtShutdownTime.Text;

            // Update Registry
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (_chkStartWithWindows.Checked)
                        {
                            key.SetValue("BeepScheduler", Application.ExecutablePath);
                        }
                        else
                        {
                            key.DeleteValue("BeepScheduler", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la mise à jour du registre : " + ex.Message);
            }
        }
    }
}
