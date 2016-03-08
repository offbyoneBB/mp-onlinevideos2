using HtmlAgilityPack;
using System.Collections.Generic;
using System.Threading;
using OnlineVideos.Sites.JSurf.Extensions;
using OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Extensions
{
    /// <summary>
    /// Parse Amazon Prime videos
    /// </summary>
    public static class AmazonPrimeVideoParser
    {
        /// <summary>
        /// Load all video summary from the specified url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<VideoInfo> LoadVideosFromUrl(this string url, AmazonBrowserSession session)
        {
            var results = new List<VideoInfo>();
            HtmlDocument doc = null;
            var tmpWeb = session;
            HtmlNode detailNode = null;

            // Attempt the URL up to 15 times as amazon wants us to use the api!
            for (int i = 0; i <= 15; i++)
            {
                doc = tmpWeb.Load(url);
                detailNode = doc.GetElementbyId("aiv-main-content");

                if (detailNode == null)
                    Thread.Sleep(400);
                else
                    break;
            }

            if (detailNode != null)
            {
                var episodeContainer = doc.GetElementbyId("dv-episode-list");
                if (episodeContainer == null || (episodeContainer != null && episodeContainer.FindFirstChildElement() == null))
                {
                    // Movie, load this video
                    var video = new VideoInfo();

                    video.Title = detailNode.SelectSingleNode(".//h1[@id = 'aiv-content-title']").FirstChild.GetInnerTextTrim();
                    //doc.DocumentNode.GetNodeByClass("product_image").Attributes["alt"].Value;
                    var infoNode = detailNode.GetNodeByClass("dv-info");

                    var dvMetaInfo = infoNode.GetNodeByClass("dv-meta-info");
                    var altTitle = detailNode.NavigatePath(new[] { 0, 0 }).FirstChild.GetInnerTextTrim();
                    video.Description = string.Format("({0}amazon {1})\r\n{2}\r\n{3} {4}",
                        video.Title == altTitle ? "" : altTitle + ", ",
                        doc.GetElementbyId("summaryStars").FindFirstChildElement() == null ? string.Empty : doc.GetElementbyId("summaryStars").FindFirstChildElement().Attributes["title"].Value,
                        infoNode.GetNodeByClass("synopsis").GetInnerTextTrim(),
                        dvMetaInfo.NavigatePath(new[] { 0 }).GetInnerTextTrim(),
                        dvMetaInfo.NavigatePath(new[] { 1 }).GetInnerTextTrim());

                    var imageUrlNode = doc.GetElementbyId("dv-dp-left-content").GetNodeByClass("dp-meta-icon-container");
                    video.Thumb = imageUrlNode == null ? string.Empty : imageUrlNode.SelectSingleNode(".//img").Attributes["src"].Value;
                    video.Airdate = detailNode.GetNodeByClass("release-year").GetInnerTextTrim();
                    video.Length = dvMetaInfo.NavigatePath(new[] { 3 }).GetInnerTextTrim();
                    video.Other = doc.GetElementbyId("ASIN").Attributes["value"].Value;
                    results.Add(video);
                }
                else
                {
                    // TV Series, load all videos
                    var episodeList = episodeContainer.GetNodesByClass("episode-list-link");
                    int layoutType = 0;

                    if (episodeList == null)
                    {
                        layoutType = 1;
                        episodeList = episodeContainer.GetNodesByClass("episode-list-item-inner");
                    }
                    if (episodeList == null)
                    {
                        layoutType = 2;
                        episodeList = episodeContainer.GetNodesByClass("dv-episode-container");
                    }
                    if (episodeList == null)
                    {
                        Log.Error("Could not load episode list!");
                        return results;
                    }
                    foreach (var item in episodeList)
                    {
                        var video = new VideoInfo();
                        var titleNode =
                            layoutType == 0 ? item.GetNodeByClass("dv-extender").NavigatePath(new[] { 0, 0 }) :
                            layoutType == 1 ? item.GetNodeByClass("episode-title") :
                            item.GetNodeByClass("dv-el-title");

                        var seen = "";
                        /*if (item.GetNodeByClass("progress-bar") == null)
                        {
                            seen = " (new)";
                        }*/
                        video.Title = Regex.Replace(titleNode.GetInnerTextTrim(), @"^\d+", m => m.Value.PadLeft(2, '0')) + seen;

                        string videoUrl = null;
                        HtmlNode imageUrlNode = null;
                        if (layoutType == 2)
                        {
                            var synopsis = item.GetNodeByClass("dv-el-synopsis-content");
                            if (synopsis != null)
                            {
                                video.Description = synopsis.FirstChild.NextSibling.GetInnerTextTrim();
                            }
                            // <div class="dv-el-packshot-image" style="background-image: url(http://ecx.images-amazon.com/images/I/....jpg);"></div>
                            imageUrlNode = item.GetNodeByClass("dv-el-packshot-image");
                            if (imageUrlNode != null)
                            {
                                var re = new Regex("\\((.*?)\\)");
                                var htmlAttribute = imageUrlNode.GetAttributeValue("style", null);
                                if (htmlAttribute != null)
                                {
                                    var match = re.Match(htmlAttribute);
                                    if (match.Groups.Count == 2)
                                        video.ThumbnailImage = match.Groups[1].Value;
                                }
                            }
                            video.Length = item.GetNodeByClass("dv-el-runtime").GetInnerTextTrim();
                            var urlNode = item.GetNodeByClass("dv-playback-container");
                            if (urlNode != null)
                                videoUrl = urlNode.GetAttributeValue("data-asin", null);
                        }
                        else
                        {
                            video.Description = titleNode.NextSibling.GetInnerTextTrim();
                            video.Airdate = item.GetNodeByClass("release-date").GetInnerTextTrim();
                            imageUrlNode = item.GetNodeByClass("episode-list-image");
                            video.Length = item.GetNodeByClass("runtime").GetInnerTextTrim();
                            videoUrl = layoutType == 0 ? titleNode.GetAttribute("href") : item.GetAttribute("href");
                            videoUrl = videoUrl.Substring(videoUrl.IndexOf("/product/") + 9);
                            videoUrl = videoUrl.Substring(0, videoUrl.IndexOf("/"));

                            if (imageUrlNode != null)
                            {
                                video.Thumb = imageUrlNode.Attributes["src"].Value;
                            }
                            else
                            {
                                imageUrlNode = doc.GetElementbyId("dv-dp-left-content").GetNodeByClass("dp-meta-icon-container");
                                video.Thumb = imageUrlNode == null ? string.Empty : imageUrlNode.SelectSingleNode(".//img").Attributes["src"].Value;
                            }
                        }
                        video.Other = videoUrl;
                        video.CleanDescriptionAndTitle();
                        results.Add(video);
                    }
                }
            }


            return results;
        }
    }
}
