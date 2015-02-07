using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OnlineVideos.Sites.WebAutomation.Extensions;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrime.Extensions
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
        public static List<VideoInfo> LoadVideosFromUrl(this string url)
        {
            var results = new List<VideoInfo>();
            HtmlDocument doc = null;
            var tmpWeb = new HtmlWeb();
            HtmlNode detailNode = null;

            // Attempt the URL up to 15 times as amazon wants us to use the api!
            for (int i = 0; i <= 15; i++)
            {
                doc = tmpWeb.Load(url);
                detailNode = doc.GetElementbyId("dv-dp-main-content");

                if (detailNode == null)
                    Thread.Sleep(400);
                else
                    break;
            }

            if (detailNode != null)
            {
                var episodeContainer = doc.GetElementbyId("dv-episode-list");
                if (episodeContainer == null || (episodeContainer != null &&  episodeContainer.FindFirstChildElement() == null))
                { 
                    // Movie, load this video
                    var video = new VideoInfo();
                    
                    video.Title = doc.DocumentNode.GetNodeByClass("product_image").Attributes["alt"].Value;
                    var infoNode = detailNode.NavigatePath(new[] { 3, 2, 0 });

                    video.Description = video.Title + " (" + detailNode.NavigatePath(new[] { 0, 0 }).FirstChild.GetInnerText().Replace("\n", string.Empty).Trim() + ", imdb " +
                                                            detailNode.NavigatePath(new[] { 3, 0, 1, 1 }).GetInnerText().Replace("\n", string.Empty) + 
                                                            ", amazon " + (doc.GetElementbyId("summaryStars").FindFirstChildElement() == null ? string.Empty : doc.GetElementbyId("summaryStars").FindFirstChildElement().Attributes["title"].Value) + ")\r\n" +
                                                            infoNode.GetNodeByClass("synopsis").GetInnerText().Replace("\n", string.Empty).Trim() + "\r\n" +
                                                            infoNode.GetNodeByClass("dv-meta-info").NavigatePath(new[] { 0 }).GetInnerText().Replace("\n", string.Empty).Trim() + " " +
                                                            infoNode.GetNodeByClass("dv-meta-info").NavigatePath(new[] { 1 }).GetInnerText().Replace("\n", string.Empty).Trim();

                    var imageUrlNode = detailNode.NavigatePath(new[] { 3, 1, 0, 0, 1 });
                    video.Thumb = imageUrlNode == null ? string.Empty : imageUrlNode.Attributes["src"].Value;
                    video.Length = infoNode.NavigatePath(new[] { 2, 3 }).GetInnerText().Replace("\n", string.Empty).Trim();
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
                        var titleNode = usesAltLayout ? item.GetNodeByClass("dv-extender").NavigatePath(new[]{0,0}) : item.GetNodeByClass("episode-title");
                         
                        video.Title = titleNode.GetInnerText().Trim();

                        video.Description = titleNode.NextSibling.GetInnerText().Replace("\n", string.Empty).Trim() + "\r\n" +
                                             "Released: " + item.GetNodeByClass("release-date").GetInnerText().Replace("\n", string.Empty).Trim();

                        var imageUrlNode = doc.GetElementbyId("dv-dp-left-content").GetNodeByClass("dp-meta-icon-container");
                        video.Thumb = imageUrlNode == null ? string.Empty : imageUrlNode.FindAllChildElements()[1].Attributes["src"].Value;
                        video.Length = item.GetNodeByClass("runtime").GetInnerText().Replace("\n", string.Empty).Trim();
                        var videoUrl = usesAltLayout ? titleNode.GetAttribute("href") : item.GetAttribute("href");
                        videoUrl = videoUrl.Substring(videoUrl.IndexOf("/product/") + 9);
                        videoUrl = videoUrl.Substring(0, videoUrl.IndexOf("/"));
                        video.Other = videoUrl;
                        results.Add(video);
                    }
                }
            }
            
            
            return results;
        }
    }
}
