using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using MediaPortal.GUI.Library;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// Description of CnnUtil.
	/// </summary>
	public class MsnbcUtil: SiteUtilBase
	{
        static Regex idRegex = new Regex(@"vPlayer\('(\d{5,10})'", RegexOptions.Compiled);

		public override string getUrl(OnlineVideos.VideoInfo video)
		{
            if (video.VideoUrl.Contains("/vp/"))
            {
                return String.Format("http://www.msnbc.msn.com/id/{0}/displaymode/1157/?t=.flv", video.VideoUrl.Substring(video.VideoUrl.LastIndexOf("#") + 1));
            }
            else
            {
                string data = GetWebData(video.VideoUrl);
                Match m = idRegex.Match(data);
                if (m.Success)
                    return String.Format("http://www.msnbc.msn.com/id/{0}/displaymode/1157/?t=.flv", m.Groups[1].Value);
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
                video.Length = rssItem.PubDateParsed.ToString("g");
                video.VideoUrl = rssItem.Guid.Text;
                loVideoList.Add(video);
            }
            return loVideoList;
        }
	}
}
