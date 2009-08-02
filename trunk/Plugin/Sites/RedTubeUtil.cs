using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class RedTubeUtil : SiteUtilBase, ISearch
    {
        static Regex videoListRegEx = new Regex(
                            @"<div\sclass=""video"">\s*
                            <a\shref=""/(?<VideoUrl>\d{1,})""\stitle=""(?<Title>[^""]*)""[^>]*>\s*
                            <img\s(?:(?!src).)*src=""(?<ImageUrl>[^""]*)""
                            (?:(?!<div\sclass=""time"">).)*<div\sclass=""time"">\s*<div[^>]*>\s*<span[^>]*>(?<Duration>[^<]*)<", 
                            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(GetData(((RssLink)category).Url));            
        }

        List<VideoInfo> Parse(string dataPage)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            if (dataPage.Length > 0)
            {
                try
                {
                    Match m = videoListRegEx.Match(dataPage);
                    while (m.Success)
                    {
                        VideoInfo videoInfo = new VideoInfo();
                        videoInfo.Title = m.Groups["Title"].Value;
                        videoInfo.VideoUrl = GetLink(m.Groups["VideoUrl"].Value);
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
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
            return loVideoList;
        }

        private string GetData(string Link)
        {
            try
            {
                int timeout = 5000;

                Cookie c = new Cookie();
                c.Name = "pp";
                c.Value = "1";
                c.Expires = DateTime.Now.AddHours(1);
                c.Domain = "www.redtube.com";

                CookieContainer cc = new CookieContainer();
                cc.Add(c);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Link);

                request.Timeout = timeout;
                request.CookieContainer = cc;

                string encodemap = "utf-8";
                WebResponse response = request.GetResponse();
                Stream receiveStream = response.GetResponseStream();
                Encoding encode = System.Text.Encoding.GetEncoding(encodemap);
                StreamReader reader = new StreamReader(receiveStream, encode);
                string str = reader.ReadToEnd();
                return str;
            }
            catch
            {
                return "";
            }
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

            dl = "http://dl.redtube.com//_videos_t4vn23s9jc5498tgj49icfj4678//";
            dl += leng + "//";
            dl += mapping + ".flv";

            // neu http://dl.redtube.com//_videos_t4vn23s9jc5498tgj49icfj4678//0000019//L9BB6X0ZX.flv
            // org http://dl.redtube.com//_videos_t4vn23s9jc5498tgj49icfj4678//0000019//L9BB6X0ZX.flv?start=0

            // http://thumbs.redtube.com/_thumbs/0000019/0019791/0019791_016.jpg

            return dl;

        }        

        #region Next|Previous Page

        static Regex nextPageRegEx = new Regex(@"<a\stitle=""Next\spage""\shref=""(?<url>[^""]*page=\d{1,})"">Next</a>", RegexOptions.Compiled | RegexOptions.CultureInvariant);        
        string nextPageUrl = "";
        bool nextPageAvailable = false;
        public override bool hasNextPage()
        {
            return nextPageAvailable;
        }

        static Regex previousPageRegEx = new Regex(@"<a\stitle=""Previous\spage""\shref=""(?<url>[^""]*page=\d{1,})"">Prev</a>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        string previousPageUrl = "";
        bool previousPageAvailable = false;
        public override bool hasPreviousPage()
        {
            return previousPageAvailable;
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return Parse(GetData("http://www.redtube.com" + nextPageUrl));
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse(GetData("http://www.redtube.com" + previousPageUrl));
        }

        #endregion

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories(Category[] configuredCategories)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (RssLink category in configuredCategories) result.Add(category.Name, category.Url);
            return result;
        }

        public List<VideoInfo> Search(string searchUrl, string query)
        {
            return Parse(string.Format(searchUrl, "http://www.redtube.com/", query));
        }

        public List<VideoInfo> Search(string searchUrl, string query, string category)
        {
            return Parse(string.Format(searchUrl, category, query));
        }

        #endregion
    }
}
