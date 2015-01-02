using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using System.Globalization;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Extensions
{
    /// <summary>
    /// Extension methods for the HtmlAgilityPack
    /// </summary>
    public static class SkyGoHtmlAgilityPackExtensions
    {

        /// <summary>
        /// Loads the content zone item from the specified url into a HtmlDocument
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static HtmlDocument LoadSkyGoContentFromUrl(this string url, string zoneName = "content")
        {
            var xmlDoc = new XmlDocument();
            // Connect to the api and download the content
            xmlDoc.Load(url);

            var redirectNode = xmlDoc.GetElementsByTagName("redirect");
            // Sometimes we get a redirect response
            if (redirectNode.Count > 0 && redirectNode[0].Attributes["fullpageredirect"].Value == "true")
            {
                var tmpUrl = new Uri(url);
                xmlDoc.Load(tmpUrl.Scheme + "://" + tmpUrl.Authority + redirectNode[0].InnerText + "&aaxmlrequest=true");
            }

            var zoneNodes = xmlDoc.GetElementsByTagName("zone");
            var content = "";
            foreach (XmlNode zone in zoneNodes)
            {
                if (zone.Attributes.Count > 0 && zone.Attributes[0].Value == zoneName)
                {
                    content = zone.InnerText.Replace("\r", "").Replace("\t", "").Replace("\n", "");
                    break;
                }
            }

            HtmlWeb hw = new HtmlWeb();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(content);
            return doc;
        }

        /// <summary>
        /// The channel info is buried in the page within a Json object string (well there is an api, but it doesn't return the video id)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<SkyGoLiveTvChannelItem> LoadSkyGoLiveTvChannelsFromUrl(this string url)
        {
            var doc = new HtmlDocument();
            var results = new List<SkyGoLiveTvChannelItem>();
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            var webResponse = (HttpWebResponse)webRequest.GetResponse();

            if (webResponse.StatusCode != HttpStatusCode.OK)
                throw new OnlineVideosException("Unable to retrieve response for Sky Go Live Tv from " + url + ", received " + webResponse.StatusCode.ToString());

            doc.Load(webResponse.GetResponseStream());

            var docText = doc.DocumentNode.InnerText;

            var position = docText.IndexOf("{\\\"silverlightConfig\\\":");
            if (position == -1) throw new ApplicationException("Unable to retrieve the silverlight configuration element from the page");

            var endPosition = docText.IndexOf(@"\""prod\""}}", position);
            if (position == -1) throw new ApplicationException("Unable to retrieve the end silverlight configuration element/channel list from the page");

            var silverlightSettings = docText.Substring(position, endPosition - position + 10).Replace("\\", string.Empty);

            var silverlightSettingsAsXml = JsonConvert.DeserializeXmlNode(silverlightSettings);

            foreach (XmlNode channel in silverlightSettingsAsXml.GetElementsByTagName("channelList"))
            {
                var newItem = new SkyGoLiveTvChannelItem();
                foreach (XmlNode channelItem in channel.ChildNodes)
                {
                    switch (channelItem.Name)
                    {
                        case "epgName":
                            newItem.ChannelName = channelItem.FirstChild.InnerText;
                            break;
                        case "epgChid":
                            newItem.ChannelId = channelItem.FirstChild.InnerText;
                            break;
                        case "mpodChid":
                            newItem.ChannelVideoId = channelItem.FirstChild.InnerText;
                            break;
                    }
                }
                newItem.ChannelImageUrl = Properties.Resources.SkyGoLiveTvImageUrl(newItem.ChannelId);
                if (!string.IsNullOrEmpty(newItem.ChannelId)) results.Add(newItem);
            }

            return results;
        }

        /// <summary>
        /// Get the now/next information and convert it into a VideoInfo object
        /// </summary>
        /// <param name="url"></param>
        /// <param name="channels"></param>
        /// <returns></returns>
        public static List<VideoInfo> LoadSkyGoLiveTvNowNextVideosFromUrl(this string url, List<SkyGoLiveTvChannelItem> channels)
        {
            var xmlDoc = new XmlDocument();
            var results = new List<VideoInfo>();

            // Connect to the api and download the content
            xmlDoc.Load(url);

            foreach (XmlNode channel in xmlDoc.GetElementsByTagName("channel"))
            {
                var channelItem = channels.Where(x => x.ChannelId == channel.GetSingleNodeText("*[name()='channelId']")).FirstOrDefault();

                if (channelItem != null)
                {
                    var video = new VideoInfo();
                    var titleAdditional = string.Empty;

                    video.Description = channel.SelectNodes("*[name()='programme']").GetLiveTvDescription(out titleAdditional);

                    video.ImageUrl = channelItem.ChannelImageUrl;
                    video.Title = channelItem.ChannelName + " (" + titleAdditional + ")";
                    video.Other = "LTV~" + channelItem.ChannelVideoId;

                    results.Add(video);

                }
            }

            return results;
        }

        /// <summary>
        /// Get the innertext of a node, or empty string if the node isn't found
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static string GetSingleNodeText(this XmlNode node, string xPath)
        {
            return (node.SelectSingleNode(xPath) == null ? string.Empty : node.SelectSingleNode(xPath).InnerText);
        }

        /// <summary>
        /// Build a description and title based on the now/next node list
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        private static string GetLiveTvDescription(this XmlNodeList nodes, out string title)
        {
            var mainDescription = string.Empty;
            var topPart = string.Empty;
            title = string.Empty;

            // Only do now/next, assume 1st 2 entries
            for (int i = 0; i < 2; i++)
            {
                if (nodes.Count > (i + 1))
                {
                    XmlNode programme = nodes[i];
                    
                    topPart += (i == 0 ? "Now: " : "Next: ");
                    var tmpTop = programme.GetSingleNodeText("*[name()='title']");
                    var startTime = DateTime.ParseExact(programme.GetSingleNodeText("*[name()='startDateTime']"), "yyyyMMddHHmmss", CultureInfo.CurrentCulture);
                    var duration = int.Parse(programme.GetSingleNodeText("*[name()='duration']"));
                    tmpTop += " - " + startTime.ToString("HH:mm");
                    tmpTop += " to " + startTime.AddSeconds(duration).ToString("HH:mm");

                    topPart += tmpTop;

                    if (string.IsNullOrEmpty(title))
                        title = tmpTop;

                    topPart += "\r\n";

                    if (string.IsNullOrEmpty(mainDescription))
                        mainDescription = programme.GetSingleNodeText("*[name()='shortDesc']");
                }
            }

            return topPart + mainDescription;        
        }
    }
}
