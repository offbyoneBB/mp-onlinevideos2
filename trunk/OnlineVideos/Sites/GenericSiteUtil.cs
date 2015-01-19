using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Linq;
using RssToolkit.Rss;
using OnlineVideos.Hoster.Base;

namespace OnlineVideos.Sites
{
    public class GenericSiteUtil : SiteUtilBase
    {
        public enum HosterResolving { None, FromUrl, ByRequest };
        public enum UrlDecoding { None, HtmlDecode, UrlDecode };

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the baseUrl for dynamic categories. Group names: 'url', 'title', 'thumb', 'description'. Will not be used if not set.")]
        protected string dynamicCategoriesRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the dynamicCategoriesRegEx.")]
        protected string dynamicCategoryUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("What type of decoding should be applied to the 'url' match of the dynamicCategoryUrlFormatString.")]
        protected UrlDecoding dynamicCategoryUrlDecoding = UrlDecoding.None;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the data retrieved to get the dynamic categories for a link to another page with more categories. Group names: 'url'. Will not be used if not set.")]
        protected string dynamicCategoriesNextPageRegEx;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for dynamic categories. Group names: 'url', 'title', 'thumb', 'description'. Will be used on the web pages resulting from the links from the dynamicCategoriesRegEx. Will not be used if not set.")]
        protected string dynamicSubCategoriesRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the dynamicSubCategoriesRegEx.")]
        protected string dynamicSubCategoryUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("What type of decoding should be applied to the 'url' match of the dynamicSubCategoriesRegEx.")]
        protected UrlDecoding dynamicSubCategoryUrlDecoding = UrlDecoding.None;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the data retrieved to get the dynamic subcategories for a link to another page with more subcategories. Group names: 'url'. Will not be used if not set.")]
        protected string dynamicSubCategoriesNextPageRegEx;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos. Group names: 'VideoUrl', 'ImageUrl', 'Title', 'Duration', 'Description', 'Airdate'.")]
        protected string videoListRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the videoListRegEx.")]
        protected string videoListRegExFormatString;
        [Category("OnlineVideosConfiguration"), Description("What type of decoding should be applied to the 'url' match of the videoListRegExFormatString.")]
        protected UrlDecoding videoListUrlDecoding = UrlDecoding.None;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to match on the video url retrieved as a result of the 'VideoUrl' match of the videoListRegEx. Groups should be named 'm0', 'm1' and so on. Only used if set.")]
        protected string videoUrlRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the video url of an item that was found in the rss. If videoUrlRegEx is set those groups will be taken as parameters.")]
        protected string videoUrlFormatString = "{0}";
        [Category("OnlineVideosConfiguration"), Description("What type of decoding should be applied to the 'm0', 'm1', ... match of the videoUrlRegEx.")]
        protected UrlDecoding videoUrlDecoding = UrlDecoding.None;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a next page link. Group should be named 'url'.")]
        protected string nextPageRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the nextPageRegEx.")]
        protected string nextPageRegExUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("What type of decoding should be applied to the 'url' match of the nextPageRegEx")]
        protected UrlDecoding nextPageRegExUrlDecoding = UrlDecoding.None;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a previous page link. Group should be named 'url'.")]
        protected string prevPageRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the prevPageRegEx.")]
        protected string prevPageRegExUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("What type of decoding should be applied to the 'url' match of the prevPageRegEx")]
        protected UrlDecoding prevPageRegExUrlDecoding = UrlDecoding.None;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a link that points to another file holding the actual playback url. Group should be named 'url'. If this is not set, the fileUrlRegEx will be used directly, otherwise first this and afterwards the fileUrlRegEx on the result.")]
        protected string playlistUrlRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string used with the 'url' match of the playlistUrlRegEx to create the Url for the playlist request.")]
        protected string playlistUrlFormatString = "{0}";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for the playback url. Groups should be named 'm0', 'm1' and so on for the url. Multiple matches will be presented as playback choices. The name of a choice will be made of the result of groups named 'n0', 'n1' and so on.")]
        protected string fileUrlRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string used with the groups (m0, m1, ..) of the regex matches of the fileUrlRegEx to create the Url for playback.")]
        protected string fileUrlFormatString = "{0}";
        [Category("OnlineVideosConfiguration"), Description("Format string used with the groups (n0, n1, ..) of the regex matches of the fileUrlRegEx to create the Name for a playback choice.")]
        protected string fileUrlNameFormatString = "{0}";
        [Category("OnlineVideosConfiguration"), Description("What type of decoding should be applied to the (m0, m1, ...) matches of the fileUrlRegEx")]
        protected UrlDecoding fileUrlDecoding = UrlDecoding.UrlDecode;
        [Category("OnlineVideosConfiguration"), Description("What type of decoding should be applied to the (n0, n1, ...) matches of the fileUrlRegEx")]
        protected UrlDecoding fileUrlNameDecoding = UrlDecoding.HtmlDecode;
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
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse an extra videoTitle for videoList.")]
        protected string videoTitle2Xml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoThumb for videoList.")]
        protected string videoThumbXml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoUrl for videoList.")]
        protected string videoUrlXml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoDuration for videoList.")]
        protected string videoDurationXml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoDescription for videoList.")]
        protected string videoDescriptionXml;
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoAirDate for videoList.")]
        protected string videoAirDateXml;
        [Category("OnlineVideosConfiguration"), Description("Some webservers don't send a header that tells the content encoding. Use this bool to enforce UTF-8")]
        protected bool forceUTF8Encoding;
        [Category("OnlineVideosConfiguration"), Description("Set an override encoding for all data retrieved from the webserver and send in searches.")]
        protected string overrideEncoding;
        [Category("OnlineVideosConfiguration"), Description("Some webservers sent headers that are considered unsafe by the .Net Framework. Use this bool to allow them.")]
        protected bool allowUnsafeHeaders;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'thumb' match retrieved from the videoThumbXml or 'ImageUrl' of the videoListRegEx.")]
        protected string videoThumbFormatString = "{0}";
        [Category("OnlineVideosConfiguration"), Description("Enables checking if the video's url or data from the url can be resolved via known hosters.")]
        protected HosterResolving resolveHoster = HosterResolving.None;
        [Category("OnlineVideosConfiguration"), Description("Post data which is send for getting the fileUrl for playback.")]
        protected string fileUrlPostString = String.Empty;
        [Category("OnlineVideosConfiguration"), Description("Enables getting the redirected url instead of the given url for playback.")]
        protected bool getRedirectedFileUrl = false;

        protected Regex regEx_dynamicCategories, regEx_dynamicCategoriesNextPage, regEx_dynamicSubCategories, regEx_dynamicSubCategoriesNextPage, regEx_VideoList, regEx_NextPage, regEx_PrevPage, regEx_VideoUrl, regEx_PlaylistUrl, regEx_FileUrl;
        protected System.Text.Encoding encodingOverride;

        protected readonly RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);


            if (!string.IsNullOrEmpty(dynamicCategoriesRegEx)) regEx_dynamicCategories = new Regex(dynamicCategoriesRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(dynamicCategoriesNextPageRegEx)) regEx_dynamicCategoriesNextPage = new Regex(dynamicCategoriesNextPageRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(dynamicSubCategoriesRegEx)) regEx_dynamicSubCategories = new Regex(dynamicSubCategoriesRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(dynamicSubCategoriesNextPageRegEx)) regEx_dynamicSubCategoriesNextPage = new Regex(dynamicSubCategoriesNextPageRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(videoListRegEx)) regEx_VideoList = new Regex(videoListRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(nextPageRegEx)) regEx_NextPage = new Regex(nextPageRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(prevPageRegEx)) regEx_PrevPage = new Regex(prevPageRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(videoUrlRegEx)) regEx_VideoUrl = new Regex(videoUrlRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(playlistUrlRegEx)) regEx_PlaylistUrl = new Regex(playlistUrlRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(fileUrlRegEx)) regEx_FileUrl = new Regex(fileUrlRegEx, defaultRegexOptions);

            if (!string.IsNullOrEmpty(overrideEncoding))
            {
                try { encodingOverride = System.Text.Encoding.GetEncoding(overrideEncoding); }
                catch (Exception ex) { Log.Warn("{0} - could not create encoding {1} : {2}", siteSettings.Name, encodingOverride, ex.Message); }

            }
        }

        public override int DiscoverDynamicCategories()
        {
            if (regEx_dynamicCategories == null)
            {
                Settings.DynamicCategoriesDiscovered = true;

                if (Settings.Categories.Count > 0 && regEx_dynamicSubCategories != null)
                {
                    for (int i = 0; i < Settings.Categories.Count; i++)
                        if (!(Settings.Categories[i] is Group))
                            Settings.Categories[i].HasSubCategories = true;
                }
            }
            else
            {
                string data = GetWebData(baseUrl, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                if (!string.IsNullOrEmpty(data))
                {
                    return ParseCategories(data);
                }
            }
            return 0; // coming here means no dynamic categories were discovered
        }

        protected virtual void ExtraCategoryMatch(RssLink category, GroupCollection matchGroups)
        {
        }

        public virtual int ParseCategories(string data)
        {
            List<Category> dynamicCategories = new List<Category>(); // put all new discovered Categories in a separate list
            Match m = regEx_dynamicCategories.Match(data);
            while (m.Success)
            {
                RssLink cat = new RssLink();
                cat.Url = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["url"].Value, dynamicCategoryUrlFormatString, dynamicCategoryUrlDecoding);
                cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                cat.Thumb = m.Groups["thumb"].Value;
                if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                cat.Description = m.Groups["description"].Value;
                if (regEx_dynamicSubCategories != null) cat.HasSubCategories = true;
                ExtraCategoryMatch(cat, m.Groups);
                dynamicCategories.Add(cat);
                m = m.NextMatch();
            }
            // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            foreach (Category cat in dynamicCategories) Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = dynamicCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
            // Paging for Categories
            if (dynamicCategories.Count > 0 && regEx_dynamicCategoriesNextPage != null)
            {
                m = regEx_dynamicCategoriesNextPage.Match(data);
                if (m.Success)
                {
                    string nextCatPageUrl = m.Groups["url"].Value;
                    if (!Uri.IsWellFormedUriString(nextCatPageUrl, System.UriKind.Absolute)) nextCatPageUrl = new Uri(new Uri(baseUrl), nextCatPageUrl).AbsoluteUri;
                    Settings.Categories.Add(new NextPageCategory() { Url = nextCatPageUrl });
                }
            }
            return dynamicCategories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            return ParseSubCategories(parentCategory, null);
        }

        protected virtual void ExtraSubCategoryMatch(RssLink category, GroupCollection matchGroups)
        {
        }


        public virtual int ParseSubCategories(Category parentCategory, string data)
        {
            if (parentCategory is RssLink && regEx_dynamicSubCategories != null)
            {
                if (data == null)
                    data = GetWebData((parentCategory as RssLink).Url, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                if (!string.IsNullOrEmpty(data))
                {
                    List<Category> dynamicSubCategories = new List<Category>(); // put all new discovered Categories in a separate list
                    Match m = regEx_dynamicSubCategories.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Url = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["url"].Value, dynamicSubCategoryUrlFormatString, dynamicSubCategoryUrlDecoding);
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                        cat.Thumb = m.Groups["thumb"].Value;
                        if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                        cat.Description = m.Groups["description"].Value;
                        cat.ParentCategory = parentCategory;
                        ExtraSubCategoryMatch(cat, m.Groups);
                        dynamicSubCategories.Add(cat);
                        m = m.NextMatch();
                    }
                    // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
                    if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
                    foreach (Category cat in dynamicSubCategories) parentCategory.SubCategories.Add(cat);
                    parentCategory.SubCategoriesDiscovered = dynamicSubCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
                    // Paging for SubCategories
                    if (parentCategory.SubCategories.Count > 0 && regEx_dynamicSubCategoriesNextPage != null)
                    {
                        m = regEx_dynamicSubCategoriesNextPage.Match(data);
                        if (m.Success)
                        {
                            string nextCatPageUrl = m.Groups["url"].Value;
                            if (!Uri.IsWellFormedUriString(nextCatPageUrl, System.UriKind.Absolute)) nextCatPageUrl = new Uri(new Uri(baseUrl), nextCatPageUrl).AbsoluteUri;
                            parentCategory.SubCategories.Add(new NextPageCategory() { Url = nextCatPageUrl, ParentCategory = parentCategory });
                        }
                    }
                }
                return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
            }
            else
            {
                return base.DiscoverSubCategories(parentCategory);
            }
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            string data = GetWebData(category.Url, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);

            if (category.ParentCategory == null)
            {
                Settings.Categories.Remove(category);
                return ParseCategories(data);
            }
            else
            {
                category.ParentCategory.SubCategories.Remove(category);
                int oldAmount = category.ParentCategory.SubCategories.Count;
                return ParseSubCategories(category.ParentCategory, data);
            }
        }

        public virtual VideoInfo CreateVideoInfo()
        {
            return new VideoInfo();
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
                    VideoInfo video = CreateVideoInfo();
                    video.Title = channel.StreamName;
                    // rtmp live stream urls need to set the live flag (if they are not yet in the MPUrlSourceFilter format)
                    if (channel.Url.ToLower().StartsWith("rtmp") && !channel.Url.Contains(MPUrlSourceFilter.RtmpUrl.ParameterSeparator))
                    {
                        video.VideoUrl = new MPUrlSourceFilter.RtmpUrl(channel.Url) { Live = true }.ToString();
                    }
                    else
                    {
                        video.VideoUrl = channel.Url;
                    }
                    video.Other = "livestream";
                    video.ImageUrl = channel.Thumb;
                    loVideoList.Add(video);
                }
            }
            return loVideoList;
        }

        public virtual string getFormattedVideoUrl(VideoInfo video)
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
                    resultUrl = ApplyUrlDecoding(resultUrl, videoUrlDecoding);
                }
            }
            else
                if (!string.IsNullOrEmpty(videoUrlFormatString)) resultUrl = string.Format(videoUrlFormatString, resultUrl);

            // 2. create an absolute Uri using the baseUrl if the current one is not and a baseUrl was given
            if (!string.IsNullOrEmpty(baseUrl) && !Uri.IsWellFormedUriString(resultUrl, UriKind.Absolute))
                resultUrl = new Uri(new Uri(baseUrl), resultUrl).AbsoluteUri;

            return resultUrl;
        }

        public virtual string getPlaylistUrl(string resultUrl)
        {
            // 3.a extra step to get a playlist file if needed
            if (regEx_PlaylistUrl != null)
            {
                string dataPage = GetWebData(resultUrl, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                Match matchPlaylistUrl = regEx_PlaylistUrl.Match(dataPage);
                if (matchPlaylistUrl.Success)
                    return FormatDecodeAbsolutifyUrl(resultUrl, matchPlaylistUrl.Groups["url"].Value, playlistUrlFormatString, UrlDecoding.UrlDecode);
                else return String.Empty; // if no match, return empty url -> error
            }
            else
                return resultUrl;
        }

        public virtual Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            string dataPage;
            if (String.IsNullOrEmpty(fileUrlPostString))
                dataPage = GetWebData(playlistUrl, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            else
                dataPage = GetWebDataFromPost(playlistUrl, fileUrlPostString, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);

            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            Match matchFileUrl = regEx_FileUrl.Match(dataPage);
            while (matchFileUrl.Success)
            {
                // apply some formatting to the url
                List<string> groupValues = new List<string>();
                List<string> groupNameValues = new List<string>();
                for (int i = 0; i < matchFileUrl.Groups.Count; i++)
                {
                    if (matchFileUrl.Groups["m" + i.ToString()].Success)
                        groupValues.Add(ApplyUrlDecoding(matchFileUrl.Groups["m" + i.ToString()].Value, fileUrlDecoding));
                    if (matchFileUrl.Groups["n" + i.ToString()].Success)
                        groupNameValues.Add(ApplyUrlDecoding(matchFileUrl.Groups["n" + i.ToString()].Value, fileUrlNameDecoding));
                }
                string foundUrl = string.Format(fileUrlFormatString, groupValues.ToArray());
                // try to JSON deserialize
                if (foundUrl.StartsWith("\"") && foundUrl.EndsWith("\""))
                {
                    try
                    {
                        string deJSONified = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(foundUrl);
                        if (!string.IsNullOrEmpty(deJSONified)) foundUrl = deJSONified;
                    }
                    catch { }
                }
                if (!playbackOptions.ContainsValue(foundUrl))
                {
                    if (groupNameValues.Count == 0) groupNameValues.Add(playbackOptions.Count.ToString()); // if no groups to build a name, use numbering
                    string urlNameToAdd = string.Format(fileUrlNameFormatString, groupNameValues.ToArray());
                    if (playbackOptions.ContainsKey(urlNameToAdd))
                        urlNameToAdd += playbackOptions.Count.ToString();
                    playbackOptions.Add(urlNameToAdd, foundUrl);
                }
                matchFileUrl = matchFileUrl.NextMatch();
            }
            return playbackOptions;
        }

        public override string getUrl(VideoInfo video)
        {
            // it is a live stream that was configured in the xml, return the url right away
            if (video.Other as string == "livestream") return video.VideoUrl;

            // set playbackoption to null, as they will be rediscovered (not doing so would result in double resolving of them)
            video.PlaybackOptions = null;

            // deserialize PlaybackOptions if they were saved in Other object (happens if they are already discovered when building the list of videos)
            if (video.Other is string && (video.Other as string).StartsWith("PlaybackOptions://"))
                video.PlaybackOptions = Utils.DictionaryFromString((video.Other as string).Substring("PlaybackOptions://".Length));

            string resultUrl = getFormattedVideoUrl(video);

            // 3. retrieve a file from the web to find the actual playback url
            if (regEx_PlaylistUrl != null || regEx_FileUrl != null)
            {
                string playListUrl = getPlaylistUrl(resultUrl);
                if (String.IsNullOrEmpty(playListUrl))
                    return String.Empty; // if no match, return empty url -> error

                // 3.b find a match in the retrieved data for the final playback url
                if (regEx_FileUrl != null)
                {
                    video.PlaybackOptions = GetPlaybackOptions(playListUrl);
                    if (video.PlaybackOptions.Count == 0) return "";// if no match, return empty url -> error
                    else
                    {
                        // return first found url as default
                        var enumer = video.PlaybackOptions.GetEnumerator();
                        enumer.MoveNext();
                        resultUrl = enumer.Current.Value;
                    }
                    if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed
                }
            }

            if (getRedirectedFileUrl)
                resultUrl = GetRedirectedUrl(resultUrl);

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                string[] keys = new string[video.PlaybackOptions.Count];
                video.PlaybackOptions.Keys.CopyTo(keys, 0);
                foreach (string key in keys)
                {
                    // try to resolve asx to mms streams
                    if (video.PlaybackOptions[key].EndsWith(".asx"))
                    {
                        string mmsUrl = null;
                        try { mmsUrl = SiteUtilBase.ParseASX(video.PlaybackOptions[key])[0]; }
                        catch { }
                        if (!string.IsNullOrEmpty(mmsUrl) && !video.PlaybackOptions.ContainsValue(mmsUrl))
                        {
                            Uri uri = new Uri(mmsUrl);
                            if (uri.Scheme == "mms")
                            {
                                string newKey = key;
                                if (newKey.IndexOf(".asx") >= 0) newKey = newKey.Replace(".asx", System.IO.Path.GetExtension(mmsUrl));
                                if (newKey.IndexOf("http") >= 0) newKey = newKey.Replace("http", uri.Scheme);
                                if (newKey == key) newKey = string.Format("{0}->[{1}.{2}]", key, uri.Scheme, System.IO.Path.GetExtension(mmsUrl));
                                video.PlaybackOptions.Add(newKey, mmsUrl);
                            }
                        }
                    }
                }
            }

            if (resultUrl.EndsWith(".asx") && (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0))
            {
                string mmsUrl = SiteUtilBase.ParseASX(resultUrl)[0];
                Uri uri = new Uri(mmsUrl);
                if (uri.Scheme == "mms")
                {
                    video.PlaybackOptions = new Dictionary<string, string>();
                    video.PlaybackOptions.Add("http:// .asx", resultUrl);
                    video.PlaybackOptions.Add(string.Format("{0}:// {1}", new Uri(mmsUrl).Scheme, System.IO.Path.GetExtension(mmsUrl)), resultUrl);
                }
            }

            if (resolveHoster == HosterResolving.FromUrl)
            {
                if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
                {
                    Uri uri = new Uri(resultUrl);
                    foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                        if (uri.Host.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                        {
                            Dictionary<string, string> options = hosterUtil.getPlaybackOptions(resultUrl);
                            if (hosterUtil is ISubtitle)
                                video.SubtitleText = ((ISubtitle)hosterUtil).SubtitleText;

                            if (options != null && options.Count > 0)
                            {
                                if (options.Count > 1) video.PlaybackOptions = options;
                                resultUrl = options.Last().Value;
                                break;
                            }
                            else
                            {
                                resultUrl = String.Empty;
                                break;
                            }
                        }
                }
                else
                {
                    // resolve all PlaybackOptions
                    List<string> valueList = video.PlaybackOptions.Values.ToList();
                    video.PlaybackOptions.Clear();
                    foreach (string value in valueList)
                    {
                        Uri uri = new Uri(value);
                        foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                            if (uri.Host.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                            {
                                Dictionary<string, string> options = hosterUtil.getPlaybackOptions(value);
                                if (hosterUtil is ISubtitle)
                                    video.SubtitleText = ((ISubtitle)hosterUtil).SubtitleText;
                                if (options != null && options.Count > 0)
                                    foreach (var option in options)
                                        video.PlaybackOptions.Add(string.Format("{0} - {1}", video.PlaybackOptions.Count + 1, option.Key), option.Value);
                            }
                    }

                }
            }
            else if (resolveHoster == HosterResolving.ByRequest)
            {
                if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
                {
                    resultUrl = parseHosterLinks(resultUrl, video);
                    if (video.PlaybackOptions == null) resultUrl = GetVideoUrl(resultUrl);
                }
                else
                {
                    List<string> valueList = video.PlaybackOptions.Values.ToList();
                    video.PlaybackOptions.Clear();
                    foreach (string value in valueList)
                        parseHosterLinks(value, video);
                }
            }

            return resultUrl;
        }

        protected static string parseHosterLinks(string link, VideoInfo video)
        {
            string webData = GetWebData(link);
            Dictionary<string, string> options = new Dictionary<string, string>();

            foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
            {
                string regEx = @"(""|')(?<url>[^(""|')]+" + hosterUtil.getHosterUrl().ToLower() + @"[^(""|')]+)(""|')";

                MatchCollection n = Regex.Matches(webData, regEx);
                List<string> results = new List<string>();
                foreach (Match m in n)
                {
                    if (!results.Contains(m.Groups["url"].Value))
                        results.Add(m.Groups["url"].Value);
                }

                foreach (string url in results)
                {
                    string decodedUrl = HttpUtility.HtmlDecode(url);
                    if (Uri.IsWellFormedUriString(decodedUrl, System.UriKind.Absolute))
                    {
                        Uri uri = new Uri(decodedUrl);
                        if (!(uri.Host.Length == uri.AbsoluteUri.Length))
                        {
                            if (decodedUrl.Contains("\\/")) decodedUrl = decodedUrl.Replace("\\/", "/");

                            if (results.Count > 1)
                            {
                                int i = 1;
                                string playbackName = hosterUtil.getHosterUrl() + " - " + i + "/" + results.Count;
                                while (options.ContainsKey(playbackName))
                                {
                                    i++;
                                    playbackName = hosterUtil.getHosterUrl() + " - " + i + "/" + results.Count;
                                }
                                options.Add(playbackName, decodedUrl);
                            }
                            else
                                options.Add(hosterUtil.getHosterUrl(), decodedUrl);
                        }
                    }
                }
            }
            if (options != null && options.Count > 0)
            {
                if (video.PlaybackOptions == null)
                {
                    if (options.Count > 1)
                    {
                        video.PlaybackOptions = new Dictionary<string, string>();
                        foreach (KeyValuePair<String, String> entry in options) video.PlaybackOptions.Add(entry.Key, entry.Value);
                    }
                    else
                        return options.Last().Value;
                }
                else
                    foreach (KeyValuePair<String, String> entry in options)
                    {
                        if (video.PlaybackOptions.ContainsKey(entry.Key))
                        {
                            int i = 2;
                            while (video.PlaybackOptions.ContainsKey(entry.Key + " - " + i))
                                i++;
                            video.PlaybackOptions.Add(entry.Key + " - " + i, entry.Value);
                        }
                        else
                            video.PlaybackOptions.Add(entry.Key, entry.Value);
                    }
                return options.Last().Value;

            }
            else
                return String.Empty;
        }

        public static string GetVideoUrl(string url)
        {
            Uri uri = new Uri(url);
            foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                if (uri.Host.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                {
                    string ret = hosterUtil.getVideoUrls(url);
                    if (!string.IsNullOrEmpty(ret))
                        return ret;
                    else
                        return String.Empty;
                }
            return url;
        }

        protected virtual void ExtraVideoMatch(VideoInfo video, GroupCollection matchGroups)
        {
        }

        protected virtual List<VideoInfo> Parse(string url, string data)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            if (string.IsNullOrEmpty(data)) data = GetWebData(url, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            if (data.Length > 0)
            {
                if (regEx_VideoList != null)
                {
                    Match m = regEx_VideoList.Match(data);
                    while (m.Success)
                    {
                        VideoInfo videoInfo = CreateVideoInfo();
                        videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                        // get, format and if needed absolutify the video url
                        videoInfo.VideoUrl = FormatDecodeAbsolutifyUrl(url, m.Groups["VideoUrl"].Value, videoListRegExFormatString, videoListUrlDecoding);
                        // get, format and if needed absolutify the thumb url
                        if (!String.IsNullOrEmpty(m.Groups["ImageUrl"].Value))
                            videoInfo.ImageUrl = FormatDecodeAbsolutifyUrl(url, m.Groups["ImageUrl"].Value, videoThumbFormatString, UrlDecoding.None);
                        videoInfo.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value);
                        videoInfo.Airdate = Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
                        videoInfo.Description = m.Groups["Description"].Value;
                        ExtraVideoMatch(videoInfo, m.Groups);
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
                            VideoInfo videoInfo = CreateVideoInfo();
                            videoInfo.Title = HttpUtility.HtmlDecode(videoItems[i].SelectSingleNode(videoTitleXml).InnerText);
                            if (!String.IsNullOrEmpty(videoTitle2Xml))
                                videoInfo.Title += ' ' + HttpUtility.HtmlDecode(videoItems[i].SelectSingleNode(videoTitle2Xml).InnerText); ;

                            videoInfo.VideoUrl = videoItems[i].SelectSingleNode(videoUrlXml).InnerText;
                            if (!string.IsNullOrEmpty(videoListRegExFormatString)) videoInfo.VideoUrl = string.Format(videoListRegExFormatString, videoInfo.VideoUrl);
                            if (!string.IsNullOrEmpty(videoThumbXml)) videoInfo.ImageUrl = videoItems[i].SelectSingleNode(videoThumbXml).InnerText;
                            if (!string.IsNullOrEmpty(videoThumbFormatString)) videoInfo.ImageUrl = string.Format(videoThumbFormatString, videoInfo.ImageUrl);
                            if (!string.IsNullOrEmpty(videoDurationXml)) videoInfo.Length = Utils.PlainTextFromHtml(videoItems[i].SelectSingleNode(videoDurationXml).InnerText);
                            if (!string.IsNullOrEmpty(videoAirDateXml)) videoInfo.Airdate = Utils.PlainTextFromHtml(videoItems[i].SelectSingleNode(videoAirDateXml).InnerText);
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
                        if (!string.IsNullOrEmpty(video.VideoUrl))
                        {
                            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 1)
                            {
                                video.Other = "PlaybackOptions://\n" + Utils.DictionaryToString(video.PlaybackOptions);
                            }
                            videoList.Add(video);
                        }
                    }
                }                

                if (regEx_NextPage != null)
                {
                    // check for next page link
                    Match mNext = regEx_NextPage.Match(data);
                    if (mNext.Success)
                    {
                        nextPageUrl = FormatDecodeAbsolutifyUrl(url, mNext.Groups["url"].Value, nextPageRegExUrlFormatString, nextPageRegExUrlDecoding);
                        nextPageAvailable = !string.IsNullOrEmpty(nextPageUrl);
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

        public override bool isPossibleVideo(string url)
        {
            if (string.IsNullOrEmpty(url)) return false; // empty string is not a video

            if (resolveHoster != HosterResolving.None)
            {
                return HosterFactory.Contains(new Uri(url)) || base.isPossibleVideo(url);
            }
            else
            {
                return base.isPossibleVideo(url);
            }
        }

        protected virtual string ApplyUrlDecoding(string text, UrlDecoding decoding)
        {
            switch (decoding)
            {
                case UrlDecoding.HtmlDecode: return HttpUtility.HtmlDecode(text);
                case UrlDecoding.UrlDecode: return HttpUtility.UrlDecode(text);
                default: return text;
            }
        }

        protected virtual string FormatDecodeAbsolutifyUrl(string currentUrl, string matchedUrl, string matchedUrlFormatString, UrlDecoding matchedUrlDecoding)
        {
            // 1. make sure the matched string is not null
            string result = matchedUrl ?? string.Empty;
            // 2. format the matched url when both the format string and the matched url aren't null or empty
            if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(matchedUrlFormatString)) result = string.Format(matchedUrlFormatString, result);
            // 3. decode the match
            result = ApplyUrlDecoding(result, matchedUrlDecoding);
            // 4. build an absolute url when needed
            if (!Uri.IsWellFormedUriString(result, UriKind.Absolute))
            {
                // 4. a) workaround for .net bug when combining uri with a query only
                if (result.StartsWith("?"))
                {
                    result = new UriBuilder(currentUrl) { Query = result.Substring(1) }.Uri.ToString();
                }
                else
                {
                    Uri uri = null;
                    if (Uri.TryCreate(new Uri(currentUrl), result, out uri))
                    {
                        result = uri.ToString();
                    }
                    else
                    {
                        result = string.Empty;
                    }
                }
            }
            return result;
        }

        #region Next Page

        protected string nextPageUrl = "";
        protected bool nextPageAvailable = false;
        public override bool HasNextPage
        {
            get { return nextPageAvailable; }
        }
        
        public override List<VideoInfo> getNextPageVideos()
        {
            return Parse(nextPageUrl, null);
        }

        #endregion

        #region Search

        public override bool CanSearch { get { return !string.IsNullOrEmpty(searchUrl); } }

        public override List<VideoInfo> Search(string query)
        {
            // if an override Encoding was specified, we need to UrlEncode the search string with that encoding
            if (encodingOverride != null) query = HttpUtility.UrlEncode(encodingOverride.GetBytes(query));

            if (string.IsNullOrEmpty(searchPostString))
            {
                return Parse(string.Format(searchUrl, query), null);
            }
            else
            {
                return Parse(searchUrl, GetWebDataFromPost(searchUrl, string.Format(searchPostString, query), GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride));
            }
        }

        #endregion

        #region Cookie

        protected virtual CookieContainer GetCookie()
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
