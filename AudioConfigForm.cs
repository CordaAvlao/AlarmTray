using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BeepScheduler
{
    public class AudioConfigForm : Form
    {
        private AudioSettings _audioSettings;
        private RadioButton _rbBeep;
        private RadioButton _rbCustom;
        private TextBox _txtCustomPath;
        private Button _btnBrowse;
        private TrackBar _sliderVolume;
        private Button _btnTest;
        private Button _btnOk;
        private Button _btnCancel;
        private System.Windows.Media.MediaPlayer _player;

        public AudioSettings Settings => _audioSettings;

        public AudioConfigForm(AudioSettings settings)
        {
            // Clone settings to avoid modifying original until OK is clicked
            _audioSettings = new AudioSettings
            {
                UseCustomSound = settings.UseCustomSound,
                CustomSoundPath = settings.CustomSoundPath,
                Volume = settings.Volume
            };

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Configuration Audio";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            GroupBox grpSource = new GroupBox { Text = "Source Audio", Top = 10, Left = 10, Width = 360, Height = 120 };
            
            _rbBeep = new RadioButton { Text = "Bip Système", Top = 25, Left = 20 };
            _rbCustom = new RadioButton { Text = "Fichier MP3", Top = 50, Left = 20 };
            
            _txtCustomPath = new TextBox { Top = 80, Left = 20, Width = 250, ReadOnly = true };
            _btnBrowse = new Button { Text = "...", Top = 78, Left = 280, Width = 40 };
            _btnBrowse.Click += (s, e) => BrowseFile();

            grpSource.Controls.AddRange(new Control[] { _rbBeep, _rbCustom, _txtCustomPath, _btnBrowse });

            GroupBox grpVol = new GroupBox { Text = "Volume", Top = 140, Left = 10, Width = 360, Height = 70 };
            _sliderVolume = new TrackBar { Top = 20, Left = 10, Width = 340, Minimum = 0, Maximum = 100, TickFrequency = 10 };
            grpVol.Controls.Add(_sliderVolume);

            _btnTest = new Button { Text = "Tester", Top = 220, Left = 10, Width = 100 };
            _btnTest.Click += (s, e) => TestSound();

            _btnOk = new Button { Text = "OK", Top = 220, Left = 200, DialogResult = DialogResult.OK, Width = 70 };
            _btnCancel = new Button { Text = "Annuler", Top = 220, Left = 280, DialogResult = DialogResult.Cancel, Width = 70 };

            this.Controls.AddRange(new Control[] { grpSource, grpVol, _btnTest, _btnOk, _btnCancel });
        }

        private void LoadData()
        {
            if (_audioSettings.UseCustomSound) _rbCustom.Checked = true;
            else _rbBeep.Checked = true;

            _txtCustomPath.Text = _audioSettings.CustomSoundPath;
            _sliderVolume.Value = _audioSettings.Volume;
        }

        private void BrowseFile()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Audio Files|*.mp3;*.wav|All Files|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _txtCustomPath.Text = ofd.FileName;
                    _rbCustom.Checked = true;
                }
            }
        }

        private void TestSound()
        {
            if (_rbCustom.Checked && File.Exists(_txtCustomPath.Text))
            {
                try
                {
                    if (_player == null) _player = new System.Windows.Media.MediaPlayer();
                    _player.Open(new Uri(_txtCustomPath.Text));
                    _player.Volume = _sliderVolume.Value / 100.0;
                    _player.Play();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur: " + ex.Message);
                }
            }
            else
            {
                Console.Beep(1000, 500);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (this.DialogResult == DialogResult.OK)
            {
                _audioSettings.UseCustomSound = _rbCustom.Checked;
                _audioSettings.CustomSoundPath = _txtCustomPath.Text;
                _audioSettings.Volume = _sliderVolume.Value;
            }
        }
    }
}
