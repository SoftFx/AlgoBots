using System.Linq;

namespace SoftFx.Common.Extensions
{
    public static class ParsingExtensions
    {
        /// <summary>
        /// Parses elements, separated by commas, from the line
        /// </summary>
        /// <param name="csvLine">A single line where elements are separated by commas</param>
        /// <returns>Elements parsed from the line</returns>
        public static string[] ParseCsvLine(this string csvLine)
        {
            return csvLine.Trim().Split(',').Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToArray();
        }
    }
}
