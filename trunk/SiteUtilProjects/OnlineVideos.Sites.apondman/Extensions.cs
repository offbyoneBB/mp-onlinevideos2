using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Pondman;
using HtmlAgilityPack;
using System.Web;

namespace OnlineVideos.Sites.Pondman
{
    /// <summary>
    /// Extension utility container for SiteUtils in this library
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Creates a comma seperated string using the elements of the collection
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string ToCommaSeperatedString(this List<string> self)
        {
            return self.Count > 0 ? self.ToString(", ") : " ";
        }

        /// <summary>
        /// Joins a string[] together with the the given seperator
        /// </summary>
        /// <param name="seperator"></param>
        /// <returns>string output</returns>
        public static string ToString(this List<string> self, string seperator)
        {
            return string.Join(seperator, self.ToArray());
        }
        
        /// <summary>
        /// Converts a HtmlNodeCollection to a generic string list
        /// </summary>
        /// <param name="nodeList"></param>
        /// <param name="stringList"></param>
        public static List<string> ToStringList(this IList<HtmlNode> self)
        {
            return self.Select(n => HttpUtility.HtmlDecode(n.InnerText.Trim())).ToList();
        }

    }
}
