using HtmlAgilityPack;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using OnlineVideos.Sites.JSurf.Extensions;
using OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors;
using System.Text.RegularExpressions;
using OnlineVideos.Sites.JSurf.Entities;

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
        /// <param name="session"></param>
        /// <returns></returns>
        public static List<VideoInfo> LoadVideosFromUrl(this string url, AmazonBrowserSession session)
        {
            var results = new List<VideoInfo>();
            HtmlDocument doc = null;
            var tmpWeb = session;
            HtmlNode detailNode = null;

            doc = tmpWeb.Load(url);

            var episodeContainer = doc.GetElementbyId("dv-episode-list");
            if (episodeContainer == null || episodeContainer.FindFirstChildElement() == null)
            {
                //detailNode = doc.GetElementbyId("av-dp-container");
                detailNode = doc.DocumentNode.GetNodeByClass("av-dp-container");
                if (detailNode == null)
                    return results;

                // Movie, load this video
                var video = new VideoInfo();

                video.Title = detailNode.SelectSingleNode(".//h1[@data-automation-id='title']")?.FirstChild?.GetInnerTextTrim();
                var description =  detailNode.SelectSingleNode(".//div[@data-automation-id='synopsis']")?.FirstChild?.GetInnerTextTrim();
                video.Description = video.Title;

                var ratingNode = detailNode.GetNodeByClass("av-icon--amazon_rating");
                var starsCls = ratingNode?.GetAttribute("class").Split(' ').FirstOrDefault(c => c.StartsWith("av-stars-"));
                if (starsCls != null)
                    video.Description += " - " + starsCls.Replace("av-stars-", "").Replace("-", ".") + "/5";

                foreach (var textSpan in detailNode.GetNodesByClass("av-badge-text"))
                    video.Description += " - " + textSpan.GetInnerTextTrim();

                video.Description += "\r\n" + description;

                var imageUrlNode = detailNode.GetNodeByClass("av-fallback-packshot");
                video.Thumb = imageUrlNode == null ? string.Empty : imageUrlNode.SelectSingleNode(".//img").Attributes["src"].Value;
                video.Airdate = detailNode.SelectSingleNode(".//div[@data-automation-id='release-year-badge']")?.FirstChild?.GetInnerTextTrim();
                video.Other = detailNode.SelectSingleNode(".//a[@data-ref='atv_dp_stream_prime_movie']")?.Attributes["data-page-title-id"]?.Value;
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
                    var extendedProperties = new ExtendedProperties();
                    var video = new VideoInfo { Other = extendedProperties };
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
                    HtmlNode imageUrlNode;
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

                        // Certification, can be different classes,i.e. dv-ages_16_and_over
                        var certificationClasses = item.GetNodesByClass("dv-el-badge dv-ages_", true);
                        if (certificationClasses != null)
                        {
                            var certification = certificationClasses.First();
                            extendedProperties.VideoProperties["Certificate"] = certification.GetInnerTextTrim();
                        }

                        // Playback progress
                        extendedProperties.VideoProperties["Progress"] = string.Format("{0:0}%", 0);
                        var progress = item.GetNodesByClass("dv-linear-progress");
                        if (progress != null && progress.Count > 0)
                        {
                            var progressSpan = progress[0].ChildNodes.FirstOrDefault(n => n.Name == "span");
                            if (progressSpan != null)
                            {
                                var width = progressSpan.Attributes["style"].Value;
                                double percent;
                                if (double.TryParse(width.Replace("width:", "").Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out percent))
                                {
                                    extendedProperties.VideoProperties["Progress"] = string.Format("{0:0}%", percent);
                                }
                            }
                        }

                        var tagValues = item.GetNodesByClass("dv-el-attr-value");
                        if (tagValues.Count == 3)
                        {
                            video.Airdate = tagValues[2].GetInnerTextTrim();
                            video.Length = tagValues[1].GetInnerTextTrim();
                        }
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
                    extendedProperties.Other = videoUrl;
                    video.CleanDescriptionAndTitle();
                    results.Add(video);
                }
            }

            return results;
        }
    }
}
