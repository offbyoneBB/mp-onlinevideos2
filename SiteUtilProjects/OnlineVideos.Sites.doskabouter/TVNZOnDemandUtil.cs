using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class TVNZOnDemandUtil : BrightCoveUtil
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
                Settings.Categories = new BindingList<Category>();
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
                //http://tvnz.co.nz/content/<contentid>/ps3_xml_skin.xml
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
                    subcat.ParentCategory = parentCategory;
                    parentCategory.SubCategories.Add(subcat);
                    AddSubcats(letter, subcat);
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string webData = GetWebData((category as RssLink).Url, forceUTF8: true);
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
                    video.VideoUrl = String.Format(@"http://tvnz.co.nz/content/{0}.xhtml", episode.Attributes["href"].Value);
                    XmlAttribute epNode = episode.Attributes["episode"];
                    if (epNode != null)
                    {
                        string epValue = epNode.Value;
                        Match m = Regex.Match(epValue, @"Series\s(?<series>[^,]*),\sEpisode\s(?<episode>[^\s]*)\s");
                        if (m.Success)
                        {
                            video.Other = String.Format(@"http://tvnz.co.nz/{0}/s{1}-ep{2}-video-{3}",
                                episode.Attributes["title"].Value.ToLowerInvariant().Replace(' ', '-'),
                                m.Groups["series"].Value, m.Groups["episode"].Value,
                                episode.Attributes["href"].Value);
                        }
                    }
                    video.Thumb = episode.Attributes["src"].Value;
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
                            video.Length = video.Length + '|' + Translation.Instance.Airdate + ": " + epinfo[1].Trim();
                    }
                    res.Add(video);
                }
            }
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string url = base.GetVideoUrl(video);
            if (String.IsNullOrEmpty(url) && video.Other != null)
            {
                video.VideoUrl = video.Other as string;
                return base.GetVideoUrl(video);
            }
            return url;
        }

    }
}
