using System;
using MediaPortal.GUI.Library;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using MediaPortal.Player;
using System.Collections.Generic;
using MediaPortal.GUI.View ;
using MediaPortal.Dialogs;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;
using System.Threading;

namespace OnlineVideos.Sites
{
	public class BreakUtil : SiteUtilBase, ISearch
	{
        public override String getUrl(VideoInfo video, SiteSettings foSite)
		{	
            String lsUrl = "";
			String lsHtml = GetWebData(video.VideoUrl);			
			Regex loPathRegex = new Regex("sGlobalFileName='([^']*)';[^;]*;.+sGlobalContentFilePath='([^']*)'");
            Regex loUrlRegex = new Regex(@"var videoPath = ""([^""]+)""");
			Match urlField = loUrlRegex.Match(lsHtml);
			if(urlField.Success){			
				lsUrl = urlField.Groups[1].Value;
				Match loMatch = loPathRegex.Match(lsHtml);
				if(loMatch.Success){
					String lsFileName = loMatch.Groups[1].Value;
					String lsPathName = loMatch.Groups[2].Value;
					lsUrl = lsUrl+lsPathName+"/"+lsFileName+".flv";
					Log.Info("break flv url = {0}",lsUrl);
				}
			}
			return lsUrl;
		}

        public override List<VideoInfo> getVideoList(Category category)
        {
            return getVideoListFromRss(((RssLink)category).Url);
        }

        List<VideoInfo> getVideoListFromRss(string url)
        {
            List<RssItem> loRssItemList = getRssDataItems(url);
			List<VideoInfo> loVideoList = new List<VideoInfo>();
			VideoInfo video;
			foreach(RssItem rssItem in loRssItemList){
				video = new VideoInfo();
				video.Description = rssItem.description;
				video.ImageUrl = rssItem.enclosure;
				video.Title = rssItem.title;
				//foreach(MediaContent content in rssItem.contentList){
				//	if(content.type.Contains("flv")){
				video.VideoUrl = rssItem.link;
				//		break;
				//	}
				loVideoList.Add(video);
			}
			return loVideoList;
		}

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories(IList<Category> configuredCategories)
        {
            return new Dictionary<string, string>();
        }
        
        public List<VideoInfo> Search(string searchUrl, string query)
        {            
            string url = string.Format(searchUrl, query);
            return getVideoListFromRss(url);
        }

        public List<VideoInfo> Search(string searchUrl, string query, string category)
        {
            return Search(searchUrl, query);
        }

        #endregion
    }
}
