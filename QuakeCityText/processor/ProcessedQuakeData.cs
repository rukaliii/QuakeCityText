using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuakeCityText.Processor
{
    class ProcessedQuakeData
    {
        public int MaxScale { get; set; }
        public int MunicipalityCount { get; set; }
        public List<string> AreaList { get; set; }
        public List<JToken> RawPoints { get; set; }
        public List<string> FormattedLines { get; set; } = new List<string>();
        public JObject RawData { get; set; }
    }
}
