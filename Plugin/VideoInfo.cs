using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OnlineVideos.Sites;
using RssToolkit.Rss;

namespace OnlineVideos
{
    public class VideoInfo
    {
        public string Title { get; set; }
        public string Title2 { get; set; }
        public string Description { get; set; }
        public string VideoUrl { get; set; }
        public string ImageUrl { get; set; }
        public string Length { get; set; }
        public string Tags { get; set; }
        public string Genres { get; set; }
        public string Cast { get; set; }
        public string StartTime { get; set; }
        public object Other { get; set; }
        public Dictionary<string, string> PlaybackOptions;
        
        /// <summary>This property is only used by the <see cref="FavoriteUtil"/> to store the Name of the Site where this Video came from.</summary>
        public string SiteName { get; set; }

        /// <summary>This property is set by the <see cref="ImageDownloader"/> to the file after downloading from <see cref="ImageUrl"/>.</summary>
        public string ThumbnailImage { get; set; }

        public VideoInfo()
        {
            Title = string.Empty;
            Title2 = string.Empty;
            Description = string.Empty;
            VideoUrl = string.Empty;
            ImageUrl = string.Empty;
            Length = string.Empty;
            Tags = string.Empty;
            StartTime = string.Empty;
            SiteName = string.Empty;
        }

        public void CleanDescription()
        {
            if (!string.IsNullOrEmpty(Description))
            {
                // decode HTML escape character
                Description = System.Web.HttpUtility.HtmlDecode(Description);

                // Replace &nbsp; with space
                Description = Regex.Replace(Description, @"&nbsp;", " ", RegexOptions.Multiline);

                // Remove double spaces
                Description = Regex.Replace(Description, @"  +", "", RegexOptions.Multiline);

                // Replace <br/> with \n
                Description = Regex.Replace(Description, @"< *br */*>", "\n", RegexOptions.IgnoreCase & RegexOptions.Multiline);

                // Remove remaining HTML tags                
                Description = Regex.Replace(Description, @"<[^>]*>", "", RegexOptions.Multiline);

                // Remove whitespace at the beginning and end
                Description = Description.Trim();
            }            
        }
        
        public override string ToString()
        {
            return string.Format("Title:{0}\nDesc:{1}\nVidUrl:{2}\nImgUrl:{3}\nLength:{4}\nTags:{5}", Title, Description, VideoUrl, ImageUrl, Length, Tags);
        }

        public double GetSecondsFromStartTime()
        {
            // Example: startTime = 02:34:25.00 should result in 9265 seconds
            double hours = new double();
            double minutes = new double();
            double seconds = new double();

            double.TryParse(StartTime.Substring(0, 2), out hours);
            double.TryParse(StartTime.Substring(3, 2), out minutes);
            double.TryParse(StartTime.Substring(6, 2), out seconds);

            seconds += (((hours * 60) + minutes) * 60);

            return seconds;
        }

