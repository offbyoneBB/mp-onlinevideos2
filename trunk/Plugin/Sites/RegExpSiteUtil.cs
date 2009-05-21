using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;

namespace OnlineVideos.Sites
{
	/// <summary>
    /// Description of RegExpSiteUtil.
	/// </summary>
    public class RegExpSiteUtil : SiteUtilBase, ISearch
	{
        static string videoListRegExpString = @"<div.*a.href=""(?<VideoUrl>http://www.empflix.com/view.php\?id\=\d+)"".*<img src=""(?<ImageUrl>http://pic.empflix.com/images/thumb/.*\.jpg)"".*</div>[\s\r\n]*<div\sclass=""videoTitle"">.+\stitle=""(?<Title>.+)"".+</div>";
        static string videoUrlRegExpString = @"http://cdn.empflix.com[^%]+";

        Regex videoListRegExp = new Regex(videoListRegExpString, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        Regex videoUrlRegExp = new Regex(videoUrlRegExpString, RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override String getUrl(VideoInfo video, SiteSettings foSite)
		{
			try
            {                             
                string dataPage = GetWebData(video.VideoUrl);
                if (dataPage.Length > 0)
                {
                    Match m = videoUrlRegExp.Match(dataPage);
                    if (m.Success)
                    {
                        return m.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return video.VideoUrl;
		}

		public override List<VideoInfo> getVideoList(Category category)
		{
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            try
            {
                string dataPage = GetWebData(((RssLink)category).Url);
                fillVideoListFromDataPage(dataPage, loVideoList);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }            
			return loVideoList;
		}

        void fillVideoListFromDataPage(string dataPage, List<VideoInfo> loVideoList)
        {
            if (dataPage.Length > 0)
            {
                Match m = videoListRegExp.Match(dataPage);
                while (m.Success)
                {
                    VideoInfo videoInfo = new VideoInfo();
                    videoInfo.Title = m.Groups["Title"].Value;
                    videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                    videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                    loVideoList.Add(videoInfo);
                    m = m.NextMatch();
                }
            }
        }

        string getHTMLDataFromPost(string url, string search)
        {            
            byte[] data = Encoding.UTF8.GetBytes("what=" + search + "&search_button=");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
            request.ContentLength = data.Length;
            request.ProtocolVersion = HttpVersion.Version10;
            Stream newStream = request.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();
            using (WebResponse response = request.GetResponse())
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
                string str = reader.ReadToEnd();
                return str;
            }
        }

        #region ISearch Member

        public Dictionary<string, string> getSearchableCategories()
        {
            return new Dictionary<string, string>();
        }
        
        public List<VideoInfo> search(string searchUrl, string query)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            try
            {                
                string dataPage = getHTMLDataFromPost(searchUrl, query);  
                fillVideoListFromDataPage(dataPage, loVideoList);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return loVideoList;
        }

        public List<VideoInfo> search(string searchUrl, string query, string category)
        {
            return search(searchUrl, query);
        }

        #endregion
    }
}
