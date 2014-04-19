using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class MyVideoUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration")]
        string serienSenderRegEx;
        [Category("OnlineVideosConfiguration")]
        string serienRegEx;
        [Category("OnlineVideosConfiguration")]
        string episodenRegEx;

        const uint PageSize = 20;
        readonly byte[] dev_id = new byte[] { 122, 81, 16, 43, 80, 80, 4, 172, 119, 119, 169, 230, 18, 74, 53, 1 };
        readonly byte[] web_id = new byte[] { 183, 155, 229, 242, 244, 158, 140, 97, 226, 10, 217, 166, 148, 59, 241, 246 };

        string nextPageUrl;
        string currentVideoTitle;

        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            Settings.Categories.Clear();
            Settings.Categories.Add(new RssLink() { Name = "Meistgesehene Videos", Url = GetApiUrl("myvideo.videos.list_popular_by_category") });
            Settings.Categories.Add(new RssLink() { Name = "Themen", Url = GetApiUrl("myvideo.base.list_categories"), HasSubCategories = true });
            Category music = new Category() { Name = "Musik", HasSubCategories = true, SubCategoriesDiscovered = true };
            music.SubCategories = new List<Category>() 
            {
                new RssLink() { Name = "Charts", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "117" } }) },
                new RssLink() { Name = "Newcomer", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "2" } }) },
                new RssLink() { Name = "Alle", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "1" } }) },
                new RssLink() { Name = "Rock", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "3" } }) },
                new RssLink() { Name = "Pop", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "4" } }) },
                new RssLink() { Name = "Rap", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "5" } }) },
                new RssLink() { Name = "Electro", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "7" } }) },
                new RssLink() { Name = "Hiphop", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "8" } }) },
                new RssLink() { Name = "Country", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "233" } }) },
                new RssLink() { Name = "Jazz", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "401" } }) },
                new RssLink() { Name = "Raggae", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "432" } }) },
                new RssLink() { Name = "Metal", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "30" } }) },
                new RssLink() { Name = "Schlager", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "464" } }) },                
                new RssLink() { Name = "80iger und 90iger", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "459" } }) },
                new RssLink() { Name = "80iger", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "11" } }) },
                new RssLink() { Name = "Klassiker", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "460" } }) },
                new RssLink() { Name = "WM Hits", ParentCategory = music, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "607" } }) }
            };
            Settings.Categories.Add(music);
            Settings.Categories.Add(new RssLink() { Name = "Serien", HasSubCategories = true, Url = "http://www.myvideo.de/Serien", Other = serienSenderRegEx });
            Category filme = new Category() { Name = "Filme", HasSubCategories = true, SubCategoriesDiscovered = true };
            filme.SubCategories = new List<Category>() 
            {
                new RssLink() { Name = "Alle Filme", ParentCategory = filme, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "435" } }) },
                new RssLink() { Name = "Trailer Demnächst im Kino", ParentCategory = filme, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "881" } }) },
                new RssLink() { Name = "Trailer Jetzt im Kino", ParentCategory = filme, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "882" } }) },
                new RssLink() { Name = "Trailer Kino Top 10", ParentCategory = filme, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "883" } }) },
                new RssLink() { Name = "Trailer Jetzt auf BlueRay", ParentCategory = filme, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "884" } }) },
                new RssLink() { Name = "Trailer BlueRay Top 10", ParentCategory = filme, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "885" } }) },
                new RssLink() { Name = "Trailer Disney", ParentCategory = filme, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "782" } }) }
            };
            Settings.Categories.Add(filme);
            Category top100 = new Category() { Name = "Top 100", HasSubCategories = true, SubCategoriesDiscovered = true };
            top100.SubCategories = new List<Category>() 
            {
                new RssLink() { Name = "Top 100 Videos Heute", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "12" } }) },
                new RssLink() { Name = "Top 100 Videos Woche", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "13" } }) },
                new RssLink() { Name = "Top 100 Videos Monat", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "14" } }) },
                new RssLink() { Name = "Top 100 Videos Ewig", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "15" } }) },
                new RssLink() { Name = "Top 100 Entertainment Woche", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "25" } }) },
                new RssLink() { Name = "Top 100 Entertainment Monat", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "26" } }) },
                new RssLink() { Name = "Top 100 Entertainment Ewig", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "27" } }) },
                new RssLink() { Name = "Top 100 Serien Woche", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "805" } }) },
                new RssLink() { Name = "Top 100 Serien Monat", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "806" } }) },
                new RssLink() { Name = "Top 100 Serien Ewig", ParentCategory = top100, Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", "807" } }) }
            };
            Settings.Categories.Add(top100);
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (IsApiUrl((parentCategory as RssLink).Url))
            {
                var json = GetWebData<Newtonsoft.Json.Linq.JObject>((parentCategory as RssLink).Url);
                parentCategory.SubCategories = new List<Category>();
                if (json["response"]["myvideo"].Value<string>("method") == "myvideo.base.list_categories")
                {
                    foreach (Newtonsoft.Json.Linq.JProperty cat in json["response"]["myvideo"]["category_list"]["category"])
                    {
                        parentCategory.SubCategories.Add(new RssLink()
                        {
                            Name = cat.Value.Value<string>("category_name"),
                            Thumb = GetPossibleThumbUrlsForThema(cat.Value.Value<string>("category_name")),
                            Url = GetApiUrl("myvideo.videos.list_popular_by_category", new NameValueCollection() { { "cat", cat.Value.Value<string>("category_id") } }),
                            Other = cat.Value.Value<string>("category_id"),
                            ParentCategory = parentCategory
                        });
                    }
                }
                if (json["response"]["myvideo"].Value<string>("method") == "myvideo.base.list_charts")
                {
                    foreach (Newtonsoft.Json.Linq.JProperty cat in json["response"]["myvideo"]["chart_list"])
                    {
                        parentCategory.SubCategories.Add(new RssLink()
                        {
                            Name = cat.Value.Value<string>("chart_title"),
                            Url = GetApiUrl("myvideo.videos.list_by_chart_id", new NameValueCollection() { { "chart_id", cat.Value.Value<string>("chart_id") } }),
                            Other = cat.Value.Value<string>("chart_id"),
                            ParentCategory = parentCategory
                        });
                    }
                }
                if (parentCategory.SubCategories.Count > 0)
                {
                    (parentCategory as RssLink).EstimatedVideoCount = (uint)parentCategory.SubCategories.Count;
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            else
            {
                string data = GetWebData((parentCategory as RssLink).Url, forceUTF8: true);
                parentCategory.SubCategories = new List<Category>();
                Match m = Regex.Match(data, parentCategory.Other as string, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = m.Groups["url"].Value;
                    if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri((parentCategory as RssLink).Url), cat.Url).AbsoluteUri;
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                    cat.Thumb = m.Groups["thumb"].Value;
                    if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri((parentCategory as RssLink).Url), cat.Thumb).AbsoluteUri;
                    cat.Description = m.Groups["description"].Value;
                    if (parentCategory.ParentCategory == null)
                    {
                        cat.Other = serienRegEx;
                        cat.HasSubCategories = true;
                    }
					if (!parentCategory.SubCategories.Any(c => (c as RssLink).Url == cat.Url))
					{
						cat.ParentCategory = parentCategory;
						parentCategory.SubCategories.Add(cat);
					}
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            nextPageUrl = null;
            HasNextPage = false;
            currentVideoTitle = null;
            if (IsApiUrl(((RssLink)category).Url))
                return GetVideosFromApiUrl(((RssLink)category).Url, (RssLink)category);
            else 
                return GetVideosFromWebsite(((RssLink)category).Url);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return GetVideosFromApiUrl(nextPageUrl);
        }
        
        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            currentVideoTitle = null;
            return GetVideosFromApiUrl(GetApiUrl("myvideo.videos.list_by_tag", new NameValueCollection() { { "tag", query } })).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
        }

        public override List<ISearchResultItem> DoSearch(string query, string category)
        {
            currentVideoTitle = null;
            if (string.IsNullOrEmpty(category)) return DoSearch(query);
            else return GetVideosFromApiUrl(GetApiUrl("myvideo.videos.list_by_category_and_tag", new NameValueCollection() { { "tag", query }, { "cat", category } })).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
        }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            var result = new Dictionary<string, string>();
            var themen = Settings.Categories.FirstOrDefault(c => c.Name == "Themen");
            if (themen != null && themen.SubCategories != null && themen.SubCategories.Count > 0)
            {
                themen.SubCategories.ForEach(c => result.Add(c.Name, c.Other.ToString()));
            }
            return result;
        }

        #endregion

        #region ContextMenu - More from User

        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, OnlineVideos.VideoInfo selectedItem)
        {
            List<ContextMenuEntry> result = new List<ContextMenuEntry>();
            if (selectedItem != null && !string.IsNullOrEmpty(selectedItem.Other as string))
            {
                result.Add(new ContextMenuEntry() { DisplayText = "Mehr " + selectedItem.Description, Other = selectedItem.Other });
            }
            return result;
        }

        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, OnlineVideos.VideoInfo selectedItem, ContextMenuEntry choice)
        {
            if (choice != null && choice.DisplayText.StartsWith("Mehr "))
            {
                currentVideoTitle = choice.DisplayText;
                return new ContextMenuExecutionResult() 
                    { ResultItems = 
                        GetVideosFromApiUrl(GetApiUrl("myvideo.videos.list_by_user", new NameValueCollection() { { "user", choice.Other as string } }))
                            .ConvertAll<ISearchResultItem>(v => v as ISearchResultItem) };
            }
            return null;
        }

        public override string getCurrentVideosTitle()
        {
            return currentVideoTitle;
        }

        #endregion

        List<VideoInfo> GetVideosFromWebsite(string url)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            string data = GetWebData(url, forceUTF8: true);
            Match m = Regex.Match(data, episodenRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            while (m.Success)
            {
                VideoInfo videoInfo = new VideoInfo();
                videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                // get, format and if needed absolutify the video url
                videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                if (!Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute)) videoInfo.VideoUrl = new Uri(new Uri(url), videoInfo.VideoUrl).AbsoluteUri;
                // get, format and if needed absolutify the thumb url
                videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                if (!string.IsNullOrEmpty(videoInfo.ImageUrl) && !Uri.IsWellFormedUriString(videoInfo.ImageUrl, System.UriKind.Absolute)) videoInfo.ImageUrl = new Uri(new Uri(url), videoInfo.ImageUrl).AbsoluteUri;
                videoInfo.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value);
                videoInfo.Airdate = Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
                videoInfo.Description = m.Groups["Description"].Value;
                videoList.Add(videoInfo);
                m = m.NextMatch();
            }
            return videoList;
        }

        List<VideoInfo> GetVideosFromApiUrl(string url, RssLink category = null)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            var json = GetWebData<Newtonsoft.Json.Linq.JObject>(url);
            Uri uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            int page = -1;
            int.TryParse(query.Get("page"), out page);
            int resultCount = -1;
            int.TryParse(json["response"]["myvideo"].Value<string>("resultCount"), out resultCount);
            if (category != null && resultCount >= 0) category.EstimatedVideoCount = (uint)resultCount;
            nextPageUrl = null;
            HasNextPage = (page > 0 && resultCount > 0 && (int)Math.Ceiling((float)resultCount / PageSize) > page);
            if (HasNextPage)
            {
                query.Set("page", (page+1).ToString());
                nextPageUrl = uri.GetLeftPart(UriPartial.Path) + "?" + query.ToString();
            }
            foreach (Newtonsoft.Json.Linq.JProperty video in json["response"]["myvideo"]["movie_list"]["movie"])
            {
                loVideoList.Add(new VideoInfo()
                {
                    Title = video.Value.Value<string>("movie_title"),
                    Description = "von: " + video.Value.Value<string>("movie_owner"),
                    Other = video.Value.Value<string>("movie_owner_id"),
                    ImageUrl = video.Value.Value<string>("movie_thumbnail"),
                    Length = video.Value.Value<string>("movie_length"),
                    Airdate = Utils.UNIXTimeToDateTime(double.Parse(video.Value.Value<string>("movie_added"))).ToString("g", OnlineVideoSettings.Instance.Locale),
                    VideoUrl = "http://www.myvideo.de/watch/" + video.Value.Value<string>("movie_id") + "/"
                });
            }
            return loVideoList;
        }

        public override String getUrl(VideoInfo video)
        {
            // get the Id of the video from the VideoUrl
            string videoId = Regex.Match(video.VideoUrl, @"watch/(\d+)/").Groups[1].Value;
            if (string.IsNullOrEmpty(videoId)) throw new OnlineVideosException("Couldn't find Video Id!");
            // build the url where we can get the encoded Xml that holds playback information
            string data = GetWebData(video.VideoUrl);
            string url = "";
            var parameters = HttpUtility.ParseQueryString(string.Empty);
            foreach (Match m in Regex.Matches(data, @"(?:(?<=var\sflashvars=[^}]*)(?:{|,)(?<var>[_\w]{2,20}):'(?<val>[^']+)'(?=[^{]*}))|(?:p.addVariable\('(?<var>[^']+)',\s*'(?<val>[^']+)'\))"))
            {
                if (m.Groups["var"].Value == "_encxml") url = HttpUtility.UrlDecode(m.Groups["val"].Value);
                else
                {
                    if (m.Groups["var"].Value == "flash_playertype" && m.Groups["val"].Value == "MTV")
                        parameters.Add("flash_playertype", "D");
                    else
                        parameters.Add(m.Groups["var"].Value, m.Groups["val"].Value);
                }
            }
			parameters["domain"] = "www.myvideo.de";
            // check if webpage uses a different type of player
            if (string.IsNullOrEmpty(url))
            {
                string sevenLoadUrl = Regex.Match(data, @"<object\s+type='application/x-shockwave-flash'\s+data='(http://de.sevenload.com[^']+)'").Groups[1].Value;
                if (!string.IsNullOrEmpty(sevenLoadUrl))
                {
                    sevenLoadUrl = GetRedirectedUrl(sevenLoadUrl);
                    if (!string.IsNullOrEmpty(sevenLoadUrl))
                    {
                        sevenLoadUrl = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(new Uri(sevenLoadUrl).Query)["configPath"]);
                        string sevenLoadXml = GetWebData(sevenLoadUrl);
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.LoadXml(sevenLoadXml);
                        video.PlaybackOptions = new Dictionary<string, string>();
                        foreach (XmlElement streamElement in xDoc.SelectNodes("//stream"))
                        {
                            video.PlaybackOptions.Add(
                                string.Format("{0} - {1}x{2} ({3})",
                                    streamElement.GetAttribute("quality"),
                                    streamElement.GetAttribute("width"),
                                    streamElement.GetAttribute("height"),
                                    streamElement.GetAttribute("codec")),
                                streamElement.InnerText);
                        }
                        return video.PlaybackOptions.Last().Value;
                    }
                }
            }
            else
            {
                // decode the xml
                string enc_data = GetWebData(url + "?" + parameters.ToString(), referer: video.VideoUrl).Split('=')[1];
                var enc_data_b = ArrayFromHexstring(enc_data);
                var p1 = ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(ASCIIEncoding.ASCII.GetString(Convert.FromBase64String("WXpnME1EZGhNRGhpTTJNM01XVmhOREU0WldNNVpHTTJOakptTW1FMU5tVTBNR05pWkRaa05XRXhNVFJoWVRVd1ptSXhaVEV3TnpsbA0KTVRkbU1tSTRNdz09"))));
                var p2 = Utils.GetMD5Hash(videoId.ToString());
                var sk = ASCIIEncoding.ASCII.GetBytes(Utils.GetMD5Hash(p1 + p2));
                byte[] dec_data = new byte[enc_data_b.Length];
                var rc4 = new Org.BouncyCastle.Crypto.Engines.RC4Engine();
                rc4.Init(false, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(sk));
                rc4.ProcessBytes(enc_data_b, 0, enc_data_b.Length, dec_data, 0);
                var dec = ASCIIEncoding.ASCII.GetString(dec_data);
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(dec);
                // get playback url from decoded xml
                XmlElement videoElement = xDoc.SelectSingleNode("//video") as XmlElement;
                string rtmpUrl = HttpUtility.UrlDecode(videoElement.GetAttribute("connectionurl"));
                if (rtmpUrl.StartsWith("rtmp"))
                {
                    rtmpUrl = rtmpUrl.Replace("rtmpe://", "rtmp://");
                    string playPath = HttpUtility.UrlDecode(videoElement.GetAttribute("source"));
					string pageUrl = HttpUtility.UrlDecode((xDoc.SelectSingleNode("//destserver") as XmlElement).InnerText) + HttpUtility.UrlDecode(videoElement.GetAttribute("video_link"));
					string swfUrl = HttpUtility.UrlDecode(Regex.Match(data, @"swfobject\.embedSWF\(\'(.+?)\'").Groups[1].Value);
                    return new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { TcUrl = rtmpUrl, SwfUrl = swfUrl, SwfVerify = true, PlayPath = playPath, PageUrl = pageUrl }.ToString();
                }
                else
                {
                    return HttpUtility.UrlDecode(videoElement.GetAttribute("path")) + HttpUtility.UrlDecode(videoElement.GetAttribute("source"));
                }
            }
            return "";
        }

        #region API Helper

        string GetApiUrl(string method, NameValueCollection additionalParameters = null, uint page = 1)
        {
            string additionalParamsString = "";
            if (additionalParameters != null && additionalParameters.Count > 0)
            {
                var col = HttpUtility.ParseQueryString(string.Empty);
                col.Add(additionalParameters);
                additionalParamsString = "&" + col.ToString();
            }
            return string.Format("https://api.myvideo.de/prod/mobile/api2_rest.php?method={0}{1}&o_format=json&page={2}&per_page={3}&dev_id={4}&website_id={5}",
                method,
                additionalParamsString,
                page,
                PageSize,
                HexstringFromArray(dev_id),
                HexstringFromArray(web_id));
        }

        bool IsApiUrl(string url)
        {
            return url.StartsWith("https://api.myvideo.de/prod/mobile/api2_rest.php");
        }

        string GetPossibleThumbUrlsForThema(string thema)
        {
            StringBuilder result = new StringBuilder();
            thema = thema.ToLower();
            var words = thema.Split(new char[]{'&'}, StringSplitOptions.RemoveEmptyEntries);
            result.Append(string.Format("http://is4.myvideo.de/de/bilder/images/channels/catm_{0}.jpg", thema.Replace('&', '_').Replace(" ", "")));
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = words[i].Trim();
                if (words[i].EndsWith("s") || words[i].EndsWith("n"))
                    words[i] = words[i].Substring(0, words[i].Length - 1);
            }
            if (words.Length > 1)
            {
                foreach(string word in words)
                {
                    result.Append(string.Format("|http://is4.myvideo.de/de/bilder/images/channels/catm_{0}.jpg", word));
                }
                result.Append(string.Format("|http://is4.myvideo.de/de/bilder/images/channels/catm_{0}.jpg", string.Join("_", words)));
            }
            return result.ToString();
        }

        #endregion

        #region Array Helper
        static byte[] ArrayFromHexstring(string s)
        {
            List<byte> a = new List<byte>();
            for (int i = 0; i < s.Length; i = i + 2)
            {
                a.Add(byte.Parse(s.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }
            return a.ToArray();
        }

        static string HexstringFromArray(byte[] array)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].ToString("x2"));
            }
            return sb.ToString();
        }
        #endregion
    }
}