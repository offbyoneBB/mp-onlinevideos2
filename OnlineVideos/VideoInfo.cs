using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace OnlineVideos
{
    public class VideoInfo : SearchResultItem
    {
        public string Title { get; set; }
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
        public virtual string CreateMatroskaXmlTag(string niceTitle, ITrackingInfo trackingInfo)
        {
            var tags = new XElement("Tags");
            if (trackingInfo != null && trackingInfo.Season != 0 && trackingInfo.VideoKind == VideoKind.TvSeries)
            {
                //show:
                tags.Add(
                    new XElement("Tag",
                        new XElement("Targets",
                            new XElement("TargetTypeValue", 70)),
                        new XElement("Simple",
                            new XElement("Name", "TITLE"),
                            new XElement("String", trackingInfo.Title))
                        ),
                //season:
                    new XElement("Tag",
                        new XElement("Targets",
                            new XElement("TargetTypeValue", 60)),
                        new XElement("Simple",
                            new XElement("Name", "PART_NUMBER"),
                            new XElement("String", trackingInfo.Season))
                        )
                );
            };

            tags.Add(
            new XElement("Tag",
                new XElement("Targets",
                    new XElement("TargetTypeValue", 50)),
                new XElement("Simple",
                    new XElement("Name", "TITLE"),
                    new XElement("String",
                      trackingInfo != null && trackingInfo.VideoKind == VideoKind.Movie && !String.IsNullOrEmpty(trackingInfo.Title) ? trackingInfo.Title : niceTitle)
                      ),
                new XElement("Simple",
                    new XElement("Name", "DESCRIPTION"),
                    new XElement("String", Description)),
                trackingInfo != null && trackingInfo.VideoKind != VideoKind.Other ?
                new XElement("Simple",
                    new XElement("Name", "CONTENT_TYPE"),
                    new XElement("String", trackingInfo.VideoKind))
                : null,
                new XElement("Simple",
                    new XElement("Name", "DATE_RELEASED"),
                    new XElement("String", trackingInfo != null && trackingInfo.Year != 0 ? trackingInfo.Year.ToString() : Airdate)),
                trackingInfo != null && trackingInfo.Episode != 0 ?
                new XElement("Simple",
                    new XElement("Name", "PART_NUMBER"),
                    new XElement("String", trackingInfo.Episode))
                : null,
                trackingInfo != null && !String.IsNullOrEmpty(trackingInfo.ID_IMDB) ?
                new XElement("Simple",
                    new XElement("Name", "IMDB"),
                    new XElement("String", trackingInfo.ID_IMDB))
                : null
            ));

            return new XDocument(new XDeclaration("1.0", "utf-8", "true"),tags).ToString();
        }

        public virtual Dictionary<string, string> GetExtendedProperties()
        {
            IVideoDetails details = Other as IVideoDetails;
            return details == null ? null : details.GetExtendedProperties();
        }

        public VideoInfo CloneForPlaylist(string videoUrl, bool withPlaybackOptions)
        {
            VideoInfo newVideoInfo = (VideoInfo)MemberwiseClone(false);
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

        /// <summary>
        /// Returns the preferred url from the PlaybackOptions
        /// Also deletes PlaybackOptions if not needed
        /// </summary>
        /// <param name="first">true if first one is preferred, false returns the last one</param>
        /// <returns></returns>
        public string GetPreferredUrl(bool first)
        {
            if (PlaybackOptions == null || PlaybackOptions.Count == 0) return VideoUrl;
            else
                if (PlaybackOptions.Count == 1)
                {
                    string resultUrl = PlaybackOptions.First().Value;
                    PlaybackOptions = null;// only one url found, PlaybackOptions not needed
                    if (String.IsNullOrEmpty(VideoUrl))
                        VideoUrl = resultUrl;
                    return resultUrl;
                }
                else
                {
                    if (first)
                        return PlaybackOptions.First().Value;
                    else
                        return PlaybackOptions.Last().Value;
                }

        }
    }
}
