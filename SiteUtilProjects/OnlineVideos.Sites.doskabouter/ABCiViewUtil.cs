using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Newtonsoft.Json.Linq;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class ABCiViewUtil : SiteUtilBase
    {
        public override int DiscoverDynamicCategories()
        {
            List<Category> dynamicCategories = new List<Category>();
            XmlDocument doc = new XmlDocument();
            doc.Load(@"http://www.abc.net.au/iview/xml/categories.xml");
            foreach (XmlNode node in doc.SelectNodes(@"//categories/category"))
            {
                RssLink cat = new RssLink();
                cat.Name = node.SelectSingleNode("name").InnerText;
                cat.HasSubCategories = true;
                AddSubcats(cat, node);
                dynamicCategories.Add(cat);
            }
            dynamicCategories.Sort();
            foreach (Category cat in dynamicCategories) Settings.Categories.Add(cat);
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
            parentCategory.SubCategories.Sort();

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
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
                    if (String.IsNullOrEmpty(video.VideoUrl))
                        video.VideoUrl = vid.Value<string>("r");
                    else
                        video.SubtitleUrl = String.Format(@"http://www.abc.net.au/iview/captions/{0}.xml", Path.GetFileNameWithoutExtension(video.VideoUrl));

                    video.Length = VideoInfo.GetDuration(vid.Value<String>("j"));
                    video.Airdate = vid.Value<String>("f");
                    video.Thumb = vid.Value<String>("s");
                    res.Add(video);

                }
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            XmlDocument doc = new XmlDocument();
            string xmlData = GetWebData(@"http://iview.abc.net.au/auth/flash/?", userAgent: "FireFox");
            doc.LoadXml(xmlData);
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", "http://www.abc.net.au/iView/Services/iViewHandshaker");
            string auth = doc.SelectSingleNode(@"a:iview/a:token", nsmRequest).InnerText;

            if (video.VideoUrl.StartsWith("rtmp://"))
            {
                string[] parts = video.VideoUrl.Split('/');
                string fileName = parts[parts.Length - 1];
                return new RtmpUrl(video.VideoUrl)
                {
                    SwfVerify = true,
                    SwfUrl = @"http://www.abc.net.au/iview/images/iview.jpg",
                    PlayPath = fileName,
                    App = "live/" + fileName + "?auth=" + auth,
                    Live = true
                }.ToString();
            }
            string host = doc.SelectSingleNode(@"a:iview/a:host", nsmRequest).InnerText;

            RtmpUrl rtmpUrl;
            //if (host.Equals("Akamai", StringComparison.InvariantCultureIgnoreCase))
            {
                rtmpUrl = new RtmpUrl(@"rtmp://cp53909.edgefcs.net////flash/playback/_definst_/" + video.VideoUrl)
                {
                    TcUrl = @"rtmp://cp53909.edgefcs.net/ondemand?auth=" + auth
                };
            }
            /*else
            {
                string authUrl = doc.SelectSingleNode(@"a:iview/a:server", nsmRequest).InnerText +
                    "?auth=" + auth;
                string vidUrl = video.VideoUrl;
                if (vidUrl.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase))
                    vidUrl = "mp4:" + vidUrl.Substring(0, vidUrl.Length - 4);
                rtmpUrl = new RtmpUrl(authUrl) { PlayPath = vidUrl };
            }*/

            rtmpUrl.SwfVerify = true;
            rtmpUrl.SwfUrl = @"http://www.abc.net.au/iview/images/iview.jpg";
            return rtmpUrl.ToString();
        }
    }
}
