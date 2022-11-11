using System;

namespace SoftFx.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static string FullTime(this DateTime time) => time.ToString("HH:mm:ss.fff");

        public static string NormalDateForm(this DateTime date) => date.ToString("yyyy-MM-dd HH:mm:ss");

        public static string FullDateTime(this DateTime date) => date.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}
