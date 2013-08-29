using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class NOSUtil : GenericSiteUtil
    {
        private enum Specials { Live, LaatsteJournaal };

        private string currentPageTitle = null;

        CookieContainer SiteConsent;

        public override void Initialize(OnlineVideos.SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            SiteConsent = new CookieContainer();
            Cookie cookie = new Cookie("npo_cc", "tmp");
            SiteConsent.Add(new Uri(baseUrl), cookie);
        }


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
            string webData = GetWebData(((RssLink)parentCategory).Url, SiteConsent, null, null, true);
            webData = GetSubString(webData, @"class=""active""", @"</ul>");
            webData = webData.Replace("><", String.Empty);
            return ParseSubCategories(parentCategory, webData);
        }

        public override String getUrl(VideoInfo video)
        {
            if (Specials.Live.Equals(video.Other))
                return ParseASX(video.VideoUrl)[0];
            if (Specials.LaatsteJournaal.Equals(video.Other))
            {
                string webData = GetWebData(video.VideoUrl, SiteConsent);
                JObject obj = JObject.Parse(webData);
                return obj.Value<string>("videofile");
            }
            string url = base.getUrl(video);
            try
            {
                string deJSONified = JsonConvert.DeserializeObject<string>('"' + url + '"');
                if (!string.IsNullOrEmpty(deJSONified)) url = deJSONified;
            }
            catch { }
            return url;
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
                    case Specials.LaatsteJournaal:
                        {
                            List<VideoInfo> res = base.getVideoList(category);
                            foreach (VideoInfo video in res)
                                if (!String.IsNullOrEmpty(video.VideoUrl))
                                {
                                    Match matchVideoUrl = Regex.Match(video.VideoUrl, @"http://nos\.nl/uitzendingen/(?<m0>\d+)-");
                                    if (matchVideoUrl.Success)
                                    {
                                        video.VideoUrl = String.Format(@"http://nos.nl/playlist/uitzending/mp4-web03/{0}.json", matchVideoUrl.Groups["m0"].Value);
                                        video.Other = Specials.LaatsteJournaal;
                                    }
                                }
                            return res;
                        }
                }
            string url = ((RssLink)category).Url;

            //url = url.Replace("$DATE", day);

            string webData = GetWebData(url, SiteConsent, null, null, true);
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

        protected override CookieContainer GetCookie()
        {
            return SiteConsent;
        }
    }
}
