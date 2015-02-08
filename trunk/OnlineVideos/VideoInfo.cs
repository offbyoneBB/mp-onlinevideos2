using System;
using System.Collections.Generic;
using System.Xml.Linq;
using RssToolkit.Rss;
using OnlineVideos.Sites;

namespace OnlineVideos
{
    public enum VideoKind { Other, TvSeries, Movie, MovieTrailer, GameTrailer, MusicVideo, News }

    public class VideoInfo : SearchResultItem
    {
        public string Title { get; set; }
        /// <summary>Used as label for the clips retrieved by <see cref="IChoice.GetVideoChoices"/></summary>
        public string Title2 { get; set; }
        public string VideoUrl { get; set; }
        public string SubtitleUrl { get; set; }
        public string SubtitleText { get; set; }
        public string Length { get; set; }
        public string Airdate { get; set; }
        public string StartTime { get; set; }
		public Dictionary<string, string> PlaybackOptions;

        /// <summary>If the SiteUtil for this VideoInfo implements <see cref="Sites.IChoice"/> setting this to true will show the details view (default), false will play the video</summary>
        public bool HasDetails { get; set; }

        public VideoInfo()
        {
            Title = string.Empty;
            Title2 = string.Empty;
            Description = string.Empty;
            VideoUrl = string.Empty;
            Thumb = string.Empty;
            Length = string.Empty;
            StartTime = string.Empty;
            HasDetails = true;
        }

        public void CleanDescriptionAndTitle()
        {
            Description = Utils.PlainTextFromHtml(Description);
            Title = Utils.PlainTextFromHtml(Title);
        }

        public override string ToString()
        {
			return string.Format("Title:{0}\r\nDesc:{1}\r\nVidUrl:{2}\r\nImgUrl:{3}\r\nLength:{4}\r\nAirdate:{5}", Title, Description, VideoUrl, Thumb, Length, Airdate);
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
		/// Create a Matroska Xml Tag (http://www.matroska.org/technical/specs/tagging/index.html) for the Video. With Title, Description and Airdate.
		/// </summary>
		/// <returns>Utf-8 encoded xml</returns>
		public virtual string CreateMatroskaXmlTag(string niceTitle)
		{
			return new XDocument(new XDeclaration("1.0", "utf-8", "true"),
			new XElement("Tags",
				new XElement("Tag",
				new XElement("Targets",
					new XElement("TargetTypeValue", 50)),
				new XElement("Simple",
					new XElement("Name", "TITLE"),
					new XElement("String", niceTitle)),
				new XElement("Simple",
					new XElement("Name", "DESCRIPTION"),
					new XElement("String", Description)),
				new XElement("Simple",
					new XElement("Name", "DATE_RELEASED"),
					new XElement("String", Airdate))
			))).ToString();
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
                video.Thumb = rssItem.GT_Image;
            }
            else if (rssItem.MediaThumbnails.Count > 0)
            {
                video.Thumb = rssItem.MediaThumbnails[0].Url;
            }
            else if (rssItem.MediaContents.Count > 0 && rssItem.MediaContents[0].MediaThumbnails.Count > 0)
            {
                video.Thumb = rssItem.MediaContents[0].MediaThumbnails[0].Url;
            }
            else if (rssItem.MediaGroups.Count > 0 && rssItem.MediaGroups[0].MediaThumbnails.Count > 0)
            {
                video.Thumb = rssItem.MediaGroups[0].MediaThumbnails[0].Url;
            }
            else if (rssItem.Enclosure != null && rssItem.Enclosure.Type != null && rssItem.Enclosure.Type.ToLower().StartsWith("image"))
            {
                video.Thumb = rssItem.Enclosure.Url;
            }

			if (!string.IsNullOrEmpty(rssItem.Blip_Runtime)) video.Length = Utils.FormatDuration(rssItem.Blip_Runtime);
            if (string.IsNullOrEmpty(video.Length)) video.Length = Utils.FormatDuration(rssItem.iT_Duration);

            // if we are forced to use the Link of the RssItem, just set the video link
            if (useLink) video.VideoUrl = rssItem.Link;

            //get the video and the length
            if (rssItem.Enclosure != null && rssItem.Enclosure.Url != null && (rssItem.Enclosure.Type == null || !rssItem.Enclosure.Type.ToLower().StartsWith("image")) && (isPossibleVideo(rssItem.Enclosure.Url.Trim()) || useLink))
            {
                video.VideoUrl = useLink ? rssItem.Link : rssItem.Enclosure.Url.Trim();

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
                    if (!useLink && content.Url != null && isPossibleVideo(content.Url.Trim())) AddToPlaybackOption(video.PlaybackOptions, content);
                    if (string.IsNullOrEmpty(video.Length)) video.Length = Utils.FormatDuration(content.Duration);
                }
            }
            if (rssItem.MediaGroups.Count > 0) // videos might be wrapped in groups, try to get the first MediaContent
            {
                foreach (RssItem.MediaGroup grp in rssItem.MediaGroups)
                {
                    foreach (RssItem.MediaContent content in grp.MediaContents)
                    {
                        if (!useLink && content.Url != null && isPossibleVideo(content.Url.Trim())) AddToPlaybackOption(video.PlaybackOptions, content);
                        if (string.IsNullOrEmpty(video.Length)) video.Length = Utils.FormatDuration(content.Duration);
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

            if (!playbackOptions.ContainsValue(content.Url.Trim()))
            {
                string baseInfo = string.Format("{0}({1}) | {2}:// | {3}",
                        content.Width > 0 || content.Height > 0 ? 
                            string.Format("{0}x{1} ", content.Width, content.Height) : 
                            "",
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
                playbackOptions.Add(info, content.Url.Trim());
            }
        }
        
        public VideoInfo CloneForPlayList(string videoUrl, bool withPlaybackOptions)
        {
            VideoInfo newVideoInfo = MemberwiseClone(false) as VideoInfo;
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
