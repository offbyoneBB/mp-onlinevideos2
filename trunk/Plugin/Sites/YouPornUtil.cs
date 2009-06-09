using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class YouPornUtil : SiteUtilBase
    {
        static Regex PreviousPageRegEx = new Regex(@"\<a\shref=""(?<url>/browse\?page=[\d]+)""\>.*Previous\</a\>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex NextPageRegEx = new Regex(@"\<a\shref=""(?<url>/browse\?page=[\d]+)""\>Next.*\</a\>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(((RssLink)category).Url);
        }

        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {
            string ret = video.VideoUrl;

            int x;
            int y;
            string data;


            data = GetData(video.VideoUrl);

            if (data.Length > 0)
            {
                x = data.IndexOf("<a href=\"http://download.youporn.com/download");
                y = 0;

                if (x != -1)
                {
                    y = data.IndexOf("\"", x + 10);
                    if (y != -1)
                    {
                        ret = data.Substring(x + 9, y - x - 9) + ".flv";

                        Log.Debug("YouPorn - Found flv " + ret);
                    }
                }
            }

            return (ret);
        }

        string nextPageUrl = "";
        bool nextPageAvailable = false;
        public override bool hasNextPage()
        {
            return nextPageAvailable;
        }

        string previousPageUrl = "";
        bool previousPageAvailable = false;
        public override bool hasPreviousPage()
        {
            return previousPageAvailable;
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return Parse("http://www.youporn.com"+nextPageUrl);
        }        

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse("http://www.youporn.com" + previousPageUrl);
        }

        List<VideoInfo> Parse(String fsUrl)
        {
            List<VideoInfo> loRssItems = new List<VideoInfo>();

            try
            {
                // receive main page
                string dataPage = GetData(fsUrl);
                Log.Debug("YouPorn - Received " + dataPage.Length + " bytes");

                // is there any data ?
                if (dataPage.Length > 0)
                {
                    // check for previous page link
                    Match mPrev = PreviousPageRegEx.Match(dataPage);
                    if (mPrev.Success)
                    {
                        previousPageAvailable = true;
                        previousPageUrl = mPrev.Groups["url"].Value;
                    }
                    else
                    {
                        previousPageAvailable = false;
                        previousPageUrl = "";
                    }

                    // check for next page link
                    Match mNext = NextPageRegEx.Match(dataPage);
                    if (mNext.Success)
                    {
                        nextPageAvailable = true;
                        nextPageUrl = mNext.Groups["url"].Value;
                    }
                    else
                    {
                        nextPageAvailable = false;
                        nextPageUrl = "";
                    }

                    // parse vidoes
                    ParseLinks(dataPage, loRssItems);
                    if (loRssItems.Count > 0)
                    {
                        ParseThumbs(dataPage, loRssItems);

                        Log.Debug("YouPorn - finish to receive " + fsUrl);                        
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }            

            return loRssItems;
        }

        string GetData(string Url)
        {
            string str = "";
            int timeout = 5000;

            try
            {
                Cookie c = new Cookie();
                c.Name = "age_check";
                c.Value = "1";
                c.Expires = DateTime.Now.AddHours(1);
                c.Domain = "www.youporn.com";

                CookieContainer cc = new CookieContainer();
                cc.Add(c);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Credentials = new NetworkCredential("", "");
                
                request.Timeout = timeout;
                request.CookieContainer = cc;

                string encodemap = "utf-8";
                WebResponse response = request.GetResponse();
                Stream receiveStream = response.GetResponseStream();
                Encoding encode = System.Text.Encoding.GetEncoding(encodemap);
                StreamReader reader = new StreamReader(receiveStream, encode);
                str = reader.ReadToEnd();
            }
            catch { }
            return str;
        }

        void ParseLinks(string Page, List<VideoInfo> loRssItems)
        {            
            int x = 0;
            int y = 0;
            int z = 0;

            int cnt = 0;

            string line;

            string url;
            string desc;
            string duration;
            string id;

            while (x != -1)
            {
                x++;
                x = Page.IndexOf("<h1><a href=\"/watch/", x);

                if (x != -1)
                {
                    y = Page.IndexOf("</a>", x);
                    if (y != -1)
                    {
                        line = Page.Substring(x + 13, y - x - 12);
                        // /watch/276735/blonde-happily-tastes-cum/">Blonde happily tastes cum<

                        z = line.IndexOf("\"");
                        if (z != -1)
                        {
                            url = line.Substring(0, z);

                            y = url.IndexOf("/", 7);

                            id = "";
                            if (y != -1) id = url.Substring(7, y - 7);

                            url = "http://www.youporn.com" + url;

                            y = line.IndexOf("<", z);
                            if (y != -1)
                            {
                                desc = line.Substring(z + 2, y - z - 2);

                                z = Page.IndexOf("<h2>", x);
                                duration = Page.Substring(z + 4, Page.IndexOf("<span>", z) - z - 4);
                                z = Page.IndexOf("</span>", z);
                                duration = duration + ":" + Page.Substring(z + 7, Page.IndexOf("</h2>", z) - z - 7);

                                Log.Debug("YouPorn - Found object " + desc + " @ " + url);

                                cnt++;
                                // add new entry
                                VideoInfo loRssItem = new VideoInfo();
                                loRssItem.SiteID = id;
                                loRssItem.Title = desc;
                                loRssItem.Length = duration;
                                loRssItem.VideoUrl = url;                                    
                                loRssItems.Add(loRssItem);
                            }
                        }
                    }
                }
            }
        }

        void ParseThumbs(string Page, List<VideoInfo> loRssItems)
        {
            int x = 0;
            int y;
            int z;

            int cnt = 0;

            string thumb;

            while (x != -1)
            {

                // <img id="thumb1" src="http://ss-3.youporn.com/screenshot/27/39/screenshot/273986_extra_large.jpg" num="8" width="160" height="120" />

                x++;
                x = Page.IndexOf("<img id=\"thumb", x);

                if (x != -1)
                {
                    cnt++;

                    y = Page.IndexOf("=", x + 10);
                    if (y != -1)
                    {
                        y = y + 2;

                        z = Page.IndexOf("\"", y);
                        if (z != -1)
                        {
                            thumb = Page.Substring(y, z - y);
                            Log.Debug("YouPorn - Found thumb " + thumb);

                            if (thumb.Contains("video-thumb"))
                            {
                                int startIndexSrc = Page.IndexOf("src=\"", z)+5;
                                int endIndexSrc = Page.IndexOf('"', startIndexSrc);
                                loRssItems[cnt - 1].ImageUrl = Page.Substring(startIndexSrc, endIndexSrc - startIndexSrc);
                            }
                            else if (thumb.Contains("screenshot"))
                            {
                                //YouPorn - Found thumb http://ss-2.youporn.com/screenshot/28/01/screenshot/280156_large.jpg

                                string t;
                                t = thumb.Substring(0, thumb.Length - 10);

                                int i = t.LastIndexOf("screenshot");
                                if (i != -1)
                                {

                                    t = t.Substring(0, i) + "screenshot_multiple" + t.Substring(i + 10);

                                    i = t.LastIndexOf("/");
                                    if (i != -1)
                                    {
                                        string u = t.Substring(i + 1);
                                        t = t + "/" + u + "_multiple_";

                                        string tmp = t;
                                        Log.Debug("YouPorn - Found ext thumb " + t);

                                        loRssItems[cnt - 1].ImageUrl = tmp + "1_extra_large.jpg";
                                    }
                                }
                            }
                            else if (thumb.Contains("/thumbnail"))
                            {
                                // single 
                                //YouPorn - Found thumb http://ss-1.youporn.com/e4/thumbnail/single_120x90/2106.jpg?cb=20081022-1
                                // http://ss-1.youporn.com/e4/thumbnail/multiple_160x120/21/2106/2.jpg?cb=1

                                string no = "0";
                                string t;

                                y = thumb.LastIndexOf("/");
                                z = thumb.IndexOf("jpg");
                                if ((y != -1) && (z != -1))
                                {
                                    no = thumb.Substring(y + 1, z - y - 2);
                                }

                                int nr = 0;
                                try
                                {
                                    nr = Convert.ToInt16(no);
                                }
                                catch { }

                                if (nr > 0)
                                {
                                    y = thumb.IndexOf("single");
                                    if (y != -1)
                                    {
                                        t = thumb.Substring(0, y);
                                        t += "multiple_160x120/";

                                        z = nr / 10000;
                                        if (z > 0)
                                        {
                                            t += string.Format("{0:00}", z) + "/";
                                        }
                                        y = (nr - (z * 10000)) / 100;

                                        t += string.Format("{0:00}", y) + "/";
                                        t += nr.ToString() + "/";

                                        string tmp = t;
                                        Log.Debug("YouPorn - Found ext2 thumb " + t);

                                        loRssItems[cnt - 1].ImageUrl = tmp + "1.jpg?cb=1";
                                    }
                                }
                            }
                            else
                            {
                                Log.Debug("no thumbnail");

                                loRssItems[cnt - 1].ImageUrl = "defaultPicture.png";
                            }
                        }
                    }
                }
            }
        }               
    }
}
