using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Web;

namespace OnlineVideos.Sites
{
    public class ABCiViewUtil : SiteUtilBase
    {
        public override int DiscoverDynamicCategories()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(@"http://www.abc.net.au/iview/xml/categories.xml");
            foreach (XmlNode node in doc.SelectNodes(@"//categories/category"))
            {
                RssLink cat = new RssLink();
                cat.Name = node.SelectSingleNode("name").InnerText;
                cat.HasSubCategories = true;
                AddSubcats(cat, node);
                Settings.Categories.Add(cat);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private void addNode(RssLink parentCat, XmlNode node, string forceName)
        {
            RssLink cat = new RssLink();
            if (forceName != null)
                cat.Name = forceName;
            else
                cat.Name = node.SelectSingleNode("name").InnerText;
            cat.ParentCategory = parentCat;
            cat.Url = @"http://tviview.abc.net.au/iview/api2/?keyword=" + node.Attributes["id"].InnerText;
            cat.HasSubCategories = true;
            cat.ParentCategory = parentCat;
            parentCat.SubCategories.Add(cat);
        }

        private void AddSubcats(RssLink parentCat, XmlNode parentNode)
        {
            parentCat.SubCategories = new List<Category>();

            addNode(parentCat, parentNode, "All");
            foreach (XmlNode node in parentNode.SelectNodes(@"category"))
                addNode(parentCat, node, null);

            parentCat.SubCategoriesDiscovered = true;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string webData = "{items:" + GetWebData(((RssLink)parentCategory).Url) + '}';
            JObject contentData = (JObject)JObject.Parse(webData);
            parentCategory.SubCategories = new List<Category>();
            if (contentData != null)
            {
                JArray items = contentData["items"] as JArray;
                if (items != null)
                {
                    foreach (JToken item in items)
                    {
                        RssLink subcat = new RssLink();
                        subcat.Name = item.Value<string>("b");
                        subcat.Description = item.Value<string>("c");
                        subcat.Thumb = item.Value<string>("d");
                        subcat.Other = item["f"];
                        subcat.ParentCategory = parentCategory;
                        parentCategory.SubCategories.Add(subcat);
                    }
                }
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> res = new List<VideoInfo>();
            JArray items = category.Other as JArray;
            if (items != null)
                foreach (JToken vid in items)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = vid.Value<String>("b");
                    video.Description = vid.Value<string>("d");
                    video.VideoUrl = vid.Value<string>("n");
                    video.Length = VideoInfo.GetDuration(vid.Value<String>("j"));
                    video.Airdate = vid.Value<String>("f");
                    video.ImageUrl = vid.Value<String>("s");
                    res.Add(video);

                }
            return res;
        }

        public override string getUrl(VideoInfo video)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(@"http://tviview.abc.net.au/iview/auth/?v2");
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", "http://www.abc.net.au/iView/Services/iViewHandshaker");
            string auth = doc.SelectSingleNode(@"a:iview/a:token", nsmRequest).InnerText;
            string host = doc.SelectSingleNode(@"a:iview/a:host", nsmRequest).InnerText;

            string vidUrl = video.VideoUrl;
            if (vidUrl.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase))
                vidUrl = "mp4:" + vidUrl.Substring(0, vidUrl.Length - 4);
            string url;
            if (host.Equals("Akamai", StringComparison.InvariantCultureIgnoreCase))
                url = String.Format("rtmpurl={0}&tcurl={1}",
                    HttpUtility.UrlEncode(@"rtmp://cp53909.edgefcs.net///flash/playback/_definst_/" + vidUrl),
                    HttpUtility.UrlEncode(@"rtmp://cp53909.edgefcs.net/ondemand?auth=" + auth)
                    );
            else
                url = String.Format("rtmpurl={0}&playpath={1}",
                    HttpUtility.UrlEncode(@"rtmp://203.18.195.10/ondemand?auth=" + auth),
                    HttpUtility.UrlEncode(vidUrl)
                    );

            url = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                string.Format("http://127.0.0.1/stream.flv?{0}&swfVfy={1}", url,
                               HttpUtility.UrlEncode(@"http://www.abc.net.au/iview/images/iview.jpg")));
            return url;
        }
    }
}
