using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// SVTPlayUtil gets streams from www.svtplay.se. It gets its categories 
    /// dynamicly and then uses rssfeeds to get the videos for each category
    /// </summary>
    public class SVTPlayUtil : GenericSiteUtil
    {        
        public override int DiscoverDynamicCategories()
        {
            int result = base.DiscoverDynamicCategories();
            
            // after GenericSiteUtil, try to get some thumbs for the categories
            foreach(RssLink c in Settings.Categories)
            {
                string id = c.Url.Substring(c.Url.LastIndexOf('/')+1);
                string path = "http://material.svtplay.se/content/2/c6/" + id.Substring(0, 2) + "/" + id.Substring(2, 2) + "/" + id.Substring(4, 2);
                c.Thumb = 
                    path + c.Thumb + "_a.jpg" + "|" +
                    path + c.Thumb + "4.jpg" + "|" +
                    path + c.Thumb.Replace("_", "").Replace("-", "") + "_a.jpg" +"|" +
                    path + "/a_" + c.Thumb.Substring(1).Replace("_", "") + "_168.jpg";
            }

            return result;
        }

        public override string getUrl(VideoInfo video)
        {
            string result = base.getUrl(video);
            
            // translate rtmp urls correctly
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                string[] keys = new string[video.PlaybackOptions.Count];
                video.PlaybackOptions.Keys.CopyTo(keys, 0);
                foreach (string key in keys)
                {                    
                    if (video.PlaybackOptions[key].StartsWith("rtmp"))
                    {
                        video.PlaybackOptions[key] = video.PlaybackOptions[key].Replace("_definst_", "?slist=");
                    }
                }
            }

            return result;
        }
    }
}
