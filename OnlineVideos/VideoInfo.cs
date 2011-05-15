using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using OnlineVideos.Sites;
using RssToolkit.Rss;

namespace OnlineVideos
{
    public class VideoInfo: System.ComponentModel.INotifyPropertyChanged
    {
        public string Title { get; set; }
        /// <summary>Used as label for the clips retrieved by <see cref="IChoice.getVideoChoices"/></summary>
        public string Title2 { get; set; }
        public string Description { get; set; }
        public string VideoUrl { get; set; }
        public string ImageUrl { get; set; }
        public string SubtitleUrl { get; set; }
        /// <summary>optional property is used by the <see cref="ImageDownloader"/> to resize the thumbnail after downloading from <see cref="ImageUrl"/> to a given aspect ratio (width/height).</summary>
        public float? ImageForcedAspectRatio { get; set; }
        public string Length { get; set; }
        public string Airdate { get; set; }
        public string StartTime { get; set; }
        public object Other { get; set; }
        public Dictionary<string, string> PlaybackOptions;

        /// <summary>This property is only used by the <see cref="FavoriteUtil"/> to store the Name of the Site where this Video came from.</summary>
        public string SiteName { get; set; }
        /// <summary>This property is only used by the <see cref="FavoriteUtil"/> to store the Id of Video, so it can be deleted from the DB.</summary>
        public int Id { get; set; }

        /// <summary>If the SiteUtil for this VideoInfo implements <see cref="IChoice"/> setting this to true will show the details view (default), false will play the video</summary>
        public bool HasDetails { get; set; }

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
            StartTime = string.Empty;
            SiteName = string.Empty;
            HasDetails = true;
        }

        public void CleanDescriptionAndTitle()
        {
            Description = Utils.PlainTextFromHtml(Description);
            Title = Utils.PlainTextFromHtml(Title);
        }

        public override string ToString()
        {
            return string.Format("Title:{0}\nDesc:{1}\nVidUrl:{2}\nImgUrl:{3}\nLength:{4}\nAirdate:{5}", Title, Description, VideoUrl, ImageUrl, Length, Airdate);
        }

        /// <summary>
        /// Can be overriden to further resolve the urls of a playbackoption.
        /// By default it only returns the url for the option given as parameter.
        /// </summary>
        /// <param name="option">key from the <see cref="PlaybackOptions"/> to get a playback url for</param>
        /// <returns>url that points to the file that can be played</returns>
        public virtual string GetPlaybackOptionUrl(string option)
        {
            return PlaybackOptions[option];
        }

        /// <summary>
        /// Example: startTime = 02:34:25.00 should result in 9265 seconds
        /// </summary>
        /// <returns></returns>
        public double GetSecondsFromStartTime()
        {
            try
            {
                double hours = 0.0d;
                double minutes = 0.0d;
                double seconds = 0.0d;

                double.TryParse(StartTime.Substring(0, 2), out hours);
                double.TryParse(StartTime.Substring(3, 2), out minutes);
                double.TryParse(StartTime.Substring(6, 2), out seconds);

                seconds += (((hours * 60) + minutes) * 60);

                return seconds;
            }
            catch (Exception ex)
            {
                Log.Warn("Error getting seconds from StartTime ({0}): {1}", StartTime, ex.Message);
                return 0.0d;
            }
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

            // PubDate in localized form if possible
            if (!string.IsNullOrEmpty(rssItem.PubDate))
            {
                try
                {
                    video.Airdate = rssItem.PubDateParsed.ToString("g", OnlineVideoSettings.Instance.Locale);
                }
                catch
                {
                    video.Airdate = rssItem.PubDate;
                }
            }

            // if no VideoUrl but PlaybackOptions are set -> put the first option as VideoUrl
            if (string.IsNullOrEmpty(video.VideoUrl) && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                video.VideoUrl = enumer.Current.Value;
                if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null; // no need for options with only one url
            }

            if (string.IsNullOrEmpty(video.VideoUrl) && isPossibleVideo(rssItem.Link) && !useLink) video.VideoUrl = rssItem.Link;

            return video;
        }

        static void AddToPlaybackOption(Dictionary<string, string> playbackOptions, RssItem.MediaContent content)
        {
            int sizeInBytes = 0;
            if (!string.IsNullOrEmpty(content.FileSize))
            {
                if (!int.TryParse(content.FileSize, out sizeInBytes))
                {
                    // with . inside the string -> already in KB
                    if (int.TryParse(content.FileSize, System.Globalization.NumberStyles.AllowThousands, null, out sizeInBytes)) sizeInBytes *= 1000;
                }
            }

            if (!playbackOptions.ContainsValue(content.Url))
            {
                string baseInfo = string.Format("{0}x{1} ({2}) | {3}:// | {4}",
                        content.Width,
                        content.Height,
                        content.Bitrate != 0 ?
                            content.Bitrate.ToString() + " kbps" :
                            (sizeInBytes != 0 ? (sizeInBytes / 1024).ToString("N0") + " KB" : ""),
                        new Uri(content.Url).Scheme,
                        System.IO.Path.GetExtension(content.Url));
                string info = baseInfo;
                int i = 1;
                while (playbackOptions.ContainsKey(info))
                {
                    info = string.Format("{0} ({1})", baseInfo, i.ToString().PadLeft(2, ' '));
                }
                playbackOptions.Add(info, content.Url);
            }
        }

        public static string GetDuration(string duration)
        {
            if (!string.IsNullOrEmpty(duration))
            {
                double seconds;
                if (double.TryParse(duration, System.Globalization.NumberStyles.None | System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"), out seconds))
                {
                    return new DateTime(TimeSpan.FromSeconds(seconds).Ticks).ToString("HH:mm:ss");
                }
                else return duration;
            }
            return "";
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public VideoInfo CloneForPlayList(string videoUrl, bool withPlaybackOptions)
        {
            VideoInfo newVideoInfo = this.MemberwiseClone() as VideoInfo;
            if (withPlaybackOptions)
            {
                if (PlaybackOptions != null) newVideoInfo.PlaybackOptions = new Dictionary<string, string>(PlaybackOptions);
            }
            else
            {
                newVideoInfo.PlaybackOptions = null;
            }
            newVideoInfo.VideoUrl = videoUrl;
            return newVideoInfo;
        } 
    }
}
