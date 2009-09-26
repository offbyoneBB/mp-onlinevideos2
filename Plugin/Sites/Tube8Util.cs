using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using MediaPortal.GUI.Library;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class Tube8Util : SiteUtilBase, ISearch
    {
        string nextPageUrl = "";
        string previousPageUrl = "";
        bool nextPageAvailable = false;
        bool previousPageAvailable = false;

        static Regex PreviousPageRegEx = new Regex(@"\<a\sclass=nounder\shref=""(?<url>[^\>]+)""\>&lt;\</a\>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex NextPageRegEx = new Regex(@"\<a\sclass=nounder\shref=""(?<url>[^\>]+)""\>&gt;\</a\>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(((RssLink)category).Url);            
        }
        
        private List<VideoInfo> Parse(String fsUrl)
        {
            List<VideoInfo> loRssItems = new List<VideoInfo>(); 

            try
            {
                previousPageAvailable = false;
                previousPageUrl = "";
                nextPageAvailable = false;
                nextPageUrl = "";

                // receive main page
                string dataPage = GetWebData(fsUrl);
                Log.Debug("Tube8 - Received " + dataPage.Length + " bytes");

                // is there any data ?
                if (dataPage.Length > 0)
                {
                    ParseLinks(dataPage, loRssItems);
                    if (loRssItems.Count > 0)
                    {
                        Log.Debug("Tube8 - finish to receive " + fsUrl);
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
            int cnt = 0;

            GetValues g = new GetValues();

            while (g.Pointer != -1)
            {
                g.Html = Page;
                g.Search = ">videosArray";
                g.Start = "'";
                g.Stop = "'";

                g = GetDataPage(g);

                if (g.Pointer != -1)
                {
                    cnt++;
                    
                    // add new entry
                    VideoInfo loRssItem = new VideoInfo();                    

                    //tmpClip.Url = g.Data;   // link
                    string h = g.Data;

                    string[] thb = new string[6];
                    g.Search = "[0]";                    
                    g = GetDataPage(g);
                    thb[0] = h + g.Data;    //ima 0     
                    /*
                    g.Search = "[1]";
                    g = GetDataPage(g);
                    thb[1] = h + g.Data;    //img 1
                    g.Search = "[2]";
                    g = GetDataPage(g);
                    thb[2] = h + g.Data;    //img 2
                    g.Search = "[3]";
                    g = GetDataPage(g);
                    thb[3] = h + g.Data;    //img 3
                    g.Search = "[4]";
                    g = GetDataPage(g);
                    thb[4] = h + g.Data;    //img 4
                    g.Search = "[5]";
                    g = GetDataPage(g);
                    thb[5] = h + g.Data;    //img 5
                    */
                    loRssItem.ImageUrl = thb[0];

                    g.Search = "href";
                    g.Start = "\"";
                    g.Stop = "\"";

                    g = GetDataPage(g);
                    loRssItem.VideoUrl = g.Data;    //video link

                    g.Search = "alt";
                    g = GetDataPage(g);
                    loRssItem.Title = g.Data; // title

                    g.Search = "tinyInfo\"><";
                    g.Start = ">";
                    g.Stop = "<";
                    g = GetDataPage(g);
                    loRssItem.Length = g.Data; // length

                    loRssItems.Add(loRssItem);
                }
            }
        }        

        public struct GetValues
        {
            public string Html;
            public int Pointer;
            public string Search;
            public string Start;
            public string Stop;
            public string Data;
        }

        private static GetValues GetDataPage(GetValues inVal)
        {
            inVal.Data = "";
            string page = inVal.Html;

            int x = page.IndexOf(inVal.Search, inVal.Pointer);
            if (x > 0)
            {
                x = page.IndexOf(inVal.Start, x + inVal.Search.Length + 1);
                if (x > 0)
                {
                    int y = page.IndexOf(inVal.Stop, x + 1);
                    if (y > 0)
                    {
                        inVal.Data = page.Substring(x + 1, y - x - 1);
                        inVal.Pointer = y + 1;
                    }
                    else
                        inVal.Pointer = x + 1;
                }
                else
                {
                    inVal.Pointer = x + 1;
                }
            }
            else
                inVal.Pointer = x;

            return inVal;
        }

        // resolve url for video
        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {
            string ret = video.VideoUrl;
            string data;

            data = GetWebData(video.VideoUrl);

            //so.addVariable('videoUrl','http://mediat03.tube8.com/flv/3c9947fb83c1a254453f79c67157576d/497f5b7c/0901/23/497a9788734a0/497a9788734a0.flv');

            GetValues g = new GetValues();
            g.Html = data;
            g.Search = "so.addVariable('videoUrl'";
            g.Start = "'";
            g.Stop = "'";

            g = GetDataPage(g);

            if (g.Pointer != -1)
            {
                ret = g.Data;
                Log.Debug("Tube8 - Found flv " + ret);
            }
            return ret;
        }

        public override bool hasNextPage()
        {
            return nextPageAvailable;
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return Parse(nextPageUrl);
        }

        public override bool hasPreviousPage()
        {
            return previousPageAvailable;
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse(previousPageUrl);
        }

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories(IList<Category> configuredCategories)
        {
            return new Dictionary<string, string>();
        }

        public List<VideoInfo> Search(string searchUrl, string query)
        {
            return Parse(string.Format(searchUrl, query));
        }

        public List<VideoInfo> Search(string searchUrl, string query, string category)
        {
            return Search(searchUrl, query);
        }

        #endregion
    }
}
