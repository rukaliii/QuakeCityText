using Newtonsoft.Json.Linq;
using QuakeCityText.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace QuakeCityText
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        EarthquakeWebSocketClient client = new EarthquakeWebSocketClient();
        private async void Form1_Load(object sender, EventArgs e)
        {
            byte[] fontBuf1 = Resources.NotoSansJP_VF;
            AddFont(fontBuf1);

            LoadShindoColors();

            var latest = await P2PQuakeClient.GetLatestEarthquakeAsync();
            if (latest != null)
            {
                Console.WriteLine("起動時の最新地震:");
                DisplayMaxScaleRegion(latest);
            }
            client = new EarthquakeWebSocketClient();
            await client.StartAsync();
            client.OnMessageReceived += (data) =>
            {
                DisplayMaxScaleRegion(data);
            };
        }
        private unsafe void AddFont(byte[] fontBuffer)
        {
            fixed (byte* pFontBuf = fontBuffer)
            {
                pfc.AddMemoryFont((IntPtr)pFontBuf, fontBuffer.Length);
            }
        }


        public static PrivateFontCollection pfc = new PrivateFontCollection();

        public void DisplayMaxScaleRegion(JObject data)
        {
            if (data["code"]?.Value<int>() != 551) return;

            var points = data["points"]?.ToObject<JArray>();
            if (points == null) return;

            int maxScale = data["earthquake"]?["maxScale"]?.Value<int>() ?? 0;

            var maxPoints = points
                .Where(p => p["scale"]?.Value<int>() == maxScale)
                .ToList();

            var addrList = maxPoints
                .Select(p => p["addr"]?.Value<string>())
                .Where(addr => !string.IsNullOrEmpty(addr))
                .Select(addr => StationNameShorter.Shorten(addr))
                .Distinct()
                .ToList();

            string areaText = string.Join(" ", addrList);

            string formatted = cityintFormat.FormatAreaString(areaText, 13);

            int lineCount = formatted.Split('\n').Length;

            if (lineCount > 7)
            {
                var prefList = maxPoints
                    .Where(p => !string.IsNullOrEmpty(p["pref"]?.Value<string>()))
                    .GroupBy(p => p["pref"]!.Value<string>())
                    .Select(g =>
                    {
                        var cities = g
                            .Select(p => p["addr"]?.Value<string>())
                            .Where(a => !string.IsNullOrEmpty(a))
                            .Select(a => StationNameShorter.Shorten(a)) // 市町村化
                            .Distinct()
                            .Count();

                        return $"{g.Key.Replace("県", "").Replace("府", "").Replace("東京都", "東京")}({cities})";
                    })
                    .ToList();

                areaText = string.Join(" ", prefList);

                formatted = cityintFormat.FormatAreaString(areaText, 13);
            }

            Bitmap canvasText = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            using (Graphics g = Graphics.FromImage(canvasText))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                g.DrawString(
                    $"震度{ShindoScale[maxScale]}",
                    new Font(pfc.Families[0], 23f, FontStyle.Bold),
                    Brushes.White,
                    10f,
                    0f
                );

                g.FillRectangle(
                    new SolidBrush(ShindoColor[maxScale]),
                    10,
                    43,
                    278,
                    2
                );

                var lines = formatted
                    .Replace(" ", "\u3000")
                    .Replace("　", "  ")
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                using (var font = new Font(pfc.Families[0], 16f, FontStyle.Regular))
                {
                    float x = 10f;
                    float y = 49f;
                    float lineSpacing = font.GetHeight(g) - 10f;

                    foreach (var line in lines)
                    {
                        g.DrawString(line, font, Brushes.White, x, y);
                        y += lineSpacing;
                    }
                }
            }

            pictureBox1.Image = canvasText;
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            await client.StopAsync();
        }


        public static Dictionary<int, string> ShindoScale = new Dictionary<int, string>
    {
        { -1, "なし"},
        { 10, "1"},
        { 20, "2"},
        { 30, "3"},
        { 40, "4"},
        { 46, "5弱以上未入電"},
        { 45, "5弱"},
        { 50, "5強"},
        { 55, "6弱"},
        { 60, "6強"},
        { 70, "7"},
    };





        // JSON から読み込むため空で初期化。LoadShindoColors() が起動時に埋める。
        public static Dictionary<int, Color> ShindoColor = new Dictionary<int, Color>();

        /// <summary>
        /// shindo_colors.json (アプリケーション実行フォルダ) を優先して読み、なければ組み込みのデフォルト値を使います。
        /// JSON の値は次のいずれかをサポートします:
        ///  - "#RRGGBB" または "#AARRGGBB"
        ///  - { "r": 255, "g": 0, "b": 0, "a": 255 }
        ///  - [r,g,b] または [a,r,g,b]
        /// </summary>
        private void LoadShindoColors()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shindo_colors.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path, Encoding.UTF8);
                    ParseAndPopulateShindoColor(json);
                }
                else
                {
                    // ファイルがない場合は既存のハードコード値をフォールバックとして設定
                    PopulateDefaultShindoColor();
                }
            }
            catch
            {
                // 読み込みに失敗したらデフォルトを使用
                PopulateDefaultShindoColor();
            }
        }

        private void ParseAndPopulateShindoColor(string json)
        {
            var jo = JObject.Parse(json);
            foreach (var prop in jo.Properties())
            {
                if (!int.TryParse(prop.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out int key))
                {
                    continue;
                }

                var token = prop.Value;
                Color color = Color.Black;

                if (token.Type == JTokenType.String)
                {
                    color = ParseColorFromString(token.Value<string>());
                }
                else if (token.Type == JTokenType.Object)
                {
                    int a = token["a"]?.Value<int>() ?? 255;
                    int r = token["r"]?.Value<int>() ?? 0;
                    int g = token["g"]?.Value<int>() ?? 0;
                    int b = token["b"]?.Value<int>() ?? 0;
                    color = Color.FromArgb(a, r, g, b);
                }
                else if (token.Type == JTokenType.Array)
                {
                    var arr = token.ToObject<int[]>();
                    if (arr.Length == 3)
                    {
                        color = Color.FromArgb(255, arr[0], arr[1], arr[2]);
                    }
                    else if (arr.Length == 4)
                    {
                        // [a,r,g,b]
                        color = Color.FromArgb(arr[0], arr[1], arr[2], arr[3]);
                    }
                }

                ShindoColor[key] = color;
            }

            // 必要なキーが欠けている場合はデフォルト値で補う
            var defaults = GetDefaultShindoColorDictionary();
            foreach (var kv in defaults)
            {
                if (!ShindoColor.ContainsKey(kv.Key))
                    ShindoColor[kv.Key] = kv.Value;
            }
        }

        private static Color ParseColorFromString(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Color.Black;

            s = s.Trim();
            if (s.StartsWith("#"))
            {
                var hex = s.TrimStart('#');
                try
                {
                    if (hex.Length == 6)
                    {
                        int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                        int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                        int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                        return Color.FromArgb(255, r, g, b);
                    }
                    else if (hex.Length == 8)
                    {
                        int a = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                        int r = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                        int g = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                        int b = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                        return Color.FromArgb(a, r, g, b);
                    }
                }
                catch
                {
                    // fallthrough
                }
            }

            try
            {
                // 名前付き色や他の形式をサポート
                return ColorTranslator.FromHtml(s);
            }
            catch
            {
                return Color.Black;
            }
        }

        private void PopulateDefaultShindoColor()
        {
            ShindoColor = GetDefaultShindoColorDictionary();
        }

        private Dictionary<int, Color> GetDefaultShindoColorDictionary()
        {
            return new Dictionary<int, Color>
            {
                { -1, Color.FromArgb(255,0,0,0)},
                { 10, Color.FromArgb(104,104,112)},
                { 20, Color.FromArgb(0,132,255)},
                { 30, Color.FromArgb(50,179,100)},
                { 40, Color.FromArgb(255,224,93)},
                { 46, Color.FromArgb(254,180,22)},
                { 45, Color.FromArgb(254,180,22)},
                { 50, Color.FromArgb(255,102,0)},
                { 55, Color.FromArgb(255,0,0)},
                { 60, Color.FromArgb(160,0,0)},
                { 70, Color.FromArgb(100,0,100)},
            };
        }
    }
}
