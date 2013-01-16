using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites.Brownard;

namespace OnlineVideos.Sites
{
    public class FourodUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("Proxy to use for WebRequests (must be in the UK). Define like this: 83.84.85.86:8116")]
        string proxy = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a username, set it here.")]
        string proxyUsername = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a password, set it here.")]
        string proxyPassword = null;
        [Category("OnlineVideosUserConfiguration"), Description("Whether to download subtitles")]
        protected bool RetrieveSubtitles = false;
        [Category("OnlineVideosConfiguration"), Description("Url of the 4od swf object")]
        string swfObjectUrl = "http://www.channel4.com/static/programmes/asset/flash/swf/4odplayer-11.31.1.swf";

        string defaultLogo;
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            defaultLogo = string.Format(@"{0}\Icons\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, siteSettings.Name);
        }

        DateTime lastRefesh = DateTime.MinValue;
        public override int DiscoverDynamicCategories()
        {
            if ((DateTime.Now - lastRefesh).TotalMinutes > 15)
            {
                BindingList<Category> dynamicCats = new BindingList<Category>();
                foreach (Category dynamicCat in loadDynamicCats())
                    dynamicCats.Add(dynamicCat);
                foreach (Category cat in Settings.Categories)
                {
                    if (cat is RssLink)
                    {
                        cat.HasSubCategories = true;
                        cat.SubCategoriesDiscovered = false;
                        if (string.IsNullOrEmpty(cat.Thumb))
                            cat.Thumb = defaultLogo;
                        dynamicCats.Add(cat);
                    }
                }
                Settings.Categories = dynamicCats;
                lastRefesh = DateTime.Now;
            }
            return Settings.Categories.Count;
        }

        List<Category> loadDynamicCats()
        {
            Regex catReg = new Regex(@"<li class=""fourOnDemandCollection""[^>]*>[\s\n]*<h2>([^<]*)</h2>[\s\n]*<ul(.*?)</ul>", RegexOptions.Singleline);
            Regex itemReg = new Regex(@"&quot;title&quot;: &quot;(.*?)&quot;[\s\r\n]*, &quot;synopsis&quot;: &quot;(.*?)&quot;[\s\r\n]*, &quot;url&quot;:  &quot;(.*?)&quot;[\s\r\n]*, &quot;img&quot;: {&quot;src&quot;: &quot;(.*?)&quot;");
            List<Category> dynamicCats = new List<Category>();
            foreach (Match catMatch in catReg.Matches(GetWebData("http://www.channel4.com/programmes/4od")))
            {
                string items = catMatch.Groups[2].Value;
                MatchCollection itemMatches = itemReg.Matches(catMatch.Groups[2].Value);
                if (itemMatches.Count < 2)
                    continue;

                Category cat = createCategory(catMatch.Groups[1].Value, "", itemMatches[0].Groups[4].Value, "", null, null);
                cat.HasSubCategories = true;
                cat.SubCategoriesDiscovered = true;
                cat.SubCategories = new List<Category>();
                foreach (Match itemMatch in itemMatches)
                {
                    string catUrl = itemMatch.Groups[3].Value;
                    if (catUrl.Contains("/programmes/")) //exclude film4 content
                        cat.SubCategories.Add(createCategory(itemMatch.Groups[1].Value, itemMatch.Groups[2].Value, itemMatch.Groups[4].Value, catUrl, null, cat));
                }
                if (cat.SubCategories.Count > 0)
                    dynamicCats.Add(cat);
            }
            return dynamicCats;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = (parentCategory as RssLink).Url;
            List<Category> subCats;
            if (url.StartsWith("http://www.channel4.com/programmes/4od/catchup"))
                subCats = getCatchupCategories(parentCategory);
            else if (url.StartsWith("http://www.channel4.com/programmes/4od/collections"))
                subCats = getCollections(parentCategory);
            else
                subCats = lGetSubCategories(parentCategory);

            if (parentCategory is NextPageCategory)
            {
                parentCategory.ParentCategory.SubCategories.Remove(parentCategory);   
                parentCategory.ParentCategory.SubCategories.AddRange(subCats);
                return parentCategory.ParentCategory.SubCategories.Count;
            }

            parentCategory.SubCategories = subCats;
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            return this.DiscoverSubCategories(category);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = (category as RssLink).Url;
            if (url.StartsWith("http://www.channel4.com/programmes/4od/catchup"))
                return getCatchupVideos(url);
            else if (url.StartsWith("http://www.channel4.com/programmes/4od/collections"))
                return getCollectionVideos(url);
            return getVideoListInternal(url, category.Name);
        }

        public override string getUrl(VideoInfo video)
        {
            string epId = video.VideoUrl;
            string url = string.Format("http://ais.channel4.com/asset/{0}", epId);

            string xml = GetWebData(url, null, new System.Collections.Specialized.NameValueCollection(), null, getProxy(), false, false, null, false);
            if (RetrieveSubtitles)
            {
                Match subtitle = new Regex("<subtitlesFileUri>(.*?)</subtitlesFileUri>").Match(xml);
                if (subtitle.Success)
                    video.SubtitleText = Utils.SubtitleReader.SAMI2SRT(GetWebData("http://ais.channel4.com" + subtitle.Groups[1].Value));
            }
            string uriData = new Regex("<uriData>(.*?)</uriData>", RegexOptions.Singleline).Match(xml).Groups[1].Value;

            string streamUri = new Regex("<streamUri>(.*?)</streamUri>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
            if (!streamUri.StartsWith("rtmp"))
            {
                Log.Info("The format of the 4od video is not supported, searching youtube for alternate stream");
                video.PlaybackOptions = YouTubeShowHandler.GetYouTubePlaybackOptions(video.Other as EpisodeInfo);
                if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
                    return video.PlaybackOptions.Last().Value;
                return null;
            }

            string token = new Regex("<token>(.*?)</token>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
            string cdn = new Regex("<cdn>(.*?)</cdn>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
            string decryptedToken = new FourodDecrypter().Decode4odToken(token);
            string auth;
            if (cdn == "ll")
            {
                string e = new Regex("<e>(.*?)</e>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
                auth = string.Format("e={0}&h={1}", e, decryptedToken);
            }
            else
            {
                string fingerprint = new Regex("<fingerprint>(.*?)</fingerprint>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
                string slist = new Regex("<slist>(.*?)</slist>", RegexOptions.Singleline).Match(uriData).Groups[1].Value;
                auth = string.Format("auth={0}&aifp={1}&slist={2}", decryptedToken, fingerprint, slist);
            }

            string playUrl = new Regex("(.*?)mp4:", RegexOptions.Singleline).Match(streamUri).Groups[1].Value;
            playUrl = playUrl.Replace(".com/", ".com:1935/");

            string playPath = new Regex("(mp4:.*)", RegexOptions.Singleline).Match(streamUri).Groups[1].Value;
            playPath = playPath + "?" + auth;

            return new MPUrlSourceFilter.RtmpUrl(playUrl + "?ovpfv=1.1&" + auth)
            {
                PlayPath = playPath,
                SwfUrl = swfObjectUrl,
                SwfVerify = true,
                Live = false
            }.ToString();
        }

        #region Default Categories

        List<Category> lGetSubCategories(Category parentCategory)
        {
            string url = (parentCategory as RssLink).Url;
            if (!url.EndsWith("/")) url += "/";
            int page = 1;
            int cats = 50;
            List<Category> subCats = new List<Category>();

            while (cats == 50)
            {
                cats = 0;
                string lUrl = url + "page-" + page.ToString();
                string html = GetWebData(lUrl);
                foreach (Match catMatch in new Regex("<li.*?<a class=\".*?\" href=\"([^\"]*)\".*?<img src=\"(.*?)\".*?<p class=\"title\">(.*?)</p>.*?<p class=\"synopsis\">(.*?)</p>", RegexOptions.Singleline).Matches(html))
                {
                    Category cat = createCategory(catMatch.Groups[3].Value, catMatch.Groups[4].Value, catMatch.Groups[2].Value, catMatch.Groups[1].Value, null, parentCategory);
                    subCats.Add(cat);
                    cats++;
                }
                page++;
            }
            return subCats;
        }

        List<VideoInfo> getVideoListInternal(string url, string seriesTitle)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            string html = GetWebData(url);

            if (Regex.IsMatch(html, @"<li id=""series\d+"" [^>]*>\s*<ol class=""episode-list"))
                html = Regex.Replace(html, @"<li id=""recentlyOn""[^>]*>\s*<ol[^>]*>.*?</ol>", "", RegexOptions.Singleline);

            string epHtml = new Regex("<ol class=\"all-series\">(.*?)</div>", RegexOptions.Singleline).Match(html).Groups[1].Value;

            foreach (Match m in new Regex("<li.*?data-episode-number=\"(.*?)\".*?data-assetid=\"(.*?)\".*?data-episodeurl=\"(.*?)\".*?data-image-url=\"(.*?)\".*?data-episodetitle=\"(.*?)\".*?data-episodeinfo=\"(.*?)\".*?data-episodesynopsis=\"(.*?)\".*?data-series-number=\"(.*?)\"", RegexOptions.Singleline).Matches(epHtml))
            {
                string title;
                string extraInfo = "";
                string epTitle = m.Groups[5].Value;
                string epInfo = m.Groups[6].Value;

                if (cleanString(epTitle).ToLower() == seriesTitle.ToLower() && epInfo != "")
                    title = epInfo;
                else
                {
                    title = epTitle;
                    if (!string.IsNullOrEmpty(epInfo))
                        extraInfo = epInfo;
                }

                string img = m.Groups[4].Value;
                if (img == "")
                    img = new Regex("<meta property=\"og:image\" content=\"(.*?)\"", RegexOptions.Singleline).Match(html).Groups[1].Value;

                VideoInfo vid = createVideoItem(m.Groups[2].Value, title, m.Groups[7].Value, img, extraInfo);
                EpisodeInfo info = new EpisodeInfo()
                {
                    SeriesTitle = seriesTitle,
                    SeriesNumber = m.Groups[8].Value
                };
                if (!string.IsNullOrEmpty(m.Groups[1].Value))
                    info.EpisodeNumber = m.Groups[1].Value;
                else
                {
                    DateTime airDate;
                    if (DateTime.TryParse(m.Groups[6].Value, out airDate))
                    {
                        info.AirDate = airDate.ToString("dd/MM/yy");
                    }
                }
                vid.Other = info;
                vids.Add(vid);
            }

            return vids;
        }

        #endregion

        #region Catchup Categories

        List<Category> getCatchupCategories(Category parentCategory)
        {
            string html = GetWebData((parentCategory as RssLink).Url);
            Regex reg = new Regex(@"<li[^>]*><a href=""(/programmes/4od/catchup/date/[^""]*)"">([^<]*)</a></li>");
            List<Category> subCats = new List<Category>();
            foreach (Match m in reg.Matches(html))
            {
                Category cat = createCategory(m.Groups[2].Value, "", null, m.Groups[1].Value, null, parentCategory);
                subCats.Add(cat);
            }
            return subCats;
        }

        List<VideoInfo> getCatchupVideos(string url)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            string html = GetWebData(url);
            Regex reg = new Regex(@"<li class=""promo\s[^>]*>[\s\n]*<a class=""[^""]*"" href=""/programmes/[^/]*/4od#(\d+)"">[\s\n]*<img src=""([^""]*)""[^>]*>[\s\n]*<span[^>]*></span>[\s\n]*<p class=""txinfo txtime"">([^<]*)</p>[\s\n]*<p class=""title"">([^<]*)</p>[\s\n]*<p class=""series-info"">([^<]*)</p>[\s\n]*</a>[\s\n]*<a.*?</a>[\s\n]*<ul.*?>[\s\n]*(<li.*?</li>[\s\n]*)*</ul>[\s\n]*<p class=""synopsis"">(.*?)</p>");
            foreach (Match m in reg.Matches(html))
            {
                string description = m.Groups[5].Value != m.Groups[4].Value ? string.Format("{0}\r\n{1}", m.Groups[5].Value, m.Groups[7].Value) : m.Groups[7].Value;
                VideoInfo video = createVideoItem(m.Groups[1].Value, m.Groups[4].Value, description, m.Groups[2].Value, m.Groups[3].Value);

                EpisodeInfo epInfo = new EpisodeInfo() { SeriesTitle = m.Groups[4].Value };
                Match ep = Regex.Match(m.Groups[5].Value, @"Series\s+(\d+)\s+Episode\s+(\d+)");
                if (ep.Success)
                {
                    epInfo.SeriesNumber = ep.Groups[1].Value;
                    epInfo.EpisodeNumber = ep.Groups[2].Value;
                }
                else
                {
                    DateTime airDate;
                    if (DateTime.TryParse(m.Groups[5].Value, out airDate))
                    {
                        epInfo.SeriesNumber = airDate.Year.ToString();
                        epInfo.AirDate = airDate.ToString("dd/MM/yy");
                    }
                }
                video.Other = epInfo;
                vids.Add(video);
            }
            vids.Sort(catchupVideosComparer);
            return vids;
        }

        #endregion

        #region Collections

        List<Category> getCollections(Category parentCategory)
        {
            string url = (parentCategory as RssLink).Url;
            int currentPage = 1;
            if(url.EndsWith("collections"))
            {
                url += "/page-1";
            }
            else if (!int.TryParse(url.Last().ToString(), out currentPage))
            {
                currentPage = 1;
            }
            
            string html = GetWebData(url);
            Regex reg = new Regex(@"<a class=""promo-link"" href=""([^""]*)"">[\s\n]*<div[^>]*>[\s\n]*<img class=""[^""]*"" src=""([^""]*)""[^>]*>[\s\n]*(<img[^>]*>[\s\n]*)*<span[^>]*></span[^>]*>[\s\n]*</div[^>]*>[\s\n]*<div[^>]*>[\s\n]*<p class=""title"">([^<]*)</p>[\s\n]*<p class=""programme-count"">(\d+)([^<]*).*?</p>[\s\n]*</div>[\s\n]*</a>[\s\n]*<ul[^>]*>[\s\n]*((<li.*?</li>[\s\n]*)*)</ul>[\s\n]*</div>[\s\n]*<p class=""synopsis"">([^<]*)");
            
            List<Category> subCats = new List<Category>();
            if (parentCategory is NextPageCategory)
                parentCategory = parentCategory.ParentCategory;

            MatchCollection matches = reg.Matches(html);
            foreach (Match m in matches)
            {
                string desc = string.Format("{0}\r\n{1}{2}", m.Groups[9].Value, m.Groups[5].Value, m.Groups[6].Value);
                foreach (Match n in new Regex("<li>([^<]*)").Matches(m.Groups[7].Value))
                    desc += string.Format("\r\n{0}", n.Groups[1].Value);

                RssLink cat = createCategory(m.Groups[4].Value, desc, m.Groups[2].Value, m.Groups[1].Value, null, parentCategory) as RssLink;
                cat.EstimatedVideoCount = uint.Parse(m.Groups[5].Value);
                subCats.Add(cat);
            }
            if (matches.Count == 20)
                subCats.Add(new NextPageCategory()
                {
                    Url = "http://www.channel4.com/programmes/4od/collections/page-" + (currentPage + 1),
                    ParentCategory = parentCategory
                });

            return subCats;
        }

        List<VideoInfo> getCollectionVideos(string url)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            string html = GetWebData(url);
            Regex reg = new Regex(@"<a href=""[^#""]*#(\d+)"">[\s\n]*<img src=""([^""]*)""[^>]*>.*?<span class=""title1"">([^<]*)</span>[\s\n]*</span>[\s\n]*(<span[^>]*>[\s\n]*<span class=""title2"">([^<]*)</span>)?.*?<div class=""synopsis"">[\s\n]*<p>([^<]*)", RegexOptions.Singleline);
            foreach (Match m in reg.Matches(html))
            {
                EpisodeInfo epInfo = new EpisodeInfo() { SeriesTitle = m.Groups[3].Value };                
                Match ep = Regex.Match(m.Groups[5].Value, @"Series\s+(\d+)\s+Episode\s+(\d+)");
                if (ep.Success)
                {
                    epInfo.SeriesNumber = ep.Groups[1].Value;
                    epInfo.EpisodeNumber = ep.Groups[2].Value;
                }
                else
                {
                    epInfo.SeriesNumber = "1";
                    epInfo.EpisodeNumber = "1";
                }

                VideoInfo vid = createVideoItem(m.Groups[1].Value, m.Groups[3].Value, m.Groups[6].Value, "http://www.channel4.com" + m.Groups[2].Value, m.Groups[5].Value);
                vid.Other = epInfo;
                vids.Add(vid);
            }
            return vids;
        }

        #endregion

        #region Search

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> cats = new List<ISearchResultItem>();
            string searchReg = "{\"imgUrl\":\"(.*?)\".*?\"value\": \"(.*?)\".*?\"siteUrl\":\"(.*?)\",\"fourOnDemand\":\"true\"}";

            string searchHTML = GetWebData("http://www.channel4.com/search/predictive/?q=" + System.Web.HttpUtility.UrlEncode(query));
            foreach (Match result in new Regex(searchReg, RegexOptions.Singleline).Matches(searchHTML))
            {
                Category cat = createCategory(result.Groups[2].Value, "", result.Groups[1].Value, result.Groups[3].Value, null, null);
                cats.Add(cat);
            }

            return cats;
        }

        #endregion

        #region YouTube Handling

        bool verifyYoutubePage(string url, string targetSeries, string targetEpisode)
        {
            Match m = Regex.Match(url, "v=([^&]*)");
            if (m.Success)
            {
                string youtubeId = m.Groups[1].Value;
                string page = GetWebData(string.Format("http://www.youtube.com/watch?v={0}&has_verified=1&has_verified=1", youtubeId));
                if (!string.IsNullOrEmpty(page))
                {
                    m = Regex.Match(page, string.Format(@"Season {0} Ep\. ([0-9]+)", targetSeries));
                    if (m.Success)
                    {
                        return m.Groups[1].Value == targetEpisode;
                    }
                }
            }
            return false;
        }

        Dictionary<string, string> youtubeTitleChanges = null;
        void getYoutubeTitleChanges()
        {
            string titleChanges = GetWebData("http://mossy-xbmc-repo.googlecode.com/git/src/plugin.video.4od/titlechanges.txt");
            youtubeTitleChanges = new Dictionary<string, string>();
            foreach (string change in titleChanges.Split("\r\n".ToCharArray()))
            {
                string[] keyVal = change.Split(',');
                if (keyVal.Length == 2)
                    youtubeTitleChanges.Add(keyVal[0], keyVal[1]);
            }
        }

        #endregion

        Category createCategory(string title, string description, string thumb, string url, object other, Category parentCategory)
        {
            if (string.IsNullOrEmpty(thumb))
                thumb = defaultLogo;
            else if (!thumb.StartsWith("http://"))
                thumb = "http://www.channel4.com" + thumb;

            return new RssLink()
            {
                Name = cleanString(title),
                Description = cleanString(description),
                Thumb = thumb,
                Url = url.StartsWith("http://") ? url : "http://www.channel4.com" + url,
                Other = other,
                ParentCategory = parentCategory
            };
        }

        VideoInfo createVideoItem(string url, string title, string description, string thumb, string extraInfo = "")
        {
            return new VideoInfo()
            {
                VideoUrl = url,
                Title = cleanString(title),
                Airdate = cleanString(extraInfo),
                Description = stripTags(description),
                ImageUrl = thumb.StartsWith("http://") ? thumb : "http://www.channel4.com" + thumb,
            };
        }

        System.Net.WebProxy getProxy()
        {
            System.Net.WebProxy proxyObj = null;
            if (!string.IsNullOrEmpty(proxy))
            {
                proxyObj = new System.Net.WebProxy(proxy);
                if (!string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword))
                    proxyObj.Credentials = new System.Net.NetworkCredential(proxyUsername, proxyPassword);
            }
            return proxyObj;
        }

        string stripTags(string s)
        {
            s = cleanString(s);
            return s.Replace("<p>", "").Replace("</p>", "\r\n");
        }

        string cleanString(string s)
        {
            return s.Replace("&amp;", "&").Replace("&pound;", "£").Replace("&hellip;", "...").Trim();
        }

        string[] timeFormats = new string[] { "htt", "h.mmtt" };
        int catchupVideosComparer(VideoInfo x, VideoInfo y)
        {
            if (x == y)
                return 0;
            DateTime xTime, yTime;
            if (x == null || !DateTime.TryParseExact(x.Airdate, timeFormats, new System.Globalization.CultureInfo("en-GB"), System.Globalization.DateTimeStyles.None, out xTime))
                return -1;
            if (y == null || !DateTime.TryParseExact(y.Airdate, timeFormats, new System.Globalization.CultureInfo("en-GB"), System.Globalization.DateTimeStyles.None, out yTime))
                return 1;
            return xTime.CompareTo(yTime);
        }
    }

    class EpisodeInfo
    {
        public string SeriesTitle { get; set; }
        public string SeriesNumber { get; set; }
        public string EpisodeNumber { get; set; }
        public string AirDate { get; set; }
    }
}