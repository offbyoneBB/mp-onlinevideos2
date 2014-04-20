using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using OnlineVideos.Sites.WebAutomation.Extensions;
using HtmlAgilityPack;
using System.Net;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations._4OD.Extensions
{
    /// <summary>
    /// Methods for Parsing the 4OD api for videos
    /// </summary>
    public static class _4ODVideoParser
    {
        /// <summary>
        /// Load the videos for the selected category - will only handle general category types
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public static List<VideoInfo> LoadGeneralVideos(Category parentCategory)
        {
            var doc = new XmlDocument();
            var result = new List<VideoInfo>();
            var path = "/brandLongFormInfo/allEpisodes/longFormEpisodeInfo"; // default the path for items without series

            doc.Load(parentCategory.CategoryInformationPage());

            if (!string.IsNullOrEmpty(parentCategory.SeriesId()))
            {
                path = "/brandLongFormInfo/allSeries/longFormSeriesInfo[seriesNumber='" + parentCategory.SeriesId() + "'] /episodes/longFormEpisodeInfo";
            }

            foreach (XmlNode node in doc.SelectNodes(path))
            {
                var item = new VideoInfo();
                item.Title = node.SelectSingleNodeText("title1") + (string.IsNullOrEmpty(node.SelectSingleNodeText("title2")) ? string.Empty : " - ") + node.SelectSingleNodeText("title2"); 
                item.Description = node.SelectSingleNodeText("synopsis");
                //item.ImageUrl = Properties.Resources._4OD_RootUrl + node.SelectSingleNodeText("pictureUrl");
                item.ImageUrl = node.SelectSingleNodeText("pictureUrl");

                DateTime airDate;

                if (DateTime.TryParse(node.SelectSingleNodeText("txTime"), out airDate))
                    item.Airdate = airDate.ToString("dd MMM yyyy");

                item.Other = doc.SelectSingleNodeText("/brandLongFormInfo/brandWst") + "~" + node.SelectSingleNodeText("requestId");
                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Load videos for the specified collection
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public static List<VideoInfo> LoadCollectionVideos(Category parentCategory)
        {
            var doc = new XmlDocument();
            var result = new List<VideoInfo>();
            var path = "/collectionInfo/longformEpisodes/simpleAssetInfo"; // default the path for collections

            doc.Load(parentCategory.CategoryInformationPage());

            foreach (XmlNode node in doc.SelectNodes(path))
            {
                var item = new VideoInfo();
                item.Title = node.SelectSingleNodeText("title1") + (string.IsNullOrEmpty(node.SelectSingleNodeText("title2")) ? string.Empty : " - ") + node.SelectSingleNodeText("title2");
                item.Description = node.SelectSingleNodeText("synopsis");
                //item.ImageUrl = Properties.Resources._4OD_RootUrl + node.SelectSingleNodeText("imagePath");
                item.ImageUrl = node.SelectSingleNodeText("imagePath");

                item.Other = MakeWebSafe(node.SelectSingleNodeText("brandTitle")) + "~" + node.SelectSingleNodeText("assetId");
                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Load videos for the specified catch up category (day/channel)
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public static List<VideoInfo> LoadCatchUpVideos(Category parentCategory)
        {
            var doc = new HtmlDocument();
            var results = new List<VideoInfo>();
            var webRequest = (HttpWebRequest)WebRequest.Create(parentCategory.CategoryInformationPage());
            var webResponse = (HttpWebResponse)webRequest.GetResponse();

            if (webResponse.StatusCode != HttpStatusCode.OK)
                throw new OnlineVideosException("Unable to retrieve response for 4OD Catch Up Video from " + parentCategory.CategoryInformationPage() + ", received " + webResponse.StatusCode.ToString());

            doc.Load(webResponse.GetResponseStream());

            var node = doc.GetElementsByTagName("span").Where(x => x.GetAttribute("class").Contains("tx-" + parentCategory.CategoryId())).FirstOrDefault();

            if (node != null)
            {
                foreach (HtmlNode listItem in node.ParentNode.ParentNode.SelectSingleNode("ul").SelectNodes("li"))
                {
                    var item = new VideoInfo();
                    item.Title = listItem.GetNodeByClass("title").InnerText + (listItem.GetNodeByClass("series-info") == null ? string.Empty : " - " + listItem.GetNodeByClass("series-info").InnerText);
                    item.Description = listItem.GetNodeByClass("synopsis").InnerText;
                    item.Airdate = listItem.GetNodeByClass("txtime").InnerText;
                    //item.ImageUrl = Properties.Resources._4OD_RootUrl + listItem.SelectSingleNode("a/img").GetAttribute("src");
                    item.ImageUrl = listItem.SelectSingleNode("a/img").GetAttribute("src");
                    item.Other = MakeWebSafe(listItem.GetNodeByClass("title").InnerText) + "~" + listItem.SelectSingleNode("a").GetAttribute("href").Split('#')[1];
                    results.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// 4OD seems to require lower-case text with "-" instead of space and the single quotes removed as part of the collection URL
        /// Unfortunately, the xml doesn't supply this text for collections 
        /// </summary>
        /// <param name="stringToFix"></param>
        /// <returns></returns>
        private static string MakeWebSafe(string stringToFix)
        {
            return stringToFix.ToLower().Replace(" ", "-").Replace("'", "").Replace(":", "").Replace(".", "").Replace(",", "").Replace("&amp;", "").ToLower();
        }
    }
}
