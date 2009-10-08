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
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class DailyMotionUtil : SiteUtilBase, ISearch
    {
        public override String getUrl(VideoInfo video)
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
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
            {
                VideoInfo video = new VideoInfo();
                video.Description = rssItem.Description;
                video.ImageUrl = rssItem.MediaThumbnails[0].Url;
                video.Title = rssItem.Title;
                video.Length = rssItem.PubDateParsed.ToString("g");
                video.VideoUrl = rssItem.Guid.Text;
                loVideoList.Add(video);
            }
            return loVideoList;            
        }

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories()
        {
            return new Dictionary<string, string>();
        }
        
        public List<VideoInfo> Search(string query)
        {            
            string url = string.Format(Settings.SearchUrl, query);
            return getVideoList(new RssLink() { Url = url });
        }

        public List<VideoInfo> Search(string query, string category)
        {
            return Search(query);
        }

        #endregion
    }
}
