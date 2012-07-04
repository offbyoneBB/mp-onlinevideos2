using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using RssToolkit.Rss;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class GameTrailersUtil : GenericSiteUtil
    {
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override string getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            string VideoUrl = "";
            string finalDownloadUrl = "";
            string baseDownloadUrl = "http://www.gametrailers.com/feeds/video_download/";
            if (data.Length > 0)
            {
                if (regEx_PlaylistUrl != null)
                {
                    Match m = regEx_PlaylistUrl.Match(data);
                    int count = 0;
                    while (m.Success && count == 0)
                    {
                        string contentid = HttpUtility.HtmlDecode(m.Groups["contentid"].Value);
                        string token = HttpUtility.HtmlDecode(m.Groups["token"].Value);
                        finalDownloadUrl = baseDownloadUrl + contentid + "/" + token;

                        string dataJson = GetWebData(finalDownloadUrl);
                        JObject o = JObject.Parse(dataJson);
                        VideoUrl = o["url"].ToString();
                        count++;
                    }
                    return VideoUrl;
                }
            }
            return null;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = ((RssLink)category).Url;
            return getVideoList(url);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return getVideoList(nextPageUrl);
        }

        protected List<VideoInfo> getVideoList(string url)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            string data = GetWebData(url);
            if (data.Length > 0)
            {
                if (regEx_VideoList != null)
                {
                    Match m = regEx_VideoList.Match(data);
                    while (m.Success)
                    {
                        VideoInfo videoInfo = CreateVideoInfo();
                        videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["gameName"].Value)  + " - " + HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                        videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                        videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                        videoInfo.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value).Replace("M", "M ").Replace("S", "S").Replace("PT0H", "").Replace("PT1H", "1H ").Replace("PT", "").Trim();
                        videoInfo.Airdate = Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
                        videoInfo.Description = m.Groups["Description"].Value;
                        videoList.Add(videoInfo);
                        m = m.NextMatch();
                    }
                }
                if (regEx_NextPage != null)
                {
                    // check for next page link
                    Match mNext = regEx_NextPage.Match(data);
                    if (mNext.Success)
                    {
                        string page = HttpUtility.ParseQueryString(new Uri(url).Query)["currentPage"];
                        if (!string.IsNullOrEmpty(page))
                        {
                            nextPageAvailable = true;
                            nextPageUrl = url.Replace("currentPage=" + page.ToString(), "currentPage=" + (int.Parse(page) + 1).ToString());
                        }
                        else
                        {
                            nextPageAvailable = true;
                            nextPageUrl = mNext.Groups["url"].Value + "/?sortBy=most_recent&currentPage=2";
                            if (!string.IsNullOrEmpty(nextPageRegExUrlFormatString)) nextPageUrl = string.Format(nextPageRegExUrlFormatString, nextPageUrl);
                            nextPageUrl = ApplyUrlDecoding(nextPageUrl, nextPageRegExUrlDecoding);
                            //if (nextPageUrl.StartsWith("?")) nextPageUrl = "search.php" + nextPageUrl;
                            if (!Uri.IsWellFormedUriString(nextPageUrl, System.UriKind.Absolute))
                            {
                                Uri uri = null;
                                if (Uri.TryCreate(new Uri(url), nextPageUrl, out uri))
                                {
                                    nextPageUrl = uri.ToString();
                                }
                                else
                                {
                                    nextPageAvailable = false;
                                    nextPageUrl = "";
                                }
                            }
                        }
                    }
                    else
                    {
                        string page = HttpUtility.ParseQueryString(new Uri(url).Query)["currentPage"];
                        nextPageAvailable = true;
                        nextPageUrl = url.Replace("currentPage=" + page.ToString(), "currentPage=" + (int.Parse(page) + 1).ToString());
                    }
                    nextPageUrl = nextPageUrl;
                }
            }

            return videoList;
        }

        public override List<VideoInfo> Search(string query)
        {
            // if an override Encoding was specified, we need to UrlEncode the search string with that encoding
            if (encodingOverride != null) query = HttpUtility.UrlEncode(encodingOverride.GetBytes(query));

            return getVideoList(string.Format(searchUrl, query));
        }
    }
}