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
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class Sf1Util : SiteUtilBase
    {
        //<img class="az_thumb" src="http://videoportal.sf.tv/cvis/videogroup/thumbnail/6fd27ab0-d10f-450f-aaa9-836f1cac97bd?width=80" width="80" height="45" alt="1 gegen 100" /></a><a class="sendung_name" href="/sendung?id=6fd27ab0-d10f-450f-aaa9-836f1cac97bd">1 gegen 100</a><p class="az_description">Gameshow, in der ein Kandidat gegen 100 Kontrahenten antritt.</p>
        string categoryRegex = @"<img\sclass=""az_thumb""\ssrc=""(?<thumb>[^""]+)""\swidth=""\d*""\sheight=""\d*""\salt=""(?<alt>[^""]+)""\s/></a><a\sclass=""sendung_name""\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a><p\sclass=""az_description"">(?<desc>[^<]+)</p>";

        //a href="/video?id=2033591c-a8fa-4490-bdd8-e58ceed95d9d;DCSext.zugang=videoportal_sendungsuebersicht">4. Januar 2010</a>
        string videoItemRegex = @"a\shref=""/video\?id=(?<url>[^;]+);DCSext.zugang=videoportal_sendungsuebersicht"">(?<title>[^<]+)</a>";

        //{"codec_video":"wmv3","codec_audio":"wmav2","bitrate":1500,"frame_width":640,"frame_height":359,"url":"http://www.sf.tv/wvxgen/index.php/vod/1gegen100/2009/10/1gegen100_20091026_200823_1500k.wmv?start=00:00:00.000&end=00:54:00.000&purged=true"}
        string videoUrlRegex = @"""codec_video"":""(?<video>[^""]+)"",""codec_audio"":""(?<audio>[^""]+)"",""bitrate"":(?<bitrate>[^,]+),""frame_width"":\d*,""frame_height"":\d*,""url"":""(?<url>[^""]+)""";

        string baseUrl = "http://videoportal.sf.tv/sendungen";

        Regex regEx_Category;
        Regex regEx_VideoItem;
        Regex regEx_VideoUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoItem = new Regex(videoItemRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoUrl = new Regex(videoUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl);
            string xmlUrl = String.Empty;

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = m.Groups["title"].Value;
                    cat.Url = "http://videoportal.sf.tv/rss/" + m.Groups["url"].Value;
                    cat.Thumb = m.Groups["thumb"].Value.Replace("?width=80", "");
                    cat.Description = m.Groups["desc"].Value;
                    cat.HasSubCategories = true;

                    Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }
            }
            return 0;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)parentCategory).Url).Channel.Items)
            {
                RssLink cat = new RssLink();

                cat.Name = rssItem.Title;
                cat.SubCategoriesDiscovered = true;
                cat.HasSubCategories = false;
                Match m = regEx_VideoItem.Match(rssItem.Description);
                if (m.Success)
                {
                    cat.Url = "http://videoportal.sf.tv/cvis/segment/" + m.Groups["url"].Value + "/.json";
                    cat.ParentCategory = parentCategory;
                    parentCategory.SubCategories.Add(cat);
                }
            }
            return Settings.Categories.Count;
        }

        public override String getUrl(VideoInfo video)
        {
            if(video.Title.Contains("WMV"))
            {
                return ParseASX(video.VideoUrl)[0];
            }
            else
            {
                string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}", OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(video.VideoUrl));
                return resultUrl;
            }
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();

            string data = GetWebData(((RssLink)category).Url);
            Match m = regEx_VideoUrl.Match(data);
            while (m.Success)
            {
                VideoInfo video = new VideoInfo();
                if (m.Groups["video"].Value.Contains("wmv3"))
                {
                    video.Title = "WMV (" + m.Groups["bitrate"].Value + "K)";
                    video.VideoUrl = m.Groups["url"].Value.Substring(0,m.Groups["url"].Value.IndexOf("?"));
                }
                else
                {
                    video.Title = "FLV (" + m.Groups["bitrate"].Value + "K)";
                    video.VideoUrl = m.Groups["url"].Value;
                }
                if( !video.VideoUrl.Contains("no streaming"))videoList.Add(video);
                m = m.NextMatch();
            }
            return videoList;
        }
    }
}