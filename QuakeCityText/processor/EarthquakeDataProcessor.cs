using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuakeCityText.Processor
{
    class EarthquakeDataProcessor
    {
        public static ProcessedQuakeData Process(JObject data)
        {
            if (data["code"]?.Value<int>() != 551) return null;

            var points = data["points"]?.ToObject<JArray>();
            if (points == null) return null;

            int maxScale = data["earthquake"]?["maxScale"]?.Value<int?>() ?? 0;

            var maxPoints = points
    .Where(p => (p["scale"]?.Value<int?>() ?? -1) == maxScale)
    .ToList();

            var addrList = maxPoints
                .Select(p => p["addr"]?.Value<string>())
                .Where(addr => !string.IsNullOrEmpty(addr))
                .Select(a => StationNameShorter.Shorten(a))
                .Distinct()
                .ToList();

            return new ProcessedQuakeData
            {
                MaxScale = maxScale,
                MunicipalityCount = addrList.Count,
                AreaList = addrList,
                RawPoints = maxPoints
            };
        }
    }
}
