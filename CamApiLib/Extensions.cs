using System;
using System.Threading;

namespace CamApiExtensions
{
    public static class ApiExtensions
    {
        public static DateTime FromUnixTime(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static long ToUnixTime(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

        public static long ToUnixTime(this DateTime? date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return date == null ? Convert.ToInt64((DateTime.Now - epoch).TotalSeconds) : Convert.ToInt64(((DateTime)date - epoch).TotalSeconds);
        }
    }
}