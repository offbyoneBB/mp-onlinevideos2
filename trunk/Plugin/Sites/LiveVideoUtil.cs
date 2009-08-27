using System;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using System.Xml;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// Description of LiveVideoUtil.
	/// </summary>
	public class LiveVideoUtil : SiteUtilBase
	{       
        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {
            CookieContainer cookieContainer = new CookieContainer();
            String lsUrl = "";

            HttpWebRequest request = WebRequest.Create(video.VideoUrl) as HttpWebRequest;
            request.UserAgent = OnlineVideoSettings.UserAgent;
            WebResponse response = request.GetResponse();
            string lsHtml = System.Web.HttpUtility.UrlDecode(response.ResponseUri.OriginalString);
            Match loMatch = Regex.Match(lsHtml, "video=([^\"]+)");
            if (loMatch.Success)
            {
                lsUrl = loMatch.Groups[1].Value;
                string url_hash = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(lsUrl + "&f=flash" + "undefined" + "LVX*7x8yzwe", "MD5").ToLower();
                lsUrl += "&f=flash" + "undefined" + "&h=" + url_hash;
                
                HttpWebRequest request2 = WebRequest.Create(lsUrl) as HttpWebRequest;
                request2.CookieContainer = cookieContainer;
                request2.UserAgent = OnlineVideoSettings.UserAgent;
                WebResponse response2 = request2.GetResponse();
                Stream receiveStream = response2.GetResponseStream();
                StreamReader reader = new StreamReader(receiveStream, System.Text.Encoding.UTF8);
                string str = reader.ReadToEnd();                

                Match loMatch2 = Regex.Match(str, @"video_id=(.+\.flv)");
                if (loMatch2.Success)
                {
                    lsUrl = System.Web.HttpUtility.UrlDecode(loMatch2.Groups[1].Value);
                }                
            }

            // the cookie in cookieContainer must be send on the request for the flv which is done through IE
            if (cookieContainer.Count > 0)
            {
                // set all IE requests in one process
                InternetSetOption(0, 42, null, 0);
                // set the cookie for IE
                bool success = SetCookie("http://cdn.livevideo.com", cookieContainer.GetCookies(new Uri("http://cdn.livevideo.com"))[0]);
                if (success) return lsUrl;
            }

            // getting here means some error occured
            return "";
        }

        public override List<VideoInfo> getVideoList(Category category)
		{
            List<RssItem> loRssItemList = getRssDataItems((category as RssLink).Url);
			List<VideoInfo> loVideoList = new List<VideoInfo>();
			VideoInfo video;
			foreach(RssItem rssItem in loRssItemList)
            {
				video = new VideoInfo();
				video.Description = rssItem.mediaDescription;
				video.ImageUrl = rssItem.mediaThumbnail;
				video.Title = rssItem.title;				
				video.VideoUrl = rssItem.link;
                if (rssItem.contentList.Count > 0) video.VideoUrl = rssItem.contentList[0].url;
				loVideoList.Add(video);
			}
			return loVideoList;
		}

        [System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern bool InternetGetCookie(string url, string cookieName, StringBuilder cookieData, ref int size);

        [System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetCookie(string lpszUrlName, string lpszCookieName, string lpszCookieData);

        [System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetOption(int hInternet, int dwOption, string lpBuffer, int dwBufferLength);

        public static bool SetCookie(string url, Cookie cookie)
        {
            return InternetSetCookie(url, cookie.Name, cookie.Value);
        }

        public static CookieContainer GetUriCookieContainer(Uri uri)
        {
            CookieContainer cookies = null;

            // Determine the size of the cookie
            int datasize = 256;
            StringBuilder cookieData = new StringBuilder(datasize);

            if (!InternetGetCookie(uri.ToString(), null, cookieData,
              ref datasize))
            {
                if (datasize < 0)
                    return null;

                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new StringBuilder(datasize);
                if (!InternetGetCookie(uri.ToString(), null, cookieData,
                  ref datasize))
                    return null;
            }

            if (cookieData.Length > 0)
            {
                cookies = new CookieContainer();
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }
            return cookies;
        }
	}
}