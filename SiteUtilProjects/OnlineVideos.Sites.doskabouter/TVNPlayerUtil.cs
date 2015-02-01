using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class TVNPlayerUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Videos per Page"), Description("Defines the default number of videos to display per page.")]
        int pageSize = 25;

        private static string contentHost = "http://tvnplayer.pl";
        private static string mediaHost = "http://redir.atmcdn.pl";
        private static string authKey = "ba786b315508f0920eca1c34d65534cd";
        private static string startUrl = "/api/?platform=ConnectedTV&terminal=Samsung&format=xml&v=2.0&authKey=" + authKey;
        private static string mediaMainUrl = "/scale/o2/tvn/web-content/m/";

        public override int DiscoverDynamicCategories()
        {
            string response = GetWebData(contentHost + startUrl + "&m=mainInfo");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);
            foreach (XmlNode node in doc.SelectNodes("//mainInfo/categories/row"))
            {
                string type = node.SelectSingleNode("type").InnerText;
                string id = node.SelectSingleNode("id").InnerText;
                string name = getXmlValue(node, "name");
                string urlQuery = String.Format("&type={0}&id={1}&limit={2}&page={{0}}&sort=newest&m=getItems",
                    type, id, pageSize);
                RssLink cat = new RssLink()
                {
                    Name = name,
                    Other = node,
                    Thumb = GetIcon(node),
                    Url = contentHost + startUrl + urlQuery,
                    HasSubCategories = (type == "catalog")
                };
                Settings.Categories.Add(cat);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            return ParseSubCategories(parentCategory, 1);
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            return ParseSubCategories(category.ParentCategory, ((TVNNextPageCategory)category).PageNr);
        }

        private int ParseSubCategories(Category parentCategory, int pageNr)
        {
            string url = ((RssLink)parentCategory).Url;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWebData(String.Format(url, pageNr)));
            List<Category> subCategories = new List<Category>();
            foreach (XmlNode node in doc.SelectNodes("//getItems/items/row"))
            {
                string title = getXmlValue(node, "title");

                string urlQuery = String.Format("&type={0}&id={1}&limit={2}&page={{0}}&sort=newest&m=getItems",
                    node.SelectSingleNode("type").InnerText, node.SelectSingleNode("id").InnerText, pageSize);

                RssLink cat = new RssLink()
                {
                    Name = title,
                    Other = node,
                    Thumb = GetIcon(node),
                    Url = contentHost + startUrl + urlQuery,
                    ParentCategory = parentCategory
                };
                subCategories.Add(cat);
            }

            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            parentCategory.SubCategories.AddRange(subCategories);

            string nrItems = getXmlValue(doc, "//getItems/count_items");
            int count;
            if (Int32.TryParse(nrItems, out count) && count > pageSize * pageNr)
            {
                parentCategory.SubCategories.Add(
                    new TVNNextPageCategory()
                    {
                        PageNr = pageNr + 1,
                        Url = url,
                        ParentCategory = parentCategory
                    }
                );
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        #region videos
        int videoPageNr = 0;
        int videoCount = 0;

        public override bool HasNextPage
        {
            get
            {
                return videoCount > pageSize * videoPageNr;
            }
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            videoPageNr = 0;
            return Parse(((RssLink)category).Url, null);
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            nextPageUrl = url;
            url = String.Format(url, ++videoPageNr);
            if (data == null)
                data = GetWebData(url);

            List<VideoInfo> videos = new List<VideoInfo>();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            foreach (XmlNode node in doc.SelectNodes("//getItems/items/row"))
            {
                string title = node.SelectSingleNode("title").InnerText;
                string episode = getXmlValue(node, "episode");

                if (!String.IsNullOrEmpty(episode) && episode != "0") title += ", odcinek " + episode;
                string season = getXmlValue(node, "season");
                if (!String.IsNullOrEmpty(season) && season != "0") title += ", sezon " + season;

                string urlQuery = String.Format("&type=episode&id={0}&limit=10&page=1&sort=newest&m=getItem&deviceScreenHeight=1080&deviceScreenWidth=1920",
                    node.SelectSingleNode("id").InnerText);
                VideoInfo video = new TVNVideoInfo()
                {
                    Title = title,
                    Description = getXmlValue(node, "lead"),
                    ImageUrl = GetIcon(node),
                    VideoUrl = contentHost + startUrl + urlQuery,
                    Airdate = getXmlValue(node, "start_date")
                };
                videos.Add(video);
            }
            string nrItems = getXmlValue(doc, "//getItems/count_items");
            Int32.TryParse(nrItems, out videoCount);
            return videos;
        }

        #endregion

        public override string GetVideoUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(webData);
            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (XmlNode node in doc.SelectNodes("//getItem/item/videos/main/video_content/row"))
            {
                try
                {
                    video.PlaybackOptions.Add(node.SelectSingleNode("profile_name").InnerText, node.SelectSingleNode("url").InnerText);
                }
                catch
                {
                }
            }

            string resultUrl = String.Empty;

            if (video.PlaybackOptions.Count != 0)
            {
                // return first found url as default
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                resultUrl = enumer.Current.Value;
            }
            if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed

            return resultUrl;
        }

        private string getXmlValue(XmlNode node, string subnode)
        {
            XmlNode sub = node.SelectSingleNode(subnode);
            if (sub != null)
                return sub.InnerText;
            return String.Empty;
        }

        private string GetIcon(XmlNode node)
        {
            XmlNodeList thumbs = node.SelectNodes("thumbnail/row");
            if (thumbs.Count > 0)
            {
                XmlNode thumb = thumbs[0].SelectSingleNode("url");
                if (thumb != null && !String.IsNullOrEmpty(thumb.InnerText))
                    return mediaHost + mediaMainUrl + thumb.InnerText + "?quality=70&dstw=290&dsth=187&type=1";
            }
            return String.Empty;
        }

        public class TVNNextPageCategory : NextPageCategory
        {
            public int PageNr;
        }

        public class TVNVideoInfo : VideoInfo
        {
            public override string GetPlaybackOptionUrl(string url)
            {
                return GetWebData(PlaybackOptions[url]);
            }
        }


    }
}
