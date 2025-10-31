using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpeechToTextApp
{
    public partial class MainForm : Form
    {
        private readonly AppSettings _settings;
        private readonly ProfanityFilter _filter = new();
        private readonly AudioCapture _audio = new();
        private WhisperService? _whisper;
        private Sce492Client? _sce;
        private readonly System.Collections.Generic.LinkedList<string> _lines = new();
        private readonly System.Collections.Generic.List<string> _completedLines = new();
        private string _currentLine = string.Empty;
        private bool _running = false;
        private volatile int _lastLevel;
        private readonly System.Windows.Forms.Timer _levelTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _captionFlushTimer = new System.Windows.Forms.Timer();
        private string _lastScePayload = string.Empty;
        private string _lastSceKey = string.Empty;
        private string _lastDisplayedText = string.Empty;

        public MainForm()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            _levelTimer.Interval = 100;
            _levelTimer.Tick += (s, e) => prgMainLevel.Value = Math.Max(0, Math.Min(100, _lastLevel));
            _captionFlushTimer.Interval = 2000;
            _captionFlushTimer.Tick += (s, e) => FlushPendingLine();
            ResetTranscriptionState();
            UpdateUi();
            Logger.Verbose = _settings.DetailedLogging;
            try { Logger.Info("App started. Log at: " + Logger.LogPath); } catch { }
        }

        private async Task EnsureInitializedAsync()
        {
            if (_whisper == null)
            {
                _whisper = new WhisperService(_settings);
                await _whisper.InitializeAsync();
                _whisper.CaptionFinalized += (s, text) => OnSegmentFinalized(text);
            }
            if (_sce == null)
            {
                _sce = new Sce492Client(_settings);
                await _sce.ConnectAsync();
            }
        }

        private void OnSegmentFinalized(string text)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnSegmentFinalized(text))); return; }

            if (string.IsNullOrWhiteSpace(text)) return;

            var normalized = text.Trim();
            if (normalized.Length == 0) return;

            if (string.Equals(normalized, _currentLine, StringComparison.OrdinalIgnoreCase))
                return;

            if (_completedLines.Count > 0 && string.Equals(normalized, _completedLines[^1], StringComparison.OrdinalIgnoreCase))
                return;

            if (_settings.ProfanityFilterEnabled)
            {
                normalized = _filter.Clean(normalized);
            }

            _currentLine = normalized;

            _captionFlushTimer.Stop();

            UpdateCaptionDisplay();

            if (EndsSentence(_currentLine))
            {
                FlushPendingLine();
            }
            else
            {
                _captionFlushTimer.Interval = 2000;
                _captionFlushTimer.Start();
            }
        }

        private async void btnToggle_Click(object sender, EventArgs e)
        {
            if (_running)
            {
                FlushPendingLine();
                _audio.Stop();
                _whisper?.Stop();
                _running = false;
                _levelTimer.Stop();
                ResetTranscriptionState();
                UpdateUi();
                return;
            }

            try
            {
                await EnsureInitializedAsync();

                _audio.AudioDataAvailable += (s, buf) => { OnAudioLevel(buf); _whisper?.PushAudio(buf); };
                _audio.Start(_settings.AudioDeviceId);
                _whisper!.Start();
                _running = true;
                ResetTranscriptionState();
                _levelTimer.Start();
                UpdateUi();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.Exception("btnToggle_Click", ex);
            }
        }

        private void UpdateUi()
        {
            btnToggle.Text = _running ? "Stop Captions" : "Start Captions";
            lblStatus.Text = _running ? "Running" : "Stopped";
        }

        private void UpdateCaptionDisplay()
        {
            var uiLines = new System.Collections.Generic.List<string>();
            foreach (var line in _completedLines)
            {
                uiLines.AddRange(WrapForDisplay(line, 56));
            }

            if (!string.IsNullOrWhiteSpace(_currentLine))
            {
                var current = _currentLine;
                if (_settings.ProfanityFilterEnabled)
                {
                    current = _filter.Clean(current);
                }
                uiLines.AddRange(WrapForDisplay(current, 56));
            }

            if (uiLines.Count == 0)
            {
                _lines.Clear();
                _lines.AddLast(string.Empty);
                _lines.AddLast(string.Empty);
                _lines.AddLast(string.Empty);
                txtCaption.Text = string.Join(Environment.NewLine, _lines);
                return;
            }

            var displayLines = uiLines.TakeLast(3).ToList();
            var combined = string.Join(" ", displayLines).Trim();
            if (string.Equals(combined, _lastDisplayedText, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _lastDisplayedText = combined;

            _lines.Clear();
            foreach (var line in displayLines)
            {
                _lines.AddLast(line);
            }
            while (_lines.Count < 3) _lines.AddLast(string.Empty);

            txtCaption.Text = string.Join(Environment.NewLine, _lines);
        }

        private void FlushPendingLine()
        {
            _captionFlushTimer.Stop();

            var trimmed = _currentLine.Trim();
            if (trimmed.Length == 0)
            {
                _currentLine = string.Empty;
                return;
            }

            if (_settings.ProfanityFilterEnabled)
            {
                trimmed = _filter.Clean(trimmed);
            }

            if (_completedLines.Count == 0 || !string.Equals(trimmed, _completedLines[^1], StringComparison.OrdinalIgnoreCase))
            {
                _completedLines.Add(trimmed);
                if (_completedLines.Count > 10)
                {
                    _completedLines.RemoveRange(0, _completedLines.Count - 10);
                }

                try { Logger.Info("Caption Finalized: " + trimmed); } catch { }
            }

            _currentLine = string.Empty;
            _lastDisplayedText = string.Empty;

            UpdateCaptionDisplay();
            SendLatestToSce();
        }

        private void SendLatestToSce()
        {
            if (_sce == null || !_settings.SceEnabled) return;

            var source = _completedLines.TakeLast(3).ToList();
            if (source.Count == 0) return;

            var formatted = FormatForSce(source, out var key);
            if (string.IsNullOrEmpty(formatted) || string.Equals(key, _lastSceKey, StringComparison.Ordinal)) return;

            _lastSceKey = key;
            _lastScePayload = formatted;
            Task.Run(() => _sce!.SendCaptionAsync(formatted));
            try { Logger.Info("SCE send key: " + key); } catch { }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dlg = new SettingsForm(_settings);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _sce?.Dispose();
                _sce = null;
                Logger.Verbose = _settings.DetailedLogging;
            }
        }
        private void openLogFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(Logger.LogPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                System.IO.Directory.CreateDirectory(dir);
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Open Log Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void OnAudioLevel(byte[] buffer)
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

        private void ResetTranscriptionState()
        {
            _captionFlushTimer.Stop();
            _completedLines.Clear();
            _currentLine = string.Empty;
            _lines.Clear();
            _lines.AddLast(string.Empty);
            _lines.AddLast(string.Empty);
            _lines.AddLast(string.Empty);
            _lastScePayload = string.Empty;
            _lastSceKey = string.Empty;
            _lastDisplayedText = string.Empty;
            txtCaption.Text = string.Empty;
        }

        private static string FormatForSce(System.Collections.Generic.IEnumerable<string> displayLines, out string key)
        {
            var normalized = new System.Collections.Generic.List<string>();

            foreach (var raw in displayLines)
            {
                var cleaned = SanitizeForSce(NormalizeWhitespace(raw ?? string.Empty).ToUpperInvariant());
                if (string.IsNullOrEmpty(cleaned))
                {
                    normalized.Add(string.Empty);
                }
                else
                {
                    normalized.AddRange(Wrap(cleaned, 32));
                }
            }

            if (normalized.Count == 0)
            {
                key = string.Empty;
                return string.Empty;
            }

            var limited = normalized.Take(3).Select(l => l.TrimEnd()).ToList();
            while (limited.Count < 3)
            {
                limited.Add(string.Empty);
            }

            key = string.Join("|", limited);

            var payloadLines = limited.Select(l => l.PadRight(32, ' '));
            return string.Join("\r\n", payloadLines);
        }

        private static bool EndsSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var trimmed = text.TrimEnd();
            if (trimmed.EndsWith("...", StringComparison.Ordinal)) return true;
            if (trimmed.EndsWith("--", StringComparison.Ordinal)) return true;
            char last = trimmed[^1];
            return last == '.' || last == '!' || last == '?' || last == ';';
        }

        private static string NormalizeWhitespace(string input)
        {
            var builder = new System.Text.StringBuilder(input.Length);
            bool wasSpace = false;
            foreach (var ch in input)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!wasSpace)
                    {
                        builder.Append(' ');
                        wasSpace = true;
                    }
                }
                else
                {
                    builder.Append(ch);
                    wasSpace = false;
                }
            }
            return builder.ToString().Trim();
        }

        private static string SanitizeForSce(string input)
        {
            var builder = new System.Text.StringBuilder(input.Length);
            foreach (var ch in input)
        {
                if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
                {
                    builder.Append(ch);
                }
                else if (ch == ' ')
                {
                    builder.Append(' ');
                }
                else if (ch == '.' || ch == ',' || ch == '!' || ch == '?' || ch == ':')
                {
                    builder.Append(ch);
                }
                // other characters dropped
            }
            return builder.ToString().Trim();
        }

        private static System.Collections.Generic.IEnumerable<string> Wrap(string text, int maxLen)
        {
            if (text.Length <= maxLen)
            {
                yield return text;
                yield break;
            }

            var words = text.Split(' ');
            var line = new System.Text.StringBuilder();
            foreach (var word in words)
            {
                var candidateLength = line.Length == 0 ? word.Length : line.Length + 1 + word.Length;
                if (candidateLength > maxLen && line.Length > 0)
                {
                    yield return line.ToString();
                    line.Clear();
                }
                if (line.Length > 0) line.Append(' ');
                line.Append(word);
            }
            if (line.Length > 0)
                yield return line.ToString();
        }

        private static System.Collections.Generic.IEnumerable<string> WrapForDisplay(string text, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var line = new System.Text.StringBuilder();
            foreach (var word in words)
            {
                var candidateLength = line.Length == 0 ? word.Length : line.Length + 1 + word.Length;
                if (candidateLength > maxLen && line.Length > 0)
                {
                    yield return line.ToString();
                    line.Clear();
                }
                if (line.Length > 0) line.Append(' ');
                line.Append(word);
            }
            if (line.Length > 0) yield return line.ToString();
        }
    }
}
