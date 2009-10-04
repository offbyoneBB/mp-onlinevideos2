using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of EmpflixUtil.
    /// </summary>
    public class EmpflixUtil : SiteUtilBase, ISearch
    {
        static string videoListRegExpString = @"<div.*a.href=""(?<VideoUrl>http://www.empflix.com/view.php\?id\=\d+)"".*<img\ssrc=""(?<ImageUrl>http://pic.*.empflix.com/images/thumb/.*\.jpg)"".*</div>[\s\r\n]*<div\sclass=""videoTitle"">.+\stitle=""(?<Title>.+)"".+</div>.*[\s\r\n]*.*<div\sclass=""videoLeft"">(?<Duration>.*)<br\s/>";
        static string playlistUrlRegExpString = @"so.addVariable\('config',\s'(?<PlaylistUrl>[^']+)'\);";
        static string nextPageRegExpString = @"<a\shref=""(?<url>.*)"">next\s&gt;&gt;</a>";
        static string previousPageRegExpString = @"<a\shref=""(?<url>.*)"">&lt;&lt;\sprev</a>";

        static Regex videoListRegExp = new Regex(videoListRegExpString, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex playlistUrlRegExp = new Regex(playlistUrlRegExpString, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex nextPageRegExp = new Regex(nextPageRegExpString, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex previousPageRegExp = new Regex(previousPageRegExpString, RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(GetWebData(((RssLink)category).Url));
        }

        public override String getUrl(VideoInfo video)
        {
            try
            {
                string dataPage = GetWebData(video.VideoUrl);
                if (dataPage.Length > 0)
                {
                    Match m = playlistUrlRegExp.Match(dataPage);
                    if (m.Success)
                    {
                        string playlistUrl = "http://cdnt.empflix.com/" + m.Groups["PlaylistUrl"].Value;
                        playlistUrl = System.Web.HttpUtility.UrlDecode(playlistUrl);
                        dataPage = GetWebData(playlistUrl);
                        if (dataPage.Length > 0)
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(dataPage);
                            string result = doc.SelectSingleNode("//file").InnerText;
                            return result + "&filetype=.flv";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return video.VideoUrl;
        }

        string nextPageUrl = "";
        bool nextPageAvailable = false;
        public override bool HasNextPage
        {
            get { return nextPageAvailable; }
        }

        string previousPageUrl = "";
        bool previousPageAvailable = false;
        public override bool HasPreviousPage
        {
            get { return previousPageAvailable; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return Parse(GetWebData("http://www.empflix.com/" + nextPageUrl));
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse(GetWebData("http://www.empflix.com/" + previousPageUrl));
        }

        List<VideoInfo> Parse(string dataPage)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            if (dataPage.Length > 0)
            {
                try
                {
                    Match m = videoListRegExp.Match(dataPage);
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
                    Match mPrev = previousPageRegExp.Match(dataPage);
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
                    Match mNext = nextPageRegExp.Match(dataPage);
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

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories()
        {
            return new Dictionary<string, string>();
        }

        public List<VideoInfo> Search(string query)
        {
            try
            {
                string dataPage = GetWebDataFromPost(Settings.SearchUrl, "what=" + query);
                return Parse(dataPage);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return new List<VideoInfo>();
            }

        }

        public List<VideoInfo> Search(string query, string category)
        {
            return Search(query);
        }

        #endregion
    }
}
