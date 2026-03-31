using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuakeCityText
{
    public class StationNameShorter
    {
        public static readonly Regex ShortenPattern = new(
            @"^(?:(?:余市町|田村市|玉村町|東村山市|武蔵村山市|羽村市|十日町市|上市町|大町市|名古屋中村区|大阪堺市.+?区|下市町|大村市|野々市市|四日市市|廿日市市|大町町)|.+?村)?(.+?島|.+?[市区町村])",
            RegexOptions.Compiled
        );

        public static string Shorten(string name)
        {
            var match = ShortenPattern.Match(name);
            if (match.Success && match.Groups.Count > 1)
                return match.Groups[1].Value;
            return name;
        }
    }
}
