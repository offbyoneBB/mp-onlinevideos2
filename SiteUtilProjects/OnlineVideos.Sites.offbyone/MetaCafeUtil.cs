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

        public override string GetVideoUrl(VideoInfo video)
        {
            string dataPage = GetWebData(video.VideoUrl);
            Match matchYouTube = regEx_youtubeCheckRegEx != null ? regEx_youtubeCheckRegEx.Match(dataPage) : Match.Empty;
            if (matchYouTube.Success)
            {
                video.PlaybackOptions = Hoster.HosterFactory.GetHoster("Youtube").GetPlaybackOptions(matchYouTube.Groups["yt"].Value);
                return (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0) ? "" : video.PlaybackOptions.First().Value;
            }
            else
            {
                string result = base.GetVideoUrl(video);
                if (!string.IsNullOrEmpty(result))
                {
                    result = result.Replace("\\", "");
                    return result;
                }
                else
                {
                    Match m = Regex.Match(dataPage, "&errorTitle=(?<error>.*?)&", RegexOptions.Singleline | RegexOptions.Multiline);
                    if (m.Success)
                    {
                        string error = HttpUtility.UrlDecode(m.Groups["error"].Value);
                        if (!string.IsNullOrEmpty(error)) throw new OnlineVideosException(error);
                    }
                    string id = Regex.Match(dataPage, @"id=""flashVars""\s+name=""flashvars""\s+value=""([^""]+)""").Groups[1].Value;
					string mediaData = System.Web.HttpUtility.ParseQueryString(id)["mediaData"];
					var json = Newtonsoft.Json.Linq.JObject.Parse(mediaData);
					string url = json.First.First["mediaURL"].ToString();
					List<string> parameters = new List<string>();
					foreach (var item in json.First.First["access"])
					{
						parameters.Add(string.Format("{0}={1}", item["key"].ToString(), item["value"].ToString()));
					}
					if (parameters.Count > 0)
					{
						url += "?" + string.Join("&", parameters.ToArray());
					}
					return url;
                }
            }
        }
    }
}
