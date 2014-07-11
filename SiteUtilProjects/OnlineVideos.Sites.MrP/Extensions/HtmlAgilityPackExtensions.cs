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

        /// <summary>
        /// Find all nodes matching a class
        /// </summary>
        /// <param name="node"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        public static List<HtmlNode> GetNodesByClass(this HtmlNode node, string className)
        {
            var results = node.DescendantNodes().Where(x => x.GetAttribute("class") == className
                                                            || x.GetAttribute("class").Contains(" " + className)
                                                            || x.GetAttribute("class").Contains(className + " "));
            if (results == null || results.Count() == 0)
                return null;
            return results.ToList();
        }

        /// <summary>
        /// Find the first child which is a html element (not text)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static HtmlNode FindFirstChildElement(this HtmlNode node)
        {
            foreach (HtmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType == HtmlNodeType.Element)
                {
                    return childNode;
                }
            }
            return null;
        }

        /// <summary>
        /// Find the first child which is a html element (not text)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static List<HtmlNode> FindAllChildElements(this HtmlNode node)
        {
            var result = new List<HtmlNode>();
            foreach (HtmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType == HtmlNodeType.Element)
                    result.Add(childNode);
            }
            return result;
        }

        /// <summary>
        /// Get the innertext of the node, or an empty string if null
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetInnerText(this HtmlNode node)
        {
            if (node == null) return string.Empty;
            if (string.IsNullOrEmpty(node.InnerText)) return string.Empty;
            return node.InnerText;
        }

        /// <summary>
        /// Navigate down a path using the position of child elements
        /// </summary>
        /// <param name="node"></param>
        /// <param name="childPositions"></param>
        /// <returns></returns>
        public static HtmlNode NavigatePath(this HtmlNode node, int[] childPositions)
        {
            HtmlNode currNode = node;
            if (node == null) return null;
            foreach (var pos in childPositions)
            {
                var tmpNode = currNode.FindAllChildElements();
                if (tmpNode.Count >= (pos + 1))
                    currNode = tmpNode[pos];
                else
                    return null;
            }
            return currNode;
        }
    }
}
