using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuakeCityText.Processor
{
    class QuakeRenderer
    {
        public static Bitmap Render(
            Size size,
            ProcessedQuakeData data,
            PrivateFontCollection pfc,
            DateTime now,
            double pageDurationSec = 2.0
        )
        {
            Bitmap bmp = new Bitmap(size.Width, size.Height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                using (var fontTitle = new Font(pfc.Families[0], 23f))
                using (var fontBody = new Font(pfc.Families[0], 16f))
                {
                    // ---- タイトル ----
                    g.DrawString(
                        $"震度{ShindoScale[data.MaxScale]} ({data.MunicipalityCount})",
                        fontTitle,
                        Brushes.White,
                        10f, 0f);


                    float startY = 49f;
                    float bottomMargin = 0f;
                    float spacing = fontBody.GetHeight(g) - 10f;
                    if (spacing <= 0) spacing = fontBody.GetHeight(g);

                    int maxLines = Math.Max(1, (int)((size.Height - startY - bottomMargin - 6f) / spacing));

                    var lines = data.FormattedLines ?? new List<string>();

                    var pages = new List<List<string>>();
                    for (int i = 0; i < lines.Count; i += maxLines)
                    {
                        pages.Add(lines.Skip(i).Take(maxLines).ToList());
                    }
                    if (pages.Count == 0) pages.Add(new List<string>());



                    double totalSec = now.TimeOfDay.TotalSeconds;
                    int pageIndex = (int)(totalSec / pageDurationSec) % pages.Count;
                    var drawLines = pages[pageIndex];
                    float y = startY;
                    foreach (var line in drawLines)
                    {
                        g.DrawString(line, fontBody, Brushes.White, 10f, y);
                        y += spacing;
                    }



                    if (pages.Count >= 2)
                    {
                        string pageText = $"{pageIndex + 1}/{pages.Count}";
                        var pageSize = g.MeasureString(pageText, fontBody);
                        g.DrawString(pageText, fontBody, Brushes.White,
                            size.Width - pageSize.Width - 10f, 13f);


                        double progress = (totalSec % pageDurationSec) / pageDurationSec;
                        if (progress < 0) progress = 0;
                        if (progress > 1) progress = 1;

                        float barHeight = 4f;
                        float barY = size.Height - barHeight;

                        float barWidth = (float)((size.Width-20) * progress);

                        using (var bgBrush = new SolidBrush(Color.DarkSlateGray))
                        {
                            g.FillRectangle(bgBrush, 10, 43, size.Width - 20, 2);
                        }

                        using (var fgBrush = new SolidBrush(Form1.ShindoColor[data.MaxScale]))
                        {
                            g.FillRectangle(fgBrush, 10, 43, barWidth, 2);
                        }
                    }
                    else
                    {
                        g.FillRectangle(
                            new SolidBrush(Form1.ShindoColor[data.MaxScale]),
                            10, 43, size.Width - 20, 2);
                    }
                }
            }

            return bmp;
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
    }
}
