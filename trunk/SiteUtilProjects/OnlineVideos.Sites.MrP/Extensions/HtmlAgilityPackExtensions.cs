using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation.Extensions
{
    /// <summary>
    /// Extension methods for htmlagilitypack
    /// </summary>
    public static class HtmlAgilityPackExtensions
    {
        /// <summary>
        /// Select all elements with the specified tag
        /// </summary>
        /// <param name="document"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static HtmlNodeCollection GetElementsByTagName(this HtmlDocument document, string tagName)
        {
            return document.DocumentNode.SelectNodes("//" + tagName);
        }

        /// <summary>
        /// Select all elements with the specified id
        /// </summary>
        /// <param name="document"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static HtmlNode GetElementById(this HtmlDocument document, string id)
        {
            return document.GetElementbyId(id);
        }

        /// <summary>
        /// Get a node attribute
        /// </summary>
        /// <param name="node"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public static string GetAttribute(this HtmlNode node, string attributeName)
        {
            if (node.Attributes.Count > 0 && node.Attributes.Contains(attributeName)) return node.Attributes[attributeName].Value;
            return string.Empty;
        }

        /// <summary>
        /// Find the first descendent node which has the specified class
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public static HtmlNode GetNodeByClass(this HtmlNode node, string className)
        {
            var result = node.DescendantNodes().Where(x => x.GetAttribute("class") == className
                                                            || x.GetAttribute("class").Contains(" " + className)
                                                            || x.GetAttribute("class").Contains(className + " "));
            if (result == null || result.Count() == 0)
                return null;
            return result.First();
        }
    }
}
