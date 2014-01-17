using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

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
    }
}
