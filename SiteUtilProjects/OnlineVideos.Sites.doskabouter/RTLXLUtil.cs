using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;
using System.Web;
using System.Net;
using System.IO;
using HybridDSP.Net.HTTP;

namespace OnlineVideos.Sites
{
    public class RTLXLUtil : SiteUtilBase
    {
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            foreach (Category cat in Settings.Categories)
                cat.HasSubCategories = true;
            return base.DiscoverDynamicCategories();
        }

        private Dictionary<Category, List<VideoInfo>> videos = new Dictionary<Category, List<VideoInfo>>();

        private int Lev2(Category parentCategory, XmlDocument doc)
        {
            Dictionary<string, Category> seasons = new Dictionary<string, Category>();
            foreach (XmlNode node in doc.SelectNodes(@"/xldata/season-list/season"))
            {
                RssLink season = AddtoParent(parentCategory, getNodeText(node, "name"), String.Empty);
                seasons.Add(getAttText(node, "key"), season);
            }

            Dictionary<string, XmlNode> episodes = new Dictionary<string, XmlNode>();
            foreach (XmlNode node in doc.SelectNodes(@"/xldata/episode-list/episode"))
                episodes.Add(getAttText(node, "key"), node);

            foreach (XmlNode node in doc.SelectNodes(@"/xldata/material_list/material"))
            {
                string seasonKey = getAttText(node, "season_key");
                Category season = seasons.ContainsKey(seasonKey) ? seasons[seasonKey] : parentCategory;
                Category parentCat = season;
                string parent = getNodeText(node, "tablabel");
                if (!String.IsNullOrEmpty(parent))
                {
                    RssLink tab = season.SubCategories == null ? null :
                        (RssLink)season.SubCategories.Find(item => item.Name.Equals(parent));
                    if (tab == null)
                        tab = AddtoParent(season, parent, String.Empty);
                    parentCat = tab;
                }
                AddToVidList(GetVideoFromNode(node, episodes), parentCat);
            }
            if (parentCategory.SubCategories == null)
                parentCategory.SubCategories = new List<Category>();

            if (parentCategory.SubCategories.Count == 1 && parentCategory.SubCategories[0].SubCategories != null)
            {
                Category sub = parentCategory.SubCategories[0];
                parentCategory.HasSubCategories = sub.HasSubCategories;
                parentCategory.SubCategoriesDiscovered = sub.SubCategoriesDiscovered;
                parentCategory.SubCategories.Clear();
                foreach (Category cat in sub.SubCategories)
                {
                    parentCategory.SubCategories.Add(cat);
                    cat.ParentCategory = parentCategory;
                }
            }
            return parentCategory.SubCategories.Count;
        }

        private int Films(Category parentCategory, XmlDocument doc)
        {
            Dictionary<string, XmlNode> episodes = new Dictionary<string, XmlNode>();
            foreach (XmlNode node in doc.SelectNodes(@"/xldata/episode-list/episode"))
                episodes.Add(getAttText(node, "key"), node);

            Dictionary<string, Category> genres = new Dictionary<string, Category>();
            SortedList<string, Category> genreList = new SortedList<string, Category>();

            foreach (XmlNode node in doc.SelectNodes(@"/xldata/genre-list/genre"))
            {
                Category cat = AddtoParent(parentCategory, node.InnerText, String.Empty);
                genreList.Add(cat.Name, cat);
                genres.Add(getAttText(node, "code"), cat);
            }
            parentCategory.SubCategories.Clear();
            genres.Add(String.Empty, AddtoParent(parentCategory, "Geen", String.Empty));
            parentCategory.SubCategories.AddRange(genreList.Values);

            foreach (XmlNode node in doc.SelectNodes(@"/xldata/material_list/material"))
            {
                XmlNode epNode;
                VideoInfo vid = GetVideoFromNode(node, episodes, out epNode);
                string cent = getNodeText(node, "tariff");
                if (!String.IsNullOrEmpty(cent))
                    vid.Description = "Betaalfilm: " + (Double.Parse(cent) / 100) + " " + vid.Description;
                string[] genreIds = getNodeText(epNode, "genre").Split('|');
                foreach (string genre in genreIds)
                {
                    Category parentCat = genres[genre];
                    AddToVidList(vid, parentCat);
                }
            }

            foreach (List<VideoInfo> vidList in videos.Values)
                vidList.Sort(CompareVideos);
            return parentCategory.SubCategories.Count;
        }

