using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class IplaUtil : GenericSiteUtil
    {
        private XmlDocument doc = null;
        private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private Dictionary<String, String> qualities;
        private Dictionary<String, String> formats;

        public override int DiscoverDynamicCategories()
        {
            qualities = new Dictionary<string, string>();
            qualities.Add("0", "Low");
            qualities.Add("1", "Medium");
            qualities.Add("2", "SD");
            qualities.Add("3", "HD");

            formats = new Dictionary<string, string>();
            formats.Add("0", "WMV");
            formats.Add("3", "FLV");
            String webData = GetWebData(baseUrl, forceUTF8: true);
            doc = new XmlDocument();
            doc.LoadXml(webData);

            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();

            foreach (Category cat in AddCats("0", doc, null))
            {
                Settings.Categories.Add(cat);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = AddCats((string)parentCategory.Other, doc, parentCategory);
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            String webData = GetWebData(((RssLink)category).Url, forceUTF8: true);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(webData);

            List<VideoInfo> result = new List<VideoInfo>();

            foreach (XmlNode vidNode in doc.SelectNodes("//resp/VoDs/vod"))
            {
                VideoInfo video = new VideoInfo();
                video.Title = vidNode.Attributes["title"].Value;
                video.Airdate = epoch.AddSeconds(float.Parse(vidNode.Attributes["timestamp"].Value)).ToString();
                video.Length = VideoInfo.GetDuration(vidNode.Attributes["dur"].Value);
                video.ImageUrl = getThumb(vidNode);

                video.PlaybackOptions = new Dictionary<string, string>();
                foreach (XmlNode urlNode in vidNode.SelectNodes("srcreq[@drmtype=\"0\" and @format!=\"2\"]"))
                {
                    string url = urlNode.Attributes["url"].Value;
                    string quality = urlNode.Attributes["quality"].Value;
                    string key = qualities.ContainsKey(quality) ? qualities[quality] : "quality " + quality;
                    string format = urlNode.Attributes["format"].Value;

                    if (formats.ContainsKey(format))
                        key = key + " " + formats[format];
                    else
                        key = key + " format: " + format;

                    if (!video.PlaybackOptions.ContainsKey(key))
                    {
                        HttpUrl httpUtl = new HttpUrl(url) { UserAgent = "this is no useragent" };
                        video.PlaybackOptions.Add(key, httpUtl.ToString());
                    }
                }
                if (video.PlaybackOptions.Count > 0)
                    result.Add(video);
            }

            return result;
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null)
                return String.Empty;
            string url = video.PlaybackOptions.Last().Value;
            if (video.PlaybackOptions.Count == 1)
                video.PlaybackOptions = null;
            return url;
        }

        private List<Category> AddCats(string pid, XmlDocument doc, Category parentCat)
        {
            SortedList<int, XmlNode> catNodes = new SortedList<int, XmlNode>();

            foreach (XmlNode catNode in doc.SelectNodes("//resp/cat[@pid=\"" + pid + "\" and @id!=\"5000296\"]"))
                catNodes.Add(Int32.Parse(catNode.Attributes["seq"].Value), catNode);

            List<Category> result = new List<Category>();
            foreach (XmlNode catNode in catNodes.Values)
            {
                string id = catNode.Attributes["id"].Value;
                RssLink cat = new RssLink()
                {
                    ParentCategory = parentCat,
                    Name = catNode.Attributes["title"].Value,
                    Description = catNode.Attributes["descr"].Value,
                    Other = id,
                    HasSubCategories = doc.SelectSingleNode("//resp/cat[@pid=\"" + id + "\"]") != null
                };
                cat.Thumb = getThumb(catNode);

                if (!cat.HasSubCategories)
                {
                    cat.Url = FormatDecodeAbsolutifyUrl(baseUrl, catNode.Attributes["id"].Value, dynamicSubCategoryUrlFormatString, dynamicSubCategoryUrlDecoding);
                }

                result.Add(cat);
            }
            return result;
        }


        private string getThumb(XmlNode node)
        {
            if (node.Attributes["thumbnail_big"] != null)
                return node.Attributes["thumbnail_big"].Value;
            else
                if (node.Attributes["thumbnail"] != null)
                    return node.Attributes["thumbnail"].Value;
            return String.Empty;
        }

    }
}
