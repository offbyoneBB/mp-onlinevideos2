using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Brownard.Extensions
{
    static class ExtensionMethods
    {
        public static string GetCleanInnerText(this HtmlNode node)
        {
            if (node == null)
                return "";
            return node.InnerText.HtmlCleanup();
        }

        public static string HtmlCleanup(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("&amp;", "&").Replace("&pound;", "£").Replace("&hellip;", "...").Replace("&#39;", "'").Replace("&#x27;", "'").Trim();
        }

        public static string ParamsCleanup(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("&amp;", "&").Replace("&#x3D;", "=").Trim();
        }
    }
}