
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.WebAutomation.Extensions;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Extensions
{
    /// <summary>
    /// Parse the details from the VideoDetails page
    /// </summary>
    public static class SkyGoVideoParser
    {
        /// <summary>
        /// Load information about a video from the video details page
        /// </summary>
        /// <param name="document"></param>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public static VideoInfo LoadVideoFromDocument(this HtmlDocument document, string videoId)
        {
            var result = new VideoInfo();
            result.Description = GetDescription(document);
            result.Title = GetTitle(document);
            result.ImageUrl = GetImageUrl(document);
            result.Length = "";
            result.Other = videoId;
            return result;
        }

        /// <summary>
        /// Load the image url
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string GetImageUrl(HtmlDocument document)
        {
            var result = string.Empty;
            var nodes = document.GetElementsByTagName("img");
            if (nodes == null) return string.Empty;
            foreach (HtmlNode item in nodes)
            {
                if (item.GetAttribute("class") == "ATI_vdImage")
                    return (item.GetAttribute("src").StartsWith("http") ? string.Empty : Properties.Resources.SkyGo_RootUrl) + item.GetAttribute("src");
            }
            return result;
        }
        
        /// <summary>
        /// Load the title
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string GetTitle(HtmlDocument document)
        {
            var result = string.Empty;
            var nodes = document.GetElementsByTagName("h1");
            if (nodes == null) return string.Empty;
            foreach (HtmlNode item in nodes)
            {
                if (item.GetAttribute("class").Contains("ATI_videoTitle"))
                {
                    result += CleanText(item.InnerText) + " ";
                    break;
                }
            }

            // Now lookup the certificate
            nodes = document.GetElementsByTagName("li");
            if (nodes == null) return string.Empty;
            foreach (HtmlNode item in nodes)
            {
                if (item.GetAttribute("class") == "ATI_metaDataCertificate")
                {
                    result += "&nbsp;(" + CleanText(item.InnerText).Replace("Cert ", "") + ")";
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Get the description - may contain multiple elements (for movies in particular)
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string GetDescription(HtmlDocument document)
        {
            var result = string.Empty;
            var nodes = document.GetElementsByTagName("div");
            if (nodes == null) return string.Empty;
            foreach (HtmlNode item in nodes)
            {
                if (item.GetAttribute("class") == "synopsis")
                {
                    result += item.ChildNodes[0].InnerHtml;

                    // Load extra info from the list adjacent
                    if (item.ChildNodes[1] != null)
                    {
                        foreach (HtmlNode listItem in item.ChildNodes[1].ChildNodes)
                        {
                            result += "\r\n";
                            result += CleanText(listItem.InnerText);
                        }
                    }
                }
            }

            // Now lookup the file size
            nodes = document.GetElementsByTagName("li");
            if (nodes == null) return string.Empty;
            foreach (HtmlNode item in nodes)
            {
                if (item.GetAttribute("class") == "ATI_metaDataSize")
                {
                    result += "\r\n" + CleanText(item.InnerText);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Just remove some characters we don't need
        /// </summary>
        /// <param name="textToClean"></param>
        /// <returns></returns>
        private static string CleanText(string textToClean)
        {
            return textToClean.Replace("\r", "").Replace("\n", "").Replace("\t", "");
        }
    }
}
