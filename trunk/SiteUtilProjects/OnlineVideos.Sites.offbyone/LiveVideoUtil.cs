using System;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Collections.Generic;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// Description of LiveVideoUtil.
	/// </summary>
	public class LiveVideoUtil : SiteUtilBase
	{       
        public override String getUrl(VideoInfo video)
        {
            string lsHtml = System.Web.HttpUtility.UrlDecode(GetRedirectedUrl(video.VideoUrl));
            Match loMatch = Regex.Match(lsHtml, "video=([^\"]+)");
            if (loMatch.Success)
            {
                string lsUrl = loMatch.Groups[1].Value;
                string url_hash = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(lsUrl + "&f=flash" + "undefined" + "LVX*7x8yzwe", "MD5").ToLower();
                lsUrl += "&f=flash" + "undefined" + "&h=" + url_hash;                
                
                string str = GetWebData(lsUrl);
                Match loMatch2 = Regex.Match(str, @"video_id=([^&]+)");
                if (loMatch2.Success)
                {
                    lsUrl = System.Web.HttpUtility.UrlDecode(loMatch2.Groups[1].Value);
                    return lsUrl;
                }                
            }            
            // getting here means some error occured
            return "";
        }

        public override List<VideoInfo> getVideoList(Category category)
		{            
			List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssItem rssItem in GetWebData<RssDocument>(((RssLink)category).Url).Channel.Items)
            {
                loVideoList.Add(VideoInfo.FromRssItem(rssItem, false, new Predicate<string>(delegate(string url) { return true; })));
			}
			return loVideoList;
		}
	}
}