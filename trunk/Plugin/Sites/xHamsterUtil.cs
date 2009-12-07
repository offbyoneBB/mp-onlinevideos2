using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class xHamsterUtil : SiteUtilBase, ISearch
    {
        [Category("OnlineVideosConfiguration"), Description("Url used for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://www.xhamster.com/search.php?q={0}";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the html page for the playback url.")]
        string fileUrlRegEx = @"'srv':\s'(?<srv>[^']+)',\s*
(?:'[^']+':\s'[^']+',\s*)?
'file':\s'(?<file>[^']+)'";

        Regex regEx_FileUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_FileUrl = new Regex(fileUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        }

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

                    // parse videos
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
                    }
                }
                if (x != -1)
                    x = x + 1;

            }
        }

        // resolve url for video
        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            if (data.Length > 0)
            {
                Match m = regEx_FileUrl.Match(data);
                if (m.Success)
                {
                    string result_url = string.Format("{0}flv2/{1}", m.Groups["srv"], m.Groups["file"]);
                    return result_url;
                }
            }
            return "";
        }

        #region Next|Previous Page

        static Regex NextPageRegEx = new Regex(@"<SPAN\sclass=navNext><A\sHREF=""(?<url>.+)"">Next</A></SPAN>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        string nextPageUrl = "";
        bool nextPageAvailable = false;
        public override bool HasNextPage
        {
            get { return nextPageAvailable; }
        }

        static Regex PreviousPageRegEx = new Regex(@"<SPAN\sclass=navPrev><A\sHREF=""(?<url>.+)"">Prev</A></SPAN>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        string previousPageUrl = "";
        bool previousPageAvailable = false;
        public override bool HasPreviousPage
        {
            get { return previousPageAvailable; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return Parse("http://www.xhamster.com" + nextPageUrl);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse("http://www.xhamster.com" + previousPageUrl);
        }

        #endregion

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories()
        {
            return new Dictionary<string, string>();
        }

        public List<VideoInfo> Search(string query)
        {
            return Parse(string.Format(searchUrl, query));
        }

        public List<VideoInfo> Search(string query, string category)
        {
            return Search(query);
        }

        #endregion
    }
}
