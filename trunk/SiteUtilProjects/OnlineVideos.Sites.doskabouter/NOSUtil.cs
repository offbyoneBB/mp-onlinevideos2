using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class NOSUtil : GenericSiteUtil
    {
        private enum Specials { Live, LaatsteJournaal };

        private string currentPageTitle = null;

        public override int DiscoverDynamicCategories()
        {
            RssLink cat = new RssLink();
            cat.Name = "Nos";
            cat.Url = @"http://nos.nl/video-en-audio/";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Nieuws";
            cat.Url = @"http://nos.nl/nieuws/video-en-audio/";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Sport";
            cat.Url = @"http://nos.nl/sport/video-en-audio/";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Live";
            cat.Other = Specials.Live;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Laatste journaals";
            cat.Url = baseUrl + "/nieuws";
            cat.Other = Specials.LaatsteJournaal;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string webData = GetWebData(((RssLink)parentCategory).Url, null, null, null, true);
            webData = GetSubString(webData, @"class=""active""", @"</ul>");
            webData = webData.Replace("><", String.Empty);
            return ParseSubCategories(parentCategory, webData);
        }

        public override String getUrl(VideoInfo video)
        {
            if (Specials.Live.Equals(video.Other))
                return ParseASX(video.VideoUrl)[0];

            string webData = GetWebData(video.VideoUrl);

            Match m = regEx_PlaylistUrl.Match(webData);
            if (!m.Success) return String.Empty;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWebData(m.Groups["url"].Value));
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", "http://xspf.org/ns/0/");

            XmlNode node = doc.SelectSingleNode(@"//a:location", nsmRequest);
            if (node != null)
                return node.InnerText;
            return String.Empty;
        }

        public override string getCurrentVideosTitle()
        {
            return currentPageTitle;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentPageTitle = null;
            if (category.Other != null)
                switch ((Specials)category.Other)
                {
                    case Specials.Live: return getLive();
                    case Specials.LaatsteJournaal: return base.getVideoList(category);
                }
            string url = ((RssLink)category).Url;

            //url = url.Replace("$DATE", day);

            string webData = GetWebData(url, null, null, null, true);
            string title = GetSubString(webData, @"id=""article"">", "</h1>");
            title = title.Replace("<h1>", String.Empty);
            title = title.Replace("<span>", String.Empty);
            title = title.Replace("</span>", String.Empty).Trim();
            if (!String.IsNullOrEmpty(title))
                currentPageTitle = title;

            webData = GetSubString(webData, @"img-list", @"class=""content-menu");
            return base.Parse(url, webData);
        }

        private List<VideoInfo> getLive()
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            VideoInfo video = new VideoInfo();
            video.Title = "Journaal24";
            video.VideoUrl = @"http://livestreams.omroep.nl/nos/journaal24-bb";
            video.Other = Specials.Live;
            videos.Add(video);

            video = new VideoInfo();
            video.Title = "Politiek24";
            video.Other = Specials.Live;
            video.VideoUrl = @"http://livestreams.omroep.nl/nos/politiek24-bb";
            videos.Add(video);
            return videos;
        }

        private string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

    }
}
