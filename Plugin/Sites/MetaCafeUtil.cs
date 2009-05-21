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
using System.Web;

namespace OnlineVideos.Sites
{
    public class MetaCafeUtil : SiteUtilBase, ISearch
    {
        static Regex loRegex = new Regex(@"mediaURL=(.*)&PostRoll",
                                          RegexOptions.IgnoreCase
                                        | RegexOptions.CultureInvariant
                                        | RegexOptions.IgnorePatternWhitespace
                                        | RegexOptions.Compiled);

        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {                        
            WebClient client = new WebClient();
            //String lsHtml = client.DownloadString(String.Format("http://www.metacafe.com/fplayer.php?itemID={0}&fs=n&t=embedded", fsId));
            String lsHtml = client.DownloadString(video.VideoUrl);            
            String lsUrl = loRegex.Match(lsHtml).Groups[1].Value;         
            lsUrl = HttpUtility.UrlDecode(lsUrl);
            lsUrl = lsUrl.Replace("gdaKey", "__gda__");
            lsUrl = lsUrl.Replace("&", "?");
            //lsUrl = lsUrl + "&txe=.flv";
            Log.Info("MetaCafe video flv url = {0}",lsUrl);
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
				video.ImageUrl = rssItem.mediaThumbnail;
				video.Title = rssItem.title;
				video.Length = rssItem.contentList[0].duration;
				//video.VideoUrl = Regex.Match(rssItem.link,@"watch/([\d]*)").Groups[1].Value;
                video.VideoUrl = rssItem.link;
				loVideoList.Add(video);				
			}
			return loVideoList;
		}

        #region ISearch Member

        public Dictionary<string, string> getSearchableCategories()
        {
            return new Dictionary<string, string>();
        }
        
        public List<VideoInfo> search(string searchUrl, string query)
        {            
            string url = string.Format(searchUrl, query);
            return getVideoListFromRss(url);
        }

        public List<VideoInfo> search(string searchUrl, string query, string category)
        {
            return search(searchUrl, query);
        }

        #endregion
    }
}
