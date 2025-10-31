using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SpeechToTextApp
{
    public class Sce492Client : IDisposable
    {
        private readonly AppSettings _settings;
        private UdpClient? _udp;
        private TcpClient? _tcp;
        private NetworkStream? _tcpStream;

        public bool Connected { get; private set; }

        public Sce492Client(AppSettings settings)
        {
            _settings = settings;
        }

        public async Task ConnectAsync()
        {
            Disconnect();
            if (!_settings.SceEnabled) return;

            try
            {
                if (_settings.SceUseUdp)
                {
                    _udp = new UdpClient();
                    _udp.Connect(_settings.SceHost, _settings.ScePort);
                    Connected = true;
                    Logger.Info($"SCE UDP connected to {_settings.SceHost}:{_settings.ScePort}");
                }
                else
                {
                    _tcp = new TcpClient();
                    await _tcp.ConnectAsync(_settings.SceHost, _settings.ScePort);
                    _tcpStream = _tcp.GetStream();
                    Connected = true;
                    Logger.Info($"SCE TCP connected to {_settings.SceHost}:{_settings.ScePort}");
                }
            }
            catch (Exception ex)
            {
                Connected = false; // suppress connection errors for testing
                Logger.Exception("SceConnect", ex);
            }
        }

        public void Disconnect()
        {
            Connected = false;
            try { _udp?.Close(); } catch { }
            try { _tcpStream?.Close(); } catch { }
            try { _tcp?.Close(); } catch { }

            _udp = null; _tcp = null; _tcpStream = null;
        }

        public async Task SendCaptionAsync(string text)
        {
            if (!_settings.SceEnabled) return;
            var payloadText = text.EndsWith("\r\n") ? text : text.TrimEnd('\r', '\n') + "\r\n";
            var payload = Encoding.ASCII.GetBytes(payloadText);

            if (!Connected)
            {
                await ConnectAsync();
                if (!Connected)
                {
                    Logger.Info("SCE send skipped; not connected");
                    return;
                }
            }

            try
            {
                if (_settings.SceUseUdp && _udp != null)
                {
                    await _udp.SendAsync(payload, payload.Length);
                    Logger.Info($"SCE UDP send: '{text}'");
                }
                else if (_tcpStream != null)
                {
                    await _tcpStream.WriteAsync(payload, 0, payload.Length);
                    await _tcpStream.FlushAsync();
                    Logger.Info($"SCE TCP send: '{text}'");
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("SceSend", ex);
                Disconnect();
            }
        }

        public void Dispose() => Disconnect();
    }
}
