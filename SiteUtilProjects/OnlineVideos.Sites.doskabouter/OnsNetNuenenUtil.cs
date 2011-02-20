using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using System.Net;
//using RssToolkit.Rss;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class OnsNetNuenenUtil : GenericSiteUtil
    {

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> res = base.Parse(((RssLink)category).Url, null);
            foreach (VideoInfo vid in res)
            {
                if (String.IsNullOrEmpty(vid.Title))
                {
                    vid.Title = vid.Description;
                    vid.Description = String.Empty;
                }
            }
            return res;
        }
    }
}
