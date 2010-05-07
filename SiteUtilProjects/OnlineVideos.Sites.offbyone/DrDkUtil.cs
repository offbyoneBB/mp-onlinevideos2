using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class DrDkUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Url pointing to a html page with a list of video podcasts.")]
        string podcasts_url = "http://www.dr.dk/Podcast/video.htm";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the podcasts_url for podcasts. Group names: 'url', 'title', 'description'.")]
        string podcasts_regEx = @"<div\sclass=""content"">\s*
<div\sclass=""txtContent"">\s*
<h2>\s*<a[^>]*>(?<title>[^<]+)</a>\s*</h2>\s*
<p>(?<description>[^<]+)</p>
(?:(?!<a\stitle=""XML"").)*<a\stitle=""XML""\shref=""(?<url>[^""]+)""";
        [Category("OnlineVideosConfiguration"), Description("Url pointing to a html page with a list of programmes.")]
        string programs_url = "http://www.dr.dk/odp/default.aspx?template=programmer";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the programs_url for categories. Group names: 'url', 'title'.")]
        string programs_regEx = @"<li>\s*<h2>\s*
<a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a>\s*
</h2>\s*</li>\s*";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the program pages that were found with the programs_regEx earlier for clips. Group names: 'clip'.")]
        string videolist_regEx = @"clipList\.push\(new\sClip\((?<clip>(?:(?!\)\);).)*)\)\);";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the program pages for more info. Group names: 'thumb', 'desc', 'length'.")]
        string videolistExtended_regEx = @"<li[^>]*>\s*
<a[^>]*>[^<]*</a>\s*
(<img\ssrc=""(?<thumb>[^""]+)""[^>]*>)?
(?:(?!<p>).)*<p>(?<desc>(?:(?!</p>).)+)</p>\s*
(?:(?!<span>\s*\().)*<span>\s*\((?<length>[^\)]+)\)\s*</span>";

        Regex regEx_Podcasts, regEx_Programs, regEx_Videolist, regEx_VideolistExtended;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(podcasts_regEx)) regEx_Podcasts = new Regex(podcasts_regEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(programs_regEx)) regEx_Programs = new Regex(programs_regEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(videolist_regEx)) regEx_Videolist = new Regex(videolist_regEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(videolistExtended_regEx)) regEx_VideolistExtended = new Regex(videolistExtended_regEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Add(new Category() { Name = "Podcasts (MP4)", HasSubCategories = true });
            Settings.Categories.Add(new Category() { Name = "dr.dk TV (WMV)", HasSubCategories = true });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            if (parentCategory.Name == "Podcasts (MP4)")
            {
                string data = GetWebData(podcasts_url);
                if (!string.IsNullOrEmpty(data))
                {
                    Match m = regEx_Podcasts.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink()
                        {
                            Url = m.Groups["url"].Value,
                            Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim()),
                            Description = m.Groups["description"].Value,
                            ParentCategory = parentCategory
                        };
                        parentCategory.SubCategories.Add(cat);
                        m = m.NextMatch();
                    }
                }
            }
            else if (parentCategory.Name == "dr.dk TV (WMV)")
            {
                string data = GetWebData(programs_url);
                if (!string.IsNullOrEmpty(data))
                {
                    Match m = regEx_Programs.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink()
                        {
                            Url = m.Groups["url"].Value,
                            Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim()),
                            ParentCategory = parentCategory
                        };
                        cat.Url = HttpUtility.HtmlDecode(cat.Url);
                        if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(programs_url), cat.Url).AbsoluteUri;
                        parentCategory.SubCategories.Add(cat);
                        m = m.NextMatch();
                    }
                }
            }
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            if (category is RssLink)
            {                
                string url = ((RssLink)category).Url;
                string data = GetWebData(url);
                if (data.Length > 0)
                {
                    if (url.ToLower().EndsWith(".xml"))
                    {
                        foreach (RssItem rssItem in RssToolkit.Rss.RssDocument.Load(data).Channel.Items)
                        {
                            VideoInfo video = VideoInfo.FromRssItem(rssItem, true, new Predicate<string>(isPossibleVideo));
                            // only if a video url was set, add this Video to the list
                            if (!string.IsNullOrEmpty(video.VideoUrl)) videoList.Add(video);
                        }
                    }
                    else
                    {
                        Match m = regEx_Videolist.Match(data);
                        while (m.Success)
                        {
                            string[] myString = m.Groups["clip"].Value.Split(',');
                            VideoInfo videoInfo = new VideoInfo()
                            { 
                                Length = myString[4].Trim(new char[] {'\'', ' '}),
                                Title = myString[5].Trim(new char[] { '\'', ' ' }),
                                VideoUrl = myString[0].Trim(new char[] {'\'', ' '})
                            };
                            
                            videoInfo.VideoUrl = HttpUtility.HtmlDecode(videoInfo.VideoUrl);
                            if (!Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute)) videoInfo.VideoUrl = new Uri(new Uri(url), videoInfo.VideoUrl).AbsoluteUri;
                            // Remove geo check
                            videoInfo.VideoUrl = videoInfo.VideoUrl.Replace("http://geo.dr.dk/findLocation/default.aspx", "http://www.dr.dk/extention/playWindowsMediaODP.aframe");
                            videoInfo.VideoUrl = videoInfo.VideoUrl.Replace("http://geo.dr.dk/findLocation/", "http://www.dr.dk/extention/playWindowsMediaODP.aframe");
                            videoInfo.VideoUrl += "&location=mydrtv";

                            DateTime parsedDate;
                            if (DateTime.TryParse(videoInfo.Length, out parsedDate)) videoInfo.Length = parsedDate.ToString("g", OnlineVideoSettings.Instance.MediaPortalLocale);
                            
                            videoList.Add(videoInfo);
                            
                            m = m.NextMatch();
                        }
                        int i = 0;
                        m = regEx_VideolistExtended.Match(data);
                        while (m.Success)
                        {
                            VideoInfo videoInfo = videoList[i];

                            string img = m.Groups["thumb"].Value;
                            if (!string.IsNullOrEmpty(img)) videoInfo.ImageUrl = img;
                            videoInfo.Description = m.Groups["desc"].Value;
                            videoInfo.Length += " | " + m.Groups["length"].Value;

                            i++;
                            m = m.NextMatch();
                        }
                    }
                }
            }
            else if (category is Group)
            {
                foreach (Channel channel in ((Group)category).Channels)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = channel.StreamName;
                    video.VideoUrl = channel.Url;
                    video.ImageUrl = channel.Thumb;
                    videoList.Add(video);
                }
            }
            return videoList;
        }

        public override string getUrl(VideoInfo video)
        {
            if (video.VideoUrl.EndsWith("&location=mydrtv"))
            {
                // todo : starttime from asx!
                return ParseASX(video.VideoUrl)[0];
            }
            return base.getUrl(video);
        }
    }
}
