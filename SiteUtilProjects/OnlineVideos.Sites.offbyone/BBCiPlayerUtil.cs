using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using RssToolkit.Rss;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class BBCiPlayerUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("Proxy to use for WebRequests (must be in the UK). Define like this: 83.84.85.86:8116")]
        string proxy = null;
        [Category("OnlineVideosConfiguration"), Description("Format string used as Url for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://feeds.bbc.co.uk/iplayer/search/tv/?q={0}";
        [Category("OnlineVideosUserConfiguration"), Description("Group similar items from the rss feeds into subcategories.")]
        bool autoGrouping = true;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used on a video thumbnail for matching a string to be replaced for higher quality")]
        string thumbReplaceRegExPattern;
        [Category("OnlineVideosConfiguration"), Description("The string used to replace the match if the pattern from the thumbReplaceRegExPattern matched")]
        string thumbReplaceString;

        public override string getUrl(VideoInfo video)
        {
            XmlDocument doc = new XmlDocument();
            XmlNamespaceManager nsmRequest;
            string id;

            System.Net.WebProxy proxyObj = null;// new System.Net.WebProxy("127.0.0.1", 8118);
            if (!string.IsNullOrEmpty(proxy)) proxyObj = new System.Net.WebProxy(proxy);

            if (video.Other == "livestream")
            {
                id = video.VideoUrl;
            }
            else
            {
                doc.LoadXml(GetWebData(video.VideoUrl));
                nsmRequest = new XmlNamespaceManager(doc.NameTable);
                nsmRequest.AddNamespace("ns1", "http://bbc.co.uk/2008/emp/playlist");
                id = doc.SelectSingleNode("//ns1:item[@kind='programme']/@identifier", nsmRequest).Value;
            }

            doc = new XmlDocument();
            doc.LoadXml(GetWebData("http://www.bbc.co.uk/mediaselector/4/mtis/stream/" + id, null, null, proxyObj)); //uk only
            nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("ns1", "http://bbc.co.uk/2008/mp/mediaselection");

            SortedList<string, string> sortedPlaybackOptions = new SortedList<string, string>(new QualityComparer());
            foreach(XmlElement mediaElem in doc.SelectNodes("//ns1:media[@kind='video']", nsmRequest))
            {
                string info = "";
                string resultUrl = "";
                foreach (XmlElement connectionElem in mediaElem.SelectNodes("ns1:connection", nsmRequest))
                {
                    /*
                    if (Array.BinarySearch<string>(new string[] {"http","sis"}, connectionElem.Attributes["kind"].Value)>=0)
                    {
                        // http
                        info = string.Format("{0}x{1} | {2}kbps| {3}", mediaElem.GetAttribute("width"), mediaElem.GetAttribute("height"), mediaElem.GetAttribute("bitrate"), connectionElem.GetAttribute("kind"));
                        resultUrl = connectionElem.Attributes["href"].Value;
                    }
                    else */
                    if (Array.BinarySearch<string>(new string[] { "akamai", "level3", "limelight" }, connectionElem.Attributes["kind"].Value) >= 0)
                    {
                        // rtmp
                        string server = connectionElem.Attributes["server"].Value;
                        string identifier = connectionElem.Attributes["identifier"].Value;
                        string auth = connectionElem.Attributes["authString"].Value;
                        string application = connectionElem.GetAttribute("application");
                        if (string.IsNullOrEmpty(application)) application = video.Other == "livestream" ? "live" : "ondemand";
                        string SWFPlayer = "http://www.bbc.co.uk/emp/10player.swf";

                        info = string.Format("{0}x{1} | {2} kbps | {3}", mediaElem.GetAttribute("width"), mediaElem.GetAttribute("height"), mediaElem.GetAttribute("bitrate"), connectionElem.Attributes["kind"].Value);
                        resultUrl = "";

                        if (connectionElem.Attributes["kind"].Value == "limelight")
                        {
                            resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?hostname={0}&port={1}&app={2}&tcUrl={3}&playpath={4}&swfVfy={5}&live={6}",
                                System.Web.HttpUtility.UrlEncode(server),
                                "1935",
                                System.Web.HttpUtility.UrlEncode(application + "?" + auth),
                                System.Web.HttpUtility.UrlEncode(string.Format("rtmp://{0}:1935/{1}", server, application + "?" + auth)),
                                System.Web.HttpUtility.UrlEncode(identifier),
                                System.Web.HttpUtility.UrlEncode(SWFPlayer),
                                (video.Other == "livestream").ToString().ToLower()));
                        }
                        else if (connectionElem.Attributes["kind"].Value == "level3")
                        {
                            application += "?" + auth;

                            if (auth.StartsWith("token=")) auth = auth.Substring(6);
                            resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?hostname={0}&port={1}&app={2}&tcUrl={3}&playpath={4}&swfVfy={5}&token={6}&live={7}",
                                System.Web.HttpUtility.UrlEncode(server),
                                "1935",
                                System.Web.HttpUtility.UrlEncode(application),
                                System.Web.HttpUtility.UrlEncode(string.Format("rtmp://{0}:1935/{1}", server, application)),
                                System.Web.HttpUtility.UrlEncode(identifier),
                                System.Web.HttpUtility.UrlEncode(SWFPlayer),
                                System.Web.HttpUtility.UrlEncode(auth),
                                (video.Other == "livestream").ToString().ToLower()));
                        }
                        else if (connectionElem.Attributes["kind"].Value == "akamai")
                        {
                            resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&playpath={1}&swfVfy={2}&live={3}",
                                System.Web.HttpUtility.UrlEncode(string.Format("rtmp://{0}:1935/{1}?{2}", server, application, auth)),
                                System.Web.HttpUtility.UrlEncode(identifier),
                                System.Web.HttpUtility.UrlEncode(SWFPlayer),
                                (video.Other == "livestream").ToString().ToLower()));
                        }                                                
                    }
                    if (resultUrl != "") sortedPlaybackOptions.Add(info, resultUrl);
                }
            }

            string lastUrl = "";
            video.PlaybackOptions = new Dictionary<string, string>();
            var enumer = sortedPlaybackOptions.GetEnumerator();
            while (enumer.MoveNext())
            {
                if (!enumer.Current.Value.Contains("3200")) lastUrl = enumer.Current.Value;
                video.PlaybackOptions.Add(enumer.Current.Key, enumer.Current.Value);
            }
            return lastUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (category is Group)
            {
                List<VideoInfo> list = new List<VideoInfo>();
                foreach (Channel channel in ((Group)category).Channels)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = channel.StreamName;
                    video.VideoUrl = channel.Url;
                    video.Other = "livestream";
                    video.ImageUrl = channel.Thumb;
                    list.Add(video);
                }
                return list;
            }
            else
            {
                if (autoGrouping)
                    return (category as RssLink).Other as List<VideoInfo>;
                else
                    return getVideoListInternal((category as RssLink).Url);
            }
        }

        DateTime lastRefresh = DateTime.MinValue;
        public override int DiscoverDynamicCategories()
        {
            if (autoGrouping)
            {
                if ((DateTime.Now - lastRefresh).TotalMinutes > 15)
                {
                    foreach (Category cat in Settings.Categories)
                    {
                        if (cat is RssLink)
                        {
                            cat.HasSubCategories = true;
                            cat.SubCategoriesDiscovered = false;
                        }
                    }
                    lastRefresh = DateTime.Now;
                }
            }
            else
            {
                Settings.DynamicCategoriesDiscovered = true;
            }
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            Dictionary<string, List<VideoInfo>> possibleSubCatStrings = new Dictionary<string, List<VideoInfo>>();
            List<VideoInfo> allOthers = new List<VideoInfo>();            
            List<VideoInfo> videos = getVideoListInternal((parentCategory as RssLink).Url);
            foreach (VideoInfo video in videos)
            {
                int colonIndex = video.Title.IndexOf(":");
                if (colonIndex > 0)
                {
                    string title = video.Title.Substring(0, colonIndex);
                    List<VideoInfo> catVids;
                    if (!possibleSubCatStrings.TryGetValue(title, out catVids)) catVids = new List<VideoInfo>();
                    catVids.Add(video);
                    video.Title = video.Title.Substring(colonIndex+1).Trim();
                    possibleSubCatStrings[title] = catVids;
                }
                else
                {
                    allOthers.Add(video);
                }
            }
            if (allOthers.Count > 0)
            {
                // add all others on top
                parentCategory.SubCategories.Add(new RssLink() { Name = "All Others", ParentCategory = parentCategory, Other = allOthers, EstimatedVideoCount = (uint)allOthers.Count });
            }
            // sort the remaining alphabetically
            string[] keys = new string[possibleSubCatStrings.Count];
            possibleSubCatStrings.Keys.CopyTo(keys, 0);
            Array.Sort(keys);
            foreach(string key in keys)
            {
                List<VideoInfo> value = possibleSubCatStrings[key];
                parentCategory.SubCategories.Add(new RssLink() { Name = key, ParentCategory = parentCategory, Other = value, EstimatedVideoCount = (uint)value.Count });
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        List<VideoInfo> getVideoListInternal(string url)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssItem rssItem in GetWebData<RssDocument>(url).Channel.Items)
            {
                VideoInfo video = VideoInfo.FromRssItem(rssItem, true, new Predicate<string>(isPossibleVideo));
                video.VideoUrl = "http://www.bbc.co.uk/iplayer/playlist/" + rssItem.Guid.Text.Substring(rssItem.Guid.Text.LastIndexOf(':') + 1);
                if (!string.IsNullOrEmpty(thumbReplaceRegExPattern)) video.ImageUrl = System.Text.RegularExpressions.Regex.Replace(video.ImageUrl, thumbReplaceRegExPattern, thumbReplaceString);
                loVideoList.Add(video);
            }
            return loVideoList;
        }

        #region Search

        public override bool CanSearch { get { return !string.IsNullOrEmpty(searchUrl); } }

        public override List<VideoInfo> Search(string query)
        {
            return getVideoListInternal(string.Format(searchUrl, query));
        }

        #endregion

    }

    class QualityComparer : IComparer<string>
    {
        #region IComparer<string> Member

        public int Compare(string x, string y)
        {
            int x_kbps = 0;
            if (!int.TryParse(x.Substring(x.IndexOf('|')+1).Replace(" kbps", ""), out x_kbps)) return 1;
            int y_kbps = 0;
            if (!int.TryParse(y.Substring(y.IndexOf('|') + 1).Replace(" kbps", ""), out y_kbps)) return -1;
            return x_kbps.CompareTo(y_kbps);
        }

        #endregion
    }
}

