using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;

namespace SpeechToTextApp
{
    public class WhisperService : IDisposable
    {
        private readonly AppSettings _settings;
        private WhisperFactory? _factory;
        private WhisperProcessor? _processor;
        private CancellationTokenSource? _cts;
        private readonly ConcurrentQueue<float> _sampleQueue = new();
        private Task? _worker;
        private int _threads = Math.Max(1, Environment.ProcessorCount / 2);

        public event EventHandler<string>? CaptionFinalized;

        public WhisperService(AppSettings settings)
        {
            _settings = settings;
            DetectOptimalSettings();
        }

        private void DetectOptimalSettings()
        {
            if (Environment.ProcessorCount >= 12 && _settings.Model != ModelSize.MediumEn)
                _settings.Model = ModelSize.MediumEn;
        }

        public Task InitializeAsync()
        {
            var modelPath = _settings.ResolveModelPath();
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model not found at {modelPath}");

            Logger.Info($"Initializing Whisper with model: {modelPath}");
            _factory = WhisperFactory.FromPath(modelPath);
            var builder = _factory.CreateBuilder()
                .WithLanguage("en")
                .WithThreads(_threads);

            if (_settings.PreferGpu)
            {
                TryEnableGpu(builder, _settings.GpuLayerCount);
            }

            _processor = builder.Build();
            Logger.Info("Whisper processor built.");

            return Task.CompletedTask;
        }

        private static void TryEnableGpu(object builder, int gpuLayers)
        {
            try
            {
                var t = builder.GetType();
                // Try WithGpu(boolean)
                var mBool = t.GetMethods().FirstOrDefault(m => m.Name.IndexOf("Gpu", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                                              m.GetParameters().Length == 1 &&
                                                              m.GetParameters()[0].ParameterType == typeof(bool));
                if (mBool != null)
                {
                    mBool.Invoke(builder, new object[] { true });
                    Logger.Info("GPU enabled via WithGpu(bool)=true");
                    return;
                }
                // Try WithGpuLayerCount(int) or similar
                var mInt = t.GetMethods().FirstOrDefault(m => m.Name.IndexOf("Gpu", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                                             m.GetParameters().Length == 1 &&
                                                             m.GetParameters()[0].ParameterType == typeof(int));
                if (mInt != null)
                {
                    mInt.Invoke(builder, new object[] { gpuLayers });
                    Logger.Info($"GPU enabled via {mInt.Name}({gpuLayers})");
                    return;
                }
                // Try any CUDA-enabling method
                var mCuda = t.GetMethods().FirstOrDefault(m => m.Name.IndexOf("Cuda", StringComparison.OrdinalIgnoreCase) >= 0 && m.GetParameters().Length == 0);
                if (mCuda != null)
                {
                    mCuda.Invoke(builder, null);
                    Logger.Info($"GPU enabled via {mCuda.Name}()");
                    return;
                }
                Logger.Info("No GPU methods found on builder; using CPU.");
            }
            catch (Exception ex)
            {
                Logger.Exception("TryEnableGpu", ex);
            }
        }

        public void PushAudio(byte[] pcm16LeMono16k)
        {
            for (int i = 0; i < pcm16LeMono16k.Length; i += 2)
            {
                short sample = (short)(pcm16LeMono16k[i] | (pcm16LeMono16k[i + 1] << 8));
                _sampleQueue.Enqueue(sample / 32768f);
            }
        }

        public void Start()
        {
            if (_processor == null) throw new InvalidOperationException("Call InitializeAsync first.");
            Stop();
            _cts = new CancellationTokenSource();
            _worker = Task.Run(() => WorkerLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _worker?.Wait(1000); } catch { }
            _cts = null; _worker = null;
        }

        private async Task WorkerLoopAsync(CancellationToken ct)
        {
            const int sampleRate = 16000;
            var window = new System.Collections.Generic.List<float>(sampleRate * 3);
            var contextBuffer = new System.Collections.Generic.List<float>(sampleRate * 5);
            int emptyChunkStreak = 0;
            Logger.Info($"Worker loop started. LatencyMs={_settings.LatencyMs}");
            while (!ct.IsCancellationRequested)
            {
                while (window.Count < sampleRate * 3 && _sampleQueue.TryDequeue(out var s))
                {
                    window.Add(s);
                }

                // Determine threshold from settings
                var thresholdSamples = Math.Max(sampleRate / 4, (int)((long)sampleRate * Math.Max(100, _settings.LatencyMs) / 1000));
                if (window.Count >= thresholdSamples) // configurable chunk
                {
                    var arr = window.ToArray();
                    // keep an overlap to preserve context across chunks
                    var overlap = Math.Max(sampleRate / 4, thresholdSamples / 2);
                    var keep = Math.Min(overlap, arr.Length);
                    window.Clear();
                    if (keep > 0)
                    {
                        for (int i = arr.Length - keep; i < arr.Length; i++) window.Add(arr[i]);
                    }

                    if (_processor != null)
                    {
                        try
                        {
                            // Log basic audio stats
                            double sumSq = 0; float maxAbs = 0;
                            for (int i = 0; i < arr.Length; i++) { var v = Math.Abs(arr[i]); if (v > maxAbs) maxAbs = v; sumSq += arr[i] * arr[i]; }
                            var rms = Math.Sqrt(sumSq / Math.Max(1, arr.Length));
                            Logger.Info($"Processing audio: samples={arr.Length} rms={rms:F4} max={maxAbs:F4}");
                            var any = false;
                            // grow context buffer (max ~5s)
                            contextBuffer.AddRange(arr);
                            if (contextBuffer.Count > sampleRate * 5)
                                contextBuffer.RemoveRange(0, contextBuffer.Count - sampleRate * 5);

                            await foreach (var segment in _processor.ProcessAsync(arr, ct))
                            {
                                var segText = (segment.Text ?? string.Empty).Trim();
                                if (!string.IsNullOrWhiteSpace(segText))
                                {
                                    Logger.Info($"Segment: '{segText}'");
                                    CaptionFinalized?.Invoke(this, segText);
                                    any = true;
                                }
                            }
                            if (!any)
                            {
                                emptyChunkStreak++;
                                Logger.Info("No segments produced for this chunk.");
                                // fallback: if several empty chunks, try a longer context
                                if (emptyChunkStreak >= 4 && contextBuffer.Count >= sampleRate * 3)
                                {
                                    var longArr = contextBuffer.ToArray();
                                    Logger.Info($"Fallback long context: samples={longArr.Length}");
                                    await foreach (var segment in _processor.ProcessAsync(longArr, ct))
                                    {
                                        var segText = (segment.Text ?? string.Empty).Trim();
                                        if (!string.IsNullOrWhiteSpace(segText))
                                        {
                                            Logger.Info($"Segment(FB): '{segText}'");
                                            CaptionFinalized?.Invoke(this, segText);
                                            any = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                emptyChunkStreak = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is System.Threading.Tasks.TaskCanceledException)
                            {
                                Logger.Info("ProcessAsync canceled (stop requested)");
                            }
                            else
                            {
                                Logger.Exception("ProcessAsync", ex);
                            }
                        }
                    }
                }
                else
                {
                    await Task.Delay(50, ct);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _processor?.Dispose();
            _factory?.Dispose();
        }
    }
}
