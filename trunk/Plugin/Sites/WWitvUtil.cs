using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class WWitvUtil : SiteUtilBase
    {
        string baseUrl = "http://www.wwitv.com/";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the baseUrl for categories.")]
        string categoriesRegex = @"<a\sclass=""rb""\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)</a>";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the videos for a category.")]
        string videosRegex = @"<a\sclass=""travel""\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a>
</td><td[^>]*><img\ssrc=""../img/[^\.]+\.gif""\swidth=""50""\sheight=""24""></td>
<td[^>]*>\s*
<a\sclass=""m""\shref=""[^""]+"">(?<length>[^<]+)</a>\s*
</td><td[^>]*><font[^>]*>(?<desc>[^>]*)</td></tr>";


        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the playlist url for a video.")]
        string playlistRegex = @"<param\sname=""src""\svalue=""(?<url>[^""]+)""\s*/>";
        

        Regex regEx_Categories;
        Regex regEx_Videos;
        Regex regEx_Playlist;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Categories = new Regex(categoriesRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videos = new Regex(videosRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Playlist = new Regex(playlistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Categories.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = m.Groups["url"].Value;
                    if (!cat.Url.StartsWith("http://")) cat.Url = baseUrl + cat.Url;
                    cat.Name = m.Groups["title"].Value.Trim();
                    m = m.NextMatch();
                    Settings.Categories.Add(cat);
                }
                Settings.DynamicCategoriesDiscovered = true;
            }
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData((category as RssLink).Url);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Videos.Match(data);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoUrl = m.Groups["url"].Value;
                    if (!video.VideoUrl.StartsWith("http://")) video.VideoUrl = baseUrl + video.VideoUrl;
                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.Length = m.Groups["length"].Value;
                    video.Description = m.Groups["desc"].Value;
                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Playlist.Match(data);
                if (m.Success)
                {
                    return m.Groups["url"].Value;
                }
            }
            return null;
        }
    }
}