        private static int CompareVideos(VideoInfo v1, VideoInfo v2)
        {
            return v1.Title.CompareTo(v2.Title);

        }

        private RssLink AddtoParent(Category parentCategory, string name, string url)
        {
            RssLink tmp = new RssLink();
            tmp.ParentCategory = parentCategory;
            tmp.Name = name;
            tmp.Url = url;
            if (parentCategory.SubCategories == null)
            {
                parentCategory.SubCategories = new List<Category>();
                parentCategory.SubCategoriesDiscovered = true;
                parentCategory.HasSubCategories = true;
            }
            parentCategory.SubCategories.Add(tmp);
            return tmp;
        }
        private VideoInfo GetVideoFromNode(XmlNode node, Dictionary<string, XmlNode> episodes)
        {
            XmlNode epNode;
            return GetVideoFromNode(node, episodes, out epNode);
        }

        private string getNodeText(XmlNode node, string name)
        {
            XmlNode tmp = node.SelectSingleNode(name);
            if (tmp != null) return tmp.InnerText;
            return String.Empty;
        }

        private string getAttText(XmlNode node, string name)
        {
            XmlAttribute tmp = node.Attributes[name];
            if (tmp != null) return tmp.Value;
            return String.Empty;
        }

        private VideoInfo GetVideoFromNode(XmlNode node, Dictionary<string, XmlNode> episodes, out XmlNode epNode)
        {
            string epKey = getAttText(node, "episode_key");
            epNode = episodes == null ? null : !episodes.ContainsKey(epKey) ? null : episodes[epKey];

            VideoInfo video = new VideoInfo();
            video.Title = getNodeText(node, "title");

            if (epNode != null && String.IsNullOrEmpty(video.Title))
                video.Title = getNodeText(epNode, "name");

            if (String.IsNullOrEmpty(video.Title)) video.Title = getNodeText(node, "name");

            if (epNode != null && String.IsNullOrEmpty(video.Title))
            {
                video.Title = getNodeText(epNode, "item_number");
                if (!String.IsNullOrEmpty(video.Title))
                    video.Title = "aflevering " + video.Title;
            }

            video.VideoUrl = getNodeText(node, "component_uri");
            if (String.IsNullOrEmpty(video.VideoUrl))
                return null;

            if (!Uri.IsWellFormedUriString(video.VideoUrl, System.UriKind.Absolute))
                video.VideoUrl = new Uri(new Uri(@"http://www.rtl.nl/"), video.VideoUrl).AbsoluteUri;

            video.Description = getNodeText(node, "synopsis");
            if (epNode != null && String.IsNullOrEmpty(video.Description))
                video.Description = getNodeText(epNode, "synopsis");

            if (getNodeText(node, "audience").Equals("DRM"))
                video.Description = "DRM " + video.Description;

            string dateCode = getNodeText(node, "broadcast_date_display");
            if (!String.IsNullOrEmpty(dateCode))
            {
                int ctime = Int32.Parse(dateCode);
                string airdate = DateTime.FromFileTime(10000000 * (long)ctime + 116444736000000000).ToString();
                if (String.IsNullOrEmpty(video.Title))
                    video.Title = "Aflevering van " + airdate;
                video.Length = '|' + Translation.Airdate + ": " + airdate;
            }

            video.ImageUrl = getNodeText(node, "thumbnail_uri");
            if (String.IsNullOrEmpty(video.ImageUrl))
                video.ImageUrl = String.Format(@"http://data.rtl.nl/system/img/71v0o4xqq2yihq1tc3gc23c2w/{0}",
                    getNodeText(node, "thumbnail_id"));
            if (!String.IsNullOrEmpty(video.ImageUrl) &&
                !Uri.IsWellFormedUriString(video.ImageUrl, System.UriKind.Absolute))
                video.ImageUrl = new Uri(new Uri(@"http://data.rtl.nl/"), video.ImageUrl).AbsoluteUri;
            return video;
        }

        private int Home(Category parentCategory, XmlDocument doc)
        {
            foreach (XmlNode node in doc.SelectNodes(@"/config/tab"))
            {
                Category tab = AddtoParent(parentCategory, getAttText(node, "label"), String.Empty);
                foreach (XmlNode vidNode in node.SelectNodes(@"material_list/material"))
                    AddToVidList(GetVideoFromNode(vidNode, null), tab);
            }
            return parentCategory.SubCategories.Count;
        }

