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

            // Attempt the URL up to 10 times as amazon wants us to use the api!
            for (int i = 0; i <= 10; i++)
            {
                doc = tmpWeb.Load(url);
                detailNode = doc.GetElementbyId("dv-dp-main-content");

                if (detailNode == null)
                    Thread.Sleep(200);
                else
                    break;
            }

            if (detailNode != null)
            {
                var episodeList = doc.GetElementbyId("dv-episode-list").FindAllChildElements();
                if (episodeList.Count == 0)
                { 
                    // Movie, load this video
                    var video = new VideoInfo();
                    //video.Title = detailNode.NavigatePath(new[] {0, 0}).FirstChild.GetInnerText().Replace("\n", string.Empty).Trim();
                    video.Title = doc.DocumentNode.GetNodeByClass("product_image").Attributes["alt"].Value;
                    var infoNode = detailNode.NavigatePath(new[] { 3, 2, 0 });
                    video.Description = video.Title + " (" + detailNode.NavigatePath(new[] { 0, 0 }).FirstChild.GetInnerText().Replace("\n", string.Empty).Trim() + ", imdb " +
                                                            detailNode.NavigatePath(new[] { 3, 0, 1, 1 }).GetInnerText().Replace("\n", string.Empty) + 
                                                            ", amazon " + doc.GetElementbyId("summaryStars").FindFirstChildElement().Attributes["title"].Value + ")\r\n" +
                                                            infoNode.NavigatePath(new[] { 1, 0 }).GetInnerText().Replace("\n", string.Empty).Trim() + "\r\n" +
                                                            infoNode.NavigatePath(new[] { 2, 0 }).GetInnerText().Replace("\n", string.Empty).Trim() + " " +
                                                            infoNode.NavigatePath(new[] { 2, 1 }).GetInnerText().Replace("\n", string.Empty).Trim();

                    var imageUrlNode = detailNode.NavigatePath(new[] { 3, 1, 0, 0, 1 });
                    video.ImageUrl = imageUrlNode == null ? string.Empty : imageUrlNode.Attributes["src"].Value;
                    video.Length = infoNode.NavigatePath(new[] { 2, 3 }).GetInnerText().Replace("\n", string.Empty).Trim();
                    video.Other = doc.GetElementbyId("ASIN").Attributes["value"].Value;
                    results.Add(video);
                }
                else
                {
                    // TV Series, load all videos
                }
            }
            
            
            return results;
        }
    }
}
