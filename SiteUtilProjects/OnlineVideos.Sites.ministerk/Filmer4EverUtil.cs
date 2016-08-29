using OnlineVideos.Hoster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class Filmer4EverUtil : LatestVideosSiteUtilBase
    {

        public class ExtendedVideoInfo : VideoInfo
        {
            public string LatestOption { get; private set; }
            public ITrackingInfo TrackingInfo { get; set; }
            //private SubtitleHandler _sh = null;
            //public SubtitleHandler SubHandler { get { return _sh; } set { _sh = value; } }
            public override string GetPlaybackOptionUrl(string option)
            {
                string u = this.PlaybackOptions[option];
                this.LatestOption = option;
                Hoster.HosterBase hoster = Hoster.HosterFactory.GetAllHosters().FirstOrDefault(h => u.ToLowerInvariant().Contains(h.GetHosterUrl().ToLowerInvariant()));
                if (hoster != null)
                {
                    string url = hoster.GetVideoUrl(u);
                    if (hoster is ISubtitle)
                        this.SubtitleText = (hoster as ISubtitle).SubtitleText;
                    //if (SubHandler != null && string.IsNullOrWhiteSpace(this.SubtitleText))
                    //    SubHandler.SetSubtitleText(this, delegate(VideoInfo v) { return TrackingInfo; }, false);
                    return url;
                }
                return "";
            }
        }

        string nextPageVideoUrl = "";

        //private SubtitleHandler sh = null;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            //sh = new SubtitleHandler("Podnapisi", "swe;eng");
        }

        public override int DiscoverDynamicCategories()
        {
            foreach(Category c in Settings.Categories)
            {
                if (c.Name == "Genrer")
                {
                    c.HasSubCategories = true;
                    c.SubCategoriesDiscovered = false;
                }
            }
            return base.DiscoverDynamicCategories();
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string data = GetWebData((parentCategory as RssLink).Url, forceUTF8: true);
            Regex r = new Regex(@"<ul class=""cl"">(?<data>.*)?<li class=""cl series"">", RegexOptions.Singleline);
            Match match = r.Match(data);
            if (match.Success)
            {
                string genreData = match.Groups["data"].Value;
                r = new Regex(@"<li class=""cl""><a href=""(?<u>[^""]*)"">(?<n>[^<]*)</a></li>", RegexOptions.Singleline);
                foreach(Match m in r.Matches(genreData))
                {
                    cats.Add(new RssLink()
                    {
                        Name = m.Groups["n"].Value.Trim(),
                        Url = m.Groups["u"].Value,
                        ParentCategory = parentCategory
                    });
                }
                r = new Regex(@"<li class=""cl series"">.*?<ol>(?<data>.*)?</ol>", RegexOptions.Singleline);
                match = r.Match(data);
                if (match.Success)
                {
                    Category tv = new Category()
                    {
                        Name = "TV-Serier",
                        HasSubCategories = true,
                        SubCategoriesDiscovered = true,
                        SubCategories = new List<Category>()
                    };
                    genreData = match.Groups["data"].Value;
                    r = new Regex(@"<li class=""cl""><a href=""(?<u>[^""]*)"">(?<n>[^<]*)</a></li>", RegexOptions.Singleline);
                    foreach (Match m in r.Matches(genreData))
                    {
                        tv.SubCategories.Add(new RssLink()
                        {
                            Name = m.Groups["n"].Value.Trim(),
                            Url = m.Groups["u"].Value,
                            ParentCategory = tv
                        });
                    }
                    cats.Insert(0, tv);
                }
            }
           
            parentCategory.SubCategories = cats;
            parentCategory.SubCategoriesDiscovered = cats.Count > 0;
            return cats.Count;
        }

        private List<VideoInfo> GetVideos(string url, bool getNext = true)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = GetWebData(url, forceUTF8: true);
            Regex r = new Regex(@"<li>\s*<a\shref=""(?<u>[^""]*)"">\s*<figure>.*?<img\ssrc=""(?<i>[^""]*)""[^>]*?width(?:(?!caption\sclass=""cl"">).)*caption\sclass=""cl"">(?<d>[^<]*)(?:(?!span\sclass=""cl"">).)*span\sclass=""cl"">(?<t>[^<]*)", RegexOptions.Singleline);
            foreach (Match m in r.Matches(data))
            {
                ExtendedVideoInfo video = new ExtendedVideoInfo();
                video.Title = m.Groups["t"].Value.Trim();
                video.Thumb = m.Groups["i"].Value;
                video.VideoUrl = m.Groups["u"].Value;
                video.Description = m.Groups["d"].Value;
                video.TrackingInfo = null;
                video.TrackingInfo = GetTrackingInfo(video);
                videos.Add(video);
            }
            if(getNext)
            {
                r = new Regex(@"next""><a\shref=""(?<u>[^""]*)");
                Match m = r.Match(data);
                HasNextPage = m.Success;
                if (HasNextPage)
                    nextPageVideoUrl = m.Groups["u"].Value;
                else
                    nextPageVideoUrl = "";
            }
            return videos;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            nextPageVideoUrl = "";
            HasNextPage = false;
            return GetVideos((category as RssLink).Url);
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            HasNextPage = false;
            return GetVideos(nextPageVideoUrl);
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            string data = GetWebData(video.VideoUrl, forceUTF8: true);
            string url = "";
            video.PlaybackOptions = new Dictionary<string, string>();
            Regex r = new Regex(@"id=""embed(?<count>\d+?)"">(?<frame>.*?)</div");
            List<Hoster.HosterBase> hosters = Hoster.HosterFactory.GetAllHosters();
            foreach (Match m in r.Matches(data))
            {
                string iframe = m.Groups["frame"].Value;
                r = new Regex(@"write\('(?<hex>.*?)'\);");
                Match match = r.Match(iframe);
                if (match.Success)
                {
                    string hex = match.Groups["hex"].Value;
                    hex = hex.Replace(@"\x", string.Empty);
                    iframe = ConvertHex(hex);
                }
                r = new Regex(@"src=""(?<url>[^""]*)");
                match = r.Match(iframe);
                if (match.Success)
                {
                    url = match.Groups["url"].Value;
                    Hoster.HosterBase hoster = hosters.FirstOrDefault(h => url.ToLowerInvariant().Contains(h.GetHosterUrl().ToLowerInvariant()));
                    if (hoster != null)
                        video.PlaybackOptions.Add((new Uri(url)).Host.Replace("www.", "") + " (" + m.Groups["count"].Value + ")", url);
                }
            }
            if (video.PlaybackOptions.Count == 0)
                return new List<string>();
            

            string latestOption = (video is ExtendedVideoInfo) ? (video as ExtendedVideoInfo).LatestOption : "";
            
            if (string.IsNullOrEmpty(latestOption))
                url = video.PlaybackOptions.First().Value;
            else if (video.PlaybackOptions.ContainsKey(latestOption))
                url = video.PlaybackOptions[latestOption];
            else
                url = video.PlaybackOptions.First().Value;

            if (inPlaylist)
                video.PlaybackOptions.Clear();
            //if (video is ExtendedVideoInfo)
            //    (video as ExtendedVideoInfo).SubHandler = sh;
            
            return new List<string>() { url };
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video is ExtendedVideoInfo && (video as ExtendedVideoInfo).TrackingInfo != null)
                return (video as ExtendedVideoInfo).TrackingInfo;
            string t = video.Title;
            //First movie
            Regex r = new Regex(@"(?<t>.*?)\((?<y>\d\d\d\d)");
            Match m = r.Match(t);
            if (m.Success)
            {
                TrackingInfo ti = new TrackingInfo();
                ti.VideoKind = VideoKind.Movie;
                ti.Title = m.Groups["t"].Value.Trim();
                uint y = 0;
                uint.TryParse(m.Groups["y"].Value, out y);
                ti.Year = y;
                return ti;
            }
            //Then TV
            r = new Regex(@"(?<t>.*)?S(?<s>\d+)E(?<e>\d+)");
            m = r.Match(t);
            if (m.Success)
            {
                TrackingInfo ti = new TrackingInfo();
                ti.VideoKind = VideoKind.TvSeries;
                ti.Title = m.Groups["t"].Value.Trim();
                uint s = 0;
                uint e = 0;
                uint.TryParse(m.Groups["s"].Value, out s);
                uint.TryParse(m.Groups["e"].Value, out e);
                ti.Season = s;
                ti.Episode = e;
                return ti;
            }
            return base.GetTrackingInfo(video);
        }

        public override bool CanSearch { get { return true; } }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            string url = "http://www.filmer4ever.com/search/?q=" + HttpUtility.UrlEncode(query);
            List<SearchResultItem> result = new List<SearchResultItem>();
            GetVideos(url).ForEach(v => result.Add(v));
            return result;
        }

        public override List<VideoInfo> GetLatestVideos()
        {
            List<VideoInfo> videos = GetVideos("http://www.filmer4ever.com/", false);
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }

        private string ConvertHex(String hexString)
        {
            try
            {
                string ascii = string.Empty;
                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;
                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;
                }
                return ascii;
            }
            catch { }
            return string.Empty;
        }

    }
}
