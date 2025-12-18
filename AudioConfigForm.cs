using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace BeepScheduler
{
    public class AudioConfigForm : Form
    {
        private AppConfig _config; // If needed, but let's stick to settings
        private AudioSettings _audioSettings;
        private RadioButton _rbIntegrated;
        private ComboBox _cmbIntegrated;
        private RadioButton _rbCustom;
        private TextBox _txtCustomPath;
        private Button _btnBrowse;
        private TrackBar _sliderVolume;
        private Button _btnTest;
        private Button _btnStop; // New Stop Button
        private Button _btnOk;
        private Button _btnCancel;
        private IWavePlayer? _waveOut;
        private AudioFileReader? _audioFile;

        public AudioSettings Settings => _audioSettings;

        public AudioConfigForm(AudioSettings settings, bool isDarkMode)
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
            ThemeHelper.ApplyTheme(this, isDarkMode);
        }

        private void InitializeComponent()
        {
            this.Text = "Configuration Audio";
            this.Size = new Size(400, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            GroupBox grpSource = new GroupBox { Text = "Source Audio", Top = 10, Left = 10, Width = 360, Height = 135 };
            
            _rbIntegrated = new RadioButton { Text = "Son Intégré", Top = 25, Left = 20, Width = 100 };
            _cmbIntegrated = new ComboBox { Top = 23, Left = 130, Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbIntegrated.Items.AddRange(new string[] { "Bip Standard", "Casio Chime", "Sirène", "Réveil Digital" });
            _cmbIntegrated.SelectedIndexChanged += (s, e) => _rbIntegrated.Checked = true;

            _rbCustom = new RadioButton { Text = "Fichier MP3 Perso", Top = 60, Left = 20, Width = 150 };
            
            _txtCustomPath = new TextBox { Top = 90, Left = 20, Width = 250, ReadOnly = true };
            _btnBrowse = new Button { Text = "...", Top = 88, Left = 280, Width = 40 };
            _btnBrowse.Click += (s, e) => BrowseFile();

            grpSource.Controls.AddRange(new Control[] { _rbIntegrated, _cmbIntegrated, _rbCustom, _txtCustomPath, _btnBrowse });

            GroupBox grpVol = new GroupBox { Text = "Volume", Top = 150, Left = 10, Width = 360, Height = 70 };
            _sliderVolume = new TrackBar { Top = 20, Left = 10, Width = 340, Minimum = 0, Maximum = 100, TickFrequency = 10 };
            _sliderVolume.ValueChanged += (s, e) => {
                if (_audioFile != null) _audioFile.Volume = (float)(_sliderVolume.Value / 100.0);
            };
            grpVol.Controls.Add(_sliderVolume);

            _btnTest = new Button { Text = "Tester", Top = 230, Left = 10, Width = 80 };
            _btnTest.Click += (s, e) => TestSound();

            _btnStop = new Button { Text = "Arrêter", Top = 230, Left = 100, Width = 80 };
            _btnStop.Click += (s, e) => StopSound();

            _btnOk = new Button { Text = "OK", Top = 230, Left = 200, DialogResult = DialogResult.OK, Width = 70 };
            _btnCancel = new Button { Text = "Annuler", Top = 230, Left = 280, DialogResult = DialogResult.Cancel, Width = 70 };

            this.Controls.AddRange(new Control[] { grpSource, grpVol, _btnTest, _btnStop, _btnOk, _btnCancel });
        }

        private void LoadData()
        {
            // Logic to determine if it's an integrated sound or custom
            bool isIntegrated = false;
            if (!_audioSettings.UseCustomSound)
            {
                isIntegrated = true;
            }
            else
            {
                // Check if the path points to our internal Sounds folder
                string soundsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
                if (_audioSettings.CustomSoundPath != null && _audioSettings.CustomSoundPath.StartsWith(soundsDir))
                {
                    isIntegrated = true;
                }
            }

            if (isIntegrated)
            {
                _rbIntegrated.Checked = true;
                // Identify which one based on filename
                if (_audioSettings.CustomSoundPath != null)
                {
                    string filename = Path.GetFileNameWithoutExtension(_audioSettings.CustomSoundPath);
                    if (Enum.TryParse<SoundType>(filename, out var type))
                    {
                        _cmbIntegrated.SelectedIndex = (int)type;
                    }
                    else _cmbIntegrated.SelectedIndex = 0;
                }
                else _cmbIntegrated.SelectedIndex = 0;
            }
            else
            {
                _rbCustom.Checked = true;
                _txtCustomPath.Text = _audioSettings.CustomSoundPath;
            }

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
            string playPath = "";
            if (_rbIntegrated.Checked)
            {
                playPath = SoundHelper.GetSoundPath((SoundType)_cmbIntegrated.SelectedIndex);
            }
            else if (_rbCustom.Checked && File.Exists(_txtCustomPath.Text))
            {
                playPath = _txtCustomPath.Text;
            }

            if (!string.IsNullOrEmpty(playPath) && File.Exists(playPath))
            {
                try
                {
                    StopSound();
                    
                    _audioFile = new AudioFileReader(playPath);
                    _audioFile.Volume = (float)(_sliderVolume.Value / 100.0);
                    
                    _waveOut = new WasapiOut(AudioClientShareMode.Shared, 100);
                    _waveOut.Init(_audioFile);
                    _waveOut.Play();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur détaillée: " + ex.ToString());
                }
            }
        }

        private void StopSound()
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            if (_audioFile != null)
            {
                _audioFile.Dispose();
                _audioFile = null;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (this.DialogResult == DialogResult.OK)
            {
                if (_rbIntegrated.Checked)
                {
                    _audioSettings.UseCustomSound = true; // Technically we use a file
                    _audioSettings.CustomSoundPath = SoundHelper.GetSoundPath((SoundType)_cmbIntegrated.SelectedIndex);
                }
                else
                {
                    _audioSettings.UseCustomSound = true;
                    _audioSettings.CustomSoundPath = _txtCustomPath.Text;
                }
                _audioSettings.Volume = _sliderVolume.Value;
            }
        }

    }
}
