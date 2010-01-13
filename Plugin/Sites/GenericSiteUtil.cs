using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of GenericSiteUtil.
    /// </summary>
    public class GenericSiteUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("FormatString for the Url used for getting the results of a search.")]
        string searchUrl = "";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the playlist data for the playback url.")]
        string fileUrlRegEx = "";

        [Category("OnlineVideosConfiguration"), Description("FormatString that will take the groups from the fileUrlRegEx as parameters.")]
        string fileUrlFormat = "";

        Regex regEx_FileUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(fileUrlRegEx)) regEx_FileUrl = new Regex(fileUrlRegEx,
                                          RegexOptions.IgnoreCase
                                        | RegexOptions.CultureInvariant
                                        | RegexOptions.Multiline
                                        | RegexOptions.Singleline
                                        | RegexOptions.IgnorePatternWhitespace
                                        | RegexOptions.Compiled);
        }

        public override String getUrl(VideoInfo video)
        {
            string resultUrl = "";

            if (regEx_FileUrl == null)
            {
                resultUrl = GetRedirectedUrl(video.VideoUrl);
                if (resultUrl.ToLower().EndsWith(".asx"))
                {
                    resultUrl = ParseASX(resultUrl)[0];
                }
            }
            else
            {
                string html = GetWebData(video.VideoUrl);
                if (!string.IsNullOrEmpty(html))
                {
                    Match urlField = regEx_FileUrl.Match(html);
                    if (urlField.Success)
                    {
                        string[] regExGroupValues = new string[urlField.Groups.Count - 1];
                        for (int i = 1; i < urlField.Groups.Count; i++) regExGroupValues[i - 1] = urlField.Groups[i].Value;
                        resultUrl = HttpUtility.UrlDecode(string.Format(fileUrlFormat, regExGroupValues));
                    }
                }
            }

            return resultUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            VideoInfo video;
            if (category is RssLink)
            {
                foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
                {
                    video = new VideoInfo();

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
                    
                    // if there is a regex for parsing html behind the link, just set the video link
                    if (regEx_FileUrl != null) video.VideoUrl = rssItem.Link;
                    
                    //get the video and the length
                    if (rssItem.Enclosure != null && (rssItem.Enclosure.Type == null || !rssItem.Enclosure.Type.ToLower().StartsWith("image")) && (isPossibleVideo(rssItem.Enclosure.Url) || regEx_FileUrl != null))
                    {
                        video.VideoUrl = regEx_FileUrl != null ? rssItem.Link : rssItem.Enclosure.Url;

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
                            if (isPossibleVideo(content.Url) || regEx_FileUrl != null)
                            {
                                video.VideoUrl = regEx_FileUrl != null ? rssItem.Link : content.Url;
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
                                if (isPossibleVideo(content.Url) || regEx_FileUrl != null)
                                {
                                    video.VideoUrl = regEx_FileUrl != null ? rssItem.Link : content.Url;
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
                    
                    // only if a video url was set, add this Video to the list
                    if (!string.IsNullOrEmpty(video.VideoUrl))
                    {
                        loVideoList.Add(video);
                    }
                }
            }
            else if (category is Group)
            {
                foreach (Channel channel in ((Group)category).Channels)
                {
                    video = new VideoInfo();
                    video.Title = channel.StreamName;
                    video.VideoUrl = channel.Url;
                    video.ImageUrl = channel.Thumb;
                    loVideoList.Add(video);
                }
            }
            return loVideoList;
        }

        #region Search

        public override bool CanSearch { get { return !string.IsNullOrEmpty(searchUrl); } }

        public override List<VideoInfo> Search(string query)
        {
            string url = string.Format(searchUrl, query);
            return getVideoList(new RssLink() { Url = url });
        }

        #endregion
    }
}
