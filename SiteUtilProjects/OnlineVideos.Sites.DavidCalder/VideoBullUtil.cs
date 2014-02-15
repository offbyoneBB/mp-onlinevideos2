 using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;


namespace OnlineVideos.Sites.DavidCalder
{
    public class VideoBullUtil : DeferredResolveUtil
    {
        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            Log.Debug("Debug Video.Title === " + video.Title);
            try
            {
                TrackingInfo tInfo = new TrackingInfo()
                {
                    //Grimm – Season 3 Episode 3 – A Dish Best Served Cold
                    Regex = Regex.Match(video.Title, @"(?<Title>.*)\s–\sSeason\s(?<Season>\d+)\sEpisode\s(?<Episode>\d+)",RegexOptions.IgnoreCase),                   
                    VideoKind = VideoKind.TvSeries                                
                };
                Match match = Regex.Match(video.Title, @"(?<Title>.*)\s–\sSeason\s(?<Season>\d+)\sEpisode\s(?<Episode>\d+)");
                Log.Debug("Debug regex match === " + tInfo.Title + " Season " + tInfo.Season + " Episode " + tInfo.Episode);
            }
            catch (Exception e)
            {
                Log.Warn("Error parsing TrackingInfo data: {0}", e.ToString());
            }

            return base.GetTrackingInfo(video);
        }

        public override VideoInfo CreateVideoInfo()
        {    
            return new SeriesVideoInfo();
        }

        public class SeriesVideoInfo : DeferredResolveVideoInfo 
        {     
            public override string GetPlaybackOptionUrl(string url)
            {
                CookieContainer cc = new CookieContainer();
                string newUrl = base.PlaybackOptions[url];
                string stripedurl = newUrl.Substring(newUrl.IndexOf("http://videobull"));
                string data = SiteUtilBase.GetWebData(stripedurl,cc,newUrl);
           
                //<a href="http://hoster/ekqiej2ito9p"
                Match n = Regex.Match(data, @"<a\shref=""(?<url>[^""]*)"">");
                if (n.Success)
                    return GetVideoUrl(n.Groups["url"].Value);
            
                //<iframe src="./vidiframe.php?linkurl=aHR0cDovL3d3dy5wdXRsb2NrZXIuY29tL2ZpbGUvRENEQUZEOEEzQkVBMjgzMg=="          
                Match n1 = Regex.Match(data, @"<iframe\ssrc="".(?<url>[^""]*)""");

                string videoUrl = "http://videobull.com/wp-content/themes/videozoom" + n1.Groups["url"].Value;
                string newData = SiteUtilBase.GetWebData(videoUrl, cc);
                Match n2 = Regex.Match(newData, @"<iframe\ssrc=""(?<url>[^""]*)""");
                if (n2.Success)
                    return GetVideoUrl(n2.Groups["url"].Value);
               
                return string.Empty;
            }
        }


        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            
            foreach (Category cat in Settings.Categories)
            {
                if (cat.Name.Contains("Season") == true)
                {                   
                    cat.Name = cat.Name.Substring(0, cat.Name.IndexOf("Season")).Trim();    
                  
                }
            }
            return res;
        }
        

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            int res = base.DiscoverNextPageCategories(category);

            foreach (RssLink cat in Settings.Categories)
            {
                if (cat.Name.Contains("Season") == true)
                {
                    cat.Name = cat.Name.Substring(0, cat.Name.IndexOf("Season")).Trim();
                }
            }
            return res;
        }

    }
}
