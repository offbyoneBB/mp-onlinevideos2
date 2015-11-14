using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.Utils
{
    /// <summary>
    /// Simple TV Guide grabber using data from xmltv.radiotimes.com
    /// </summary>
    class TVGuideGrabber
    {
        const string RADIO_TIMES_URL = "http://xmltv.radiotimes.com/xmltv/{0}.dat";
        static readonly Regex idReg = new Regex(@"[?&]guideid=(\d+)", RegexOptions.Compiled);

        public static bool TryGetId(string url, out string id)
        {
            Match match = idReg.Match(url);
            if (match.Success)
            {
                id = match.Groups[1].Value;
                return true;
            }
            id = null;
            return false;
        }

        public static string RemoveId(string url)
        {
            return idReg.Replace(url, "");
        }

        public static bool TryGetNowNextForChannel(string radioTimesId, out NowNextDetails nowNext)
        {
            nowNext = null;
            //Retrieve .dat page for channel
            string guide = WebCache.Instance.GetWebData(string.Format(RADIO_TIMES_URL, radioTimesId));
            if (string.IsNullOrEmpty(guide))
                return false;

            TimeZoneInfo gmt = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            DateTime guideTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, gmt);
            //split into individual programmes
            string[] programmes = guide.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string programme in programmes)
            {
                string[] programmeInfo = programme.Split('~');
                if (programmeInfo.Length != 23) //not a valid line
                    continue;

                DateTime progDate = DateTime.Parse(programmeInfo[19], new CultureInfo("en-GB")); //uk date format
                DateTime progStartTime = progDate.Add(TimeSpan.Parse(programmeInfo[20]));
                DateTime progEndTime = progDate.Add(TimeSpan.Parse(programmeInfo[21]));

                //Programme might span 2 days, e.g starts at 23.50 and ends at 01.00
                if (progEndTime < progStartTime)
                    progEndTime = progEndTime.AddDays(1);

                if (nowNext != null) //if previous was current programme, we must be on next programme
                {
                    nowNext.NextTitle = programmeInfo[0];
                    nowNext.NextStart = convertToLocalTime(progStartTime, gmt);
                    nowNext.NextEnd = convertToLocalTime(progEndTime, gmt);
                    break; //only get Now/Next
                }

                //if programme starts before current time and ends after current time it is currently playing
                if (progEndTime > guideTime && progStartTime <= guideTime)
                {
                    nowNext = new NowNextDetails();
                    nowNext.NowTitle = programmeInfo[0];
                    nowNext.NowDescription = programmeInfo[17];
                    nowNext.NowStart = convertToLocalTime(progStartTime, gmt);
                    nowNext.NowEnd = convertToLocalTime(progEndTime, gmt);
                }
            }
            return nowNext != null;
        }

        static string convertToLocalTime(DateTime dateTime, TimeZoneInfo sourceTimeZone)
        {
            return TimeZoneInfo.ConvertTime(dateTime, sourceTimeZone, TimeZoneInfo.Local).ToShortTimeString();
        }
    }

    class NowNextDetails
    {
        public string NowTitle { get; set; }
        public string NowStart { get; set; }
        public string NowEnd { get; set; }
        public string NowDescription { get; set; }
        public string NextTitle { get; set; }
        public string NextStart { get; set; }
        public string NextEnd { get; set; }

        public string Format(string format)
        {
            return format.Replace("<nowtitle>", NowTitle)
            .Replace("<nowdescription>", NowDescription)
            .Replace("<nowstart>", NowStart)
            .Replace("<nowend>", NowEnd)
            .Replace("<nexttitle>", NextTitle)
            .Replace("<nextstart>", NextStart)
            .Replace("<nextend>", NextEnd)
            .Replace("<newline>", Environment.NewLine);
        }
    }
}
