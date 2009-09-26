using System;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;
using System.Threading;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class DailyMotionUtil : SiteUtilBase, ISearch
    {
        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {
            String lsUrl = "";        	
        	String lsHtml = GetWebData(video.VideoUrl);
        	Match loMatch = Regex.Match(lsHtml,"addVariable\\(\"video\",\\s\"([^\"]*)");
        	if(loMatch.Success){
                String lsTemp = loMatch.Groups[1].Value;
                lsTemp = System.Web.HttpUtility.UrlDecode(lsTemp);
                loMatch = Regex.Match(lsTemp, "([^@]*)@@spark");
                if(loMatch.Success){
                    lsUrl = loMatch.Groups[1].Value; 
        		    lsUrl += "&txe=.flv";
                    if (!lsUrl.StartsWith("http://")) lsUrl = "http://www.dailymotion.com" + lsUrl;
                    return lsUrl;
                }
        	}
        		Log.Info("Dailymotion video not found. Site could have changed layout.");
        	    return "";
            
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
				video.ImageUrl = rssItem.mediaThumbnail;
				video.Title = rssItem.title;
				video.Length = rssItem.contentList[0].duration;
				video.VideoUrl = rssItem.guid;
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
