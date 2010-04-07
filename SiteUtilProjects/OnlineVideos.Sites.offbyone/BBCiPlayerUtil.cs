using System;
using System.Collections.Generic;
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

        public override string getUrl(VideoInfo video)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWebData(video.VideoUrl));
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("ns1", "http://bbc.co.uk/2008/emp/playlist");
            string id = doc.SelectSingleNode("//ns1:item[@kind='programme']/@identifier", nsmRequest).Value;

            System.Net.WebProxy proxyObj = null; // new System.Net.WebProxy("127.0.0.1", 8118);
            if (!string.IsNullOrEmpty(proxy)) proxyObj = new System.Net.WebProxy(proxy);

            doc = new XmlDocument();
            doc.LoadXml(GetWebData("http://www.bbc.co.uk/mediaselector/4/mtis/stream/" + id, null, null, proxyObj)); //uk only
            nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("ns1", "http://bbc.co.uk/2008/mp/mediaselection");

            List<string> urls = new List<string>();
            foreach(XmlElement mediaElem in doc.SelectNodes("//ns1:media[@kind='video']", nsmRequest))
            {                
                foreach (XmlElement connectionElem in mediaElem.SelectNodes("ns1:connection", nsmRequest))
                {
                    if (Array.BinarySearch<string>(new string[] {"http","sis"}, connectionElem.Attributes["kind"].Value)>=0)
                    {
                        // http
                        string resultUrl = connectionElem.Attributes["href"].Value;
                        urls.Add(resultUrl);
                    }
                    else if (Array.BinarySearch<string>(new string[] { "akamai", "level3", "limelight" }, connectionElem.Attributes["kind"].Value) >= 0)
                    {
                        // rtmp
                        string server = connectionElem.Attributes["server"].Value;
                        string identifier = connectionElem.Attributes["identifier"].Value;
                        string auth = connectionElem.Attributes["authString"].Value;
                        string application = "ondemand";
                        string SWFPlayer = "http://www.bbc.co.uk/emp/9player.swf?revision=7276";
                        string PlayPath = identifier;

                        if (connectionElem.Attributes["kind"].Value == "limelight")
                        {
                            application = connectionElem.Attributes["application"].Value + "?" + auth;
                            PlayPath = identifier + "?" + auth;
                            SWFPlayer = "http://www.bbc.co.uk/emp/9player.swf?revision=10344_10753";
                        }
                        else if (mediaElem.Attributes["encoding"].Value == "h264")
                        {
                            PlayPath = identifier;                
                            identifier = PlayPath.Substring("mp4:".Length);
                            SWFPlayer = "http://www.bbc.co.uk/emp/9player.swf?revision=10344_10753";
                        }

                        string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?hostname={1}&port={2}&app={3}&tcUrl={4}&playpath={5}&swfurl={6}",
                            OnlineVideoSettings.RTMP_PROXY_PORT,
                            System.Web.HttpUtility.UrlEncode(server),
                            "1935",
                            System.Web.HttpUtility.UrlEncode(application),
                            System.Web.HttpUtility.UrlEncode(string.Format("rtmp://{0}:1935/{1}", server, application)),
                            System.Web.HttpUtility.UrlEncode(PlayPath),
                            System.Web.HttpUtility.UrlEncode(SWFPlayer),
                            System.Web.HttpUtility.UrlEncode(auth));

                        urls.Add(resultUrl);
                    }
                }                
            }
            urls.Sort();
            return urls[0];
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return (category as RssLink).Other as List<VideoInfo>;
        }
        
        public override int DiscoverDynamicCategories()
        {
            foreach (Category cat in Settings.Categories)
            {
                cat.HasSubCategories = true;
                cat.SubCategoriesDiscovered = false; // todo speedup: only every x mins
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
            foreach (RssItem rssItem in GetWebDataAsRss(url).Channel.Items)
            {
                VideoInfo video = VideoInfo.FromRssItem(rssItem, true, new Predicate<string>(isPossibleVideo));
                video.VideoUrl = "http://www.bbc.co.uk/iplayer/playlist/" + rssItem.Guid.Text.Substring(rssItem.Guid.Text.LastIndexOf(':') + 1);
                loVideoList.Add(video);
            }
            return loVideoList;
        }
    }
}
