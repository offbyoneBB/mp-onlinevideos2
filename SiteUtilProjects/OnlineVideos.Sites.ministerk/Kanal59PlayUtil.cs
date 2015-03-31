using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Xml;
using System.Net;
using Newtonsoft.Json;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class Kanal59PlayUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Url of the swf file that used for playing the videos and rtmp verification")]
        protected string swfPlayer;

        [Category("OnlineVideosConfiguration"), Description("Url to site api")]
        protected string apiBaseUrl;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Download Subtitles"), Description("Choose if you want to download available subtitles or not.")]
        protected bool retrieveSubtitles = true;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string data = GetWebData(apiBaseUrl + "/listPrograms");
            JArray programs = (JArray)JsonConvert.DeserializeObject(data);
            List<Category> programCategories = new List<Category>();
            Category programCategory;
            foreach (JToken program in programs)
            {
                uint episedeCount = (uint)program["playableEpisodesCount"];
                if (!(bool)program["premium"] && episedeCount > 0)
                {
                    programCategory = new RssLink()
                    {
                        Name = (string)program["name"],
                        Description = (string)program["description"],
                        Thumb = (string)program["photoWithLogoUrl"],
                        Url = program["id"].ToString(),
                        EstimatedVideoCount = episedeCount,
                        SubCategories = new List<Category>(),
                        HasSubCategories = true
                    };

                    JArray seasons = (JArray)program["seasonNumbersWithContent"];
                    if (seasons != null)
                    {
                        foreach (JToken season in seasons)
                        {
                            programCategory.SubCategories.Add(new RssLink()
                            {
                                ParentCategory = programCategory,
                                Name = "Säsong " + season.ToString(),
                                Url = season.ToString(),
                                HasSubCategories = false
                            });

                        }
                    }
                    programCategory.SubCategories.Reverse();
                    programCategory.SubCategoriesDiscovered = programCategory.SubCategories.Count > 0;
                    Settings.Categories.Add(programCategory);
                }
            }

            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string videosUrl = string.Format("{0}/getMobileSeasonContent?programId={1}&seasonNumber={2}&format=ALL_MOBILE", apiBaseUrl, (category.ParentCategory as RssLink).Url, (category as RssLink).Url);
            JObject data = GetWebData<JObject>(videosUrl);
            JArray episodes = (JArray)data["episodes"];
            List<VideoInfo> videos = new List<VideoInfo>();
            foreach (JToken episode in episodes)
            {
                //Airtime not always there...
                if (episode["streams"].Any(s => !s["drmProtected"].Value<bool>()))
                {
                    JToken air = episode["shownOnTvDateTimestamp"];
                    string airtime = "";
                    if (air != null)
                    {
                        DateTime airDT = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((double)air).ToLocalTime();
                        airtime = airDT.ToString("g", OnlineVideoSettings.Instance.Locale);
                    }

                    VideoInfo video = new VideoInfo()
                    {
                        Thumb = (string)episode["posterUrl"],
                        Title = string.Format("{0} - {1} {2}: {3}", category.ParentCategory.Name, category.Name, (string)episode["episodeText"], (string)episode["title"]),
                        Description = (string)episode["description"],
                        Airdate = airtime,
                        VideoUrl = episode["id"].ToString(),
                        Other = videosUrl,
                        PlaybackOptions = new Dictionary<string, string>()
                    };
                    videos.Add(video);
                }
            }
            if (videos.Count == 0 && episodes.Count > 0)
            {
                throw new OnlineVideosException(string.Format("Only DRM protected content: \"{0} - {1} \"", category.ParentCategory.Name, category.Name), false);
            }
            return videos;
        }

        public override List<String> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            JObject data = GetWebData<JObject>(video.GetOtherAsString());
            video.PlaybackOptions.Clear();
            JArray episodes = (JArray)data["episodes"];
            JToken episode = episodes.FirstOrDefault(e => e["id"].ToString() == video.VideoUrl);
            IEnumerable<JToken> hlsStreams = episode["streams"].Where(s => s["format"].Value<string>().ToLower() == "ipad" && !s["drmProtected"].Value<bool>());
            if (hlsStreams == null || hlsStreams.Count() == 0)
                hlsStreams = episode["streams"].Where(s => s["format"].Value<string>().ToLower() == "iphone" && !s["drmProtected"].Value<bool>());
            if (hlsStreams != null && hlsStreams.Count() > 0)
            {
                try
                {
                    string url = hlsStreams.First()["source"].Value<string>();
                    string m3u8 = GetWebData(url);
                    Regex rgx = new Regex(@"WIDTH=(?<bitrate>\d+)[^c]*(?<url>[^\.]*\.m3u8)");
                    foreach (Match m in rgx.Matches(m3u8))
                    {
                        video.PlaybackOptions.Add(m.Groups["bitrate"].Value.ToString(), Regex.Replace(url, @"([^/]*?.m3u8)", delegate(Match match)
                        {
                            return m.Groups["url"].Value;
                        }));
                    }
                    video.PlaybackOptions = video.PlaybackOptions.OrderByDescending(p => int.Parse(p.Key)).ToDictionary(kvp => ((int.Parse(kvp.Key) /1000) + " kbps (HLS)"), kvp => kvp.Value);
                }
                catch { }
            }
            IEnumerable<JToken> streams = episode["streams"].Where(s => s["format"].Value<string>().ToLower() == "flash" && !s["drmProtected"].Value<bool>());
            string streamBaseUrl = (string)episode["streamBaseUrl"];
            Dictionary<string, string> rtmpD = new Dictionary<string, string>();
            if (streamBaseUrl != null && streams != null && streams.Count() > 0)
            {
                foreach (JToken stream in streams)
                {
                    MPUrlSourceFilter.RtmpUrl url = new MPUrlSourceFilter.RtmpUrl(streamBaseUrl)
                    {
                        SwfUrl = swfPlayer,
                        SwfVerify = true,
                        PlayPath = (string)stream["source"]
                    };
                    rtmpD.Add(((int)stream["bitrate"] / 1000).ToString(), url.ToString());
                }
                rtmpD = rtmpD.OrderByDescending(p => int.Parse(p.Key)).ToDictionary(kvp => (kvp.Key + " kbps (RTMP)"), kvp => kvp.Value);
                video.PlaybackOptions = video.PlaybackOptions.Concat(rtmpD).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                if (retrieveSubtitles && (bool)episode["hasSubtitle"])
                {
                    string subData = GetWebData(string.Format("{0}/subtitles/{1}", apiBaseUrl, episode["id"].ToString()));
                    JArray subtitleJson = (JArray)JsonConvert.DeserializeObject(subData);
                    video.SubtitleText = formatSubtitle(subtitleJson);

                }
            }
            string firsturl = video.PlaybackOptions.First().Value;
            if (inPlaylist)
                video.PlaybackOptions.Clear();
            return new List<string>() { firsturl };
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            string f = base.GetFileNameForDownload(video, category, url);
            if (f.EndsWith(".m3u8"))
                f = f.Replace(".m3u8", ".mp4");
            return f; 
        }
        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            Regex rgx = new Regex(@"(?<VideoKind>TvSeries)(?<Title>[^-]*).*?[Ss]äsong.*?(?<Season>\d+).*?[Aa]vsnitt.*?(?<Episode>\d+)");
            Match m = rgx.Match("TvSeries" + video.Title);
            ITrackingInfo ti = new TrackingInfo() { Regex = m };
            return ti;
        }

        private string formatSubtitle(JArray subtitleJson)
        {
            string srt = string.Empty;
            string srtFormat = "{0}\n{1} --> {2}\n{3}\n\n";
            TimeSpan t;
            int i = 1;
            string start;
            string stop;
            foreach (JToken line in subtitleJson)
            {
                t = TimeSpan.FromMilliseconds((double)line["startMillis"]);
                start = string.Format("{0:D2}:{1:D2}:{2:D2},{3:D3}", t.Hours, t.Minutes, t.Seconds, t.Milliseconds);
                t = TimeSpan.FromMilliseconds((double)line["endMillis"]);
                stop = string.Format("{0:D2}:{1:D2}:{2:D2},{3:D3}", t.Hours, t.Minutes, t.Seconds, t.Milliseconds);
                srt += string.Format(srtFormat, i++, start, stop, (string)line["text"]);
            }
            return srt;
        }
    }

}