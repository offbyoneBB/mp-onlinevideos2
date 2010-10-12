using System;
using System.Linq;
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
                video.PlaybackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(matchYouTube.Groups["yt"].Value);
                return (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0) ? "" : video.PlaybackOptions.First().Value;
            }
            else
            {
                string result = base.getUrl(video);
                if (!string.IsNullOrEmpty(result)) result = result.Replace("\\", "");
                return result;
            }
        }
    }
}
