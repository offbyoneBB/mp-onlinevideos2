using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class CBCUtil : GenericSiteUtil
    {
        private string feedPID;
        private static string platformRoot = @"http://cbc.feeds.theplatform.com/ps/JSON/PortalService/2.2/";
        private static string feedPIDUrl = @"http://www.cbc.ca/video/js/SWFVideoPlayer.js";
        private static string videoListUrl = platformRoot + @"/getReleaseList?PID={0}&query=CategoryIDs|{1}&sortDescending=true&endIndex=500";

        private static Regex feedPIDRegex = new Regex(@"{PID:\s""(?<feedPID>[^""]*)"",",
                                                      RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {
            string webData = GetWebData(feedPIDUrl);
            Match m = feedPIDRegex.Match(webData);
            if (m.Success)
                feedPID = m.Groups["feedPID"].Value;
            else
                Log.Error("Feed PID not found for cbc.ca");

            List<Category> lst = new List<Category>();
            foreach (Category cat in getChildren("1221254309"))
                lst.Add(cat);
            lst.Sort();
            foreach (Category cat in lst)
                Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            foreach (Category cat in getChildren((string)parentCategory.Other))
            {
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);
            }

            Category vidCat = new Category();
            vidCat.Name = "videos";
            vidCat.Other = parentCategory.Other;
            vidCat.ParentCategory = parentCategory;
            parentCategory.SubCategories.Add(vidCat);

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private List<VideoInfo> getVideosForUrl(string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            JObject contentData = GetWebData<JObject>(url);
            
            // keep track of contentIDs, since some videos have multiple bitrates
            Dictionary<string, VideoInfo> videoDictionary = new Dictionary<string, VideoInfo>();
            if (contentData != null)
            {
                JArray items = contentData["items"] as JArray;
                if (items != null)
                {
                    foreach (JToken item in items)
                    {
                        string contentID = item.Value<string>("contentID");
                        
                        VideoInfo video = CreateVideoInfo();
                        video.Description = item.Value<string>("description");
                        video.VideoUrl = item.Value<string>("playerURL");
                        video.Title = item.Value<string>("title");

                        long len = item.Value<long>("length");
                        video.Length = VideoInfo.GetDuration((len / 1000).ToString());

                        long air = item.Value<long>("airdate");
                        string Airdate = new DateTime((air * 10000) + 621355968000000000, DateTimeKind.Utc).ToString();
                        if (!String.IsNullOrEmpty(Airdate))
                        {
                            video.Airdate = Airdate;
                        }
                        JArray assets = item["assets"] as JArray;
                        if (assets != null && assets.First != null)
                            video.ImageUrl = assets.First.Value<string>("URL");

                        if (videoDictionary.ContainsKey(contentID))
                        {
                            // we have already seen this contentID earlier
                            // so we must mark this video as having multiple playback options
                            VideoInfo existingVideo = videoDictionary[contentID];
                            if (existingVideo.Other == null)
                            {
                                existingVideo.Other = url;
                            }
                        }
                        else
                        {
                            videoDictionary.Add(contentID, video);
                            result.Add(video);
                        }
                    }
                }
            }

            return result;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = string.Format(videoListUrl, feedPID, (string) category.Other);
            return getVideosForUrl(url);
        }

        private List<Category> getChildren(string parentID)
        {
            JObject contentData = GetWebData<JObject>(platformRoot + @"getCategoryList?PID=" + feedPID + "&field=ID&field=title&field=parentID&field=description&field=customData&field=hasChildren&field=fullTitle&query=ParentIDs|" + parentID);
            List<Category> dynamicCategories = new List<Category>();
            if (contentData != null)
            {
                JArray items = contentData["items"] as JArray;
                if (items != null)
                {
                    foreach (JToken item in items)
                    {
                        Category cat = new Category();
                        cat.Name = item.Value<string>("title");

                        cat.Other = item.Value<string>("ID");
                        cat.HasSubCategories = item.Value<bool>("hasChildren");
                        dynamicCategories.Add(cat);
                    }
                }
                return dynamicCategories;
            }
            return null;
        }

        public override string getUrl(VideoInfo video)
        {
            string result = string.Empty;

            string url = video.Other as string;
            if (!string.IsNullOrEmpty(url))
            {
                // we have multiple video playback options to consider for this video
                video.PlaybackOptions = new Dictionary<string, string>();
                // keep track of bitrates and URLs
                Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();
                    
                // first re-examine the JSON data to get the contentID for this video
                JObject contentData = GetWebData<JObject>(url);
                if (contentData != null)
                {
                    JArray items = contentData["items"] as JArray;
                    if (items != null)
                    {
                        string contentId = string.Empty;
                        foreach (JToken item in items)
                        {
                            string playerUrl = item.Value<string>("playerURL");
                            if (video.VideoUrl.Equals(playerUrl))
                            {
                                contentId = item.Value<string>("contentID");
                                break;
                            }
                        }
                        
                        // once the contentID is found, look for other videos with same contentID
                        foreach (JToken item in items) {
                            if (contentId.Equals(item.Value<string>("contentID")))
                            {
                                int bitrate = (int) item.Value<long>("bitrate") / 1000;
                                if (!urlsDictionary.ContainsKey(bitrate))
                                {
                                    urlsDictionary.Add(bitrate, item.Value<string>("playerURL"));
                                }
                            }
                        }
                        
                        // sort the URLs ascending by bitrate
                        foreach (var item in urlsDictionary.OrderBy(u => u.Key))
                        {
                            video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                            // return last URL as the default (will be the highest bitrate)
                            result = item.Value;
                        }
                    }
                }
            }
            else
            {
                result = createRtmpUrl(video.VideoUrl);
            }
            return result;
        }
        
        public static string createRtmpUrl(string url)
        {
            string result = url;
            // cannot use GetWebData to figure out VideoUrl as the URL responds with
            // HTTP/1.1 302 Found
            // with a Location header containing the rtmp:// URL
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.AllowAutoRedirect = false;
            Log.Debug(@"Making manual HttpWebRequest for {0}", url);

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                // retrieve the RTMP URL from the Location header
                string rtmpUrlFromHeader = response.GetResponseHeader("Location");
                Log.Debug(@"RTMP URL: {0}", rtmpUrlFromHeader);
                
                // split on <break>
                string[] pathParts = rtmpUrlFromHeader.Split(new string[] { "<break>" }, StringSplitOptions.None);
                string host = pathParts[0];
                string playPath = pathParts[1];

                if (playPath.EndsWith(@".mp4") && !playPath.StartsWith(@"mp4:"))
                {
                    // prepend with mp4:
                    playPath = @"mp4:" + playPath;
                }
                else if (playPath.EndsWith(@".flv"))
                {
                    // strip extension
                    playPath = playPath.Substring(0, playPath.Length - 4);
                }
                Log.Debug(@"Host: {0}, PlayPath: {1}", host, playPath);
                result = new MPUrlSourceFilter.RtmpUrl(host) { PlayPath = playPath }.ToString();
            }
            return result;
        }
        
        public override VideoInfo CreateVideoInfo()
        {
            return new CBCVideoInfo();
        }

        private class CBCVideoInfo : VideoInfo {
            // class created solely for the purpose of overriding GetPlaybackOptionUrl
            public override string GetPlaybackOptionUrl(string option)
            {
                return CBCUtil.createRtmpUrl(PlaybackOptions[option]);
            }
        }
    }    
}
