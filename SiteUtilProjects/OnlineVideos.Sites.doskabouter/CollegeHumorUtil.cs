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

            return base.ParseSubCategories(parentCategory, data);
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
                string h = vids.Value<string>("high_quality");
                if (!String.IsNullOrEmpty(h))
                    video.PlaybackOptions.Add("high", h);
                return video.PlaybackOptions.Last().Value;
            }
            //probably youtube
            JToken vid = jt["video"]["youtubeId"];
            foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                if (hosterUtil.getHosterUrl().ToLower().Equals("youtube.com"))
                {
                    video.PlaybackOptions = hosterUtil.getPlaybackOptions(@"http://youtube.com/" + vid.Value<string>());
                }
            return video.PlaybackOptions.Last().Value;
        }
    }
}
