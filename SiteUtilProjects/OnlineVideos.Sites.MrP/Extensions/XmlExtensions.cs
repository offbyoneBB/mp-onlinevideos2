using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OnlineVideos.Sites.WebAutomation.Extensions
{
    /// <summary>
    /// Extension methods for xml Documents
    /// </summary>
    public static class XmlExtensions
    {
        /// <summary>
        /// Helper which will select the inner text of a single node, or return the string defined in the resultIfNull 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="path"></param>
        /// <param name="resultIfNull"></param>
        /// <returns></returns>
        public static string SelectSingleNodeText(this XmlDocument doc, string path, string resultIfNull = "")
        {
            var node = doc.SelectSingleNode(path);
            if (node == null) return resultIfNull;
            return node.InnerText;
        }

        /// <summary>
        /// Helper which will select the inner text of a single node, or return the string defined in the resultIfNull 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="path"></param>
        /// <param name="resultIfNull"></param>
        /// <returns></returns>
        public static string SelectSingleNodeText(this XmlNode node, string path, string resultIfNull = "")
        {
            var resultNode = node.SelectSingleNode(path);
            if (resultNode == null) return resultIfNull;
            return resultNode.InnerText;
        }
    }
}