        public static VideoInfo FromRssItem(RssItem rssItem, bool useLink, System.Predicate<string> isPossibleVideo)
        {
            VideoInfo video = new VideoInfo() { PlaybackOptions = new Dictionary<string, string>() };

            // Title - prefer from MediaTitle tag if available
            if (!String.IsNullOrEmpty(rssItem.MediaTitle)) video.Title = rssItem.MediaTitle;
            else video.Title = rssItem.Title;

            // Description - prefer MediaDescription tag if available
            if (!String.IsNullOrEmpty(rssItem.MediaDescription)) video.Description = rssItem.MediaDescription;
            else video.Description = rssItem.Description;

            // Try to find a thumbnail
            if (!string.IsNullOrEmpty(rssItem.GT_Image))
            {
                video.ImageUrl = rssItem.GT_Image;
            }
            else if (rssItem.MediaThumbnails.Count > 0)
            {
                video.ImageUrl = rssItem.MediaThumbnails[0].Url;
            }
            else if (rssItem.MediaContents.Count > 0 && rssItem.MediaContents[0].MediaThumbnails.Count > 0)
            {
                video.ImageUrl = rssItem.MediaContents[0].MediaThumbnails[0].Url;
            }
            else if (rssItem.MediaGroups.Count > 0 && rssItem.MediaGroups[0].MediaThumbnails.Count > 0)
            {
                video.ImageUrl = rssItem.MediaGroups[0].MediaThumbnails[0].Url;
            }
            else if (rssItem.Enclosure != null && rssItem.Enclosure.Type != null && rssItem.Enclosure.Type.ToLower().StartsWith("image"))
            {
                video.ImageUrl = rssItem.Enclosure.Url;
            }

            if (rssItem.Blip_Runtime != 0) video.Length = TimeSpan.FromSeconds(rssItem.Blip_Runtime).ToString();
            if (string.IsNullOrEmpty(video.Length)) video.Length = GetDuration(rssItem.iT_Duration);

            // if we are forced to use the Link of the RssItem, just set the video link
            if (useLink) video.VideoUrl = rssItem.Link;

            //get the video and the length
            if (rssItem.Enclosure != null && (rssItem.Enclosure.Type == null || !rssItem.Enclosure.Type.ToLower().StartsWith("image")) && (isPossibleVideo(rssItem.Enclosure.Url) || useLink))
            {
                video.VideoUrl = useLink ? rssItem.Link : rssItem.Enclosure.Url;

                if (string.IsNullOrEmpty(video.Length) && !string.IsNullOrEmpty(rssItem.Enclosure.Length))
                {
                    int bytesOrSeconds = 0;
                    if (int.TryParse(rssItem.Enclosure.Length, out bytesOrSeconds))
                    {
                        if (bytesOrSeconds > 18000) // won't be longer than 5 hours if Length is guessed as seconds, so it's bytes
                            video.Length = (bytesOrSeconds / 1024).ToString("N0") + " KB";
                        else
                            video.Length = TimeSpan.FromSeconds(bytesOrSeconds).ToString();
                    }
                    else
                    {
                        video.Length = rssItem.Enclosure.Length;
                    }
                }
            }
            if (rssItem.MediaContents.Count > 0) // try to get the first MediaContent
            {
                foreach (RssItem.MediaContent content in rssItem.MediaContents)
                {
                    if (!useLink && isPossibleVideo(content.Url)) AddToPlaybackOption(video.PlaybackOptions, content);
                    if (string.IsNullOrEmpty(video.Length)) video.Length = GetDuration(content.Duration);
                }
            }
            if (rssItem.MediaGroups.Count > 0) // videos might be wrapped in groups, try to get the first MediaContent
            {
                foreach (RssItem.MediaGroup grp in rssItem.MediaGroups)
                {
                    foreach (RssItem.MediaContent content in grp.MediaContents)
                    {
                        if (!useLink && isPossibleVideo(content.Url)) AddToPlaybackOption(video.PlaybackOptions, content);
                        if (string.IsNullOrEmpty(video.Length)) video.Length = GetDuration(content.Duration);
                    }
                }
            }

            // Append the length with the pubdate
            if (!string.IsNullOrEmpty(rssItem.PubDate))
            {
                if (!string.IsNullOrEmpty(video.Length)) video.Length += " | ";
                try
                {
                    video.Length += rssItem.PubDateParsed.ToString("g", OnlineVideoSettings.Instance.MediaPortalLocale);
                }
                catch
                {
                    video.Length += rssItem.PubDate;
                }
            }

            // if only one url found as playbackoptions but nothing set a video url -> set the one option directly as url
            if (video.PlaybackOptions.Count == 1 && string.IsNullOrEmpty(video.VideoUrl))
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                video.VideoUrl = enumer.Current.Value;
                video.PlaybackOptions = null;
            }

            return video;
        }

        static void AddToPlaybackOption(Dictionary<string, string> playbackOptions, RssItem.MediaContent content)
        {
            if (!playbackOptions.ContainsValue(content.Url))
                playbackOptions.Add(
                    string.Format("{0}x{1} ({2}) | {3}:// | {4}", 
                        content.Width, 
                        content.Height,
                        content.Bitrate != 0 ? 
                            content.Bitrate.ToString() + " kbps" : 
                            (content.FileSize != 0 ? (content.FileSize / 1024).ToString("N0") + " KB" : ""), 
                        new Uri(content.Url).Scheme, 
                        System.IO.Path.GetExtension(content.Url)), 
                    content.Url);
        }

