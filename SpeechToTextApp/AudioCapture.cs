using System;
using NAudio.Wave;

namespace SpeechToTextApp
{
    public class AudioCapture : IDisposable
    {
        private WaveInEvent? _waveIn;
        public event EventHandler<byte[]>? AudioDataAvailable;

        public static (string id, string name)[] ListInputDevices()
        {
            var list = new System.Collections.Generic.List<(string,string)>();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);
                list.Add(($"wavein:{i}", caps.ProductName));
            }
            return list.ToArray();
        }

        public void Start(string deviceId = "")
        {
            Stop();
            int deviceIndex = 0;
            if (deviceId.StartsWith("wavein:") && int.TryParse(deviceId.Split(':')[1], out var idx))
                deviceIndex = idx;

            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceIndex,
                WaveFormat = new WaveFormat(16000, 16, 1)
            };
            _waveIn.DataAvailable += (s, a) =>
            {
                var buffer = new byte[a.BytesRecorded];
                Array.Copy(a.Buffer, buffer, a.BytesRecorded);
                AudioDataAvailable?.Invoke(this, buffer);
            };
            _waveIn.StartRecording();
            try { Logger.Info($"AudioCapture started. Device={deviceId} Index={deviceIndex}"); } catch { }
        }

        public void Stop()
        {
            try { _waveIn?.StopRecording(); } catch { }
            try { _waveIn?.Dispose(); } catch { }
            _waveIn = null;
            try { Logger.Info("AudioCapture stopped."); } catch { }
        }

        public void Dispose() => Stop();
    }
}
