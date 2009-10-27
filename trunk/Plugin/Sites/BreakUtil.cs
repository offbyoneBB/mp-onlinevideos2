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
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
	public class BreakUtil : SiteUtilBase, ISearch
	{
        [Category("OnlineVideosConfiguration"), Description("Url used for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://rss.break.com/keyword/{0}/";

        public override String getUrl(VideoInfo video)
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
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
            {
                VideoInfo video = new VideoInfo();
                video.Description = rssItem.Description;
                video.ImageUrl = rssItem.Enclosure.Url;
                video.Title = rssItem.Title;
                video.VideoUrl = rssItem.Link;
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
            string url = string.Format(searchUrl, query);
            return getVideoList(new RssLink() { Url = url });
        }

        public List<VideoInfo> Search(string query, string category)
        {
            return Search(query);
        }

        #endregion
    }
}
