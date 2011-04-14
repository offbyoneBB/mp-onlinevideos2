using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace OnlineVideos.Sites.Pondman
{
    public static class Utility
    {

        public static string UrlEncode(string self)
        {
            return System.Web.HttpUtility.UrlEncode(self).Replace("+", "%20");
        }

        public static HtmlNode ToHtmlNode(string data)
        {
            if (String.IsNullOrEmpty(data))
                return null;

            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.OptionOutputAsXml = true;
                doc.LoadHtml(data);

                return doc.DocumentNode;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
