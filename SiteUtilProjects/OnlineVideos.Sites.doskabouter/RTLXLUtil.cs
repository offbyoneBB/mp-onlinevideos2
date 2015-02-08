using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;
using System.Web;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites
{
    public class RTLXLUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("Show DRM content")]
        bool allowDRM = false;

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
                if (vid != null)
                {
                    string cent = getNodeText(node, "tariff");
                    if (!String.IsNullOrEmpty(cent))
                    {
                        vid.Description = "Betaalfilm: " + (Double.Parse(cent) / 100) + " " + vid.Description;
                    }

                    string[] genreIds = getNodeText(epNode, "genre").Split('|');
                    foreach (string genre in genreIds)
                    {
                        Category parentCat = genres[genre];
                        AddToVidList(vid, parentCat);
                    }
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
                parentCategory.HasSubCategories = true;
            }
            parentCategory.SubCategoriesDiscovered = true;
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

            string audienceText = getNodeText(node, "audience");

            //audience: DRM | ALL | ALLEEN_NL
            //blacklist: DRM + ALLEEN_NL, case insensitive
            if (audienceText.Equals("DRM", StringComparison.CurrentCultureIgnoreCase)
               || audienceText.Equals("ALLEEN_NL", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!allowDRM)
                {
                    return null; //don't add DRM content
                }
                video.Description = "DRM " + video.Description;
            }

            string dateCode = getNodeText(node, "broadcast_date_display");
            if (!String.IsNullOrEmpty(dateCode))
            {
                int ctime = Int32.Parse(dateCode);
                string airdate = DateTime.FromFileTime(10000000 * (long)ctime + 116444736000000000).ToString();
                if (String.IsNullOrEmpty(video.Title))
                    video.Title = "Aflevering van " + airdate;
                video.Length = '|' + Translation.Instance.Airdate + ": " + airdate;
            }

            video.Thumb = getNodeText(node, "thumbnail_uri");
            if (String.IsNullOrEmpty(video.Thumb))
                video.Thumb = String.Format(@"http://data.rtl.nl/system/img/71v0o4xqq2yihq1tc3gc23c2w/{0}",
                    getNodeText(node, "thumbnail_id"));
            if (!String.IsNullOrEmpty(video.Thumb) &&
                !Uri.IsWellFormedUriString(video.Thumb, System.UriKind.Absolute))
                video.Thumb = new Uri(new Uri(@"http://data.rtl.nl/"), video.Thumb).AbsoluteUri;
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

                if (DateTime.Today.DayOfWeek == dow)
                {
                    //today (same day) so add label.
                    day += " (today)";
                }

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
                    vid.Length = '|' + Translation.Instance.Airdate + ": " + airdate;
                    AddToVidList(vid, tab);
                }
            }
            return parentCategory.SubCategories.Count;
        }

        private void AddVideo(Category parentCategory, string path, VideoInfo video)
        {
            string[] subPaths = path.Split('\t');

            Category curr = parentCategory;

            foreach (string sub in subPaths)
            {
                RssLink tab = (curr.SubCategories == null) ? null :
                 (RssLink)curr.SubCategories.Find(item => item.Name.Equals(sub, StringComparison.InvariantCultureIgnoreCase));
                if (tab == null)
                    tab = AddtoParent(curr, sub, String.Empty);
                curr = tab;
            }

            if (curr.Other == null)
                curr.Other = new List<VideoInfo>();
            ((List<VideoInfo>)curr.Other).Add(video);
        }

        private DateTime lastChecked = DateTime.MinValue;

        private int Ipad(Category parentCategory)
        {
            lastChecked = DateTime.Now;
            XmlDocument doc = new XmlDocument();
            string t = GetWebData(((RssLink)parentCategory).Url);
            doc.LoadXml(t);
            foreach (XmlNode item in doc.SelectNodes("//items/item"))
            {
                string title = item.SelectSingleNode("title").InnerText;
                string serieNaam = item.SelectSingleNode("serienaam").InnerText;
                DateTime aired = DateTime.Parse(item.SelectSingleNode("broadcastdatetime").InnerText);

                VideoInfo videoInfo = new VideoInfo();
                videoInfo.Other = true; // url is direct link to mp4, so no geturl needed
                videoInfo.Title = title;
                videoInfo.VideoUrl = item.SelectSingleNode("movie").InnerText;
                if (!Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute))
                    videoInfo.VideoUrl = @"http://iptv.rtl.nl/nettv/" + videoInfo.VideoUrl;

                videoInfo.Thumb = item.SelectSingleNode("thumbnail").InnerText;
                if (!Uri.IsWellFormedUriString(videoInfo.Thumb, System.UriKind.Absolute))
                    videoInfo.Thumb = @"http://iptv.rtl.nl/nettv/" + videoInfo.Thumb;
                videoInfo.Length = '|' + Translation.Instance.Airdate + ": " + aired.ToString();
                videoInfo.Description = item.SelectSingleNode("samenvattinglang").InnerText;

                string airDate = aired.ToShortDateString();
                AddVideo(parentCategory, "Datum\t" + airDate, videoInfo);
                if (!String.IsNullOrEmpty(serieNaam))
                {
                    string pre = serieNaam.ToUpperInvariant();
                    if (!(pre[0] >= 'A' && pre[0] <= 'Z'))
                        pre = "0-9";
                    else
                        pre = pre[0].ToString();
                    AddVideo(parentCategory, "A - Z\t" + pre + '\t' + serieNaam,
                        videoInfo);
                }

            }

            Category az = parentCategory.SubCategories.Find(item => item.Name.Equals("A - Z"));
            if (az != null && az.SubCategories != null)
            {
                az.SubCategories.Sort(CategoryComparer);
                foreach (Category sub in az.SubCategories)
                {
                    if (sub.SubCategories != null)
                        sub.SubCategories.Sort(CategoryComparer);
                }
            }
            parentCategory.SubCategoriesDiscovered = false;
            return parentCategory.SubCategories.Count;
        }

        private int CategoryComparer(Category cat1, Category cat2)
        {
            return String.Compare(cat1.Name, cat2.Name);

        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;
            if (!url.StartsWith("http://www.rtl.nl/"))
            {
                if (parentCategory.SubCategories != null && parentCategory.SubCategories.Count > 0 &&
                    OnlineVideoSettings.Instance.CacheTimeout > 0 &&
                    (DateTime.Now - lastChecked).TotalMinutes < OnlineVideoSettings.Instance.CacheTimeout)
                    return parentCategory.SubCategories.Count;

                return Ipad(parentCategory);
            }
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

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other != null)
                return (List<VideoInfo>)category.Other;
            if (videos.ContainsKey(category))
                return videos[category];
            else
                return new List<VideoInfo>();
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            if (true.Equals(video.Other))
                return video.VideoUrl;
            string url = video.VideoUrl.Replace(@"rtl.nl/components", @"rtl.nl/system/video/wvx/components") + @"/1500.wvx?utf8=ok";
            string webData = GetWebData(url);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(webData);
            List<string> urls = Helpers.AsxUtils.ParseASX(GetWebData(url));
            string url2 = urls.Count > 1 ? urls[1] : urls[0];
            return url2;
        }

    }
}
