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
        static Regex idRegex = new Regex("#(.*)");

		public override string getUrl(OnlineVideos.VideoInfo video, OnlineVideos.SiteSettings foSite)
		{
            return String.Format("http://www.msnbc.msn.com/default.cdnx/id/{0}/displaymode/1157/?t=.flv", video.VideoUrl);
		}

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<RssItem> loRssItemList = getRssDataItems(((RssLink)category).Url);
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            VideoInfo video;            
            foreach (RssItem rssItem in loRssItemList)
            {
                video = new VideoInfo();
                video.Description = rssItem.description;
                video.ImageUrl = rssItem.contentList[0].url;
                video.Title = rssItem.title.Replace("Video: ", "");
                video.Length = rssItem.contentList[0].duration;
                video.VideoUrl = idRegex.Match(rssItem.guid).Groups[1].Value;
                loVideoList.Add(video);
            }
            return loVideoList;
        }
	}
}