        static string GetDuration(string duration)
        {
            if (!string.IsNullOrEmpty(duration))
            {
                uint seconds = 0;
                if (uint.TryParse(duration, out seconds)) return TimeSpan.FromSeconds(seconds).ToString();
                else return duration;
            }
            return "";
        }

        #region YouTube Helper

        static readonly int[] fmtOptionsQualitySorted = new int[] { 37, 22, 35, 18, 34, 5, 0, 17, 13 };
        static Regex swfJsonArgs = new Regex(@"(?:var\s)?(?:swfArgs|'SWF_ARGS')\s*(?:=|\:)\s(?<json>\{.+\})", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public void GetYouTubePlaybackOptions()
        {
            if (PlaybackOptions != null) return; // don't do this twice

            string videoId = VideoUrl;
            if (videoId.ToLower().Contains("youtube.com"))
            {
                // get an Id from the Url
                int p = videoId.LastIndexOf('/') + 1;
                int q = videoId.IndexOf('?', p);
                if (q < 0) videoId.IndexOf('&', p);
                if (q < 0) q = videoId.Length;
                videoId = videoId.Substring(p, q - p);
            }

            Dictionary<string, string> Items = new Dictionary<string, string>();
            try
            {
                string contents = Sites.SiteUtilBase.GetWebData(string.Format("http://youtube.com/get_video_info?video_id={0}", videoId));
                foreach (string s in contents.Split('&')) Items.Add(s.Split('=')[0], s.Split('=')[1]);
                if (Items["status"] == "fail")
                {
                    contents = Sites.SiteUtilBase.GetWebData(string.Format("http://www.youtube.com/watch?v={0}", videoId));
                    Match m = swfJsonArgs.Match(contents);
                    if (m.Success)
                    {
                        Items.Clear();
                        object data = Jayrock.Json.Conversion.JsonConvert.Import(m.Groups["json"].Value);
                        foreach (string z in (data as Jayrock.Json.JsonObject).Names)
                        {
                            Items.Add(z, (data as Jayrock.Json.JsonObject)[z].ToString());
                        }
                    }
                }
            }
            catch { }


            string Token = "";
            string[] FmtMap = null;

            if (Items.ContainsKey("token"))
                Token = Items["token"];
            if (Token == "" && Items.ContainsKey("t"))
                Token = Items["t"];
            if (Token != "")
            {
                if (Items.ContainsKey("fmt_map"))
                {
                    FmtMap = System.Web.HttpUtility.UrlDecode(Items["fmt_map"]).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    Array.Sort(FmtMap, new Comparison<string>(delegate(string a, string b)
                    {
                        int a_i = int.Parse(a.Substring(0, a.IndexOf("/")));
                        int b_i = int.Parse(b.Substring(0, b.IndexOf("/")));
                        int index_a = Array.IndexOf(fmtOptionsQualitySorted, a_i);
                        int index_b = Array.IndexOf(fmtOptionsQualitySorted, b_i);
                        return index_b.CompareTo(index_a);
                    }));
                }                
                PlaybackOptions = FmtMapToPlaybackOptions(FmtMap, Token, videoId);
                if (PlaybackOptions.Count == 0) // no or empty fmt_map -> add a default url
                {
                    PlaybackOptions.Add("", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&ext=.flv", videoId, Token));
                }
            }
        }

        static Dictionary<string, string> FmtMapToPlaybackOptions(string[] fmtMap, string token, string videoId)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (fmtMap != null && fmtMap.Length > 0)
            {
                foreach (string fmtValue in fmtMap)
                {
                    int fmtValueInt = int.Parse(fmtValue.Substring(0, fmtValue.IndexOf("/")));
                    switch (fmtValueInt)
                    {
                        case 0:
                        case 5:
                        case 34:
                            result.Add("320x240 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .flv", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "flv")); break;
                        case 13:
                        case 17:
                            result.Add("176x144 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "mp4")); break;
                        case 18:
                            result.Add("480x360 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "mp4")); break;
                        case 35:
                            result.Add("640x480 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .flv", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "flv")); break;
                        case 22:
                            result.Add("1280x720 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "mp4")); break;
                        case 37:
                            result.Add("1920x1080 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "mp4")); break;
                        default:
                            result.Add("Unknown | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .???", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}", videoId, token, fmtValueInt)); break;
                    }
                }
            }
            return result;
        }
               
        #endregion
    }    
}
