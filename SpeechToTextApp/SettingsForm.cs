using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SpeechToTextApp
{
    public partial class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        private AudioCapture? _testCapture;
        private readonly System.Windows.Forms.Timer _levelTimer = new System.Windows.Forms.Timer();
        private volatile int _lastLevel;

        public SettingsForm(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            _levelTimer.Interval = 100;
            _levelTimer.Tick += (s, e) => prgLevel.Value = Math.Max(0, Math.Min(100, _lastLevel));
            LoadValues();
        }

        private void LoadValues()
        {
            txtHost.Text = _settings.SceHost;
            numPort.Value = Math.Max(numPort.Minimum, Math.Min(numPort.Maximum, _settings.ScePort));
            chkUdp.Checked = _settings.SceUseUdp;
            chkEnableSce.Checked = _settings.SceEnabled;

            cmbAudio.Items.Clear();
            var devices = AudioCapture.ListInputDevices();
            foreach (var device in devices)
            {
                cmbAudio.Items.Add($"{device.id}|{device.name}");
            }
            if (!string.IsNullOrEmpty(_settings.AudioDeviceId))
            {
                var match = devices.FirstOrDefault(d => d.id == _settings.AudioDeviceId);
                if (!string.IsNullOrEmpty(match.id))
                {
                    cmbAudio.SelectedItem = $"{match.id}|{match.name}";
                }
            }
            if (cmbAudio.SelectedIndex < 0 && cmbAudio.Items.Count > 0)
            {
                cmbAudio.SelectedIndex = 0;
            }

            var modelPath = SafeResolveModelPath();
            txtModelPath.Text = modelPath;

            chkProfanity.Checked = _settings.ProfanityFilterEnabled;
            chkUseGpu.Checked = _settings.PreferGpu;
            chkDetailed.Checked = _settings.DetailedLogging;

            var latency = Math.Max(trkLatency.Minimum, Math.Min(trkLatency.Maximum, _settings.LatencyMs));
            trkLatency.Value = latency;
            UpdateLatencyLabel();
        }

        private string SafeResolveModelPath()
        {
            try
            {
                var path = _settings.ResolveModelPath();
                return Path.GetFullPath(path);
            }
            catch
            {
                return _settings.ResolveModelPath();
            }
        }

        private void btnBrowseModel_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Whisper models (*.bin)|*.bin|All files (*.*)|*.*",
                Title = "Select Whisper model"
            };

            if (!string.IsNullOrWhiteSpace(txtModelPath.Text))
            {
                try
                {
                    var currentPath = Path.GetFullPath(txtModelPath.Text);
                    dlg.InitialDirectory = Path.GetDirectoryName(currentPath) ?? AppContext.BaseDirectory;
                    dlg.FileName = Path.GetFileName(currentPath);
                }
                catch { }
            }

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtModelPath.Text = dlg.FileName;
            }
        }

        private void btnTestAudio_Click(object sender, EventArgs e)
        {
            if (_testCapture == null)
            {
                var audioSel = cmbAudio.SelectedItem?.ToString() ?? string.Empty;
                var deviceId = audioSel.Contains("|") ? audioSel.Split('|')[0] : audioSel;
                _testCapture = new AudioCapture();
                _testCapture.AudioDataAvailable += OnTestAudioData;
                try
                {
                    _testCapture.Start(deviceId);
                    _levelTimer.Start();
                    btnTestAudio.Text = "Stop";
                }
                catch
                {
                    StopTestAudio();
                }
            }
            else
            {
                StopTestAudio();
            }
        }

        private void OnTestAudioData(object? sender, byte[] buffer)
        {
            int max = 0;
            for (int i = 0; i + 1 < buffer.Length; i += 2)
            {
                int sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                int abs = Math.Abs(sample);
                if (abs > max) max = abs;
            }
            _lastLevel = (int)Math.Min(100, Math.Round(max / 32767.0 * 100.0));
        }

        private void StopTestAudio()
        {
            try { _levelTimer.Stop(); } catch { }
            try { _testCapture?.Stop(); } catch { }
            try { _testCapture?.Dispose(); } catch { }
            _testCapture = null;
            _lastLevel = 0;
            try { prgLevel.Value = 0; } catch { }
            btnTestAudio.Text = "Test";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            _settings.SceHost = txtHost.Text.Trim();
            _settings.ScePort = (int)numPort.Value;
            _settings.SceUseUdp = chkUdp.Checked;
            _settings.SceEnabled = chkEnableSce.Checked;

            _settings.SerialPortName = null;
            _settings.SerialBaud = 9600;

            var audioSel = cmbAudio.SelectedItem?.ToString() ?? "";
            _settings.AudioDeviceId = audioSel.Contains("|") ? audioSel.Split('|')[0] : audioSel;

            var modelPath = txtModelPath.Text.Trim();
            if (!string.IsNullOrEmpty(modelPath))
            {
                try
                {
                    var fullPath = Path.GetFullPath(modelPath);
                    _settings.SelectedModelFile = fullPath;
                    _settings.ModelDir = Path.GetDirectoryName(fullPath) ?? _settings.ModelDir;
                }
                catch
                {
                    _settings.SelectedModelFile = modelPath;
                }
            }

            _settings.PreferGpu = chkUseGpu.Checked;
            _settings.ProfanityFilterEnabled = chkProfanity.Checked;
            _settings.LatencyMs = trkLatency.Value;
            _settings.DetailedLogging = chkDetailed.Checked;

            _settings.Save();
            StopTestAudio();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void trkLatency_ValueChanged(object sender, EventArgs e)
        {
            UpdateLatencyLabel();
        }

        private void UpdateLatencyLabel()
        {
            lblLatency.Text = $"Latency: {trkLatency.Value} ms";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopTestAudio();
            base.OnFormClosed(e);
        }
    }
}
