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
            String lsUrl = "";

            HttpWebRequest request = WebRequest.Create(video.VideoUrl) as HttpWebRequest;
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
            WebResponse response = request.GetResponse();
            string lsHtml = System.Web.HttpUtility.UrlDecode(response.ResponseUri.OriginalString);
            Match loMatch = Regex.Match(lsHtml, "video=([^\"]+)");
            if (loMatch.Success)
            {
                lsUrl = loMatch.Groups[1].Value;
                string url_hash = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(lsUrl + "&f=flash" + "undefined" + "LVX*7x8yzwe", "MD5").ToLower();
                lsUrl += "&f=flash" + "undefined" + "&h=" + url_hash;

                HttpWebRequest request2 = WebRequest.Create(lsUrl) as HttpWebRequest;
                request2.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
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
            return lsUrl;
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
	}
}
/*
 <Site name="Live Video" util="LiveVideo" agecheck="false" enabled="false">
      <Username />
      <Password />
      <SearchUrl />
      <Categories>
        <Category xsi:type="RssLink" name="Featured Videos">http://rss.livevideo.com/rss/rss.ashx?v=Featured</Category>
        <Category xsi:type="RssLink" name="New Videos">http://rss.livevideo.com/rss/rss.ashx?v=Newest</Category>
        <Category xsi:type="RssLink" name="Most Viewed Videos">http://rss.livevideo.com/rss/rss.ashx?v=MostViewed</Category>
        <Category xsi:type="RssLink" name="Most Discussed Videos">http://rss.livevideo.com/rss/rss.ashx?v=MostDiscussed</Category>
        <Category xsi:type="RssLink" name="Most Hit Votes Videos">http://rss.livevideo.com/rss/rss.ashx?v=MostHit</Category>
        <Category xsi:type="RssLink" name="Most Miss Votes Videos">http://rss.livevideo.com/rss/rss.ashx?v=MostMiss</Category>
        <Category xsi:type="RssLink" name="Arts &amp; Animation">http://rss.livevideo.com/rss/rss.ashx?catid=1</Category>
        <Category xsi:type="RssLink" name="Auto &amp; Vehicles">http://rss.livevideo.com/rss/rss.ashx?catid=6</Category>
        <Category xsi:type="RssLink" name="Comedy">http://rss.livevideo.com/rss/rss.ashx?catid=7</Category>
        <Category xsi:type="RssLink" name="Entertainment">http://rss.livevideo.com/rss/rss.ashx?catid=8</Category>
        <Category xsi:type="RssLink" name="Extreme">http://rss.livevideo.com/rss/rss.ashx?catid=3</Category>
        <Category xsi:type="RssLink" name="Music">http://rss.livevideo.com/rss/rss.ashx?catid=9</Category>
        <Category xsi:type="RssLink" name="News">http://rss.livevideo.com/rss/rss.ashx?catid=10</Category>
        <Category xsi:type="RssLink" name="People">http://rss.livevideo.com/rss/rss.ashx?catid=11</Category>
        <Category xsi:type="RssLink" name="Pets &amp; Animals">http://rss.livevideo.com/rss/rss.ashx?catid=12</Category>
        <Category xsi:type="RssLink" name="Science &amp; Technology">http://rss.livevideo.com/rss/rss.ashx?catid=13</Category>
        <Category xsi:type="RssLink" name="Sports">http://rss.livevideo.com/rss/rss.ashx?catid=5</Category>
        <Category xsi:type="RssLink" name="Travel &amp; Places">http://rss.livevideo.com/rss/rss.ashx?catid=14</Category>
        <Category xsi:type="RssLink" name="Video Blogs">http://rss.livevideo.com/rss/rss.ashx?catid=17</Category>
        <Category xsi:type="RssLink" name="Video Comments">http://rss.livevideo.com/rss/rss.ashx?catid=18</Category>
        <Category xsi:type="RssLink" name="Video Games">http://rss.livevideo.com/rss/rss.ashx?catid=15</Category>
      </Categories>
    </Site>
*/