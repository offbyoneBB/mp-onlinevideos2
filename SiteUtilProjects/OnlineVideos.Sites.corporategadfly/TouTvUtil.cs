using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TouTvUtil : GenericSiteUtil
    {
        private static Regex subcaretoryRegex = new Regex(@"<div\sclass=""repertoire_groupeNivTitre"">\s+<a\sdata-stats-action=""Emission""\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a>\s+</div>\s+<div\sclass=""repertoire_btnPlus""\sstyle=""[^""]*"">\s+<!--Bulle-->\s+<div\sclass=""repertoire_wrapperDetailsEmission"">\s+<span\sclass=""repertoire_flecheBullePlusDetails""></span>\s+<div\sclass=""repertoire_detailsEmission"">\s+<p><strong>DURÉE\s:</strong>\s[^<]*</p>\s+(?<season>(<p><strong>SAISON[^:]*:</strong>\s[^<]*</p>\s+)+)<p><strong>PAYS\s:</strong>[^<]*</p>\s+</div>\s+</div>\s+<!--/Bulle-->\s+</div>\s+<div\sclass=""repertoire_groupeNivGenre"">\s+<a\sdata-stats-action=""Emission""\shref=""[^""]*"">(?<category>[^<]*)</a>\s+</div>",
                                                          RegexOptions.Compiled);
        private static Regex seasonRegex = new Regex(@"SAISON",
                                                     RegexOptions.Compiled);
        private static Regex episodeListRegex = new Regex(@"data-initialdata=""(?<episodeListInitialData>[^""]*)"">",
                                                        RegexOptions.Compiled);
        private static Regex jsonOpenBracketRegex = new Regex(@"^\[",
                                                        RegexOptions.Compiled);
        private static Regex jsonCloseBracketRegex = new Regex(@"\]$",
                                                        RegexOptions.Compiled);
        private static Regex jsonEndingCommaRegex = new Regex(@",$",
                                                              RegexOptions.Compiled);
        private static Regex rtmpUrlRegex = new Regex(@"^(?<host>rtmp.*?)(\{break\}|\<break\>)(?<playPath>.*?)$",
            RegexOptions.Compiled);
        private static Regex singleVideoCategoryRegex = new Regex(@"<div\sclass=""emissionEpisode_containerContenuPub\s+clearfix"">\s+<div\sclass=""emissionEpisode_contenuEpisode"">\s+<div\sclass=""clearfix"">\s+<h1>(?<Title>[^<]*)</h1>\s+(<span\sclass=""codeAge\s[^""]*"">[^<]*</span>\s+)*</div>\s+(<div\sclass=""emissionEpisode_descriptionToggle""><p>[^<]*</p>\s+<div\sid=""facebookLikeEpisode""><fb:like.*?</fb:like></div>\s+</div>\s+<div\sclass=""emissionEpisode_containerBtn\s+clearfix"">\s+<span\sclass=""btnGeneral\sdescriptionFloat\sbtGeneralAvecFleche"">Description\s<span\sclass=""fleche""></span></span>\s+</div>\s+)*<span\sclass=""emissionEpisode_degrade""></span>\s+<div\sclass=""emissionEpisode_containerTxt"">\s+<div\sclass=""emissionEpisode_titreSaisonEpisode"">\s+<h2>[^<]*</h2>\s+<span\sclass=""codeAge\s[^""]*"">[^<]*</span>\s+</div>\s+<br\sclass=""clear""\s/>\s+<p>(?<Description>[^<]*)</p>\s+</div>\s+<div\sclass=""emissionEpisode_plusInfoToggle"">\s+<p\sID=""PDateEpisode""><small>Date\sde\sdiffusion\s:</small>\s<strong>(?<Airdate>[^<]*)</strong></p>",
            RegexOptions.Compiled);
        private static Regex singleVideoCategoryImageUrlRegex = new Regex(@"<meta\scontent=""(?<ImageUrl>[^""]*)""\sproperty=""og:image""",
                                                                          RegexOptions.Compiled);
        private static Regex manifestRegex = new Regex(@"\((?<json>[^\)]*)\)",
                                                       RegexOptions.Compiled);
        
        private static string baseUrlPrefix = @"http://www.tou.tv";
        private static string seasonListUrl = baseUrlPrefix + @"/Emisode/GetVignetteSeason?emissionId={0}&season={1}";
        private static string userAgent = @"Mozilla/5.0 (Windows NT 5.1; rv:17.0) Gecko/20100101 Firefox/17.0";
        private static string mainfestUrlFormat = @"{0}&g={1}";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string webData = GetWebData(baseUrl);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in regEx_dynamicCategories.Matches(webData))
                {
                    RssLink cat = new RssLink();

                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.Url = baseUrl;
                    cat.HasSubCategories = true;

                    Settings.Categories.Add(cat);
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            RssLink parentRssLink = (RssLink) parentCategory;
            string webData = GetWebData(parentRssLink.Url);
            
            string encodedName = HttpUtility.HtmlEncode(parentCategory.Name);
            
            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in subcaretoryRegex.Matches(webData))
                {
                    if (m.Groups["category"].Value.Equals(encodedName))
                    {
                        RssLink cat = new RssLink();
    
                        cat.ParentCategory = parentCategory;
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        string season = HttpUtility.HtmlDecode(m.Groups["season"].Value);
                        cat.HasSubCategories = seasonRegex.Matches(season).Count != 1;

                        cat.Url = string.Format(@"{0}{1}", baseUrlPrefix, m.Groups["url"].Value);
    
                        parentCategory.SubCategories.Add(cat);
                    }
                }
            }
            
            // no categories found could possibly mean that we are dealing with
            // a category which has multiple seasons and multiple episodes
            //
            // For example, "Les arnaqueurs" in "Séries et téléromans"
            if (parentCategory.SubCategories.Count == 0 )
            {
                Match episodeListMatch = episodeListRegex.Match(webData);
                if (episodeListMatch.Success)
                {
                    string json = HttpUtility.HtmlDecode(episodeListMatch.Groups["episodeListInitialData"].Value);
                    if (json != null)
                    {
                        // replace opening and closing brackets [] with empty
                        json = jsonOpenBracketRegex.Replace(json, "");
                        json = jsonCloseBracketRegex.Replace(json, "");
                        
                        JToken episodeListInitialData = JToken.Parse(json);
                        
                        string seriesId = episodeListInitialData.Value<string>("EmissionId");
                        JArray seasons = episodeListInitialData["SeasonList"] as JArray;
                        
                        foreach (int season in seasons)
                        {
                            Log.Debug(@"Season: {0}", season);
                            
                            RssLink cat = new RssLink();
                            cat.ParentCategory = parentCategory;
                            cat.Name = string.Format(@"Saison {0}", season);
                            cat.HasSubCategories = false;
                            cat.Url = string.Format(seasonListUrl, seriesId, season);
                            
                            parentCategory.SubCategories.Add(cat);
                        }
                    }
                }
            }
            
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        protected override List<VideoInfo> Parse(string url, string data)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            if (string.IsNullOrEmpty(data)) data = GetWebData(url, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            if (data.Length > 0)
            {
                Match episodeListMatch = episodeListRegex.Match(data);
                if (episodeListMatch.Success)
                {
                    Log.Debug(@"InitialData: {0}", episodeListMatch.Groups["episodeListInitialData"].Value);
                    string json = HttpUtility.HtmlDecode(episodeListMatch.Groups["episodeListInitialData"].Value);
                    if (json != null)
                    {
                        // replace opening and closing brackets [] with empty 
                        json = jsonOpenBracketRegex.Replace(json, "");
                        json = jsonCloseBracketRegex.Replace(json, "");
                        Log.Debug(@"JSON after brackets removed: {0}", json);
                        JToken episodeListInitialData = JToken.Parse(json);

                        JArray episodes = episodeListInitialData["EpisodeVignetteList"] as JArray;
                        foreach (JToken episode in episodes) {
                            VideoInfo videoInfo = CreateVideoInfo();
                            videoInfo.Title = episode.Value<string>("DetailsViewSaison");
                            videoInfo.ImageUrl = episode.Value<string>("DetailsViewImageUrlL");
                            videoInfo.Length = episode.Value<string>("DetailsViewDureeEpisode");
                            videoInfo.Airdate = episode.Value<string>("DetailsViewDateEpisode");
                            videoInfo.Description = episode.Value<string>("DetailsFullDescription");
                            videoInfo.VideoUrl = new Uri(new Uri(baseUrlPrefix), episode.Value<string>("DetailsViewUrl")).AbsoluteUri;
                            videoList.Add(videoInfo);
                        }
                    }
                }
            }
            
            // no videos found could mean that we are possibly on a category that only has a single video,
            // so create a category with a single video (use a separate regular expression to find info)
            //
            // For example, "1 jour 24 heures 34 millions de vies" in "Films et documentaires"
            if (videoList.Count == 0)
            {
                Log.Debug("No videos found, attempting to find single video");
            
                VideoInfo videoInfo = CreateVideoInfo();
                videoInfo.VideoUrl = url;
                Match m = singleVideoCategoryRegex.Match(data);
                if (m.Success)
                {
                    videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                    videoInfo.Airdate = Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
                    videoInfo.Description = m.Groups["Description"].Value;
                    
                    Match imageMatch = singleVideoCategoryImageUrlRegex.Match(data);
                    if (imageMatch.Success)
                    {
                        videoInfo.ImageUrl = imageMatch.Groups["ImageUrl"].Value;
                    }
                    videoList.Add(videoInfo);
                }
            }

            // no videos found could mean that we are possibly on a category with multiple
            // seasons and multiple episodes. use JSON to find the episodes
            //
            // For example, "Les arnaqueurs" in "Séries et téléromans"
            if (videoList.Count == 0 && data.StartsWith("[{"))
            {
                Log.Debug(@"No videos found, attempting JSON parsing for {0}", url);
                
                // replace opening and closing brackets [] with empty
                string json = jsonOpenBracketRegex.Replace(data, "");
                json = jsonCloseBracketRegex.Replace(json, "");
                
                // treat contents as JSON
                JToken episodeListInitialData = JToken.Parse(json);

                JArray episodes = episodeListInitialData["EpisodeVignetteList"] as JArray;
                
                foreach (JToken episode in episodes) {
                    VideoInfo videoInfo = CreateVideoInfo();
                    videoInfo.Title = episode.Value<string>("DetailsViewSaison");
                    videoInfo.ImageUrl = episode.Value<string>("DetailsViewImageUrlL");
                    videoInfo.Length = episode.Value<string>("DetailsViewDureeEpisode");
                    videoInfo.Airdate = episode.Value<string>("DetailsViewDateEpisode");
                    videoInfo.Description = episode.Value<string>("DetailsFullDescription");
                    videoInfo.VideoUrl = new Uri(new Uri(baseUrlPrefix), episode.Value<string>("DetailsViewUrl")).AbsoluteUri;
                    videoList.Add(videoInfo);
                }
            }
            
            return videoList;
        }
        
        public override string getUrl(VideoInfo video)
        {
            string playListUrl = getPlaylistUrl(video.VideoUrl);
            if (String.IsNullOrEmpty(playListUrl))
                return String.Empty; // if no match, return empty url -> error
            
            Log.Debug(@"video: {0}", video.Title);
            string result = string.Empty;

            string data = GetWebData(playListUrl);
            Log.Debug(@"Validation loaded from {0}", playListUrl);

            if (!string.IsNullOrEmpty(data))
            {
                Match manifestMatch = manifestRegex.Match(data);
                string jsonData = jsonEndingCommaRegex.Replace(manifestMatch.Groups["json"].Value, "");
                JToken json = JToken.Parse(jsonData);
                string manifestUrl = string.Format(mainfestUrlFormat, json.Value<string>("url"), GetRandomChars(12));
                Log.Debug(@"Manifest URL: {0}", manifestUrl);
                
                if (!string.IsNullOrEmpty(manifestUrl))
                {
                    result = new MPUrlSourceFilter.HttpUrl(manifestUrl) {
                        UserAgent = userAgent,
                    }.ToString();
                }
            }
            return result;
        }

        string GetRandomChars(int amount)
        {
            var random = new Random();
            var sb = new StringBuilder(amount);
            for (int i = 0; i < amount;i++ ) sb.Append(Encoding.ASCII.GetString(new byte[] { (byte)random.Next(65, 90) }));
            return sb.ToString();
        }

    }
}
