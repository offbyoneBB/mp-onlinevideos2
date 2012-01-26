using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class GameTrailersUtil : GenericSiteUtil
    {
        public override string getUrl(VideoInfo video)
        {
            // always get playbackoptions back from string in Other property if they are set
            if (video.Other is string && (video.Other as string).StartsWith("PlaybackOptions://"))
                video.PlaybackOptions = Utils.DictionaryFromString((video.Other as string).Substring("PlaybackOptions://".Length));

            if (!string.IsNullOrEmpty(video.VideoUrl)) return base.getUrl(video);
            else if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                // resolve all playbackoptions
                Dictionary<string, string> newPlaybackOptions = new Dictionary<string,string>();
                foreach (var item in video.PlaybackOptions)
                {
                    newPlaybackOptions.Add(item.Key, base.getUrl(new VideoInfo() { VideoUrl = item.Value }));
                }
                video.PlaybackOptions = newPlaybackOptions;
                return video.PlaybackOptions.First().Value;
            }
            else return null;
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
            string data = GetWebData(url, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            if (data.Length > 0)
            {
                if (regEx_VideoList != null)
                {
                    Match m = regEx_VideoList.Match(data);
                    while (m.Success)
                    {
                        VideoInfo videoInfo = CreateVideoInfo();
                        videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                        // get, format and if needed absolutify the video url(s)
                        Dictionary<string, string> videoUrls = new Dictionary<string, string>();
                        for (int i = 0; i < m.Groups["VideoUrl"].Captures.Count; i++)
                        {
                            string anUrl = m.Groups["VideoUrl"].Captures[i].Value;
                            if (!string.IsNullOrEmpty(videoListRegExFormatString)) anUrl = string.Format(videoListRegExFormatString, anUrl);
							anUrl = ApplyUrlDecoding(anUrl, videoListUrlDecoding);
							if (!Uri.IsWellFormedUriString(anUrl, System.UriKind.Absolute)) anUrl = new Uri(new Uri(baseUrl), anUrl).AbsoluteUri;
                            videoUrls.Add(m.Groups["PostTitle"].Captures.Count - 1 >= i ? m.Groups["PostTitle"].Captures[i].Value : (i + 1).ToString(), anUrl);
                        }
                        if (videoUrls.Count > 1) videoInfo.Other = "PlaybackOptions://\n" + Utils.DictionaryToString(videoUrls);
                        else if (videoUrls.Count == 1) videoInfo.VideoUrl = videoUrls.First().Value;
                        else continue;
                        // get, format and if needed absolutify the thumb url
                        videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                        if (!string.IsNullOrEmpty(videoThumbFormatString)) videoInfo.ImageUrl = string.Format(videoThumbFormatString, videoInfo.ImageUrl);
                        if (!string.IsNullOrEmpty(videoInfo.ImageUrl) && !Uri.IsWellFormedUriString(videoInfo.ImageUrl, System.UriKind.Absolute)) videoInfo.ImageUrl = new Uri(new Uri(baseUrl), videoInfo.ImageUrl).AbsoluteUri;
                        videoInfo.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value);
                        videoInfo.Airdate = Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
                        videoInfo.Description = m.Groups["Description"].Value;
                        videoList.Add(videoInfo);
                        m = m.NextMatch();
                    }
                }
                else if (!string.IsNullOrEmpty(videoItemXml))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    XmlNodeList videoItems = doc.SelectNodes(videoItemXml);
                    for (int i = 0; i < videoItems.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(videoTitleXml) && !string.IsNullOrEmpty(videoUrlXml))
                        {
                            VideoInfo videoInfo = CreateVideoInfo();
                            videoInfo.Title = HttpUtility.HtmlDecode(videoItems[i].SelectSingleNode(videoTitleXml).InnerText);
                            if (!String.IsNullOrEmpty(videoTitle2Xml))
                                videoInfo.Title += ' ' + HttpUtility.HtmlDecode(videoItems[i].SelectSingleNode(videoTitle2Xml).InnerText); ;

                            videoInfo.VideoUrl = videoItems[i].SelectSingleNode(videoUrlXml).InnerText;
                            if (!string.IsNullOrEmpty(videoListRegExFormatString)) videoInfo.VideoUrl = string.Format(videoListRegExFormatString, videoInfo.VideoUrl);
                            if (!string.IsNullOrEmpty(videoThumbXml)) videoInfo.ImageUrl = videoItems[i].SelectSingleNode(videoThumbXml).InnerText;
                            if (!string.IsNullOrEmpty(videoThumbFormatString)) videoInfo.ImageUrl = string.Format(videoThumbFormatString, videoInfo.ImageUrl);
                            if (!string.IsNullOrEmpty(videoDurationXml)) videoInfo.Length = Utils.PlainTextFromHtml(videoItems[i].SelectSingleNode(videoDurationXml).InnerText);
                            if (!string.IsNullOrEmpty(videoAirDateXml)) videoInfo.Airdate = Utils.PlainTextFromHtml(videoItems[i].SelectSingleNode(videoAirDateXml).InnerText);
                            if (!string.IsNullOrEmpty(videoDescriptionXml)) videoInfo.Description = videoItems[i].SelectSingleNode(videoDescriptionXml).InnerText;
                            videoList.Add(videoInfo);
                        }
                    }
                }
                else
                {
                    foreach (RssItem rssItem in RssToolkit.Rss.RssDocument.Load(data).Channel.Items)
                    {
                        VideoInfo video = VideoInfo.FromRssItem(rssItem, regEx_FileUrl != null, new Predicate<string>(isPossibleVideo));
                        // only if a video url was set, add this Video to the list
                        if (!string.IsNullOrEmpty(video.VideoUrl))
                        {
                            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 1)
                            {
                                video.Other = "PlaybackOptions://\n" + Utils.DictionaryToString(video.PlaybackOptions);
                            }
                            videoList.Add(video);
                        }
                    }
                }

                if (regEx_NextPage != null)
                {
                    // check for next page link
                    Match mNext = regEx_NextPage.Match(data);
                    if (mNext.Success)
                    {
                        string page = HttpUtility.ParseQueryString(new Uri(url).Query)["page"];
                        if (!url.Contains("search.php") && !string.IsNullOrEmpty(page))
                        {
                            nextPageAvailable = true;
                            nextPageUrl = url.Replace("page=" + page.ToString(), "page=" + (int.Parse(page) + 1).ToString());
                        }
                        else
                        {
                            nextPageAvailable = true;
                            nextPageUrl = mNext.Groups["url"].Value;
                            if (!string.IsNullOrEmpty(nextPageRegExUrlFormatString)) nextPageUrl = string.Format(nextPageRegExUrlFormatString, nextPageUrl);
							nextPageUrl = ApplyUrlDecoding(nextPageUrl, nextPageRegExUrlDecoding);
                            if (nextPageUrl.StartsWith("?")) nextPageUrl = "search.php" + nextPageUrl;
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
                        nextPageAvailable = false;
                        nextPageUrl = "";
                    }
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
