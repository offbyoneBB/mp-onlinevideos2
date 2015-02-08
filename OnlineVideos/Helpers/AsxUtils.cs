using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Helpers
{
    public static class AsxUtils
    {
        public static List<String> ParseASX(string data)
        {
            string asxData = data.ToLower();
            MatchCollection videoUrls = Regex.Matches(asxData, @"<ref\s+href\s*=\s*\""(?<url>[^\""]*)");
            List<String> urlList = new List<String>();
            foreach (Match videoUrl in videoUrls)
            {
                urlList.Add(videoUrl.Groups["url"].Value);
            }
            return urlList;
        }

        public static string ParseASX(string data, out string startTime)
        {
            startTime = "";
            string asxData = data.ToLower();
            XmlDocument asxDoc = new XmlDocument();
            asxDoc.LoadXml(asxData);
            XmlElement entryElement = asxDoc.SelectSingleNode("//entry") as XmlElement;
            if (entryElement == null) return "";
            XmlElement refElement = entryElement.SelectSingleNode("ref") as XmlElement;
            if (entryElement == null) return "";
            XmlElement startElement = entryElement.SelectSingleNode("starttime") as XmlElement;
            if (startElement != null) startTime = startElement.GetAttribute("value");
            return refElement.GetAttribute("href");
        }
    }
}
