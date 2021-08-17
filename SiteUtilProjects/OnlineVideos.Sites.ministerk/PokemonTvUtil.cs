using OnlineVideos.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class PokemonTvUtil : SiteUtilBase
    {
        protected enum Country
        {
            Brasil,
            Danmark,
            Deutschland,
            España,
            Finland,
            France,
            Ireland,
            Italia,
            Nederland,
            Norge,
            Sverige,
            UK,
            US,
            Россия
        }

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Country"), Description("Select default country")]
        protected Country country = Country.US;

        private RssLink currentCountry;

        private List<Category> countries = new List<Category>()
        {
            new RssLink()
            {
                Name = Country.Brasil.ToString(),
                Url = "http://www.pokemon.com/br/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.Danmark.ToString(),
                Url = "http://www.pokemon.com/dk/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.Deutschland.ToString(),
                Url = "http://www.pokemon.com/de/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.España.ToString(),
                Url = "http://www.pokemon.com/es/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.Finland.ToString(),
                Url = "http://www.pokemon.com/fi/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.France.ToString(),
                Url = "http://www.pokemon.com/fr/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.Ireland.ToString(),
                Url = "http://www.pokemon.com/uk/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.Italia.ToString(),
                Url = "http://www.pokemon.com/it/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.Nederland.ToString(),
                Url = "http://www.pokemon.com/nl/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.Norge.ToString(),
                Url = "http://www.pokemon.com/no/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.Sverige.ToString(),
                Url = "http://www.pokemon.com/se/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.UK.ToString(),
                Url = "http://www.pokemon.com/uk/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.US.ToString(),
                Url = "http://www.pokemon.com/us/",
                HasSubCategories = true
            },
            new RssLink()
            {
                Name = Country.Россия.ToString(),
                Url = "http://www.pokemon.com/ru/",
                HasSubCategories = true
            }
        };


        public override int DiscoverDynamicCategories()
        {
            currentCountry = countries.FirstOrDefault(c => c.Name == country.ToString()) as RssLink;
            string data = GetWebData(currentCountry.Url);
            string pattern = @"class=""watch"">\s*<a\shref=""(?<url>[^""]*)(?:(?!title_pokemontv).)*title_pokemontv"">(?<title>[^<]*)";
            RegexOptions regexOptions = RegexOptions.Singleline;
            Regex regex = new Regex(pattern, regexOptions);
            Match m = regex.Match(data);
            if (m.Success)
            {
                Settings.Categories.Add(
                    new RssLink()
                    {
                        Name = m.Groups["title"].Value.Trim(),
                        Url = "http://www.pokemon.com" + m.Groups["url"].Value
                    });
            }
            pattern = @"<a\shref=""(?<url>[^""]*)""\srel=""""\stitle="""">\s*(?<title>[^<]*)<i\sclass=""icon-";
            regex = new Regex(pattern, regexOptions);
            m = regex.Match(data);
            if (m.Success)
            {
                Settings.Categories.Add(
                    new RssLink()
                    {
                        Name = m.Groups["title"].Value.Trim() + " (" + currentCountry.Name + ")",
                        HasSubCategories = true,
                        SubCategoriesDiscovered = true,
                        SubCategories = countries
                    });
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count == 2;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Settings.Categories.Clear();
            Settings.DynamicCategoriesDiscovered = false;
            country = (Country)Enum.Parse(typeof(Country), parentCategory.Name);
            List<OnlineVideos.Reflection.FieldPropertyDescriptorByRef> props = GetUserConfigurationProperties();
            OnlineVideos.Reflection.FieldPropertyDescriptorByRef prop = props.First(p => p.DisplayName == "Country");
            SetConfigValueFromString(prop, country.ToString());
            int ret = DiscoverDynamicCategories();
            parentCategory.SubCategories = new List<Category>();
            parentCategory.SubCategories.AddRange(Settings.Categories);
            return ret;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = GetWebData((category as RssLink).Url);
            //First without season and episode
            string pattern = @"data-video-id=""(?<VideoUrl>[^""]*)(?:(?!\sdata-video-poster).)*\sdata-video-poster=""(?<ImageUrl>[^""]*)""\s*data-video-title=""(?<Title>[^""]*)""\s*data-video-summary=""(?<Description>[^""]*)";
            RegexOptions regexOptions = RegexOptions.Singleline;
            Regex regex = new Regex(pattern, regexOptions);
            foreach (Match match in regex.Matches(data))
            {
                if (match.Success)
                {
                    string thumb = match.Groups["ImageUrl"].Value;
                    if (thumb.StartsWith("/")) thumb = "http://assets.pokemon.com/assets" + thumb;
                    videos.Add(
                        new VideoInfo()
                        {
                            Title = match.Groups["Title"].Value,
                            VideoUrl = string.Format("http://production-ps.lvp.llnw.net/r/PlaylistService/media/{0}/getMobilePlaylistByMediaId", match.Groups["VideoUrl"].Value),
                            Thumb = thumb,
                            Description = match.Groups["Description"].Value
                        });
                }
            }
            //The seasons + episodes

            pattern = @"data-video-id=""(?<VideoUrl>[^""]*)(?:(?!\sdata-video-poster).)*\sdata-video-poster=""(?<ImageUrl>[^""]*)""\s*data-video-title=""(?<Title>[^""]*)""\s*data-video-season=""(?<Season>[^""]*)""\s*data-video-episode=""(?<Episode>[^""]*)""\s*data-video-summary=""(?<Description>[^""]*)";
            regex = new Regex(pattern, regexOptions);
            foreach (Match match in regex.Matches(data))
            {
                if (match.Success)
                {
                    string thumb = match.Groups["ImageUrl"].Value;
                    string title = match.Groups["Title"].Value;
                    string se = match.Groups["Season"].Value;
                    if (!string.IsNullOrWhiteSpace(se))
                        se += "x";
                    se += match.Groups["Episode"].Value + " ";
                    if (!string.IsNullOrWhiteSpace(se))
                        title = se + title;


                    if (thumb.StartsWith("/")) thumb = "http://assets.pokemon.com/assets" + thumb;
                    videos.Add(
                        new VideoInfo()
                        {
                            Title = title,
                            VideoUrl = string.Format("http://production-ps.lvp.llnw.net/r/PlaylistService/media/{0}/getMobilePlaylistByMediaId", match.Groups["VideoUrl"].Value),
                            Thumb = thumb,
                            Description = match.Groups["Description"].Value
                        });
                }
            }

            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            string url = "";
            Regex regex = new Regex(@"""HttpLiveStreaming"",""mobileUrl"":""(?<url>[^""]*)");
            Match m = regex.Match(GetWebData(video.VideoUrl));
            if (m.Success)
            {
                string playlistUrl = m.Groups["url"].Value;
                video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(GetWebData(playlistUrl), playlistUrl, HlsStreamInfoFormatter.Bitrate);
                url = video.PlaybackOptions.First().Value;
                if (inPlaylist)
                    video.PlaybackOptions.Clear();
            }
            return new List<string>() { url };
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            return Helpers.FileUtils.GetSaveFilename(video.Title) + ".mp4";
        }
    }
}
