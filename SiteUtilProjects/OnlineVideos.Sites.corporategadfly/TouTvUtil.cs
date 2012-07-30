using System;
using System.Collections.Generic;
using System.Linq;
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
        private static Regex jsonOpenRegex = new Regex(@"^\[",
                                                       RegexOptions.Compiled);
        private static Regex jsonCloseRegex = new Regex(@"\]$",
                                                        RegexOptions.Compiled);
        private static Regex rtmpUrlRegex = new Regex(@"^(?<host>rtmp.*?)(\{break\}|\<break\>)(?<playPath>.*?)$",
            RegexOptions.Compiled);
        private static Regex singleVideoCategoryRegex = new Regex(@"<div\sclass=""emissionEpisode_containerContenuPub\s+clearfix"">\s+<div\sclass=""emissionEpisode_contenuEpisode"">\s+<div\sclass=""clearfix"">\s+<h1>(?<Title>[^<]*)</h1>\s+</div>\s+<span\sclass=""emissionEpisode_degrade""></span>\s+<div\sclass=""emissionEpisode_containerTxt"">\s+<div\sclass=""emissionEpisode_titreSaisonEpisode"">\s+<h2>[^<]*</h2>\s+<span\sclass=""codeAge\semissionEpisode_ageTitreEmission"">[^<]*</span>\s+</div>\s+<br\sclass=""clear""\s/>\s+<p>(?<Description>[^<]*)</p>\s+</div>\s+<div\sclass=""emissionEpisode_plusInfoToggle"">\s+<p\sID=""PDateEpisode""><small>Date\sde\sdiffusion\s:</small>\s<strong>(?<Airdate>[^<]*)</strong></p>",
            RegexOptions.Compiled);
        private static Regex singleVideoCategoryImageUrlRegex = new Regex(@"<meta\scontent=""(?<ImageUrl>[^""]*)""\sproperty=""og:image""",
                                                                          RegexOptions.Compiled);
        
        private static string baseUrlPrefix = @"http://www.tou.tv";

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
                        json = jsonOpenRegex.Replace(json, "");
                        json = jsonCloseRegex.Replace(json, "");
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

            return videoList;
        }
        
        public override string getUrl(VideoInfo video)
        {
            string playListUrl = getPlaylistUrl(video.VideoUrl);
            if (String.IsNullOrEmpty(playListUrl))
                return String.Empty; // if no match, return empty url -> error
            
            Log.Debug(@"video: {0}", video.Title);
            string result = string.Empty;

            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

            XmlDocument xml = GetWebData<XmlDocument>(playListUrl);

            Log.Debug(@"SMIL loaded");

            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(xml.NameTable);
            nsmRequest.AddNamespace("a", @"http://www.w3.org/2001/SMIL20/Language");

            XmlNode metaBase = xml.SelectSingleNode(@"//a:meta", nsmRequest);
            string metaBaseValue = metaBase.Attributes["base"].Value;

            // base URL may be stored in the base attribute of <meta> tag
            string baseRtmp = metaBaseValue.StartsWith("rtmp") ? metaBaseValue : String.Empty;

            foreach (XmlNode node in xml.SelectNodes("//a:body/a:switch/a:video", nsmRequest))
            {
                int bitrate = int.Parse(node.Attributes["system-bitrate"].Value);
                // do not bother unless bitrate is non-zero
                if (bitrate == 0) continue;

                string url = node.Attributes["src"].Value;
                if (!string.IsNullOrEmpty(baseRtmp))
                {
                    // prefix url with base (from <meta> tag) and artifical <break>
                    url = baseRtmp + @"<break>" + url;
                }
                Log.Debug(@"bitrate: {0}, url: {1}", bitrate / 1000, url);

                if (url.StartsWith("rtmp"))
                {
                    Match rtmpUrlMatch = rtmpUrlRegex.Match(url);

                    if (rtmpUrlMatch.Success && !urlsDictionary.ContainsKey(bitrate / 1000))
                    {
                        string host = rtmpUrlMatch.Groups["host"].Value;
                        string playPath = rtmpUrlMatch.Groups["playPath"].Value;
                        if (playPath.EndsWith(@".mp4") && !playPath.StartsWith(@"mp4:"))
                        {
                            // prepend with mp4:
                            playPath = @"mp4:" + playPath;
                        }
                        else if (playPath.EndsWith(@".flv"))
                        {
                            // strip extension
                            playPath = playPath.Substring(0, playPath.Length - 4);
                        }
                        Log.Debug(@"Host: {0}, PlayPath: {1}", host, playPath);
                        MPUrlSourceFilter.RtmpUrl rtmpUrl = new MPUrlSourceFilter.RtmpUrl(host) { PlayPath = playPath };
                        urlsDictionary.Add(bitrate / 1000, rtmpUrl.ToString());
                    }
                }
            }

            // sort the URLs ascending by bitrate
            foreach (var item in urlsDictionary.OrderBy(u => u.Key))
            {
                video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                // return last URL as the default (will be the highest bitrate)
                result = item.Value;
            }
            return result;
        }

    }
}
