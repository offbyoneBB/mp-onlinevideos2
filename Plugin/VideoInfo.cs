using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RssToolkit.Rss;

namespace OnlineVideos
{
    public class VideoInfo
    {
        public string Title;
        public string Title2;
        public string Description = "";
        public string VideoUrl = "";
        public string ImageUrl = "";
        public string Length;
        public string Tags = "";
        public string StartTime = "";
        public object Other;
        public Dictionary<string, string> PlaybackOptions;
        
        /// <summary>This field is only used by the <see cref="FavoriteUtil"/> to store the Name of the Site where this Video came from.</summary>
        public string SiteName = "";

        public void CleanDescription()
        {
            if (!string.IsNullOrEmpty(Description))
            {
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

                // decode HTML escape character
                Description = System.Web.HttpUtility.HtmlDecode(Description);
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
            VideoInfo video = new VideoInfo();

            // Title - prefer from MediaTitle tag if available
            if (!String.IsNullOrEmpty(rssItem.MediaTitle)) video.Title = rssItem.MediaTitle;
            else video.Title = rssItem.Title;

            // Description - prefer MediaDescription tag if available
            if (!String.IsNullOrEmpty(rssItem.MediaDescription)) video.Description = rssItem.MediaDescription;
            else video.Description = rssItem.Description;

            // Try to find a thumbnail
            if (rssItem.MediaThumbnails.Count > 0)
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

            // if we are forced to use the Link of the RssItem, just set the video link
            if (useLink) video.VideoUrl = rssItem.Link;

            //get the video and the length
            if (rssItem.Enclosure != null && (rssItem.Enclosure.Type == null || !rssItem.Enclosure.Type.ToLower().StartsWith("image")) && (isPossibleVideo(rssItem.Enclosure.Url) || useLink))
            {
                video.VideoUrl = useLink ? rssItem.Link : rssItem.Enclosure.Url;

                if (!string.IsNullOrEmpty(rssItem.Enclosure.Length))
                {
                    int bytesOrSeconds = 0;
                    if (int.TryParse(rssItem.Enclosure.Length, out bytesOrSeconds))
                    {
                        if (bytesOrSeconds > 18000) // won't be longer than 5 hours if Length is guessed as seconds, so it's bytes
                            video.Length = (bytesOrSeconds / 1024).ToString("N0") + " KB";
                        else
                            video.Length = rssItem.Enclosure.Length + " sec";
                    }
                    else
                    {
                        video.Length = rssItem.Enclosure.Length;
                    }
                }
            }
            else if (rssItem.MediaContents.Count > 0) // try to get the first MediaContent
            {
                foreach (RssItem.MediaContent content in rssItem.MediaContents)
                {
                    if (isPossibleVideo(content.Url) || useLink)
                    {
                        video.VideoUrl = useLink ? rssItem.Link : content.Url;
                        uint seconds = 0;
                        if (uint.TryParse(content.Duration, out seconds)) video.Length = TimeSpan.FromSeconds(seconds).ToString();
                        else video.Length = content.Duration;
                        break;
                    }
                }
            }
            else if (rssItem.MediaGroups.Count > 0) // videos might be wrapped in groups, try to get the first MediaContent
            {
                foreach (RssItem.MediaGroup grp in rssItem.MediaGroups)
                {
                    foreach (RssItem.MediaContent content in grp.MediaContents)
                    {
                        if (isPossibleVideo(content.Url) || useLink)
                        {
                            video.VideoUrl = useLink ? rssItem.Link : content.Url;
                            uint seconds = 0;
                            if (uint.TryParse(content.Duration, out seconds)) video.Length = TimeSpan.FromSeconds(seconds).ToString();
                            else video.Length = content.Duration;
                            break;
                        }
                    }
                }
            }

            // Append the length with the pubdate
            if (!string.IsNullOrEmpty(rssItem.PubDate))
            {
                if (!string.IsNullOrEmpty(video.Length)) video.Length += " | ";
                try
                {
                    video.Length += rssItem.PubDateParsed.ToString("g");
                }
                catch
                {
                    video.Length += rssItem.PubDate;
                }
            }

            return video;
        }
    }    
}
