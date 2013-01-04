using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class Sf1Util : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for dynamic categories. Group names: 'url', 'title', 'thumb', 'description'. Will be used on the web pages resulting from the links from the dynamicCategoriesRegEx. Will not be used if not set.")]
        protected string dynamicSubCategoriesRegEx;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos. Group names: 'VideoUrl', 'ImageUrl', 'Title', 'Duration', 'Description', 'Airdate'.")]
        protected string videoListRegEx;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the actual playback urls with some info ('audio', 'video', 'bitrate', 'url').")]
        protected string videoUrlRegex;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the video url of an item that was found in the rss.")]
        protected string videoUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to split the categories html for subcategories")]
        protected string subCategoriesSplit;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to get the title for a subcategory")]
        protected string subCategoriesName;

        Regex regEx_dynamicSubCategories, regEx_VideoList, regEx_VideoUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_dynamicSubCategories = new Regex(dynamicSubCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            regEx_VideoList = new Regex(videoListRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            regEx_VideoUrl = new Regex(videoUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            foreach (var cat in Settings.Categories)
            {
                cat.HasSubCategories = true;
                cat.SubCategories = new List<Category>();
            }
            Settings.DynamicCategoriesDiscovered = true;
            return 0;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData((parentCategory as RssLink).Url);
            var splits = Regex.Split(data, subCategoriesSplit);
            bool first = true;
            foreach (string split in splits)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                string groupTitle = Regex.Match(split, subCategoriesName).Groups["title"].Value;
                Category group = new Category() { Name = groupTitle, ParentCategory = parentCategory, HasSubCategories = true, SubCategories = new List<Category>(), SubCategoriesDiscovered = true };
                Match m = regEx_dynamicSubCategories.Match(split);
                while (m.Success)
                {
                    RssLink subCat = new RssLink();
                    subCat.Url = m.Groups["url"].Value;
                    if (!String.IsNullOrEmpty(subCat.Url) && !Uri.IsWellFormedUriString(subCat.Url, System.UriKind.Absolute))
                        subCat.Url = new Uri(new Uri((parentCategory as RssLink).Url), subCat.Url).AbsoluteUri;
                    subCat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                    subCat.Thumb = m.Groups["thumb"].Value;
                    subCat.Thumb = subCat.Thumb.Substring(0, subCat.Thumb.IndexOf("?"));
                    if (!String.IsNullOrEmpty(subCat.Thumb) && !Uri.IsWellFormedUriString(subCat.Thumb, System.UriKind.Absolute))
                        subCat.Thumb = new Uri(new Uri((parentCategory as RssLink).Url), subCat.Thumb).AbsoluteUri;
                    subCat.Description = m.Groups["description"].Value;
                    subCat.ParentCategory = group;
                    group.SubCategories.Add(subCat);
                    m = m.NextMatch();
                }
                parentCategory.SubCategories.Add(group);
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();

            string data = GetWebData((category as RssLink).Url);
            if (data.Length > 0)
            {
                if (regEx_VideoList != null)
                {
                    Match m = regEx_VideoList.Match(data);
                    while (m.Success)
                    {
                        VideoInfo videoInfo = new VideoInfo();
                        videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                        videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                        if (!String.IsNullOrEmpty(videoInfo.VideoUrl) && !Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute))
                            videoInfo.VideoUrl = new Uri(new Uri((category as RssLink).Url), videoInfo.VideoUrl).AbsoluteUri;
                        videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                        if (!String.IsNullOrEmpty(videoInfo.ImageUrl) && !Uri.IsWellFormedUriString(videoInfo.ImageUrl, System.UriKind.Absolute))
                            videoInfo.ImageUrl = new Uri(new Uri((category as RssLink).Url), videoInfo.ImageUrl).AbsoluteUri;
                        videoInfo.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value);
                        videoList.Add(videoInfo);
                        m = m.NextMatch();
                    }
                }
            }

            return videoList;
        }

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(string.Format(videoUrlFormatString, HttpUtility.ParseQueryString(new Uri(video.VideoUrl).Query)["id"]));
            Match m = regEx_VideoUrl.Match(data);
            video.PlaybackOptions = new Dictionary<string, string>();
            while (m.Success)
            {
                if (!m.Groups["url"].Value.Contains("no streaming"))
                {
                    if (m.Groups["video"].Value.Contains("wmv3"))
                    {
                        string title = "WMV (" + m.Groups["bitrate"].Value + "K)";
                        string url = m.Groups["url"].Value.Substring(0, m.Groups["url"].Value.IndexOf("?")).Replace("\\/", "/");
                        url = ParseASX(url)[0];
                        video.PlaybackOptions.Add(title, url);
                    }
                    else
                    {
                        string title = "FLV (" + m.Groups["bitrate"].Value + "K)";
                        string url = m.Groups["url"].Value.Replace("\\/", "/");
                        url = new MPUrlSourceFilter.RtmpUrl(url) { SwfUrl = "http://videoportal.sf.tv/flash/videoplayer.swf", SwfVerify = true }.ToString();
                        video.PlaybackOptions.Add(title, url);
                    }
                }
                m = m.NextMatch();
            }

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                return video.PlaybackOptions.First().Value;
            }
            return "";
        }

    }
}