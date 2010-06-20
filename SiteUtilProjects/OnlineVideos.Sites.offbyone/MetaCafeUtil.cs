using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class MetaCafeUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to check if video is linked to youtube. Value of the group named yt should hold the youtube video id.")]
        protected string youtubeCheckRegEx;

        protected Regex regEx_youtubeCheckRegEx;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(youtubeCheckRegEx)) regEx_youtubeCheckRegEx = new Regex(youtubeCheckRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);            
        }

        public override string getUrl(VideoInfo video)
        {
            string dataPage = GetWebData(video.VideoUrl);
            Match matchYouTube = regEx_youtubeCheckRegEx.Match(dataPage);
            if (matchYouTube.Success)
            {
                string cachedUrl = video.VideoUrl;
                video.PlaybackOptions = null;
                video.VideoUrl = matchYouTube.Groups["yt"].Value;
                video.GetYouTubePlaybackOptions();
                video.VideoUrl = cachedUrl;
                if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
                {
                    return "";// if no match, return empty url -> error
                }
            }
            else
            {
                string result = base.getUrl(video);
                if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
                {
                    return "";// if no match, return empty url -> error
                }
                else
                {
                    var enumer = video.PlaybackOptions.GetEnumerator();
                    enumer.MoveNext();
                    video.PlaybackOptions[enumer.Current.Key] = enumer.Current.Value.Replace("\\", "");
                    
                }
            }
            var enumer2 = video.PlaybackOptions.GetEnumerator();
            enumer2.MoveNext();
            return enumer2.Current.Value;
        }
    }
}
