using System;
using System.IO;
using System.Text;

namespace SpeechToTextApp
{
    public static class Logger
    {
        private static readonly object _sync = new object();
        private static string _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpeechToTextApp",
            "log.txt");
        public static bool Verbose { get; set; } = false;

        public static string LogPath => _logPath;

        public static void SetPath(string path)
        {
            lock (_sync)
            {
                _logPath = path;
                EnsureDir();
            }
        }

        private static void EnsureDir()
        {
            try
            {
                var dir = Path.GetDirectoryName(_logPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            }
            catch { }
        }

        public static void Info(string message)
        {
            if (!Verbose) return;
            Write("INFO", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Exception(string context, Exception ex)
        {
            Write("EXC", context + ": " + ex.Message + "\n" + ex);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (_sync)
                {
                    EnsureDir();
                    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logPath, line, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
