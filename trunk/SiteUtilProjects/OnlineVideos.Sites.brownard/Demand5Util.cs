using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Net;
using System.Linq;
using System.IO;
using System.Web;
using OnlineVideos.AMF;
using OnlineVideos.Hoster;
using OnlineVideos.Sites.Brownard;

namespace OnlineVideos.Sites
{
    public class Demand5Util : SiteUtilBase
    {
        const string BASE_URL = "http://www.channel5.com";

        [Category("OnlineVideosUserConfiguration"), Description("Proxy to use for WebRequests (must be in the UK). Define like this: 83.84.85.86:8116")]
        string proxy = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a username, set it here.")]
        string proxyUsername = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a password, set it here.")]
        string proxyPassword = null;
        [Category("OnlineVideosConfiguration"), Description("HashValue")]
        protected string hashValue = ""; //"8e8e110bb9d7d95eb3c3e500a86a21024eccd983";
        [Category("OnlineVideosConfiguration"), Description("PlayerID")]
        protected double playerId = 1707001743001;
        [Category("OnlineVideosUserConfiguration"), Description("Select stream automatically?")]
        protected bool AutoSelectStream = false;
        [Category("OnlineVideosUserConfiguration"), Description("Stream quality preference\r\n1 is low, 5 high")]
        protected int StreamQualityPref = 5;

        DateTime lastRefresh = DateTime.MinValue;
        public override int DiscoverDynamicCategories()
        {
            if ((DateTime.Now - lastRefresh).TotalMinutes > 15)
            {
                foreach (Category cat in Settings.Categories)
                {
                    RssLink rss = cat as RssLink;
                    if (rss != null)
                    {
                        if (!rss.Url.Contains("recently_on_tv_episodes"))
                        {
                            cat.HasSubCategories = true;
                            cat.SubCategoriesDiscovered = false;
                        }
                        if (string.IsNullOrEmpty(cat.Thumb))
                            cat.Thumb = string.Format(@"{0}\Icons\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, Settings.Name);
                    }
                }
                lastRefresh = DateTime.Now;
            }

            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            Regex reg;
            string url;
            if ((parentCategory as RssLink).Url.EndsWith("topshows"))
            {
                reg = new Regex(@"<li[^>]*>\s*<a href=""([^""]*)"">\s*<span[^>]*>\s*<span[^>]*>[^>]*>\s*<span[^>]*>\s*<span[^>]*>[^>]*>\s*<img.*? src=""([^""]*)""[^>]*>\s*<span[^>]*>\s*<span[^>]*>Full episodes</span>(.|\s)*?<em>([^<]*)</em>");
                url = "http://www.channel5.com/shows";
            }
            else
            {
                reg = new Regex(@"<a href=""([^""]*)"" class=""clearfix"">\s*<span[^>]*>\s*<span[^>]*>[^<]*</span>\s*<img.*? src=""([^""]*)""[^>]*>\s*<span[^>]*>\s*<span[^>]*>Full episode</span>(.|\s)*?<em>([^<]*)</em>");
                url = (parentCategory as RssLink).Url;
            }

            string html = GetWebData(url);

            foreach (Match m in reg.Matches(html))
            {
                RssLink cat = new RssLink();
                cat.Url = BASE_URL + m.Groups[1].Value;
                cat.Thumb = m.Groups[2].Value;
                cat.Name = cleanString(m.Groups[4].Value);
                cat.ParentCategory = parentCategory;
                cats.Add(cat);
            }

            parentCategory.SubCategories = cats;
            parentCategory.SubCategoriesDiscovered = true;

            return cats.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is List<VideoInfo>)
                return (List<VideoInfo>)category.Other;

            RssLink rss = category as RssLink;
            if (rss.Url.Contains("recently_on_tv_episodes"))
                return getRecentlyOnVids(rss.Url);

            List<VideoInfo> vids = getVideoListInternal(rss.Url + "/episodes");
            if (vids.Count == 0)
                vids = getVideoListInternal(rss.Url);

            return vids;
        }

        private List<VideoInfo> getRecentlyOnVids(string url)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            string html = GetWebData(url);
            Regex reg = new Regex(@"<li[^>]*>\s*<p>\s*<span .*?</span>\s*<span .*?</span>\s*</p>\s*<a href=""([^""]*)"">\s*<span[^>]*>\s*<span.*?</span>\s*<img.*? src=""([^""]*)"".*?>\s*<span[^>]*>\s*<span[^>]*>Full Episode</span>(.|\s)*?<em>([^<]*)</em>\s*<small>([^<]*)</small>");
            foreach (Match m in reg.Matches(html))
            {
                VideoInfo vid = new VideoInfo();
                vid.VideoUrl = BASE_URL + m.Groups[1].Value;
                vid.ImageUrl = m.Groups[2].Value;
                vid.Title = cleanString(m.Groups[4].Value);
                vid.Airdate = cleanString(m.Groups[5].Value);
                vids.Add(vid);
            }

