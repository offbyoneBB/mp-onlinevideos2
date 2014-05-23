using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class TFPlayUtil : SiteUtilBase
    {
        public enum Languages { Svenska, English, Norsk, Dansk, Soumi };

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Prefered TV-Series subtitle language"), Description("Pick your prefered subtitle language for TV-Series content")]
        Languages preferedLanguage = Languages.Svenska;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Fallback TV-Series subtitle language"), Description("Pick your fallback subtitle language if there is no subtitles in your prefered language.")]
        Languages fallbackLanguage = Languages.English;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Load movie timeout"), Description("In seconds. Onlinvideos default 20 seconds, TFPlay default 60 seconds.")]
        uint httpReceiveDataTimeoutInSec = 60;

        public override int DiscoverDynamicCategories()
        {
            foreach (var cat in Settings.Categories) cat.HasSubCategories = true;
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private List<Category> GenerateSubCategories(Category parentCategory)
        {
            HtmlDocument doc = GetWebData<HtmlDocument>((parentCategory as RssLink).Url);
            HtmlNode videosDiv = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'videos')]");
            HtmlNodeCollection videoNodes = videosDiv.SelectNodes("div");
            List<Category> subCategories = new List<Category>();
            if (videoNodes != null)
            {
                foreach (HtmlNode videoDiv in videoNodes)
                {
                    RssLink category = new RssLink();
                    category.Name = videoDiv.GetAttributeValue("title", "");
                    category.Url = videoDiv.GetAttributeValue("data-href", "");
                    Regex rgx = new Regex(@"<hr\s*/>([^""]*)");
                    Match m = rgx.Match(videoDiv.GetAttributeValue("data-content", ""));
                    if (m.Success)
                    {
                        category.Description = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(m.Groups[1].Value)).Replace("\r", "").Replace("\n", "").Trim();
                    }
                    IEnumerable<HtmlNode> languages = videoDiv.Descendants("img").Where(i => i.GetAttributeValue("title", "") == "Subtitle");
                    if (languages != null && languages.Count() > 0)
                    {
                        string subtitles = "";
                        foreach (HtmlNode language in languages)
                        {
                            switch (language.GetAttributeValue("src", ""))
                            {
                                case "images/flags/se.png":
                                    subtitles += Languages.Svenska + " ";
                                    break;
                                case "images/flags/en.png":
                                    subtitles += Languages.English + " ";
                                    break;
                                case "images/flags/no.png":
                                    subtitles += Languages.Norsk + " ";
                                    break;
                                case "images/flags/dk.png":
                                    subtitles += Languages.Dansk + " ";
                                    break;
                                case "images/flags/fi.png":
                                    subtitles += Languages.Soumi + " ";
                                    break;
                                default:
                                    break;
                            }
                        }

                        category.Description += "\nSubtitle(s): " + subtitles;
                    }

                    HtmlNode image = videoDiv.SelectSingleNode("img");
                    if (image != null)
                        category.Thumb = "http://tfplay.org/" + image.GetAttributeValue("src", "");
                    category.HasSubCategories = false;
                    category.ParentCategory = parentCategory;
                    category.Other = VideoKind.TvSeries;
                    subCategories.Add(category);
                }
            }
            return subCategories;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            if (parentCategory.Name == "Genres")
            {
                HtmlDocument doc = GetWebData<HtmlDocument>((parentCategory as RssLink).Url);
                foreach (HtmlNode genre in doc.DocumentNode.Descendants("a").Where(n => n.GetAttributeValue("href", "").StartsWith("http://tfplay.org/media/genre/")))
                {
                    parentCategory.SubCategories.Add(new RssLink() { Name = genre.InnerText, Url = genre.GetAttributeValue("href", ""), HasSubCategories = true, ParentCategory = parentCategory });
                }
            }
            else
            {
                parentCategory.SubCategories = GenerateSubCategories(parentCategory);

            }

            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string data = GetWebData<string>((category as RssLink).Url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(data);
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlNode moviePlaybackLink = doc.DocumentNode.SelectSingleNode("//a[@class = 'btn btn-primary btn-block']");
            if (moviePlaybackLink != null)
            {
                ITrackingInfo ti = new TrackingInfo();
                Regex rgx = new Regex(@"http://www.imdb.com/title/(tt\d{7})");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    ti.ID_IMDB = m.Groups[1].Value;
                }
                rgx = new Regex(@"<td>Year</td>[^>]*>(\d{4})");
                m = rgx.Match(data);
                uint y = 0;
                if (m.Success)
                {
                    uint.TryParse(m.Groups[1].Value, out y);
                    ti.Year = y;
                }
                ti.Title = category.Name;
                ti.VideoKind = VideoKind.Movie;

                doc = GetWebData<HtmlDocument>(moviePlaybackLink.GetAttributeValue("href", ""));
                HtmlNode videoTag = doc.DocumentNode.SelectSingleNode("//video");
                string url = videoTag.SelectSingleNode("source").GetAttributeValue("src", "");
                var tracks = videoTag.SelectNodes("track");
                if (tracks != null && tracks.Count > 0)
                {
                    foreach (HtmlNode track in tracks)
                    {
                        videos.Add(new VideoInfo()
                        {
                            Title = category.Name + " (Subtitle: " + track.GetAttributeValue("label", "") + ")",
                            //SubtitleText = GetWebData(track.GetAttributeValue("src", ""), null, null, null, true, false, null, Encoding.UTF8),
                            SubtitleUrl = track.GetAttributeValue("src", ""),
                            Other = ti,
                            Description = category.Description,
                            ImageUrl = category.Thumb,
                            VideoUrl = url
                        });
                    }
                }
                else
                {
                    videos.Add(new VideoInfo()
                    {
                        Title = category.Name,
                        Other = ti,
                        Description = category.Description,
                        ImageUrl = category.Thumb,
                        VideoUrl = url
                    });
                }
            }
            else
            {
                foreach (HtmlNode seasonDiv in doc.DocumentNode.Descendants("div").Where(n => n.GetAttributeValue("id", "").StartsWith("season")))
                {
                    string season = seasonDiv.SelectSingleNode("h5").InnerText;
                    Regex rgx = new Regex(@"Season\s(\d+)");
                    Match m = rgx.Match(season);
                    uint s = 0;
                    if (m.Success)
                    {
                        uint.TryParse(m.Groups[1].Value, out s);
                    }
                    foreach (HtmlNode a in seasonDiv.Descendants("a"))
                    {
                        string episode = a.InnerText;
                        rgx = new Regex(@"Episode\s(\d+)");
                        m = rgx.Match(episode);
                        uint e = 0;
                        if (m.Success)
                        {
                            uint.TryParse(m.Groups[1].Value, out e);
                        }
                        ITrackingInfo ti = new TrackingInfo() { Title = category.Name, Season = s, Episode = e, VideoKind = VideoKind.TvSeries };
                        videos.Add(new VideoInfo()
                        {
                            Title = category.Name + " " + s + "x" + e,
                            Other = ti,
                            Description = category.Description,
                            ImageUrl = category.Thumb,
                            VideoUrl = a.GetAttributeValue("href", "")
                        });
                    }
                }
            }
            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            string url = "";
            if ((video.Other as ITrackingInfo).VideoKind == VideoKind.Movie)
            {
                if (!string.IsNullOrEmpty(video.SubtitleUrl))
                {
                    video.SubtitleText = GetWebData(video.SubtitleUrl, null, null, null, true, false, null, Encoding.UTF8);
                    video.SubtitleUrl = null;
                }
                url = video.VideoUrl;
            }
            else if ((video.Other as ITrackingInfo).VideoKind == VideoKind.TvSeries)
            {
                HtmlDocument doc = GetWebData<HtmlDocument>(video.VideoUrl);
                HtmlNode videoTag = doc.DocumentNode.SelectSingleNode("//video");
                url = videoTag.SelectSingleNode("source").GetAttributeValue("src", "");
                var tracks = videoTag.SelectNodes("track");
                if (tracks != null)
                {
                    string subtitle;
                    HtmlNode track = tracks.FirstOrDefault(n => n.GetAttributeValue("label", "") == preferedLanguage.ToString());
                    if (track == null)
                        track = tracks.FirstOrDefault(n => n.GetAttributeValue("label", "") == fallbackLanguage.ToString());
                    if (track != null)
                    {
                        subtitle = track.GetAttributeValue("src", "");
                        subtitle = GetWebData(subtitle, null, null, null, true, false, null, Encoding.UTF8);
                        video.SubtitleText = subtitle;
                    }
                }
            }
            MPUrlSourceFilter.HttpUrl httpUrl = new MPUrlSourceFilter.HttpUrl(url);
            httpUrl.ReceiveDataTimeout = (int)httpReceiveDataTimeoutInSec * 1000;
            url = httpUrl.ToString();
            return url;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            return video.Other as TrackingInfo;
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> results = new List<ISearchResultItem>();
            foreach (Category c in GenerateSubCategories(new RssLink() { Name = "Search", Url = "http://tfplay.org/search/?q=" + HttpUtility.UrlDecode(query) }))
                results.Add(c);
            return results;
        }
    }
}
