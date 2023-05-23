using System;

namespace Shared.Domain.Common
{
    public static class TimeEpochUtility
    {
        public static readonly DateTime EPOCH_DATE = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static int SecondsSinceUtcEpoch()
        {
            TimeSpan elapsedTime = DateTime.UtcNow - EPOCH_DATE;
            return (int)elapsedTime.TotalSeconds;
        }

        public static int SecondsElapsed(long epochtime)
        {
            DateTime date = EPOCH_DATE.AddSeconds(epochtime);
            TimeSpan elapsedTime = DateTime.UtcNow - date;
            return (int)elapsedTime.TotalSeconds;
        }

        public static DateTime UtcDateFromEpoch (int epochtime)
        {
            return EPOCH_DATE.AddSeconds(epochtime);
        }

        public static int UtcEpochFromDate(DateTime date)
        {
            TimeSpan elapsedTime = date - EPOCH_DATE;
            return (int)elapsedTime.TotalSeconds;
        }

        public static int CalendarPeriodKeyFromDate(DateTime date)
        {
            var result = date.Year * 1000000 + date.Month * 10000 + date.Day * 100 + date.Hour;
            return result;
        }

        public static DateTime? DateFromCalendarPeriodKey(int periodKey)
        {
            DateTime? result = null;
            int len = (int)Math.Ceiling(Math.Log10(periodKey));
            if (len > 6)
            {
                int year = periodKey / 1000000;
                int month = (periodKey - year * 1000000) / 10000;
                int day = (periodKey - year * 1000000 - month * 10000) / 100;
                int hour = periodKey - year * 1000000 - month * 10000 - day * 100;

                result = new DateTime(year, month, day, hour, 0 , 0);
            }
            return result;
        }
    }
}