            return vids;
        }

        List<string> nextPageUrls = new List<string>();
        List<VideoInfo> getVideoListInternal(string url)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            string html = GetWebData(url);
            Regex reg = new Regex(@"<li class=""clearfix"">\s*<div[^>]*>\s*<a[^>]*>\s*<span[^>]*>\s*<span[^>]*>[^>]*>\s*<img.*? src=""([^""]*)""[^>]*>\s*<span[^>]*>\s*<span[^>]*>Full Episode</span>(.|\s)*?<li class=""date"">\d+[:]\d+ ([^<]*)</li>(.|\s)*?<h3><a href=""([^""]*)"">([^<]*)</a></h3>\s*<p class=""description"">([^<]*)</p>");
            foreach (Match m in reg.Matches(html))
            {
                VideoInfo vid = new VideoInfo();
                vid.ImageUrl = m.Groups[1].Value;
                vid.Airdate = m.Groups[3].Value;
                vid.VideoUrl = BASE_URL + m.Groups[5].Value;
                vid.Title = cleanString(m.Groups[6].Value);
                vid.Description = cleanString(m.Groups[7].Value);
                vids.Add(vid);
            }

            if (!url.EndsWith("&subepisodes=yes"))
            {
                reg = new Regex(@"<h2><span class=""sifr_white""><a href="".*?[?]([^""]*)"">[^<]*</a>.*?<img.*? src="".*?[?](\d+)"".*?</h2>\s*</div><!-- /.group_heading -->\s*</div><!-- /.group_container -->");
                foreach (Match m in reg.Matches(html))
                    vids.AddRange(getVideoListInternal(string.Format("{0}/previous_episodes?_={1}&{2}&subepisodes=yes", url, m.Groups[2], m.Groups[1])));
            }
            return vids;
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            List<ISearchResultItem> results = new List<ISearchResultItem>();
            string html = GetWebData("http://www.channel5.com/quick_search?q=" + query);
            Regex reg = new Regex(@"""path"":""([^""]*)"",""thumbnail_url"":""([^""]*)"",""title"":""([^""]*)""");
            foreach (Match m in reg.Matches(html))
            {
                RssLink rss = new RssLink();
                rss.Url = BASE_URL + m.Groups[1].Value;
                List<VideoInfo> vids = GetVideos(rss);
                if (vids.Count < 1)
                    continue;
                rss.Thumb = BASE_URL + m.Groups[2].Value;
                rss.Name = cleanString(m.Groups[3].Value);
                rss.EstimatedVideoCount = (uint)vids.Count;
                rss.Other = vids;
                results.Add(rss);
            }

