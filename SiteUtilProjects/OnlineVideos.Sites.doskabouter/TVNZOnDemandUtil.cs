using System;
using System.Collections.Generic;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites
{
    public class TVNZOnDemandUtil : SiteUtilBase
    {
        private string baseUrl = @"http://tvnz.co.nz";

        public override int DiscoverDynamicCategories()
        {
            string webData = GetWebData(@"http://tvnz.co.nz/content/ps3_navigation/ps3_xml_skin.xml");
            if (!string.IsNullOrEmpty(webData))
            {
                List<Category> dynamicCategories = new List<Category>(); // put all new discovered Categories in a separate list
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webData);
                XmlNodeList cats = doc.SelectNodes(@"Menu/MenuItem[@type=""shows"" or @type=""alphabetical""]");
                foreach (XmlNode node in cats)
                {
                    RssLink cat = new RssLink();
                    cat.Url = baseUrl + node.Attributes["href"].Value;
                    cat.Name = node.Attributes["title"].Value;
                    cat.HasSubCategories = node.Attributes["type"].Value != "shows";
                    dynamicCategories.Add(cat);
                }
                // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
                foreach (Category cat in dynamicCategories) Settings.Categories.Add(cat);
                Settings.DynamicCategoriesDiscovered = true;
                return dynamicCategories.Count; // return the number of discovered categories
            }
            return 0;
        }

        private void AddSubcats(XmlNode letter, RssLink cat)
        {
            cat.HasSubCategories = true;
            cat.SubCategories = new List<Category>();
            XmlNodeList shows = letter.SelectNodes("Show");
            foreach (XmlNode node in shows)
            {
                RssLink show = new RssLink();
                show.Name = node.Attributes["title"].Value;
                show.Description = "channel: " + node.Attributes["channel"].Value;
                if (node.Attributes["episodes"] != null)
                    show.Description += ", episodes: " + node.Attributes["episodes"].Value;
                if (node.Attributes["videos"] != null)
                    show.Description += ", videos: " + node.Attributes["videos"].Value;
                show.Url = baseUrl + node.Attributes["href"].Value;
                show.ParentCategory = cat;
                cat.SubCategories.Add(show);
            }
            cat.SubCategoriesDiscovered = true;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            // only for alphabetical
            string webData = GetWebData((parentCategory as RssLink).Url);
            if (!string.IsNullOrEmpty(webData))
            {
                parentCategory.SubCategories = new List<Category>();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webData);

                XmlNodeList letters = doc.SelectNodes(@"Group/Letter");
                foreach (XmlNode letter in letters)
                {
                    RssLink subcat = new RssLink();
                    subcat.Name = letter.Attributes["label"].Value;
                    parentCategory.SubCategories.Add(subcat);
                    AddSubcats(letter, subcat);
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string webData = GetWebData((category as RssLink).Url, null, true);
            List<VideoInfo> res = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webData);

                XmlNodeList episodes = doc.SelectNodes(@"Group/Episode|Group/Extra");
                foreach (XmlNode episode in episodes)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = episode.Attributes["title"].Value;
                    string subTitle = episode.Attributes["sub-title"].Value;
                    if (!String.IsNullOrEmpty(subTitle))
                        video.Title += ": " + subTitle;
                    video.Description = episode.InnerText;
                    video.VideoUrl = baseUrl + episode.Attributes["href"].Value.Split('?')[0];
                    video.ThumbnailImage = episode.Attributes["src"].Value;
                    string[] epinfo = episode.Attributes["episode"].Value.Split('|');
                    if (epinfo.Length == 1)
                        video.Length = epinfo[0].Trim();
                    else
                    {
                        if (epinfo.Length > 0)
                            video.Description = epinfo[0].Trim() + " " + video.Description;
                        if (epinfo.Length > 2)
                            video.Length = epinfo[2].Trim();
                        if (epinfo.Length > 1)
                            video.Length = video.Length + '|' + Translation.Airdate + ": " + epinfo[1].Trim();
                    }
                    res.Add(video);
                }
            }
            return res;
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video)
        {
            List<string> res = new List<string>();
            XmlDocument doc = new XmlDocument();
            string webData = GetWebData(video.VideoUrl);
            if (String.IsNullOrEmpty(webData)) return res;
            doc.LoadXml(webData);
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", @"http://www.w3.org/ns/SMIL");
            XmlNodeList nodes = doc.SelectNodes("//a:smil/a:body/a:seq", nsmRequest);

            foreach (XmlNode node in nodes)
            {
                XmlNode vid = node.SelectSingleNode("a:video", nsmRequest);
                if (vid == null)
                {
                    int largestBitrate = 0;
                    foreach (XmlNode sub in node.SelectNodes("a:par/a:video", nsmRequest))
                    {
                        int bitRate = int.Parse(sub.Attributes["systemBitrate"].InnerText);
                        if (bitRate > largestBitrate)
                        {
                            largestBitrate = bitRate;
                            vid = sub;
                        }
                    }
                }
                string url = vid.Attributes["src"].InnerText;
                if (url.StartsWith("rtmp"))
                    //rtmpe://fms-streaming.tvnz.co.nz/tvnz.co.nz/2010/thebigbandtheory_s3_e19_part1_700k
                    url = url.Replace("rtmp:", "rtmpe://fms-streaming.tvnz.co.nz/tvnz.co.nz/");
                res.Add(url);
            }
            return res;
        }

    }
}
