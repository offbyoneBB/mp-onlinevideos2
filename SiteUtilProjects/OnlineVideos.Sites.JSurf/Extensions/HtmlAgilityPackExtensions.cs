using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.Sites.JSurf.Extensions
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
            if (node != null && node.Attributes.Count > 0 && node.Attributes.Contains(attributeName)) return node.Attributes[attributeName].Value;
            return string.Empty;
        }

        /// <summary>
        /// Find the first descendent node which has the specified class
        /// </summary>
        /// <param name="node"></param>
        /// <param name="className"></param>
        /// <param name="allowPartialMatch">if <c>true</c>, also partial class names will be matched (no trailing/leading space)</param>
        /// <returns></returns>
        public static HtmlNode GetNodeByClass(this HtmlNode node, string className, bool allowPartialMatch = false)
        {
            var allNodes = GetNodesByClass(node, className);
            return allNodes != null ? allNodes.FirstOrDefault() : null;
        }

        /// <summary>
        /// Find all nodes matching a class
        /// </summary>
        /// <param name="node"></param>
        /// <param name="className"></param>
        /// <param name="allowPartialMatch">if <c>true</c>, also partial class names will be matched (no trailing/leading space)</param>
        /// <returns></returns>
        public static List<HtmlNode> GetNodesByClass(this HtmlNode node, string className, bool allowPartialMatch = false)
        {
            var results = node.DescendantNodes().Where(x =>
            {
                var clsAttr = x.GetAttribute("class") ?? "";
                return clsAttr == className
                       || clsAttr.Contains((allowPartialMatch ? "" : " ") + className)
                       || clsAttr.Contains(className + (allowPartialMatch ? "" : " "));
            }).ToList();
            if (results.Count == 0)
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
            if (node == null) return null;
            return node.ChildNodes.FirstOrDefault(childNode => childNode.NodeType == HtmlNodeType.Element);
        }

        /// <summary>
        /// Find the first child which is a html element (not text or script blocks)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static List<HtmlNode> FindAllChildElements(this HtmlNode node)
        {
            return node.ChildNodes.Where(childNode => childNode.NodeType == HtmlNodeType.Element && childNode.Name != "script").ToList();
        }

        /// <summary>
        /// Find all child nodes of the given node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static List<HtmlNode> FindAllChildElementsRecursive(this HtmlNode node, Func<HtmlNode, bool> filter)
        {
            var htmlNodes = FindAllChildElements(node).Where(filter).ToList();
            foreach (HtmlNode childNode in node.ChildNodes)
                htmlNodes.AddRange(FindAllChildElementsRecursive(childNode, filter));
            return htmlNodes;
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
        /// Get the innertext of the node, or an empty string if null. Result will be cleared from newlines and trimmed by spaces.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetInnerTextTrim(this HtmlNode node)
        {
            string result = GetInnerText(node);
            return result.Replace("\n", string.Empty).Trim();
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
