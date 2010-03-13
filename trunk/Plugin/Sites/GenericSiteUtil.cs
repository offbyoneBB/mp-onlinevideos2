using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;
using System.Xml;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class GenericSiteUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the baseUrl for dynamic categories. Group names: 'url', 'title'. Will not be used if not set.")]
        protected string dynamicCategoriesRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the dynamicCategoriesRegEx.")]
        protected string dynamicCategoryUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for dynamic categories. Group names: 'url', 'title'. Will be used on the web pages resulting from the links from the dynamicCategoriesRegEx. Will not be used if not set.")]
        protected string dynamicSubCategoriesRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the dynamicSubCategoriesRegEx.")]
        protected string dynamicSubCategoryUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos. Group names: 'VideoUrl', 'ImageUrl', 'Title', 'Duration', 'Description'.")]
        protected string videoListRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the videoListRegEx.")]
        protected string videoListRegExFormatString;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to match on the video url retrieved as a result of the 'VideoUrl' match of the videoListRegEx. Groups should be named 'm0', 'm1' and so on. Only used if set.")]
        protected string videoUrlRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the video url of an item that was found in the rss. If videoUrlRegEx is set those groups will be taken as parameters.")]
        protected string videoUrlFormatString = "{0}";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a next page link. Group should be named 'url'.")]
        protected string nextPageRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the nextPageRegEx.")]
        protected string nextPageRegExUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a previous page link. Group should be named 'url'.")]
        protected string prevPageRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the prevPageRegEx.")]
        protected string prevPageRegExUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a link that points to another file holding the actual playback url. Group should be named 'url'. If this is not set, the fileUrlRegEx will be used directly, otherwise first this and afterwards the fileUrlRegEx on the result.")]
        protected string playlistUrlRegEx;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for the playback url. Groups should be named 'm0', 'm1' and so on.")]
        protected string fileUrlRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string used with the groups of the regex matches of the fileUrlRegEx to create the Url for playback.")]
        protected string fileUrlFormatString = "{0}";
        [Category("OnlineVideosConfiguration"), Description("Format string used as Url for getting the results of a search. {0} will be replaced with the query.")]
        protected string searchUrl;
        [Category("OnlineVideosConfiguration"), Description("Format string that should be sent as post data for getting the results of a search. {0} will be replaced with the query. If this is not set, search will be executed normal as GET.")]
        protected string searchPostString;
        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        protected string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Cookies that need to be send with each request. Comma-separated list of name=value. Domain will be taken from the base url.")]
        protected string cookies;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoItem for videoList.")]
        protected string videoItemXml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoTitle for videoList.")]
        protected string videoTitleXml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoThumb for videoList.")]
        protected string videoThumbXml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoUrl for videoList.")]
        protected string videoUrlXml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoDuration for videoList.")]
        protected string videoDurationXml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoDescription for videoList.")]
        protected string videoDescriptionXml;
        [Category("OnlineVideosConfiguration"), Description("Boolean used for forcing UTF8 encoding on received data.")]
        protected bool forceUTF8Encoding;

        Regex regEx_dynamicCategories, regEx_dynamicSubCategories, regEx_VideoList, regEx_NextPage, regEx_PrevPage, regEx_VideoUrl, regEx_PlaylistUrl, regEx_FileUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(dynamicCategoriesRegEx)) regEx_dynamicCategories = new Regex(dynamicCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(dynamicSubCategoriesRegEx)) regEx_dynamicSubCategories = new Regex(dynamicSubCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(videoListRegEx)) regEx_VideoList = new Regex(videoListRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(nextPageRegEx)) regEx_NextPage = new Regex(nextPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(prevPageRegEx)) regEx_PrevPage = new Regex(prevPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(videoUrlRegEx)) regEx_VideoUrl = new Regex(videoUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(playlistUrlRegEx)) regEx_PlaylistUrl = new Regex(playlistUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(fileUrlRegEx)) regEx_FileUrl = new Regex(fileUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
        }

        public override int DiscoverDynamicCategories()
        {
            if (regEx_dynamicCategories == null)
            {
                Settings.DynamicCategoriesDiscovered = true;

                if (Settings.Categories.Count > 0 && regEx_dynamicSubCategories != null)
                {
                    for (int i = 0; i < Settings.Categories.Count; i++)
                        Settings.Categories[i].HasSubCategories = true;
                }
            }
            else
            {
                string data = GetWebData(baseUrl, GetCookie());
                if (!string.IsNullOrEmpty(data))
                {
                    Settings.Categories.Clear();
                    Match m = regEx_dynamicCategories.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Url = m.Groups["url"].Value;
                        if (!string.IsNullOrEmpty(dynamicCategoryUrlFormatString)) cat.Url = string.Format(dynamicCategoryUrlFormatString, cat.Url);
                        if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                        if (regEx_dynamicSubCategories != null) cat.HasSubCategories = true;
                        Settings.Categories.Add(cat);
                        m = m.NextMatch();
                    }
                    Settings.DynamicCategoriesDiscovered = true;
                }
            }
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData((parentCategory as RssLink).Url, GetCookie());
            if (!string.IsNullOrEmpty(data))
            {
                parentCategory.SubCategories = new List<Category>();
                Match m = regEx_dynamicSubCategories.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = m.Groups["url"].Value;
                    if (!string.IsNullOrEmpty(dynamicSubCategoryUrlFormatString)) cat.Url = string.Format(dynamicSubCategoryUrlFormatString, cat.Url);
                    if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                    cat.ParentCategory = parentCategory;
                    parentCategory.SubCategories.Add(cat);
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> loVideoList = null;
            if (category is RssLink)
            {
                return Parse(((RssLink)category).Url, null);
            }
            else if (category is Group)
            {
                loVideoList = new List<VideoInfo>();
                foreach (Channel channel in ((Group)category).Channels)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = channel.StreamName;
                    video.VideoUrl = channel.Url;
                    video.ImageUrl = channel.Thumb;
                    loVideoList.Add(video);
                }
            }
            return loVideoList;
        }

        public override string getUrl(VideoInfo video)
        {
            string resultUrl = video.VideoUrl;
            // 1. do some formatting with the videoUrl
            if (regEx_VideoUrl != null)
            {
                Match matchVideoUrl = regEx_VideoUrl.Match(resultUrl);
                if (matchVideoUrl.Success)
                {
                    List<string> groupValues = new List<string>();
                    for (int i = 0; i < matchVideoUrl.Groups.Count; i++)
                        if (matchVideoUrl.Groups["m" + i.ToString()].Success)
                            groupValues.Add(HttpUtility.UrlDecode(matchVideoUrl.Groups["m" + i.ToString()].Value));
                    resultUrl = string.Format(videoUrlFormatString, groupValues.ToArray());
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(videoUrlFormatString)) resultUrl = string.Format(videoUrlFormatString, resultUrl);
            }
            
            // 2. create an absolute Uri using the baseUrl if the current one is not and a baseUrl was given
            if (!string.IsNullOrEmpty(baseUrl) && !Uri.IsWellFormedUriString(resultUrl, UriKind.Absolute))
            {
                resultUrl = new Uri(new Uri(baseUrl), resultUrl).AbsoluteUri;
            }
            // 3. retrieve a file from the web to find the actual playback url
            if (regEx_PlaylistUrl != null || regEx_FileUrl != null)
            {
                string dataPage = GetWebData(resultUrl, GetCookie());
                // 3.a extra step to get a playlist file if needed
                if (regEx_PlaylistUrl != null)
                {
                    Match matchPlaylistUrl = regEx_PlaylistUrl.Match(dataPage);
                    if (matchPlaylistUrl.Success)
                    {
                        dataPage = GetWebData(HttpUtility.UrlDecode(matchPlaylistUrl.Groups["url"].Value), GetCookie());
                    }
                    else return ""; // if no match, return empty url -> error
                }
                // 3.b find a match in the retrieved data for the final playback url
                if (regEx_FileUrl != null)
                {
                    Match matchFileUrl = regEx_FileUrl.Match(dataPage);
                    if (matchFileUrl.Success)
                    {
                        // apply some formatting to the url
                        List<string> groupValues = new List<string>();
                        for (int i = 0; i < matchFileUrl.Groups.Count; i++)
                            if (matchFileUrl.Groups["m" + i.ToString()].Success)
                                groupValues.Add(HttpUtility.UrlDecode(matchFileUrl.Groups["m" + i.ToString()].Value));
                        resultUrl = string.Format(fileUrlFormatString, groupValues.ToArray());
                    }
                    else return ""; // if no match, return empty url -> error
                    
                }
            }
            return resultUrl;
        }

        List<VideoInfo> Parse(string url, string data)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            if (string.IsNullOrEmpty(data)) data = GetWebData(url, GetCookie(),null,null,forceUTF8Encoding);
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
                        if (!string.IsNullOrEmpty(videoListRegExFormatString)) videoInfo.VideoUrl = string.Format(videoListRegExFormatString, videoInfo.VideoUrl);
                        if (!Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute)) videoInfo.VideoUrl = new Uri(new Uri(baseUrl), videoInfo.VideoUrl).AbsoluteUri;
                        videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                        videoInfo.Length = Regex.Replace(m.Groups["Duration"].Value, "(<[^>]+>)", "");
                        videoInfo.Description = m.Groups["Description"].Value;
                        videoList.Add(videoInfo);
                        m = m.NextMatch();
                    }
                }
                else if (!string.IsNullOrEmpty(videoItemXml))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    XmlNodeList videoItems = doc.SelectNodes(videoItemXml);
                    for (int i = 0; i < videoItems.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(videoTitleXml) && !string.IsNullOrEmpty(videoUrlXml))
                        {
                            VideoInfo videoInfo = new VideoInfo();
                            videoInfo.Title = HttpUtility.HtmlDecode(videoItems[i].SelectSingleNode(videoTitleXml).InnerText);
                            videoInfo.VideoUrl = videoItems[i].SelectSingleNode(videoUrlXml).InnerText;
                            if (!string.IsNullOrEmpty(videoListRegExFormatString)) videoInfo.VideoUrl = string.Format(videoListRegExFormatString, videoInfo.VideoUrl);
                            if (!string.IsNullOrEmpty(videoThumbXml)) videoInfo.ImageUrl = videoItems[i].SelectSingleNode(videoThumbXml).InnerText;
                            if (!string.IsNullOrEmpty(videoDurationXml)) videoInfo.Length = Regex.Replace(videoItems[i].SelectSingleNode(videoDurationXml).InnerText, "(<[^>]+>)", "");
                            if (!string.IsNullOrEmpty(videoDescriptionXml)) videoInfo.Description = videoItems[i].SelectSingleNode(videoDescriptionXml).InnerText;
                            videoList.Add(videoInfo);
                        }
                    }
                }
                else
                {
                    foreach (RssItem rssItem in RssToolkit.Rss.RssDocument.Load(data).Channel.Items)
                    {
                        VideoInfo video = VideoInfo.FromRssItem(rssItem, regEx_FileUrl != null, new Predicate<string>(isPossibleVideo));
                        // only if a video url was set, add this Video to the list
                        if (!string.IsNullOrEmpty(video.VideoUrl)) videoList.Add(video);
                    }
                }

                if (regEx_PrevPage != null)
                {
                    // check for previous page link
                    Match mPrev = regEx_PrevPage.Match(data);
                    if (mPrev.Success)
                    {
                        previousPageAvailable = true;
                        previousPageUrl = mPrev.Groups["url"].Value;
                        if (!string.IsNullOrEmpty(prevPageRegExUrlFormatString)) previousPageUrl = string.Format(prevPageRegExUrlFormatString, previousPageUrl);
                        if (!Uri.IsWellFormedUriString(previousPageUrl, System.UriKind.Absolute))
                        {
                            Uri uri = null;
                            if (Uri.TryCreate(new Uri(url), previousPageUrl, out uri))
                            {
                                previousPageUrl = uri.ToString();
                            }
                            else
                            {
                                previousPageAvailable = false;
                                previousPageUrl = "";
                            }
                        }
                    }
                    else
                    {
                        previousPageAvailable = false;
                        previousPageUrl = "";
                    }
                }

                if (regEx_NextPage != null)
                {
                    // check for next page link
                    Match mNext = regEx_NextPage.Match(data);
                    if (mNext.Success)
                    {
                        nextPageAvailable = true;
                        nextPageUrl = mNext.Groups["url"].Value;
                        if (!string.IsNullOrEmpty(nextPageRegExUrlFormatString)) nextPageUrl = string.Format(nextPageRegExUrlFormatString, nextPageUrl);
                        if (!Uri.IsWellFormedUriString(nextPageUrl, System.UriKind.Absolute))
                        {
                            Uri uri = null;
                            if (Uri.TryCreate(new Uri(url), nextPageUrl, out uri))
                            {
                                nextPageUrl = uri.ToString();
                            }
                            else
                            {
                                previousPageAvailable = false;
                                nextPageUrl = "";
                            }
                        }
                    }
                    else
                    {
                        nextPageAvailable = false;
                        nextPageUrl = "";
                    }
                }
            }

            return videoList;
        }

        #region Next/Previous Page

        string nextPageUrl = "";
        bool nextPageAvailable = false;
        public override bool HasNextPage
        {
            get { return nextPageAvailable; }
        }

        string previousPageUrl = "";
        bool previousPageAvailable = false;
        public override bool HasPreviousPage
        {
            get { return previousPageAvailable; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return Parse(nextPageUrl, null);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse(previousPageUrl, null);
        }

        #endregion

        #region Search

        public override bool CanSearch { get { return !string.IsNullOrEmpty(searchUrl); } }

        public override List<VideoInfo> Search(string query)
        {
            if (string.IsNullOrEmpty(searchPostString))
            {
                return Parse(string.Format(searchUrl, query), null);
            }
            else
            {
                return Parse(searchUrl, GetWebDataFromPost(searchUrl, string.Format(searchPostString, query)));
            }
        }

        #endregion

        #region Cookie

        CookieContainer GetCookie()
        {
            if (string.IsNullOrEmpty(cookies)) return null;

            CookieContainer cc = new CookieContainer();
            string[] myCookies = cookies.Split(',');
            foreach (string aCookie in myCookies)
            {
                string[] name_value = aCookie.Split('=');
                Cookie c = new Cookie();
                c.Name = name_value[0];
                c.Value = name_value[1];
                c.Expires = DateTime.Now.AddHours(1);
                c.Domain = new Uri(baseUrl).Host;
                cc.Add(c);
            }
            return cc;
        }

        #endregion
    }
}
