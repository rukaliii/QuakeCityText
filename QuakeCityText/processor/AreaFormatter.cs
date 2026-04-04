using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuakeCityText.Processor
{
    class AreaFormatter
    {
        public static string Format(Graphics g, List<string> areas, Font font, float maxWidth)
        {
            string text = string.Join(" ", areas);
            return cityintFormat.WrapTextByWidth(g, text, font, maxWidth);
        }

        public static string CompressToPref(List<JToken> points)
        {
            var prefList = points
                .Where(p => !string.IsNullOrEmpty(p["pref"]?.Value<string>()))
                .GroupBy(p => p["pref"]!.Value<string>())
                .Select(g =>
                {
                    var cities = g
                        .Select(p => p["addr"]?.Value<string>())
                        .Where(a => !string.IsNullOrEmpty(a))
                        .Select(a => StationNameShorter.Shorten(a))
                        .Distinct()
                        .Count();

                    return $"{g.Key.Replace("県", "").Replace("府", "").Replace("東京都", "東京")}({cities})";
                });

            return string.Join(" ", prefList);
        }
    }
}
