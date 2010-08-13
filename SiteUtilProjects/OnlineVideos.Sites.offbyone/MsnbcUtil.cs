using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
	/// <summary>
    /// Description of MsnbcUtil.
	/// </summary>
	public class MsnbcUtil: SiteUtilBase
	{
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the video id from an html page.")]
        string idRegEx = @"vPlayer\('(\d{5,10})'";
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the video url of an item that was found in the rss.")]
        string videoUrlFormatString = "http://www.msnbc.msn.com/id/{0}/displaymode/1157/?t=.flv";

        Regex regEx_Id;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Id = new Regex(idRegEx, RegexOptions.Compiled);
        }

		public override string getUrl(OnlineVideos.VideoInfo video)
		{
            if (video.VideoUrl.Contains("/vp/"))
            {
                return String.Format(videoUrlFormatString, video.VideoUrl.Substring(video.VideoUrl.LastIndexOf("#") + 1));
            }
            else
            {
                string data = GetWebData(video.VideoUrl);
                Match m = regEx_Id.Match(data);
                if (m.Success)
                    return String.Format(videoUrlFormatString, m.Groups[1].Value);
                else
                    return "";
            }
		}

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();                
            foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
            {
                if (rssItem.MediaContents.Count == 0) continue;
                if (rssItem.MediaContents[0].Medium != "video") continue;

                VideoInfo video = new VideoInfo();
                video.Description = rssItem.Description;
                video.ImageUrl = rssItem.MediaContents[0].Url;
                video.Title = rssItem.Title.Replace("Video: ", "");
                video.Length = rssItem.PubDateParsed.ToString("g", OnlineVideoSettings.Instance.Locale);
                video.VideoUrl = rssItem.Guid.Text;
                loVideoList.Add(video);
            }
            return loVideoList;
        }
	}
}
