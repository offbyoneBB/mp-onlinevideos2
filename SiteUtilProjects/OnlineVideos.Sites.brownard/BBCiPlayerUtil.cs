using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class BBCiPlayerUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("Proxy to use for WebRequests (must be in the UK). Define like this: 83.84.85.86:8116")]
        string proxy = null;
		[Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a username, set it here.")]
		string proxyUsername = null;
		[Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a password, set it here.")]
        string proxyPassword = null;
        [Category("OnlineVideosUserConfiguration"), Description("Whether to download subtitles")]
        protected bool RetrieveSubtitles = false;
        [Category("OnlineVideosConfiguration"), Description("Format string used as Url for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://feeds.bbc.co.uk/iplayer/search/tv/?q={0}";
        [Category("OnlineVideosUserConfiguration"), Description("Group similar items from the rss feeds into subcategories.")]
        bool autoGrouping = true;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used on a video thumbnail for matching a string to be replaced for higher quality")]
        protected string thumbReplaceRegExPattern;
        [Category("OnlineVideosConfiguration"), Description("The string used to replace the match if the pattern from the thumbReplaceRegExPattern matched")]
		protected string thumbReplaceString;

        //TV Guide options
        [Category("OnlineVideosUserConfiguration"), Description("Whether to retrieve current program info for live streams.")]
        protected bool retrieveTVGuide = true;
        [Category("OnlineVideosConfiguration"), Description("The layout to use to display TV Guide info, possible wildcards are <nowtitle>,<nowdescription>,<nowstart>,<nowend>,<nexttitle>,<nextstart>,<nextend>,<newline>")]
        protected string tvGuideFormatString;

        public override string getUrl(VideoInfo video)
        {
            XmlDocument doc = new XmlDocument();
            XmlNamespaceManager nsmRequest;
            string id;
            System.Net.WebProxy proxyObj = getProxy();

            if (video.Other == "livestream")
            {
                return getLiveUrls(video);//id = video.VideoUrl;
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

            if (RetrieveSubtitles)
            {
                XmlNode captionNode = doc.SelectSingleNode("//ns1:media[@kind='captions']", nsmRequest);
                if (captionNode != null)
                {
                    XmlNode captionConnection = captionNode.SelectSingleNode("ns1:connection", nsmRequest);
                    if (captionConnection != null && captionConnection.Attributes["href"] != null)
                    {
                        string sub = GetWebData(captionConnection.Attributes["href"].Value);
                        video.SubtitleText = OnlineVideos.Sites.Utils.SubtitleReader.TimedText2SRT(sub);
                    }
                }
            }

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
                        if (connectionElem.Attributes["protocol"] == null || connectionElem.Attributes["protocol"].Value != "rtmp")
                            continue;

                        string server = connectionElem.Attributes["server"].Value;
                        string identifier = connectionElem.Attributes["identifier"].Value;
                        string auth = connectionElem.Attributes["authString"].Value;
                        string application = connectionElem.GetAttribute("application");
						if (string.IsNullOrEmpty(application)) application = video.Other == "livestream" ? "live" : "ondemand";
                        string SWFPlayer = "http://www.bbc.co.uk/emp/releases/iplayer/revisions/617463_618125_4/617463_618125_4_emp.swf"; // "http://www.bbc.co.uk/emp/10player.swf";

                        info = string.Format("{0}x{1} | {2} kbps | {3}", mediaElem.GetAttribute("width"), mediaElem.GetAttribute("height"), mediaElem.GetAttribute("bitrate"), connectionElem.Attributes["kind"].Value);
                        resultUrl = "";
                        if (connectionElem.Attributes["kind"].Value == "limelight")
                        {
                            resultUrl = new MPUrlSourceFilter.RtmpUrl(string.Format("rtmp://{0}:1935/{1}", server, application + "?" + auth), server, 1935)
							{ 
								App = application + "?" + auth,
                                PlayPath = identifier,
								SwfUrl = SWFPlayer,
								SwfVerify = true,
								Live = video.Other == "livestream" 
							}.ToString();
                        }
                        else if (connectionElem.Attributes["kind"].Value == "level3")
                        {
							resultUrl = new MPUrlSourceFilter.RtmpUrl(string.Format("rtmp://{0}:1935/{1}", server, application + "?" + auth), server, 1935) 
							{
								App = application + "?" + auth,
								PlayPath = identifier,
								SwfUrl = SWFPlayer,
								SwfVerify = true,
								Token = auth,
								Live = video.Other == "livestream"
							}.ToString();
                        }
                        else if (connectionElem.Attributes["kind"].Value == "akamai")
                        {
							resultUrl = new MPUrlSourceFilter.RtmpUrl(string.Format("rtmp://{0}:1935/{1}?{2}", server, application, auth)) 
							{
								PlayPath = identifier + "?" + auth,
								SwfUrl = SWFPlayer,
								SwfVerify = true,
								Live = video.Other == "livestream"
							}.ToString();
                        }                                                
                    }
                    if (resultUrl != "") sortedPlaybackOptions.Add(info, resultUrl);
                }
            }

			if (sortedPlaybackOptions.Count == 0)
			{
				var errorNodes = doc.SelectNodes("//ns1:error", nsmRequest);
				if (errorNodes.Count > 0) throw new OnlineVideosException(string.Format("BBC says: {0}", ((XmlElement)errorNodes[0]).GetAttribute("id")));
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

        string getLiveUrls(VideoInfo video)
        {
            System.Net.WebProxy proxyObj = getProxy();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWebData("http://www.bbc.co.uk/mediaselector/playlists/hds/pc/ak/" + video.VideoUrl, null, null, proxyObj));
            SortedList<string, string> sortedPlaybackOptions = new SortedList<string, string>(new QualityComparer());
            foreach (XmlElement mediaElem in doc.GetElementsByTagName("media"))
            {
                string url = null;
                if(mediaElem.Attributes["href"] != null)
                    url = mediaElem.Attributes["href"].Value + "?live=true";
                string bitrate = "";
                if (mediaElem.Attributes["bitrate"] != null)
                    bitrate = mediaElem.Attributes["bitrate"].Value;
                if (!string.IsNullOrEmpty(url))
                    sortedPlaybackOptions.Add(bitrate + " kbps", url);
            }

            string lastUrl = "";
            video.PlaybackOptions = new Dictionary<string, string>();
            var enumer = sortedPlaybackOptions.GetEnumerator();
            while (enumer.MoveNext())
            {
                lastUrl = enumer.Current.Value;
                video.PlaybackOptions.Add(enumer.Current.Key, enumer.Current.Value);
            }
            return lastUrl; //"http://bbcfmhds.vo.llnwd.net/hds-live/livepkgr/_definst_/bbc1/bbc1_1500.f4m";
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

                    int argIndex = channel.Url.IndexOf('?');
                    if(argIndex < 0) video.VideoUrl = channel.Url;
                    else video.VideoUrl = channel.Url.Remove(argIndex);

                    video.Other = "livestream";
                    video.ImageUrl = channel.Thumb;
                    if (retrieveTVGuide && argIndex > -1)
                    {
                        Utils.TVGuideGrabber tvGuide = new Utils.TVGuideGrabber();
                        if (tvGuide.GetNowNextForChannel(channel.Url))
                            video.Description = tvGuide.FormatTVGuide(tvGuideFormatString);
                    }

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
            parentCategory.SubCategories = discoverSubCategoriesLocal(parentCategory, (parentCategory as RssLink).Url);
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        List<Category> discoverSubCategoriesLocal(Category parentCategory, string url)
        {
            List<Category> subCategories = new List<Category>();
            Dictionary<string, List<VideoInfo>> possibleSubCatStrings = new Dictionary<string, List<VideoInfo>>();
            //List<VideoInfo> allOthers = new List<VideoInfo>();            
            List<VideoInfo> videos = getVideoListInternal(url);
            foreach (VideoInfo video in videos)
            {
                string title = video.Title;
                int colonIndex = title.IndexOf(":");
                if (colonIndex > 0)
                {
                    title = video.Title.Substring(0, colonIndex);
                    video.Title = video.Title.Substring(colonIndex + 1).Trim();                    
                }

                List<VideoInfo> catVids;
                if (!possibleSubCatStrings.TryGetValue(title, out catVids)) 
                    catVids = new List<VideoInfo>();
                catVids.Add(video);
                possibleSubCatStrings[title] = catVids;
                //else
                //{
                //    allOthers.Add(video);
                //}
            }
            //if (allOthers.Count > 0)
            //{
            //    // add all others on top
            //    parentCategory.SubCategories.Add(new RssLink() { Name = "All Others", ParentCategory = parentCategory, Thumb = allOthers[0].ImageUrl, Other = allOthers, EstimatedVideoCount = (uint)allOthers.Count });
            //}
			// sort the remaining alphabetically
			string[] keys = new string[possibleSubCatStrings.Count];
			possibleSubCatStrings.Keys.CopyTo(keys, 0);
			Array.Sort(keys);
			foreach (string key in keys)
			{
				List<VideoInfo> value = possibleSubCatStrings[key];
				subCategories.Add(new RssLink() { Name = key, ParentCategory = parentCategory, Thumb = value[0].ImageUrl, Other = value, EstimatedVideoCount = (uint)value.Count });
			}

            return subCategories;
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

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> results = new List<ISearchResultItem>();
            foreach (Category cat in discoverSubCategoriesLocal(null, string.Format(searchUrl, query)))
                results.Add(cat);
            return results;
        }

        public override bool CanSearch { get { return !string.IsNullOrEmpty(searchUrl); } }

        //public override List<VideoInfo> Search(string query)
        //{
        //    return getVideoListInternal(string.Format(searchUrl, query));
        //}

        #endregion

        System.Net.WebProxy getProxy()
        {
            System.Net.WebProxy proxyObj = null;// new System.Net.WebProxy("127.0.0.1", 8118);
            if (!string.IsNullOrEmpty(proxy))
            {
                proxyObj = new System.Net.WebProxy(proxy);
                if (!string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword))
                    proxyObj.Credentials = new System.Net.NetworkCredential(proxyUsername, proxyPassword);
            }
            return proxyObj;
        }
    }

	class QualityComparer : IComparer<string>
	{
		#region IComparer<string> Member

		public int Compare(string x, string y)
		{
			int x_kbps = 0;
			if (!int.TryParse(Regex.Match(x, @"(\d+) kbps").Groups[1].Value, out x_kbps)) return 1;
			int y_kbps = 0;
			if (!int.TryParse(Regex.Match(y, @"(\d+) kbps").Groups[1].Value, out y_kbps)) return -1;

			int compare = x_kbps.CompareTo(y_kbps);
			if (compare == 0) //if bitrates same, sort alphabetically
				compare = x.CompareTo(y);
			return compare;
		}

		#endregion
	}
}

