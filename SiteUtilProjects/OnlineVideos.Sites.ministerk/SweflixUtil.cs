using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class SweflixUtil : LatestVideosSiteUtilBase
    {
        string baseUrl = "http://www.sweflix.ws";

        public override int DiscoverDynamicCategories()
        {
            RssLink filmer = new RssLink() { Name = "Filmer", Url = baseUrl };
            Settings.Categories.Add(filmer);
            HtmlDocument doc = GetWebData<HtmlDocument>(baseUrl);

            HtmlNode moviehome = doc.DocumentNode.SelectSingleNode("//div[@id = 'moviehome']");
            HtmlNode categorias = moviehome.SelectSingleNode("div[@class = 'categorias']");
            Category kategorier = new RssLink() { Name = "Kategorier", HasSubCategories = true, SubCategories = new List<Category>(), SubCategoriesDiscovered = true };
            foreach (HtmlNode item in categorias.SelectNodes(".//li"))
            {
                HtmlNode a = item.SelectSingleNode("a");
                kategorier.SubCategories.Add(new RssLink() { ParentCategory = kategorier, Name = a.InnerText.Trim(), Url = a.GetAttributeValue("href", "") });
            }
            Settings.Categories.Add(kategorier);

            HtmlNode filtroy = moviehome.SelectSingleNode("div[@class = 'filtro_y']");
            Category ar = new RssLink() { Name = "År", HasSubCategories = true, SubCategories = new List<Category>(), SubCategoriesDiscovered = true };
            foreach (HtmlNode item in filtroy.SelectNodes(".//li"))
            {
                HtmlNode a = item.SelectSingleNode("a");
                ar.SubCategories.Add(new RssLink() { ParentCategory = ar, Name = a.InnerText.Trim(), Url = a.GetAttributeValue("href", "") });
            }
            Settings.Categories.Add(ar);
            Settings.DynamicCategoriesDiscovered = true;
            return 3;
        }

        int currentPage = 1;
        string currentUrl = "";
        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;
            currentPage = 1;
            currentUrl = (category as RssLink).Url + "/page/{0}";
            List<VideoInfo> videos = GetVideos(currentUrl, currentPage);
            HasNextPage = videos.Count >= 20;
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            HasNextPage = false;
            currentPage++;
            List<VideoInfo> videos = GetVideos(currentUrl, currentPage);
            HasNextPage = videos.Count >= 20;
            return videos;
        }

        private List<VideoInfo> GetVideos(string url, int page)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlDocument doc = GetWebData<HtmlDocument>(string.Format(url, page));
            HtmlNodeCollection movies = doc.DocumentNode.SelectNodes("//div[@class = 'item' and contains(@id,'mt-')]");
            if (movies != null)
            {
                foreach (HtmlNode movie in movies)
                {
                    VideoInfo video = new VideoInfo();
                    ITrackingInfo ti = new TrackingInfo() { VideoKind = VideoKind.Movie };
                    HtmlNode a = movie.SelectSingleNode("a");
                    if (a != null)
                    {
                        video.VideoUrl = a.GetAttributeValue("href", "");
                        HtmlNode img = a.SelectSingleNode(".//img");
                        if (img != null)
                        {
                            video.Thumb = img.GetAttributeValue("src", "");
                        }
                    }
                    HtmlNode h2 = movie.SelectSingleNode(".//h2");
                    if (h2 != null)
                    {
                        video.Title = HttpUtility.HtmlDecode(h2.InnerText.Trim());
                        ti.Title = video.Title;
                    }
                    HtmlNode desc = movie.SelectSingleNode(".//span[@class = 'ttx']");
                    if (desc != null)
                    {
                        video.Description = HttpUtility.HtmlDecode(desc.InnerText.Trim());
                    }
                    HtmlNode airDate = movie.SelectSingleNode(".//span[@class = 'year']");
                    if (airDate != null)
                    {
                        video.Airdate = HttpUtility.HtmlDecode(airDate.InnerText.Trim());
                        uint y = 0;
                        uint.TryParse(video.Airdate, out y);
                        ti.Year = y;
                    }
                    video.Other = ti;
                    videos.Add(video);
                }
            }
            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            HtmlDocument doc = GetWebData<HtmlDocument>(video.VideoUrl);
            HtmlNode iframe = doc.DocumentNode.SelectSingleNode("//div[@class = 'movieplay']/iframe");
            string url = iframe.GetAttributeValue("src", "");
            if (video.Other is ITrackingInfo)
            {
                Regex rgx = new Regex(@"href=""http://www.imdb.com/title/(?<imdb>tt\d+)/");
                Match m = rgx.Match(doc.DocumentNode.InnerHtml);
                if (m.Success)
                {
                    string imdb = m.Groups["imdb"].Value;
                    (video.Other as ITrackingInfo).ID_IMDB = imdb;
                }
            }
            Hoster.HosterBase host = Hoster.HosterFactory.GetAllHosters().FirstOrDefault(h => url.ToLowerInvariant().Contains(h.GetHosterUrl().ToLowerInvariant()));
            if (host == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                return new List<string>();
            }
            else
            {
                if (inPlaylist)
                {
                    video.PlaybackOptions = new Dictionary<string, string>();
                    return new List<string>() { host.GetVideoUrl(url) };
                }
                else
                {
                    video.PlaybackOptions = host.GetPlaybackOptions(url);
                    return new List<string>() { video.PlaybackOptions.First().Value };
                }
            }
        }


        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            HasNextPage = false;
            currentPage = 1;
            currentUrl = baseUrl + "/page/{0}?s=" + HttpUtility.UrlEncode(query);
            GetVideos(currentUrl, currentPage).ForEach(v => results.Add(v));
            HasNextPage = results.Count >= 20;
            return results;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video.Other is ITrackingInfo)
                return video.Other as ITrackingInfo;
            return base.GetTrackingInfo(video);
        }

        public override List<VideoInfo> GetLatestVideos()
        {
            List<VideoInfo> videos = GetVideos(baseUrl, 1);
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }
    }
}
