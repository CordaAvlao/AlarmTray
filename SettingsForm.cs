using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BeepScheduler
{
    public class SettingsForm : Form
    {
        private AppConfig _config;
        private DataGridView _gridSchedule;
        private Button _btnSave;
        private Button _btnCancel;
        private CheckBox _chkStartWithWindows;

        public AppConfig UpdatedConfig => _config;

        public SettingsForm(AppConfig config)
        {
            _config = config;
            InitializeComponent();
            LoadConfigData();
        }

        private void InitializeComponent()
        {
            this.Text = "Paramètres Beep Scheduler V2.1";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Grid
            _gridSchedule = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 300,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true
            };

            // Columns
            var colDelete = new DataGridViewButtonColumn
            {
                HeaderText = "",
                Text = "X",
                UseColumnTextForButtonValue = true,
                Width = 30,
                ToolTipText = "Supprimer l'alarme"
            };
            var colDays = new DataGridViewTextBoxColumn
            {
                HeaderText = "Jours (L M M J V S D)",
                ReadOnly = true,
                Width = 200
            };
            var colTimes = new DataGridViewTextBoxColumn
            {
                HeaderText = "Heures (ex: 08:00; 14:30)",
                DataPropertyName = "Times"
            };
            var colAudio = new DataGridViewButtonColumn
            {
                HeaderText = "Audio",
                Text = "Configurer",
                UseColumnTextForButtonValue = true,
                Width = 80
            };

            // Add columns to grid
            _gridSchedule.Columns.Add(colDelete);
            _gridSchedule.Columns.Add(colDays);
            _gridSchedule.Columns.Add(colTimes);
            _gridSchedule.Columns.Add(colAudio);

            // Event handlers
            _gridSchedule.CellContentClick += Grid_CellContentClick;
            _gridSchedule.CellPainting += Grid_CellPainting;
            _gridSchedule.CellMouseClick += Grid_CellMouseClick;

            // Bottom Buttons
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            _btnSave = new Button { Text = "Enregistrer", Top = 10, Left = 400, DialogResult = DialogResult.OK };
            _btnSave.Click += SaveChanges;
            _btnCancel = new Button { Text = "Annuler", Top = 10, Left = 490, DialogResult = DialogResult.Cancel };
            _chkStartWithWindows = new CheckBox { Text = "Démarrer avec Windows", Top = 15, Left = 10, Width = 200, AutoSize = true };
            pnlBottom.Controls.AddRange(new Control[] { _chkStartWithWindows, _btnSave, _btnCancel });

            // Add controls to form
            this.Controls.Add(_gridSchedule);
            this.Controls.Add(pnlBottom);
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
            // Delete column (index 0)
            if (e.ColumnIndex == 0)
            {
                var row = _gridSchedule.Rows[e.RowIndex];
                var schedule = row.DataBoundItem as DaySchedule;
                if (schedule != null)
                {
                    var list = (System.ComponentModel.BindingList<DaySchedule>)_gridSchedule.DataSource;
                    list.Remove(schedule);
                }
                return;
            }
            // Audio column is now index 3
            if (e.ColumnIndex == 3)
            {
                var row = _gridSchedule.Rows[e.RowIndex];
                if (row.DataBoundItem is DaySchedule schedule)
                {
                    using (var audioForm = new AudioConfigForm(schedule.Audio))
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
            if (e.RowIndex >= 0 && e.ColumnIndex == 1) // Days column
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

                var schedule = _gridSchedule.Rows[e.RowIndex].DataBoundItem as DaySchedule;
                if (schedule == null) return;

                string[] letters = { "L", "M", "M", "J", "V", "S", "D" };
                DayOfWeek[] days = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

                int circleSize = 20;
                int spacing = 5;
                int startX = e.CellBounds.X + 5;
                int startY = e.CellBounds.Y + (e.CellBounds.Height - circleSize) / 2;

                using (var brushActive = new SolidBrush(Color.DodgerBlue))
                using (var brushInactive = new SolidBrush(Color.LightGray))
                using (var pen = new Pen(Color.Gray))
                using (var textBrush = new SolidBrush(Color.White))
                using (var textBrushInactive = new SolidBrush(Color.Black))
                {
                    for (int i = 0; i < 7; i++)
                    {
                        var rect = new Rectangle(startX + (circleSize + spacing) * i, startY, circleSize, circleSize);
                        bool isSelected = schedule.Days.Contains(days[i]);
                        e.Graphics.FillEllipse(isSelected ? brushActive : brushInactive, rect);
                        e.Graphics.DrawEllipse(pen, rect);
                        var size = e.Graphics.MeasureString(letters[i], this.Font);
                        float textX = rect.X + (rect.Width - size.Width) / 2;
                        float textY = rect.Y + (rect.Height - size.Height) / 2;
                        e.Graphics.DrawString(letters[i], this.Font, isSelected ? textBrush : textBrushInactive, textX, textY);
                    }
                }
                e.Handled = true;
            }
        }

        private void Grid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1)
            {
                var schedule = _gridSchedule.Rows[e.RowIndex].DataBoundItem as DaySchedule;
                if (schedule == null) return;

                int circleSize = 20;
                int spacing = 5;
                int startX = 5; // relative to cell

                DayOfWeek[] days = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

                for (int i = 0; i < 7; i++)
                {
                    int x = startX + (circleSize + spacing) * i;
                    if (e.X >= x && e.X <= x + circleSize)
                    {
                        var day = days[i];
                        if (schedule.Days.Contains(day))
                            schedule.Days.Remove(day);
                        else
                            schedule.Days.Add(day);
                        _gridSchedule.InvalidateCell(e.ColumnIndex, e.RowIndex);
                        break;
                    }
                }
            }
        }

        private void SaveChanges(object sender, EventArgs e)
        {
            _config.Schedules = ((System.ComponentModel.BindingList<DaySchedule>)_gridSchedule.DataSource).ToList();

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
