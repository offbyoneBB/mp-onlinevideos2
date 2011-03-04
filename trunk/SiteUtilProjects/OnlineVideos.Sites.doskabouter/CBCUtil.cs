using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml;
using System.Web;
using System.IO;

namespace OnlineVideos.Sites
{
    public class CBCUtil : SiteUtilBase
    {
        private string feedPID;
        private string platformRoot = @"http://cbc.feeds.theplatform.com/ps/JSON/PortalService/2.2/";
        private string baseUrl = @"http://www.cbc.ca/video";

        public override int DiscoverDynamicCategories()
        {
            string webData = GetWebData(baseUrl);
            Match m = Regex.Match(webData, @"var\sfeedPID\s=\s""(?<feedPID>[^""]*)""");
            if (m.Success)
                feedPID = m.Groups["feedPID"].Value;

            Search("being erica");
            foreach (Category cat in getChildren("0"))
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

        private List<VideoInfo> getvideos(JToken contentData)
        {
            List<VideoInfo> res = new List<VideoInfo>();
            if (contentData != null)
            {
                JArray items = contentData["items"] as JArray;
                if (items != null)
                {
                    foreach (JToken item in items)
                    {
                        VideoInfo video = new VideoInfo();
                        video.Title = item.Value<string>("title");
                        video.Description = item.Value<string>("description");
                        video.VideoUrl = item.Value<string>("playerURL");

                        long len = item.Value<long>("length");
                        video.Length = VideoInfo.GetDuration((len / 1000).ToString());

                        long air = item.Value<long>("airdate");
                        string Airdate = new DateTime((air * 10000) + 621355968000000000, DateTimeKind.Utc).ToString();
                        if (!String.IsNullOrEmpty(Airdate))
                            video.Length = video.Length + '|' + Translation.Airdate + ": " + Airdate;

                        JArray assets = item["assets"] as JArray;
                        if (assets != null && assets.First != null)
                            video.ImageUrl = assets.First.Value<string>("URL");
                        res.Add(video);
                    }
                }
            }

            return res;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = platformRoot + @"/getReleaseList?PID=" + feedPID + "&query=CategoryIDs|" +
                (string)category.Other + "&endIndex=500";
            JObject contentData = GetWebData<JObject>(url);
            return getvideos(contentData);
        }


        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            string url = @"http://www.cbc.ca/search/cbc?json=true&sitesearch=www.cbc.ca/video/watch&q=" + HttpUtility.UrlEncode(query);
            JObject contentData = GetWebData<JObject>(url);
            JToken searchRes = contentData["searchResults"];

            return getvideos(searchRes);
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
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWebData(video.VideoUrl));

            string rtmpUrl = doc.SelectSingleNode("//playlist/choice/url").InnerText;
            Uri uri = new Uri(rtmpUrl);
            NameValueCollection nn = HttpUtility.ParseQueryString(uri.Query);
            string auth = nn["auth"];
            string[] pathParts = rtmpUrl.Split(new string[] { "<break>" }, StringSplitOptions.None);

            string url = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&playpath={1}&auth={2}",
                HttpUtility.UrlEncode(pathParts[0]),
                HttpUtility.UrlEncode(pathParts[1].Replace(".flv", String.Empty)),
                //HttpUtility.UrlEncode(@"http://www.cbc.ca/video/swf/UberPlayer.swf"),
                auth));
            return url;
        }
    }
}