        private void AddToVidList(VideoInfo video, Category cat)
        {
            if (video == null) return;
            List<VideoInfo> vidList;
            if (!videos.ContainsKey(cat))
            {
                vidList = new List<VideoInfo>();
                videos.Add(cat, vidList);
            }
            else
                vidList = videos[cat];
            vidList.Add(video);
        }

        private int Gemist(Category parentCategory, XmlDocument doc)
        {
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", @"http://s4mns.rtl.nl/system/xmlns/s4m");

            Dictionary<string, Category> days = new Dictionary<string, Category>();
            foreach (XmlNode node in doc.SelectNodes(@"/a:episodes/a:episode", nsmRequest))
            {
                int ctime = Int32.Parse(getNodeText(node, "broadcast_timestamp"));
                DateTime dateTime = DateTime.FromFileTime(10000000 * (long)ctime + 116444736000000000);
                DayOfWeek dow = dateTime.DayOfWeek;
                string day = CultureInfo.CurrentUICulture.DateTimeFormat.DayNames[(int)dow];
                Category tab;
                if (!days.ContainsKey(day))
                {
                    tab = AddtoParent(parentCategory, day, String.Empty);
                    days.Add(day, tab);
                }
                else
                    tab = days[day];
                VideoInfo vid = GetVideoFromNode(node, null);
                if (vid != null)
                {
                    string airdate = dateTime.ToString();
                    if (String.IsNullOrEmpty(vid.Title))
                        vid.Title = "Aflevering van " + airdate;
                    vid.Length = '|' + Translation.Airdate + ": " + airdate;
                    AddToVidList(vid, tab);
                }
            }
            return parentCategory.SubCategories.Count;
        }

        //TODO: move to siteutilbase before releasing 0.27
        private static string GetWebData(string url, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null)
        {
            try
            {
                Log.Debug("get webdata from {0}", url);
                // try cache first
                string cachedData = WebCache.Instance[url];
                if (cachedData != null) return cachedData;

                // request the data
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(true);
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                if (userAgent == null)
                    request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                else
                    request.UserAgent = userAgent;
                request.Accept = "*/*";
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                if (!String.IsNullOrEmpty(referer)) request.Referer = referer; // set refere if give
                if (cc != null) request.CookieContainer = cc; // set cookies if given
                if (proxy != null) request.Proxy = proxy;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();
                Encoding encoding = Encoding.UTF8;
                if (!forceUTF8 && !String.IsNullOrEmpty(response.CharacterSet)) encoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
                using (StreamReader reader = new StreamReader(responseStream, encoding, true))
                {
                    string str = reader.ReadToEnd().Trim();
                    // add to cache if HTTP Status was 200 and we got more than 500 bytes (might just be an errorpage otherwise)
                    if (response.StatusCode == HttpStatusCode.OK && str.Length > 500) WebCache.Instance[url] = str;
                    return str;
                }
            }
            finally
            {
                // disable unsafe header parsing if it was enabled
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(false);
            }
        }

