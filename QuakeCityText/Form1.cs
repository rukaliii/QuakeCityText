using Newtonsoft.Json.Linq;
using QuakeCityText.Processor;
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
        private JObject _lastMaxScaleData = null;
        private async void Form1_Load(object sender, EventArgs e)
        {
            byte[] fontBuf1 = Resources.NotoSansJP_VF;
            AddFont(fontBuf1);

            LoadShindoColors();
            StationNameShorter.Initialize("points.json");
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
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => DisplayMaxScaleRegion(data)));
                }
                else
                {
                    DisplayMaxScaleRegion(data);
                }
            };

            {
                _pageTimer = new System.Windows.Forms.Timer();
                _pageTimer.Interval = 80;
                _pageTimer.Tick += (s, e) =>
                {
                    if (_lastMaxScaleData != null)
                    {
                        DisplayMaxScaleRegion(_lastMaxScaleData);
                    }
                };
                _pageTimer.Start();
            }
        }
        private unsafe void AddFont(byte[] fontBuffer)
        {
            fixed (byte* pFontBuf = fontBuffer)
            {
                pfc.AddMemoryFont((IntPtr)pFontBuf, fontBuffer.Length);
            }
        }


        public static PrivateFontCollection pfc = new PrivateFontCollection();
        private System.Windows.Forms.Timer _pageTimer;
        private ProcessedQuakeData _cachedProcessed;

        public void DisplayMaxScaleRegion(JObject data)
        {
            if (data == null) return;

            _lastMaxScaleData = data;

            int _lastWidth = -1;
            bool widthChanged = _lastWidth != pictureBox1.Width;
            _lastWidth = pictureBox1.Width;

            if (_cachedProcessed == null || widthChanged)
            {
                var processed = EarthquakeDataProcessor.Process(data);
                if (processed == null) return;

                using (Bitmap tmp = new Bitmap(1, 1))
                using (Graphics g = Graphics.FromImage(tmp))
                using (var font = new Font(pfc.Families[0], 16f))
                {
                    float maxWidth = pictureBox1.Width - 20f;

                    string formatted = AreaFormatter.Format(
                        g,
                        processed.AreaList,
                        font,
                        maxWidth
                    );

                    processed.FormattedLines = formatted.Split('\n').ToList();
                }

                _cachedProcessed = processed;
            }

            pictureBox1.Image?.Dispose();

            pictureBox1.Image = QuakeRenderer.Render(
                pictureBox1.Size,
                _cachedProcessed,
                pfc,
                DateTime.Now,
                7.5
            );
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            await client.StopAsync();
        }




        //JSON から読み込むため空で初期化。LoadShindoColors() が起動時に埋める。
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
                    PopulateDefaultShindoColor();
                }
            }
            catch
            {
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

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            DisplayMaxScaleRegion(_lastMaxScaleData);
        }
    }
}
