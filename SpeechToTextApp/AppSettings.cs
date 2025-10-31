using System;
using System.IO;
using System.Text.Json;

namespace SpeechToTextApp
{
    public enum ModelSize { SmallEn, MediumEn }

    public class AppSettings
    {
        public string SceHost { get; set; } = "127.0.0.1";
        public int ScePort { get; set; } = 5000;
        public bool SceUseUdp { get; set; } = true;
        public bool SceEnabled { get; set; } = false;
        public string? SerialPortName { get; set; } = null;
        public int SerialBaud { get; set; } = 9600;

        public string AudioDeviceId { get; set; } = ""; // NAudio device friendly name
        public ModelSize Model { get; set; } = ModelSize.SmallEn; // kept for backward compatibility

        public string ModelDir { get; set; } = "models"; // where .bin lives
        public string SmallModelFile { get; set; } = "ggml-small.en.bin"; // legacy
        public string MediumModelFile { get; set; } = "ggml-medium.en.bin"; // legacy
        public string SelectedModelFile { get; set; } = "ggml-small.en.bin";

        public bool PreferGpu { get; set; } = true;
        public int GpuLayerCount { get; set; } = 50; // best-effort offload, if supported

        public bool ProfanityFilterEnabled { get; set; } = true;
        public int LatencyMs { get; set; } = 1000;
        public bool DetailedLogging { get; set; } = false;

        public static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpeechToTextApp", "config.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public string ResolveModelPath()
        {
            if (!string.IsNullOrWhiteSpace(SelectedModelFile))
            {
                if (Path.IsPathRooted(SelectedModelFile))
                {
                    return SelectedModelFile;
                }

                var rootedDir = EnsureRooted(ModelDir);
                return Path.Combine(rootedDir, SelectedModelFile);
            }

            var legacyFile = Model == ModelSize.SmallEn ? SmallModelFile : MediumModelFile;
            var legacyDir = EnsureRooted(ModelDir);
            return Path.Combine(legacyDir, legacyFile);
        }

        private static string EnsureRooted(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                return AppContext.BaseDirectory;
            }

            if (Path.IsPathRooted(dir))
            {
                return dir;
            }

            return Path.Combine(AppContext.BaseDirectory, dir);
        }
    }
}
