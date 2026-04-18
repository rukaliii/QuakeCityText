using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuakeCityText
{
    public class EarthquakeWebSocketClient : IDisposable
    {
        private readonly Uri _uri;

        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private Task _mainTask;
        private readonly object _sync = new();

        private bool _running = false;

        public event Action<JObject> OnMessageReceived;

        public EarthquakeWebSocketClient(string url = "wss://api.p2pquake.net/v2/ws")
        {
            _uri = new Uri(url);
        }
        public Task StartAsync()
        {
            lock (_sync)
            {
                if (_running)
                    return Task.CompletedTask;

                _running = true;
                _cts = new CancellationTokenSource();
                _mainTask = Task.Run(() => MainLoop(_cts.Token));
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            Task task = null;
            ClientWebSocket ws = null;

            lock (_sync)
            {
                if (!_running)
                    return;

                _running = false;

                _cts.Cancel();

                task = _mainTask;
                ws = _ws;
            }

            try
            {
                if (ws != null &&
                    (ws.State == WebSocketState.Open ||
                     ws.State == WebSocketState.CloseReceived))
                {
                    await ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "close",
                        CancellationToken.None);
                }
            }
            catch { }

            try
            {
                if (task != null)
                    await task;
            }
            catch { }

            lock (_sync)
            {
                _ws?.Dispose();
                _ws = null;

                _cts?.Dispose();
                _cts = null;

                _mainTask = null;
            }
        }

        private async Task MainLoop(CancellationToken token)
        {
            int retry = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    lock (_sync)
                    {
                        _ws?.Dispose();

                        _ws = new ClientWebSocket();
                        _ws.Options.KeepAliveInterval =
                            TimeSpan.FromSeconds(60);
                    }

                    await _ws.ConnectAsync(_uri, token);
                    Console.WriteLine("WebSocket接続成功");

                    retry = 0;

                    await ReceiveLoop(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("接続エラー: " + ex.Message);
                }

                retry++;

                int delay = Math.Min(30000, retry * 2000);
                Console.WriteLine($"再接続待機 {delay}ms");

                try
                {
                    await Task.Delay(delay, token);
                }
                catch
                {
                    break;
                }
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8192];

            while (!token.IsCancellationRequested &&
                   _ws != null &&
                   _ws.State == WebSocketState.Open)
            {
                try
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _ws.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            token);

                        if (result.MessageType ==
                            WebSocketMessageType.Close)
                        {
                            Console.WriteLine("サーバー切断");
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);

                    } while (!result.EndOfMessage);

                    string message =
                        Encoding.UTF8.GetString(ms.ToArray());

                    ProcessMessage(message);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine("通信切断: " + ex.Message);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("受信エラー: " + ex.Message);
                    return;
                }
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                var json = JObject.Parse(message);

                int code =
                    json["code"]?.Value<int?>() ?? -1;

                if (code != 551)
                    return;

                OnMessageReceived?.Invoke(json);
            }
            catch
            {
                Console.WriteLine("JSON解析失敗");
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}
