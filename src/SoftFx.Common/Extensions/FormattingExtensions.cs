using System;
using TickTrader.Algo.Api.Math;

namespace SoftFx.Common.Extensions
{
    public static class FormattingExtensions
    {
        /// <summary>
        /// Converts to string with fixed number of fractional digits
        /// </summary>
        /// <returns>Value string</returns>
        public static string ToString(this double val, int digits)
        {
            return val.ToString($"F{digits}");
        }

        /// <summary>
        /// Rounds value to <code>digits</code> and converts to string with fixed number of fractional digits
        /// </summary>
        /// <returns>Rounded value string</returns>
        public static string ToRoundedString(this double val, int digits)
        {
            return val.Round(digits).ToString($"F{digits}");
        }

        /// <summary>
        /// Converts to string with fixed number of fractional digits
        /// </summary>
        /// <returns>Value string</returns>
        public static string ToString(this double val, double step)
        {
            return val.ToString($"F{step.Digits()}");
        }

        /// <summary>
        /// Rounds value to <code>step</code> and converts to string with fixed number of fractional digits
        /// </summary>
        /// <returns>Rounded value string</returns>
        public static string ToRoundedString(this double val, double step)
        {
            return val.Round(step).ToString($"F{step.Digits()}");
        }

        /// <summary>
        /// Converts to string with at least <code>minDigits</code> fractional digits
        /// and optional <code>maxDigits - minDigits</code> fractional digits
        /// </summary>
        /// <returns>Value string</returns>
        public static string ToString(this double val, int minDigits, int maxDigits)
        {
            return val.ToString($"0.{new string('0', minDigits)}{new string('#', maxDigits)}");
        }

        /// <summary>
        /// Round value to <code>maxDigits</code> and converts to string with at least
        /// <code>minDigits</code> fractional digits and optional <code>maxDigits - minDigits</code> fractional digits
        /// </summary>
        /// <returns>Rounded value string</returns>
        public static string ToRoundedString(this double val, int minDigits, int maxDigits)
        {
            return val.Round(maxDigits).ToString($"0.{new string('0', minDigits)}{new string('#', maxDigits)}");
        }

        /// <summary>
        /// Convert to sortable time string. Format: "yyyy-MM-dd HH:mm:ss.fff"
        /// </summary>
        /// <returns>Value string</returns>
        public static string ToLogTimeString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.ffff");
        }
    }
}
