using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites
{
    public class Sat1Util : SiteUtilBase
    {
        public enum ShowClips { OnlyFullEpisodes, FullAndClips };

        [Category("OnlineVideosUserConfiguration"), Description("Filter short movies and clips and show only full episodes.")]
        ShowClips showClips = ShowClips.FullAndClips;

        int pastSearch = 3;
        int futureSearch = 1;

        string baseUrl = "http://cms030.dc2.qcn3.movenetworks.com/cms/publish/vod2/vodchannelcatalog/sat1/15/catalog.json";
        string vidListBaseUrl = "http://cms030.dc2.qcn3.movenetworks.com/cms/publish/pro7/vodtldatesorted//";

        string rtmp_d_at_ch = "rtmpt://pssimsat1fs.fplive.net:80/pssimsat1ls/geo_d_at_ch/";
        string rtmp_d = "rtmpt://pssimsat1fs.fplive.net:80/pssimsat1ls/geo_d/";
        string rtmp_ww = "rtmpt://pssimsat1fs.fplive.net:80/pssimsat1ls/geo_worldwide/";

        string nameRegex = @"""name"":\s""(?<title>[^""]+)"",";
        string idRegex = @"""id"":\s(?<id>[^,]+),""";
        string clipFileNameRegex = @"""uploadFilename"":\s""(?<name>[^""]+)"",";
        string clipBroadcastDateRegex = @"""broadcast_date"":\s""(?<tag>[^""]+)"",";
        string clipVideoTypeRegex = @"""video_type"":\s""(?<tag>[^""]+)"",";
        string clipDescriptionRegex = @"""description"":\s""(?<tag>[^""]+)"",";
        string clipDurationRegex = @"""estDuration"":\s(?<tag>[^,]+),";
        string thumb_urlRegex = @"""thumb_url"":\s""(?<thumb>[^""]+)""";
        string cliplistRegex = @"clipList"":\s\[(?<cliplist>[^\]]+)\]";
        string clipGeoRegex = @"""geoblocking"":\s""(?<tag>[^""]+)"",";
        string clipExtRegex = @"""flashSuffix"":\s""(?<tag>[^""]+)"",";

        Regex regEx_Name;
        Regex regEx_Id;
        Regex regEx_Thumb;
        Regex regEx_Cliplist;
        Regex regEx_ClipFileName;
        Regex regEx_clipBroadcastDate;
        Regex regEx_clipVideoType;
        Regex regEx_clipDescription;
        Regex regEx_clipDuration;
        Regex regEx_clipGeo;
        Regex regEx_clipExt;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Name = new Regex(nameRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Id = new Regex(idRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Thumb = new Regex(thumb_urlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Cliplist = new Regex(cliplistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_ClipFileName = new Regex(clipFileNameRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_clipBroadcastDate = new Regex(clipBroadcastDateRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_clipVideoType = new Regex(clipVideoTypeRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_clipDescription = new Regex(clipDescriptionRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_clipDuration = new Regex(clipDurationRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_clipGeo = new Regex(clipGeoRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_clipExt = new Regex(clipExtRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        private string HtmlDecodeEx(string s)
        {
            string ret = HttpUtility.HtmlDecode(s);
            ret = ret.Replace("\\u00fc", "ü");
            ret = ret.Replace("\\u00dc", "Ü");
            ret = ret.Replace("\\u00e4", "ä");
            ret = ret.Replace("\\u00c4", "Ä");
            ret = ret.Replace("\\u00df", "ß");
            ret = ret.Replace("\\u00f6", "ö");

            ret = ret.Replace("\n", "");
            return ret;
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string data = GetWebData(baseUrl);

            if (!string.IsNullOrEmpty(data))
            {
                int idx1, idx2;

                idx1 = data.IndexOf("{\"name\": \"");
                while (idx1 > -1)
                {
                    idx2 = data.IndexOf("{\"name\": \"", idx1 + 1);
                    string cut = "";
                    if (idx2 > idx1)
                        cut = data.Substring(idx1, idx2 - idx1);
                    else
                        cut = data.Substring(idx1);

                    RssLink cat = new RssLink();

                    Match m = regEx_Name.Match(cut);
                    if (m.Success)
                    {
                        cat.Name = HtmlDecodeEx(m.Groups["title"].Value);
                        m = regEx_Id.Match(cut);
                        if (m.Success)
                        {
                            cat.Url = m.Groups["id"].Value;
                            Match n = regEx_Thumb.Match(cut);
                            if (n.Success)
                            {
                                if (cat.Name.Contains("Staffel") || cat.Name.Contains("Woche") || cat.Name.Contains("/"))
                                    Settings.Categories[Settings.Categories.Count - 1].Thumb = n.Groups["thumb"].Value;
                                else
                                    cat.Thumb = n.Groups["thumb"].Value;

                            }
                            if (cut.Contains("link_url") && !cat.Name.Contains("Staffel") && !cat.Name.Contains("hidden"))
                                Settings.Categories.Add(cat);
                        }
                    }
                    idx1 = idx2;
                }
                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            string url = video.VideoUrl;
            string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}", OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(url));
            return resultUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            DateTime startDate = DateTime.Today.Subtract(DateTime.Today.AddMonths(pastSearch).Subtract(DateTime.Today));
            string start = string.Format("{0:00}{1:00}{2:00}", startDate.Year - 2000, startDate.Month, startDate.Day);
            string end = string.Format("{0:00}{1:00}{2:00}", startDate.Year - 2000, startDate.AddMonths(pastSearch + futureSearch).Month, startDate.Day);
            
            string url = vidListBaseUrl + (category as RssLink).Url + "/" + start + "/" + end + "/listing.json";
            string data = GetWebData(url);

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Cliplist.Match(data);
                while (m.Success)
                {
                    int idx1, idx2;

                    data = m.Groups["cliplist"].Value;
                    idx1 = data.IndexOf("{\"name\": \"");
                    while (idx1 > -1)
                    {
                        string cut = "";
                        VideoInfo video = new VideoInfo();

                        idx2 = data.IndexOf("{\"name\": \"", idx1 + 1);
                        if (idx2 > idx1)
                            cut = data.Substring(idx1, idx2 - idx1);
                        else
                            cut = data.Substring(idx1);

                        Match n = regEx_Name.Match(cut);
                        if (n.Success)
                        {
                            video.Title = HtmlDecodeEx(n.Groups["title"].Value);
                            Match o = regEx_ClipFileName.Match(cut);
                            if (o.Success)
                            {
                                Match q = regEx_clipGeo.Match(cut);
                                if (q.Success)
                                {
                                    if (q.Groups["tag"].Value.Contains("ww"))
                                        video.VideoUrl = rtmp_ww;

                                    else if (q.Groups["tag"].Value.Contains("de_at_ch"))
                                        video.VideoUrl = rtmp_d_at_ch;

                                    else
                                        video.VideoUrl = rtmp_d;

                                }
                                else
                                    video.VideoUrl = rtmp_d_at_ch;

                                q = regEx_clipExt.Match(cut);
                                if (q.Success)
                                {
                                    if (q.Groups["tag"].Value.Contains("mp4"))
                                    {
                                        video.VideoUrl = video.VideoUrl + "mp4:";
                                        video.VideoUrl = video.VideoUrl + o.Groups["name"].Value;
                                        video.VideoUrl = video.VideoUrl.Substring(0, video.VideoUrl.Length - 3);
                                        video.VideoUrl = video.VideoUrl + "f4v";
                                    }
                                    else
                                    {
                                        video.VideoUrl = video.VideoUrl + o.Groups["name"].Value;
                                        video.VideoUrl = video.VideoUrl.Substring(0, video.VideoUrl.Length - 3);
                                        video.VideoUrl = video.VideoUrl + q.Groups["tag"].Value;
                                    }
                                }
                                else
                                {
                                    video.VideoUrl = video.VideoUrl + o.Groups["name"].Value;
                                    video.VideoUrl = video.VideoUrl.Substring(0, video.VideoUrl.Length - 3);
                                    video.VideoUrl = video.VideoUrl + "flv";
                                }

                                Match p = regEx_clipDescription.Match(cut);
                                if (p.Success)
                                    video.Description = HtmlDecodeEx(p.Groups["tag"].Value);

                                p = regEx_clipDuration.Match(cut);
                                if (p.Success)
                                    video.Length = p.Groups["tag"].Value;

                                p = regEx_clipBroadcastDate.Match(cut);
                                if (p.Success)
                                    video.Title = video.Title + " (" + p.Groups["tag"].Value + ")";

                                if (showClips == ShowClips.OnlyFullEpisodes)
                                {
                                    p = regEx_clipVideoType.Match(cut);
                                    if (p.Success)
                                    {
                                        if (p.Groups["tag"].Value.Contains("full"))
                                            videos.Add(video);
                                    }
                                }
                                else
                                {
                                    p = regEx_clipVideoType.Match(cut);
                                    if (p.Success)
                                    {
                                        if (!p.Groups["tag"].Value.Contains("full"))
                                            video.Title = video.Title + " (clip)";
                                    }
                                    else
                                        video.Title = video.Title + " (clip)";
                                    videos.Add(video);
                                }

                            }

                        }

                        idx1 = idx2;
                    }
                    m = m.NextMatch();
                }
            }
            if (videos.Count < 1 && pastSearch != 10)
            {
                pastSearch = 10;
                return getVideoList(category);
            }
            pastSearch = 3;
            return videos;
        }
    }
}