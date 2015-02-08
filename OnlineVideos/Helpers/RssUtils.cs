using RssToolkit.Rss;
using System;
using System.Collections.Generic;

namespace OnlineVideos.Helpers
{
    public static class RssUtils
    {
        public static VideoInfo VideoInfoFromRssItem(RssItem rssItem, bool useLink, System.Predicate<string> isPossibleVideo)
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

            if (!string.IsNullOrEmpty(rssItem.Blip_Runtime)) video.Length = Helpers.TimeUtils.TimeFromSeconds(rssItem.Blip_Runtime);
            if (string.IsNullOrEmpty(video.Length)) video.Length = Helpers.TimeUtils.TimeFromSeconds(rssItem.iT_Duration);

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
                    if (string.IsNullOrEmpty(video.Length)) video.Length = Helpers.TimeUtils.TimeFromSeconds(content.Duration);
                }
            }
            if (rssItem.MediaGroups.Count > 0) // videos might be wrapped in groups, try to get the first MediaContent
            {
                foreach (RssItem.MediaGroup grp in rssItem.MediaGroups)
                {
                    foreach (RssItem.MediaContent content in grp.MediaContents)
                    {
                        if (!useLink && content.Url != null && isPossibleVideo(content.Url.Trim())) AddToPlaybackOption(video.PlaybackOptions, content);
                        if (string.IsNullOrEmpty(video.Length)) video.Length = Helpers.TimeUtils.TimeFromSeconds(content.Duration);
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
    }
}
