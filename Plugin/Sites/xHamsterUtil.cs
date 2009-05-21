using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class xHamsterUtil : SiteUtilBase
    {
        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(((RssLink)category).Url);
        }

        List<VideoInfo> Parse(String fsUrl)
        {
            List<VideoInfo> loRssItems = new List<VideoInfo>(); 
            
            try
            {
                // receive main page
                string dataPage = GetWebData(fsUrl);
                Log.Debug("xHamster - Received " + dataPage.Length + " bytes");

                // is there any data ?
                if (dataPage.Length > 0)
                {
                    ParseLinks(dataPage, loRssItems);
                    if (loRssItems.Count > 0)
                    {
                        ParseThumbs(dataPage, loRssItems);

                        Log.Debug("xHamster - finish to receive " + fsUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return loRssItems;
        }

        private void ParseLinks(string Page, List<VideoInfo> loRssItems)
        {
            int x = 0;
            int y = 0;
            int z = 0;

            int cnt = 0;

            string line;

            string url;
            string desc;
            string id;

            while (x != -1)
            {
                x++;
                x = Page.IndexOf("moduleFeaturedTitle", x);

                if (x != -1)
                {
                    y = Page.IndexOf("</a>", x);
                    if (y != -1)
                    {
                        line = Page.Substring(x + 29, y - x - 28);
                        // <div class=moduleFeaturedTitle><a href="/movies/97942/kari_and_lex_steele_m27.html">Kari and Lex Steele M27</a></div>

                        z = line.IndexOf("\"");
                        if (z != -1)
                        {
                            url = line.Substring(z + 1);
                            z = url.IndexOf("\"");

                            if (z != -1)
                            {

                                url = url.Substring(0, z);
                                y = url.IndexOf("/", 8);

                                id = "";
                                if (y != -1) id = url.Substring(8, y - 8);

                                url = "http://www.xhamster.com" + url;

                                y = line.IndexOf(">");
                                if (y != -1)
                                {
                                    z = line.IndexOf("<");
                                    if (z != -1)
                                    {
                                        desc = line.Substring(y + 1, z - y - 1);
                                        //Debug.WriteLine("xHamster - Found object " + desc + " @ " + url);

                                        cnt++;
                                        // add new entry
                                        VideoInfo loRssItem = new VideoInfo();
                                        loRssItem.Title = desc;
                                        loRssItem.VideoUrl = url;
                                        loRssItem.VideoID = cnt;
                                        loRssItem.SiteID = id;
                                        loRssItems.Add(loRssItem);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ParseThumbs(string Page, List<VideoInfo> loRssItems)
        {
            int cnt = 0;

            int x = 0;
            int y = 0;
            int z = 0;

            string line;

            while (x != -1)
            {
                x = Page.IndexOf("this.src=", x);

                if (x != -1)
                {
                    cnt++;

                    y = Page.IndexOf("\"", x + 10);
                    if (y != -1)
                    {
                        line = Page.Substring(x + 10, y - x - 10);

                        y = line.LastIndexOf("/");
                        z = line.LastIndexOf(".");

                        string file = line.Substring(y + 1, z - y - 1);
                        string lnk = line.Substring(0, y);

                        y = file.IndexOf("_");
                        string id = file.Substring(y + 1);

                        VideoInfo loRssItem = loRssItems[cnt - 1];
                        loRssItem.ImageUrl = lnk + "/1_" + id + ".jpg";
                        loRssItem.SiteID = id;                        
                    }
                }
                if (x != -1)
                    x = x + 1;

            }
        }

        // resolve url for video
        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {
            // so.addVariable('srv','18');
            // http://dl18.xhamster.com/flv2/97883_Blonde_whore_gets_het_mouth_fucked_FM14.flv

            string ret = video.VideoUrl;

            int x;
            int y;
            string data = GetWebData(video.VideoUrl);

            if (data.Length > 0)
            {
                x = data.IndexOf("addVariable('srv'");
                if (x != -1)
                {
                    x += 19;
                    y = data.IndexOf("'", x);
                    if (y != -1)
                    {
                        string s = data.Substring(x, y - x);

                        x = data.IndexOf("addVariable('file'");
                        if (x != -1)
                        {
                            x += 20;
                            y = data.IndexOf("'", x);
                            if (y != -1)
                            {
                                string l = data.Substring(x, y - x);

                                ret = "http://dl" + s.ToString() + ".xhamster.com/flv2/" + l;
                                Log.Debug("YouPorn - Found flv " + ret);
                            }
                        }
                    }
                }
            }

            return (ret);
        }
        
    }
}