        private int Ipad(Category parentCategory)
        {
            string webData = GetWebData(((RssLink)parentCategory).Url, null, null, null, false, false, @"Mozilla/5.0(iPad; U; CPU iPhone OS 3_2 like Mac OS X; en-us) AppleWebKit/531.21.10 (KHTML, like Gecko) Version/4.0.4 Mobile/7B314 Safari/531.21.10");

            Match m = Regex.Match(webData, @"<li><a\shref=""(?<url>\?day[^""]*)"">(?<title>[^<]*)<");
            while (m.Success)
            {
                RssLink tab = AddtoParent(parentCategory, m.Groups["title"].Value, @"http://www.rtl.nl/service/gemist/device/ipad/feed/index.xml" + m.Groups["url"].Value);
                tab.Other = true;
                m = m.NextMatch();
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;
            if (url.Contains("ipad"))
                return Ipad(parentCategory);
            string webData = GetWebData(url);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(webData);
            XmlNode modeNode = doc.SelectSingleNode(@"/xldata[@mode]");
            string mode = modeNode == null ? String.Empty : modeNode.Attributes["mode"].InnerText;
            switch (mode)
            {
                case "films": return Films(parentCategory, doc);
                case "abstract": return Lev2(parentCategory, doc);
                case "a-z": return AZ(parentCategory, doc);
            }
            if (doc.SelectNodes(@"/config/tab").Count != 0)
                return Home(parentCategory, doc);
            if (webData.Contains(@"s4m:episodes"))
                return Gemist(parentCategory, doc);
            return 0;
        }

        private int AZ(Category parentCategory, XmlDocument doc)
        {
            SortedDictionary<string, Category> subcats = new SortedDictionary<string, Category>();
            foreach (XmlNode node in doc.SelectNodes(@"/xldata/abstract-list/abstract"))
            {
                string name = getNodeText(node, "name");
                string parent = name.Substring(0, 1);
                if (parent[0] >= '0' && parent[0] <= '9')
                    parent = "0-9";
                else
                    parent = parent.ToUpper();

                Category thisParent;
                if (!subcats.ContainsKey(parent))
                {
                    subcats.Add(parent, new Category());
                    thisParent = subcats[parent];
                    thisParent.ParentCategory = parentCategory;
                    thisParent.Name = parent;
                }
                else
                    thisParent = subcats[parent];

                AddtoParent(thisParent, name,
                    String.Format(@"http://www.rtl.nl/system/s4m/xldata/abstract/{0}.xml", getAttText(node, "key"))).HasSubCategories = true;

            }
            parentCategory.SubCategories = new List<Category>();
            foreach (Category cat in subcats.Values)
                parentCategory.SubCategories.Add(cat);
            parentCategory.SubCategoriesDiscovered = true;
            parentCategory.HasSubCategories = true;
            return parentCategory.SubCategories.Count;
        }

        private List<VideoInfo> IpadVideoList(Category category)
        {
            string webData = GetWebData(((RssLink)category).Url, null, null, null, false, false, @"Mozilla/5.0(iPad; U; CPU iPhone OS 3_2 like Mac OS X; en-us) AppleWebKit/531.21.10 (KHTML, like Gecko) Version/4.0.4 Mobile/7B314 Safari/531.21.10");
            Match m = Regex.Match(webData, @"&files=(?<data>[^\)]*)\)");
            string[] imageUrls1 = null;
            string[] imageUrls2 = null;
            if (m.Success)
            {
                imageUrls1 = m.Groups["data"].Value.Split('~');
                m = m.NextMatch();
                if (m.Success)
                    imageUrls2 = m.Groups["data"].Value.Split('~');
            }
            string[] imageUrls = new string[2 * (Math.Max(imageUrls1.Length, imageUrls2.Length))];
            for (int i = 0; i < imageUrls.Length; i++)
            {
                if (i % 2 == 0)
                    imageUrls[i] = imageUrls1 != null && i / 2 < imageUrls1.Length ? imageUrls1[i / 2] : String.Empty;
                else
                    imageUrls[i] = imageUrls2 != null && i / 2 < imageUrls2.Length ? imageUrls2[i / 2] : String.Empty;
            }

            List<VideoInfo> result = new List<VideoInfo>();
            int cnt = 0;
            m = Regex.Match(webData, @"<li\sclass=""video_item"">(?:(?!ns_url).)*ns_url=(?<VideoUrl>[^""]*)""[^>]*>[^>]*>[^>]*>(?<Title>[^<]*)[^>]*>[^>]*>(?<Airdate>[^<]*)");
            while (m.Success)
            {
                VideoInfo videoInfo = new VideoInfo();
                videoInfo.Other = true;
                videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                videoInfo.ImageUrl = cnt < imageUrls.Length ? @"http://iptv.rtl.nl/nettv/" + imageUrls[cnt].Split(',')[0] : null;
                string Airdate = m.Groups["Airdate"].Value;
                if (!String.IsNullOrEmpty(Airdate))
                    videoInfo.Length = videoInfo.Length + '|' + Translation.Airdate + ": " + Airdate;
                result.Add(videoInfo);
                m = m.NextMatch();
                cnt++;
            }
            return result;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (true.Equals(category.Other))
                return IpadVideoList(category);
            if (videos.ContainsKey(category))
                return videos[category];
            else
                return new List<VideoInfo>();
        }

        public override string getUrl(VideoInfo video)
        {
            if (true.Equals(video.Other))
                return video.VideoUrl;
            string url = video.VideoUrl.Replace(@"rtl.nl/components", @"rtl.nl/system/video/wvx/components") + @"/1500.wvx?utf8=ok";
            string webData = GetWebData(url);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(webData);
            List<string> urls = ParseASX(url);
            string url2 = urls.Count > 1 ? urls[1] : urls[0];
            return url2;
        }
    }
}
