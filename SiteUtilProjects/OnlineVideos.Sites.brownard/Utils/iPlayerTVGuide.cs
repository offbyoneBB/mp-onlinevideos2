using OnlineVideos.Sites.Brownard.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.Utils
{
    class iPlayerTVGuide
    {
        const string GUIDE_URL = "http://www.bbc.co.uk/iplayer/schedules/{0}";
        static readonly Regex idReg = new Regex(@"[?&]guideid=([^&]*)");
        static readonly Regex guideReg = new Regex(@"<div class=""broadcast-start"">([^<]*)</div>.*?<div class=""title"">([^<]*)</div>\s*<div class=""subtitle"">([^<]*)</div>.*?<p class=""synopsis"">\s*<span>([^<]*)</span>", RegexOptions.Singleline);

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

        public static bool TryGetNowNextForChannel(string id, out NowNextInfo nowNext)
        {
            nowNext = null;
            string guide = WebCache.Instance.GetWebData(string.Format(GUIDE_URL, id));
            if (string.IsNullOrEmpty(guide))
                return false;

            TimeZoneInfo gmt = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            DateTime guideTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, gmt);
            DateTime guideDate = guideTime.Date;
            DateTime lastStartTime = guideDate;

            var matches = guideReg.Matches(guide);
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                string startTimeStr = match.Groups[1].Value.Trim();
                DateTime startTime = guideDate.Add(TimeSpan.Parse(startTimeStr));
                if (startTime < lastStartTime)
                {
                    guideDate.AddDays(1);
                    startTime.AddDays(1);
                }
                lastStartTime = startTime;

                string endTimeStr = i < matches.Count - 1 ? matches[i + 1].Groups[1].Value.Trim() : null;

                if (nowNext != null)
                {
                    nowNext.NextTitle = formatTitle(match.Groups[2].Value, match.Groups[3].Value);
                    nowNext.NextStart = startTimeStr;
                    nowNext.NextEnd = endTimeStr;
                    return true;
                }

                if (startTime > guideTime)
                    return false;

                if (startTime <= guideTime && (endTimeStr == null || guideDate.Add(TimeSpan.Parse(endTimeStr)) > guideTime))
                {
                    nowNext = new NowNextInfo
                    {
                        NowStart = startTimeStr,
                        NowEnd = endTimeStr,
                        NowTitle = formatTitle(match.Groups[2].Value, match.Groups[3].Value),
                        NowDescription = match.Groups[4].Value.HtmlCleanup()
                    };
                }
            }
            return false;
        }

        static string formatTitle(string title, string subtitle)
        {
            if (!string.IsNullOrEmpty(subtitle))
                return string.Format("{0}: {1}", title.HtmlCleanup(), subtitle.HtmlCleanup());
            return title.HtmlCleanup();
        }
    }

    class NowNextInfo
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