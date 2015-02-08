using System;

namespace OnlineVideos.Helpers
{
    public static class TimeUtils
    {
        public static DateTime UNIXTimeToDateTime(double unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime).ToLocalTime();
        }

        /// <summary>
        /// Parse a string and return a canonical representation if it represented a time.
        /// </summary>
        /// <param name="duration">Should contain a number that is assumed to be in seconds.</param>
        /// <returns></returns>
        public static string TimeFromSeconds(string duration)
        {
            if (!string.IsNullOrEmpty(duration))
            {
                double seconds;
                if (double.TryParse(duration, System.Globalization.NumberStyles.None | System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"), out seconds))
                {
                    return new DateTime(TimeSpan.FromSeconds(seconds).Ticks).ToString("HH:mm:ss");
                }
                else return duration;
            }
            return "";
        }

        /// <summary>
        /// Example: time = 02:34:25.00 should result in 9265 seconds
        /// </summary>
        /// <returns></returns>
        public static double SecondsFromTime(string time)
        {
            try
            {
                double hours = 0.0d;
                double minutes = 0.0d;
                double seconds = 0.0d;

                double.TryParse(time.Substring(0, 2), out hours);
                double.TryParse(time.Substring(3, 2), out minutes);
                double.TryParse(time.Substring(6, 2), out seconds);

                seconds += (((hours * 60) + minutes) * 60);

                return seconds;
            }
            catch (Exception ex)
            {
                Log.Warn("Error getting seconds from StartTime ({0}): {1}", time, ex.Message);
                return 0.0d;
            }
        }
    }
}
