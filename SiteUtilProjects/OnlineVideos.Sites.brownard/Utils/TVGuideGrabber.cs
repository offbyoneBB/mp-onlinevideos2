using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Brownard.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.Utils
{
    /// <summary>
    /// Simple TV Guide grabber using data from radiotimes.com
    /// </summary>
    class TVGuideGrabber
    {
        const string RADIOTIMES_JSON_URL = "http://www.radiotimes.com/rt-service/schedule/get?startdate={0}&hours=3&totalWidthUnits=898&channels={1}";
        static readonly TimeZoneInfo GMT = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        static readonly Regex GUIDE_ID_REGEX = new Regex(@"[?&]guideid=(\d+)");

        public static bool TryGetIdAndRemove(ref string url, out string id)
        {
            Match match = GUIDE_ID_REGEX.Match(url);
            if (match.Success)
            {
                id = match.Groups[1].Value;
                url = url.Remove(match.Index, match.Length);
                return true;
            }
            id = null;
            return false;
        }

        public static bool TryGetNowNext(string radioTimesId, out NowNextDetails nowNext)
        {
            DateTime guideTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, GMT);
            string requestTime = new DateTime(guideTime.Year, guideTime.Month, guideTime.Day, guideTime.Hour, 0, 0).ToString("dd-MM-yyyy HH:mm:ss");
            string url = string.Format(RADIOTIMES_JSON_URL, requestTime, radioTimesId);

            JObject guideData = WebCache.Instance.GetWebData<JObject>(url);
            nowNext = null;
            if (guideData == null)
                return false;

            JArray channels = guideData["Channels"] as JArray;
            if (channels == null || channels.Count == 0)
                return false;

            JArray listings = channels[0]["TvListings"] as JArray;
            if (listings == null || listings.Count == 0)
                return false;

            foreach (JToken listing in listings)
            {
                //remove UTC indicator, the times don't appear to be UTC but GMT
                DateTime startTime = DateTime.Parse(listing.Value<string>("StartTimeMF").Replace("Z", ""), CultureInfo.InvariantCulture);
                DateTime endTime = DateTime.Parse(listing.Value<string>("EndTimeMF").Replace("Z", ""), CultureInfo.InvariantCulture);

                if (nowNext != null)
                {
                    nowNext.NextTitle = listing.Value<string>("Title");
                    nowNext.NextDescription = listing.Value<string>("Description");
                    nowNext.NextStart = convertToLocalTime(startTime, GMT);
                    nowNext.NextEnd = convertToLocalTime(endTime, GMT);
                    break;
                }

                if (startTime <= guideTime && endTime > guideTime)
                {
                    nowNext = new NowNextDetails
                    {
                        NowTitle = listing.Value<string>("Title"),
                        NowDescription = listing.Value<string>("Description"),
                        NowStart = convertToLocalTime(startTime, GMT),
                        NowEnd = convertToLocalTime(endTime, GMT)
                    };
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
        public string NextDescription { get; set; }

        public string Format(string format)
        {
            return format.Replace("<nowtitle>", NowTitle.HtmlCleanup())
            .Replace("<nowdescription>", NowDescription.HtmlCleanup())
            .Replace("<nowstart>", NowStart)
            .Replace("<nowend>", NowEnd)
            .Replace("<nexttitle>", NextTitle.HtmlCleanup())
            .Replace("<nextdescription>", NowDescription.HtmlCleanup())
            .Replace("<nextstart>", NextStart)
            .Replace("<nextend>", NextEnd)
            .Replace("<newline>", Environment.NewLine);
        }
    }
}