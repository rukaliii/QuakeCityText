using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuakeCityText
{
    public class StationNameShorter
    {
        private static Dictionary<string, string> map;

        public static void Initialize(string areaJsonPath)
        {
            var list = JsonConvert.DeserializeObject<List<Data1>>(File.ReadAllText(areaJsonPath));

            map = list
                .GroupBy(x => x.name)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().city.name
                );
        }

        public static string Shorten(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            if (map != null && map.TryGetValue(name, out var city))
                return city;

            int idx = name.IndexOf("市");
            if (idx >= 0) return name.Substring(0, idx + 1);

            idx = name.IndexOf("町");
            if (idx >= 0) return name.Substring(0, idx + 1);

            idx = name.IndexOf("村");
            if (idx >= 0) return name.Substring(0, idx + 1);

            return name;
        }
    }
    public class Data1
    {
        public Area area { get; set; }
        public City city { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string furigana { get; set; }
    }
    public class Area
    {
        public string code { get; set; }
        public string name { get; set; }
        public string furigana { get; set; }
    }

    public class City
    {
        public string code { get; set; }
        public string name { get; set; }
        public string furigana { get; set; }
    }
}
