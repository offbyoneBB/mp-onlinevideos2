using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json.Linq;
using OnlineVideos.Hoster.Base;

namespace OnlineVideos.Sites
{
    public class CollegeHumorUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories != null)
                foreach (Category cat in Settings.Categories)
                    cat.HasSubCategories = true;
            return base.DiscoverDynamicCategories();
        }

        public override int ParseSubCategories(Category parentCategory, string data)
        {
            if (true.Equals(parentCategory.Other))
            {
                Regex tmp = regEx_dynamicSubCategories;
                regEx_dynamicSubCategories = new Regex(@"<div\sclass=""grid3\ssketch-group"">\s*<a\shref=""(?<url>[^""]*)""\stitle=""[^""]*""\sclass=""thumb"">\s*<img\ssrc=""(?<thumb>[^""]*)""\swidth=""175""\sheight=""98""\salt=""[^""]*"">\s*<strong>(?<title>[^<]*)</strong>\s*</a>", defaultRegexOptions);
                regEx_dynamicSubCategoriesNextPage = regEx_NextPage;
                int result = base.ParseSubCategories(parentCategory, data);
                regEx_dynamicSubCategories = tmp;
                regEx_dynamicSubCategoriesNextPage = null;
                return result;
            }

            int res = base.ParseSubCategories(parentCategory, data);
            if (parentCategory.Name == "Sketch Comedy")
                foreach (Category subcat in parentCategory.SubCategories)
                {
                    subcat.HasSubCategories = true;
                    subcat.Other = true;
                }
            return res;
        }

        public override string getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            JToken jt = JObject.Parse(data) as JToken;
            JToken vids = jt["video"]["mp4"];
            if (vids != null)
            {
                video.PlaybackOptions = new System.Collections.Generic.Dictionary<string, string>();
                video.PlaybackOptions.Add("low", vids.Value<string>("low_quality"));
                video.PlaybackOptions.Add("high", vids.Value<string>("high_quality"));
                return video.PlaybackOptions.Last().Value;
            }
            //probably youtube
            JToken vid = jt["video"]["youtubeId"];
            //http://youtube.com/
            foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                if (hosterUtil.getHosterUrl().ToLower().Equals("youtube.com"))
                {
                    video.PlaybackOptions = hosterUtil.getPlaybackOptions(@"http://youtube.com/" + vid.Value<string>());
                }
            return video.PlaybackOptions.Last().Value;
            //Hoster.Hos

            string res = base.getUrl(video);// for embedded youtube
            if (String.IsNullOrEmpty(res))
            {
                string webData = GetWebData(video.VideoUrl);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webData);
                XmlNode node = doc.SelectSingleNode("//videoplayer/video/file");
                if (node != null) return node.InnerText + "?hdcore=2.6.8";
            }
            return res;
        }
    }
}
