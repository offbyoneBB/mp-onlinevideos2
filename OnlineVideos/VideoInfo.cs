using OnlineVideos.Sites;
using System.Collections.Generic;
using System.Xml.Linq;

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
            Description = Helpers.StringUtils.PlainTextFromHtml(Description);
            Title = Helpers.StringUtils.PlainTextFromHtml(Title);
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

        public virtual Dictionary<string, string> GetExtendedProperties()
        {
            IVideoDetails details = Other as IVideoDetails;
            return details == null ? null : details.GetExtendedProperties();
        }
        
        public VideoInfo CloneForPlaylist(string videoUrl, bool withPlaybackOptions)
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
