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
                    var usesAltLayout = false;

                    if (episodeList == null)
                    {
                        usesAltLayout = true;
                        episodeList = episodeContainer.GetNodesByClass("episode-list-item-inner");
                    }

                    foreach (var item in episodeList)
                    {
                        var video = new VideoInfo();
                        var titleNode = usesAltLayout ? item.GetNodeByClass("dv-extender").NavigatePath(new[] { 0, 0 }) : item.GetNodeByClass("episode-title");

                        var seen = "";
                        /*if (item.GetNodeByClass("progress-bar") == null)
                        {
                            seen = " (new)";
                        }*/
                        video.Title = Regex.Replace(titleNode.GetInnerTextTrim(), @"^\d+", m => m.Value.PadLeft(2, '0')) + seen;

                        video.Description = titleNode.NextSibling.GetInnerTextTrim();
                        video.Airdate = item.GetNodeByClass("release-date").GetInnerTextTrim();

                        var imageUrlNode = item.GetNodeByClass("episode-list-image");
                        if (imageUrlNode != null)
                        {
                            video.Thumb = imageUrlNode.Attributes["src"].Value;
                        }
                        else
                        {
                            imageUrlNode = doc.GetElementbyId("dv-dp-left-content").GetNodeByClass("dp-meta-icon-container");
                            video.Thumb = imageUrlNode == null ? string.Empty : imageUrlNode.SelectSingleNode(".//img").Attributes["src"].Value;
                        }
                        var videoUrl = usesAltLayout ? titleNode.GetAttribute("href") : item.GetAttribute("href");
                        videoUrl = videoUrl.Substring(videoUrl.IndexOf("/product/") + 9);
                        videoUrl = videoUrl.Substring(0, videoUrl.IndexOf("/"));
                        video.Other = videoUrl;
                        video.Length = item.GetNodeByClass("runtime").GetInnerTextTrim();
                        video.CleanDescriptionAndTitle();
                        results.Add(video);
                    }
                }
            }


            return results;
        }
    }
}