            return results;
        }

        string cleanString(string s)
        {
            return s.Replace("&amp;", "&").Replace("&pound;", "£").Trim();
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string webdata = GetWebData(video.VideoUrl);
            //Match m = new Regex(@"playerID=(\d+).*?videoPlayer=ref:(C\d+)").Match(webdata);
            EpisodeInfo info = new EpisodeInfo();
            Match m;
            if ((m = Regex.Match(webdata, @"<div class=""secondary_nav_header"".*?>.*?<h\d><span.*?>(.*?)<", RegexOptions.Singleline)).Success)
                info.SeriesTitle = m.Groups[1].Value;
            if ((m = Regex.Match(webdata, @"<h3 class=""episode_header""><span.*?>(.*?)<")).Success)
            {
                string result = m.Groups[1].Value;
                if ((m = Regex.Match(result, @"Series (\d+)")).Success)
                    info.SeriesNumber = m.Groups[1].Value;
                if ((m = Regex.Match(result, @"Episode (\d+)")).Success)
                    info.EpisodeNumber = m.Groups[1].Value;
            }
            if ((m = Regex.Match(webdata, @"<p>First broadcast at (.*?)</p>")).Success)
            {
                DateTime airDate;
                if (DateTime.TryParse(m.Groups[1].Value, out airDate))
                {
                    info.AirDate = airDate.ToString("d MMMM");
                    if (string.IsNullOrEmpty(info.SeriesNumber))
                        info.SeriesNumber = airDate.Year.ToString();
                }
            }

            video.Other = info;

            m = new Regex(@"videoPlayer=ref:(C\d+)").Match(webdata);
            if (!m.Success)
                return String.Empty;

            AMFObject viewerExperience = getViewerExperience(m.Groups[1].Value, video.VideoUrl);
            AMFObject mediaDTO = viewerExperience.GetArray("programmedContent").GetObject("videoPlayer").GetObject("mediaDTO");
            if (!string.IsNullOrEmpty(mediaDTO.GetStringProperty("drmMetadataURL")))
            {
                video.PlaybackOptions = YouTubeShowHandler.GetYouTubePlaybackOptions(video.Other as EpisodeInfo);
                if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
                    return video.PlaybackOptions.Last().Value;
                return null;
            }

            AMFArray renditions = mediaDTO.GetArray("renditions");
            return FillPlaybackOptions(video, renditions);
        }

        AMFObject getViewerExperience(string contentRefId, string videoUrl)
        {
            AMFObject contentOverride = new AMFObject("com.brightcove.experience.ContentOverride");
            contentOverride.Add("contentId", double.NaN);
            contentOverride.Add("target", "videoPlayer");
            contentOverride.Add("contentRefId", contentRefId);
            contentOverride.Add("featuredRefId", null);
            contentOverride.Add("contentRefIds", null);
            contentOverride.Add("featuredId", double.NaN);
            contentOverride.Add("contentIds", null);
            contentOverride.Add("contentType", 0);
            AMFArray array = new AMFArray();
            array.Add(contentOverride);

            AMFObject ViewerExperienceRequest = new AMFObject("com.brightcove.experience.ViewerExperienceRequest");
            ViewerExperienceRequest.Add("TTLToken", String.Empty);
            ViewerExperienceRequest.Add("playerKey", String.Empty);
            ViewerExperienceRequest.Add("deliveryType", double.NaN);
            ViewerExperienceRequest.Add("contentOverrides", array);
            ViewerExperienceRequest.Add("URL", videoUrl);

            ViewerExperienceRequest.Add("experienceId", playerId);

            AMFSerializer ser = new AMFSerializer();
            byte[] data = ser.Serialize(ViewerExperienceRequest, hashValue);

            string requestUrl = "http://c.brightcove.com/services/messagebroker/amf?playerid=" + playerId;

            return GetResponse(requestUrl, data);
        }

        private string FillPlaybackOptions(VideoInfo video, AMFArray renditions)
        {
            SortedList<string, string> options = new SortedList<string, string>(new StreamComparer());

            for (int i = 0; i < renditions.Count; i++)
            {
                AMFObject rendition = renditions.GetObject(i);
                int encodingRate = rendition.GetIntProperty("encodingRate");
                string nm = String.Format("{0}x{1} | {2} kbps",
                    rendition.GetIntProperty("frameWidth"), rendition.GetIntProperty("frameHeight"),
                    encodingRate / 1000);
                string url = HttpUtility.UrlDecode(rendition.GetStringProperty("defaultURL"));
                if (url.StartsWith("rtmp"))
                {
                    string auth = String.Empty;
                    if (url.Contains('?'))
                        auth = '?' + url.Split('?')[1];
                    string[] parts = url.Split('&');

                    string rtmp = parts[0] + auth;
                    string playpath = parts[1].Split('?')[0] + auth;

                    url = new MPUrlSourceFilter.RtmpUrl(rtmp) 
                    { 
                        PlayPath = playpath,
                        SwfUrl = "http://admin.brightcove.com/viewer/us20111207.0737/connection/ExternalConnection_2.swf",
                        SwfVerify = true
                    }.ToString();
                }
                if (!options.ContainsKey(nm))
                    options.Add(nm, url);
            }

            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> key in options)
                video.PlaybackOptions.Add(key.Key, key.Value);

            return StreamComparer.GetBestPlaybackUrl(video.PlaybackOptions, StreamQualityPref, AutoSelectStream);
        }

        private AMFObject GetResponse(string url, byte[] postData)
        {
            //Log.Debug("get webdata from {0}", url);

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null) return null;
            request.Method = "POST";
            request.ContentType = "application/x-amf";
            request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
            request.Referer = "http://admin.brightcove.com/viewer/us20111122.1604/federatedVideoUI/BrightcovePlayer.swf";
            request.Timeout = 15000;
            request.ContentLength = postData.Length;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

            System.Net.WebProxy proxy = getProxy();
            if (proxy != null)
                request.Proxy = proxy;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(postData, 0, postData.Length);
            requestStream.Close();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();

                AMFDeserializer des = new AMFDeserializer(responseStream);
                AMFObject obj = des.Deserialize();
                return obj;
            }

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

    }    
}
