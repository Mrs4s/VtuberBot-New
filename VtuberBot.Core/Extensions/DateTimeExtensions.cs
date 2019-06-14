using System;
using System.Collections.Generic;
using System.Text;

namespace VtuberBot.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToTimestamp(this DateTime @this)
        {
            var dateStart = new DateTime(1970, 1, 1, 8, 0, 0);
            return  (long)(@this - dateStart).TotalSeconds;
        }

        public static DateTime TimestampToDateTime(long ts,bool usec=false)
        {
            var dtStart = new DateTime(1970, 1, 1, 8, 0, 0);
            var lTime = usec ? ts : ts * 10000000;
            var toNow = new TimeSpan(lTime);
            var targetDt = dtStart.Add(toNow);
            return targetDt;
        }
    }
}
