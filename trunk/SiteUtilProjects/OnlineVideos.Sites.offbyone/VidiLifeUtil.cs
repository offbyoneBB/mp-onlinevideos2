using System;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.ComponentModel;
using System.Threading;

namespace OnlineVideos.Sites
{
    public class VidiLifeUtil : SiteUtilBase 
    {
        Regex loPathRegex = new Regex(@"so\.addVariable\('xml',\s'([^']+)'");
        Regex loFlvRegex = new Regex(@"<flv8>([^<]+)</flv8>");

        public override String GetVideoUrl(VideoInfo video)
        {
            string lsUrl = "";
        	String lsHtml = GetWebData(video.VideoUrl);
            Match match = loPathRegex.Match(lsHtml);
            if (match.Success)
            {
                lsUrl = System.Web.HttpUtility.UrlDecode(match.Groups[1].Value);
                lsUrl = "http://" + new Uri(video.VideoUrl).DnsSafeHost + System.Web.HttpUtility.UrlDecode(lsUrl);
                string lsXml = GetWebData(lsUrl);
                Match matchFlv = loFlvRegex.Match(lsXml);
                if (matchFlv.Success)
                {
                    lsUrl = matchFlv.Groups[1].Value;
                }                
            }
        	return lsUrl;
        }

        public override List<VideoInfo> GetVideos(Category category)
		{            
			List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssToolkit.Rss.RssItem rssItem in GetWebData<RssToolkit.Rss.RssDocument>(((RssLink)category).Url).Channel.Items)
            {
                VideoInfo video = new VideoInfo();
				video.Description = rssItem.Description;
				video.Thumb = rssItem.MediaThumbnails[0].Url;
				video.Title = rssItem.Title;				
				video.VideoUrl = rssItem.Link;
				loVideoList.Add(video);
			}
			return loVideoList;
		}        
    }
}

/*
    <Site name="VidiLife" util="VidiLife" agecheck="false" enabled="false">
      <Username />
      <Password />
      <SearchUrl />
      <Categories>
        <Category xsi:type="RssLink" name="Top 20 New Videos">http://rss.vidilife.com/rss.aspx</Category>
        <Category xsi:type="RssLink" name="Funny Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_funny</Category>
        <Category xsi:type="RssLink" name="Music Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_music</Category>
        <Category xsi:type="RssLink" name="Hollywood Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_hollywood</Category>
        <Category xsi:type="RssLink" name="Stupid Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_stupid</Category>
        <Category xsi:type="RssLink" name="Sexy Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_sexy</Category>
        <Category xsi:type="RssLink" name="Amateur Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_amateur</Category>
        <Category xsi:type="RssLink" name="Hot Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_hot</Category>
        <Category xsi:type="RssLink" name="Crazy Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_crazy</Category>
        <Category xsi:type="RssLink" name="Streaming Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_streaming</Category>
        <Category xsi:type="RssLink" name="Online Videos">http://rss.vidilife.com/rss.aspx?FeedName=videos_online</Category>
        <Category xsi:type="RssLink" name="Funny Commercial">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory1</Category>
        <Category xsi:type="RssLink" name="Animation">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory2</Category>
        <Category xsi:type="RssLink" name="Real-Life Video">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory3</Category>
        <Category xsi:type="RssLink" name="Independent Film">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory4</Category>
        <Category xsi:type="RssLink" name="Music Video">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory5</Category>
        <Category xsi:type="RssLink" name="News">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory6</Category>
        <Category xsi:type="RssLink" name="Video Blog">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory7</Category>
        <Category xsi:type="RssLink" name="Sports">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory8</Category>
        <Category xsi:type="RssLink" name="My Vacation/ Vacation Spots">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory9</Category>
        <Category xsi:type="RssLink" name="Automotive">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory10</Category>
        <Category xsi:type="RssLink" name="Extreme Video">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory11</Category>
        <Category xsi:type="RssLink" name="Animals">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory12</Category>
        <Category xsi:type="RssLink" name="Family">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory13</Category>
        <Category xsi:type="RssLink" name="Kids">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory14</Category>
        <Category xsi:type="RssLink" name="School">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory15</Category>
        <Category xsi:type="RssLink" name="Natural Wonders">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory16</Category>
        <Category xsi:type="RssLink" name="Comedy">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory17</Category>
        <Category xsi:type="RssLink" name="Educational">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory18</Category>
        <Category xsi:type="RssLink" name="Instructional">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory19</Category>
        <Category xsi:type="RssLink" name="Hot Male">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory20</Category>
        <Category xsi:type="RssLink" name="Hot Female">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory21</Category>
        <Category xsi:type="RssLink" name="Business/ Advertising">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory22</Category>
        <Category xsi:type="RssLink" name="Real Estate">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory23</Category>
        <Category xsi:type="RssLink" name="Cooking">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory24</Category>
        <Category xsi:type="RssLink" name="Party">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory105</Category>
        <Category xsi:type="RssLink" name="Video Games">http://rss.vidilife.com/rss.aspx?FeedName=SearchByCategory107</Category>
      </Categories>
    </Site>
*/