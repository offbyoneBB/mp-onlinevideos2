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
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class MetaCafeUtil : SiteUtilBase, ISearch
    {
        [Category("OnlineVideosConfiguration"), Description("Url used for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://www.metacafe.com/f/tags/{0}/rss.xml";

        static Regex loRegex = new Regex(@"mediaURL=(.*)&PostRoll",
                                          RegexOptions.IgnoreCase
                                        | RegexOptions.CultureInvariant
                                        | RegexOptions.IgnorePatternWhitespace
                                        | RegexOptions.Compiled);

        public override String getUrl(VideoInfo video)
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
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
            {
                VideoInfo video = new VideoInfo();
                video.Description = rssItem.MediaDescription;
                video.ImageUrl = rssItem.MediaThumbnails[0].Url;
                video.Title = rssItem.MediaTitle;
                video.Length = rssItem.MediaContents[0].Duration.ToString();
                //video.VideoUrl = Regex.Match(rssItem.link,@"watch/([\d]*)").Groups[1].Value;
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
