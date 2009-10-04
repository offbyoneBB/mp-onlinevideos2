using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using MediaPortal.GUI.Library;

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
            List<RssItem> loRssItemList = getRssDataItems(((RssLink)category).Url);
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            VideoInfo video;            
            foreach (RssItem rssItem in loRssItemList)
            {
                if (rssItem.contentList.Count == 0) continue;
                if (rssItem.contentList[0].medium != "video") continue;

                video = new VideoInfo();
                video.Description = rssItem.description;
                video.ImageUrl = rssItem.contentList[0].url;
                video.Title = rssItem.title.Replace("Video: ", "");
                video.Length = rssItem.contentList[0].duration;
                if (string.IsNullOrEmpty(video.Length)) video.Length = rssItem.pubDate;
                video.VideoUrl = rssItem.guid;
                loVideoList.Add(video);
            }
            return loVideoList;
        }
	}
}
