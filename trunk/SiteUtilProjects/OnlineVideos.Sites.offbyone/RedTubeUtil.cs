using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Specialized;

namespace OnlineVideos.Sites
{
    public class RedTubeUtil : SiteUtilBase
    {
        static Regex videoListRegEx = new Regex(
                            @"<div\sclass=""video"">\s*
                            <a\shref=""/(?<VideoUrl>\d{1,})""\stitle=""(?<Title>[^""]*)""[^>]*>\s*
                            <img\s(?:(?!src).)*src=""(?<ImageUrl>[^""]*)""
                            (?:(?!<div\sclass=""time"">).)*<div\sclass=""time"">\s*<div[^>]*>\s*<span[^>]*>(?<Duration>[^<]*)<",
                            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex videoRegEx = new Regex(@"so\.addParam\(""flashvars"",""(?<flashvars>[^""]+)""\);", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [Category("OnlineVideosConfiguration"), Description("Url used for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://www.redtube.com/?search={0}";

        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(GetWebData(((RssLink)category).Url, CookieContainer));
        }

        List<VideoInfo> Parse(string dataPage)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            if (dataPage.Length > 0)
            {
                Match m = videoListRegEx.Match(dataPage);
                while (m.Success)
                {
                    VideoInfo videoInfo = new VideoInfo();
                    videoInfo.Title = m.Groups["Title"].Value;
                    videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                    videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                    videoInfo.Length = m.Groups["Duration"].Value;
                    loVideoList.Add(videoInfo);
                    m = m.NextMatch();
                }

                // check for previous page link
                Match mPrev = previousPageRegEx.Match(dataPage);
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
                Match mNext = nextPageRegEx.Match(dataPage);
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
            return loVideoList;
        }

        public override String getUrl(VideoInfo video)
        {
            string result = "";

            string data = GetWebData("http://www.redtube.com/" + video.VideoUrl, CookieContainer);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = videoRegEx.Match(data);
                if (m.Success)
                {
                    string flashvarsString = m.Groups["flashvars"].Value;
                    NameValueCollection paramsHash = System.Web.HttpUtility.ParseQueryString(flashvarsString);
                    string param = paramsHash["hash_flv"];
                    string leng = GetLink(video.VideoUrl);
                    result = leng + param;
                }
            }
            return result;
        }
        
        private string GetLink(string no)
        {
            string dl = "";
            Int64 nr = Convert.ToInt64(no);

            string[] map = {"R", "1", "5", "3", "4", "2", "O", "7",
                      "K", "9", "H", "B", "C", "D", "X", "F",
                      "G", "A", "I", "J", "8", "L", "M", "Z",
                      "6", "P", "Q", "0", "S", "T", "U", "V",
                      "W", "E", "Y", "N"};
            //int id = 19791;

            // org http://dl.redtube.com/_videos_t4vn23s9jc5498tgj49icfj4678/0000019/L9BB6X0ZX.flv?start=0

            // 1000

            string file = string.Format("{0:0000000}", nr);
            string leng = string.Format("{0:0000000}", nr / 1000);

            int value = 0;
            for (int i = 0; i < 7; i++)
            {
                value += (i + 1) * Convert.ToInt16(file[i] - 48);
            }
            string mv = value.ToString();

            value = 0;
            for (int i = 0; i < mv.Length; i++)
            {
                value += Convert.ToInt16(mv[i] - 48);
            }
            string qv = string.Format("{0:00}", value);

            string mapping = "";
            mapping = mapping + map[Convert.ToInt16(file[3]) - 48 + value + 3]; // char=0 map[48-48+3+3]=map[6] = "O"
            mapping = mapping + qv[1];                          // "3"
            mapping = mapping + map[Convert.ToInt16(file[0]) - 48 + value + 2]; // char=0 map[48-48+3+2]=map[5] = "2"
            mapping = mapping + map[Convert.ToInt16(file[2]) - 48 + value + 1]; // char=0 map[48-48+3+1]=map[4] = "4"
            mapping = mapping + map[Convert.ToInt16(file[5]) - 48 + value + 6]; // char=7 map[55-48+3+6]=map[16] = "G"
            mapping = mapping + map[Convert.ToInt16(file[1]) - 48 + value + 5]; // char=0 map[48-48+3+5]=map[8] = "K"
            mapping = mapping + qv[0];                          // "0"
            mapping = mapping + map[Convert.ToInt16(file[4]) - 48 + value + 7]; // char=4 map[4+3+7]=map[14] = "X"
            mapping = mapping + map[Convert.ToInt16(file[6]) - 48 + value + 4]; // char=7 map[7+3+4]=map[14] = "X"

            // L9BB6X0ZX

            dl = "http://ug54.redtube.com/467f9bca32b1989277b48582944f325afa3374/";
            dl += leng + "/";
            dl += mapping + ".flv";

            // neu http://dl.redtube.com//_videos_t4vn23s9jc5498tgj49icfj4678//0000019//L9BB6X0ZX.flv
            // org http://dl.redtube.com//_videos_t4vn23s9jc5498tgj49icfj4678//0000019//L9BB6X0ZX.flv?start=0

            // http://thumbs.redtube.com/_thumbs/0000019/0019791/0019791_016.jpg

            return dl;

        }

        CookieContainer CookieContainer
        {
            get
            {
                Cookie c = new Cookie() { Name = "pp", Value = "1", Expires = DateTime.Now.AddHours(1), Domain = "www.redtube.com" };
                CookieContainer cc = new CookieContainer();
                cc.Add(c);
                return cc;
            }
        }

        public override string GetFileNameForDownload(VideoInfo video, string url)
        {
            return ImageDownloader.GetSaveFilename(video.Title) + ".flv";            
        }

        #region Next|Previous Page

        static Regex nextPageRegEx = new Regex(@"<a\stitle=""Next\spage""\shref=""(?<url>[^""]*page=\d{1,})"">Next</a>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        //static Regex nextPageRegEx = new Regex(@"<a\sclass=p\shref='(?<url>/[^\?]*\?page=\d{1,5})'>Next</a>", RegexOptions.Compiled | RegexOptions.CultureInvariant);        
        string nextPageUrl = "";
        bool nextPageAvailable = false;
        public override bool HasNextPage
        {
            get { return nextPageAvailable; }
        }

        static Regex previousPageRegEx = new Regex(@"<a\stitle=""Previous\spage""\shref=""(?<url>[^""]*page=\d{1,})"">Prev</a>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        //static Regex previousPageRegEx = new Regex(@"<a\sclass=p\shref='(?<url>/[^\?]*\?page=\d{1,5})'>Prev</a>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        string previousPageUrl = "";
        bool previousPageAvailable = false;
        public override bool HasPreviousPage
        {
            get { return previousPageAvailable; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return Parse(GetWebData("http://www.redtube.com" + nextPageUrl, CookieContainer));
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse(GetWebData("http://www.redtube.com" + previousPageUrl, CookieContainer));
        }

        #endregion

        #region Search

        public override bool CanSearch { get { return true; } }
     
        public override List<VideoInfo> Search(string query)
        {
            return Parse(GetWebData(string.Format(searchUrl, query), CookieContainer));
        }

        #endregion
    }
}
