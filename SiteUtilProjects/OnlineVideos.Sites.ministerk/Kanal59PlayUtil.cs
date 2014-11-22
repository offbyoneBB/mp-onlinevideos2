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

        public override List<VideoInfo> getVideoList(Category category)
        {
            string videosUrl = string.Format("{0}/getMobileSeasonContent?programId={1}&seasonNumber={2}&format=FLASH", apiBaseUrl, (category.ParentCategory as RssLink).Url, (category as RssLink).Url);
            JObject data = GetWebData<JObject>(videosUrl);
            JArray episodes = (JArray)data["episodes"];
            List<VideoInfo> videos = new List<VideoInfo>();
            bool widevineRequired = false;
            foreach (JToken episode in episodes)
            {
                if (!(bool)episode["widevineRequired"])
                {
                    //Airtime not always there...
                    JToken air = episode["shownOnTvDateTimestamp"];
                    string airtime = "";
                    if (air != null)
                    {
                        DateTime airDT = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((double)air).ToLocalTime();
                        airtime = airDT.ToString("g", OnlineVideoSettings.Instance.Locale);
                    }

                    VideoInfo video = new VideoInfo()
                    {
                        ImageUrl = (string)episode["posterUrl"],
                        Title = string.Format("{0}: {1}", (string)episode["episodeText"], (string)episode["title"]),
                        Description = (string)episode["description"],
                        Airdate = airtime,
                        //Length = episode["length"].ToString() Not working correctly...
                        PlaybackOptions = new Dictionary<string, string>(),
                    };
                    JArray streams = (JArray)episode["streams"];
                    string streamBaseUrl = (string)episode["streamBaseUrl"];
                    if (streamBaseUrl != null)
                    {
                        int maxBitrate = 0;
                        foreach (JToken stream in streams)
                        {
                            int bitrate = (int)stream["bitrate"] / 1000;
                            maxBitrate = maxBitrate > bitrate ? maxBitrate : bitrate;
                            MPUrlSourceFilter.RtmpUrl url = new MPUrlSourceFilter.RtmpUrl(streamBaseUrl)
                            {
                                SwfUrl = swfPlayer,
                                SwfVerify = true,
                                PlayPath = (string)stream["source"]
                            };
                            video.PlaybackOptions.Add(string.Format("{0} kbps", bitrate), url.ToString());
                        }
                        video.VideoUrl = video.PlaybackOptions.First(po => po.Key == maxBitrate.ToString() + " kbps").Value;
                        if ((bool)episode["hasSubtitle"])
                        {
                            video.SubtitleUrl = string.Format("{0}/subtitles/{1}", apiBaseUrl, episode["id"].ToString());
                        }
                        videos.Add(video);
                    }
                }
                else
                {
                    widevineRequired = true;
                }
            }
            if (videos.Count < 1 && widevineRequired)
            {
                throw new OnlineVideosException(string.Format("All \"{0} - {1} \" episodes DRM protected", category.ParentCategory.Name, category.Name), false);
            }

            videos.Reverse();
            return videos;
        }

        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            if (!string.IsNullOrEmpty(video.SubtitleUrl) && retrieveSubtitles)
            {
                string subData = GetWebData(video.SubtitleUrl);
                JArray subtitleJson = (JArray)JsonConvert.DeserializeObject(subData);
                video.SubtitleText = formatSubtitle(subtitleJson);
            }
           
            if (inPlaylist)
            {
                video.Other = video.PlaybackOptions;
                video.PlaybackOptions = null;
            }
            else
            {
                if (video.PlaybackOptions == null && video.Other != null)
                {
                    video.PlaybackOptions = video.Other as Dictionary<string, string>;
                    video.Other = null;
                }
            }
            return new List<string>() { video.VideoUrl };
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