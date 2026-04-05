using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuakeCityText
{
    public class EarthquakeWebSocketClient
    {
        private readonly Uri _uri;
        private ClientWebSocket _ws;
        private readonly CancellationTokenSource _cts = new();

        public event Action<JObject> OnMessageReceived;

        public EarthquakeWebSocketClient(string url = "wss://api.p2pquake.net/v2/ws")
        {
            _uri = new Uri(url);
        }

        public async Task StartAsync()
        {
            _ = Task.Run(MainLoop);
        }

        private async Task MainLoop()
        {
            int retry = 0;

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    _ws?.Dispose();
                    _ws = new ClientWebSocket();
                    _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);

                    await _ws.ConnectAsync(_uri, _cts.Token);
                    Console.WriteLine("WebSocket接続成功");

                    _ = Task.Run(PingLoop);
                    retry = 0;

                    await ReceiveLoop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("接続エラー: " + ex.Message);
                    System.Windows.Forms.MessageBox.Show("WebSocket接続に失敗しました。\n" + ex.Message, "エラー (WebSocket)", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }

                retry++;

                int delay = Math.Min(30000, retry * 2000);
                Console.WriteLine($"再接続待機 {delay}ms");

                await Task.Delay(delay, _cts.Token);
            }
        }

        private async Task PingLoop()
        {
            while (_ws?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var buffer = Encoding.UTF8.GetBytes("ping");
                    await _ws.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        _cts.Token);
                }
                catch { }

                await Task.Delay(15000, _cts.Token);
            }
        }
        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];

            while (_ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _ws.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            _cts.Token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("サーバが接続を終了");
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);

                    } while (!result.EndOfMessage);

                    string message = Encoding.UTF8.GetString(ms.ToArray());

                    try
                    {
                        var json = JObject.Parse(message);

                        int code = json["code"]?.Value<int?>() ?? -1;
                        if (code != 551)
                        {
                            continue;
                        }
                        OnMessageReceived?.Invoke(json);
                    }
                    catch
                    {
                        Console.WriteLine("JSONパース失敗");
                        System.Windows.Forms.MessageBox.Show("受信したメッセージのJSONパースに失敗しました。\n" + message, "エラー (JSONパース)", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine("WebSocket例外: " + ex.Message);
                    System.Windows.Forms.MessageBox.Show("WebSocket通信中にエラーが発生しました。\n" + ex.Message, "エラー (WebSocket通信)", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("受信エラー: " + ex.Message);
                    System.Windows.Forms.MessageBox.Show("メッセージ受信中にエラーが発生しました。\n" + ex.Message, "エラー (受信)", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return;
                }
            }
        }

        public async Task StopAsync()
        {
            _cts.Cancel();

            try
            {
                if (_ws?.State == WebSocketState.Open)
                {
                    await _ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "close",
                        CancellationToken.None);
                }
            }
            catch { }

            _ws?.Dispose();
        }
    }
}