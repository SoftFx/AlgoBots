using System.Globalization;

namespace SoftFx.Utility
{
    public static class Parser
    {
        public static bool TryGetDouble(string str, out double value)
        {
            return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static string InvariantString(double val, string format = "F1") => val.ToString(format, CultureInfo.InvariantCulture);
    }
}