using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuakeCityText
{
    public class cityintFormat
    {
        public static string FormatAreaString(string area, int maxCharactersPerLine)
        {
            List<int> breakIndexes = new List<int>();
            int currentIndex = maxCharactersPerLine;
            while (currentIndex < area.Length)
            {
                while (currentIndex > 0 && !char.IsWhiteSpace(area[currentIndex]))
                {
                    currentIndex--;
                }
                if (currentIndex == 0)
                {
                    breakIndexes.Add(maxCharactersPerLine);
                    currentIndex = maxCharactersPerLine;
                }
                else
                {
                    breakIndexes.Add(currentIndex + 1);
                    currentIndex += maxCharactersPerLine;
                }
            }
            int i = 0;
            foreach (int breakIndex in breakIndexes.OrderByDescending((int j) => j))
            {
                area = area.Insert(breakIndex, Environment.NewLine);
                i++;
            }
            return area;
        }

        public static string WrapTextByWidth(Graphics g, string text, Font font, float maxWidth)
        {
            var words = text.Split(' ');
            var lines = new List<string>();

            string currentLine = "";

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine)
                    ? word
                    : currentLine + " " + word;

                var size = g.MeasureString(testLine, font);

                if (size.Width > maxWidth)
                {
                    if (!string.IsNullOrEmpty(currentLine))
                        lines.Add(currentLine);

                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);

            return string.Join("\n", lines);
        }
    }
}
