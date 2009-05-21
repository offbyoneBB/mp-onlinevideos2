using System;
using System.Collections.Generic;
using System.Xml;

namespace OnlineVideos
{
    public class RssItem
    {
        public String title;
        public String link;
        public String guid;
        public String pubDate;
        public String description;
        public String author;
        public String mediaTitle;
        public String mediaDescription;
        public String mediaThumbnail;
        public String mediaCategory;
        public String exInfoImage;
        public List<MediaContent> contentList = new List<MediaContent>();
        public String gameID;
        public String enclosure;
        public String enclosureDuration;
        public String feedBurnerOrigLink;
    }

    public class MediaContent
    {
        public String url;
        public String type;
        public String medium;
        public String duration;
        public String width;
        public String height;
    }

    public static class RssWrapper
    {
        public static List<RssItem> GetRssItems(string XmlContent)
        {
            List<RssItem> loRssItems = new List<RssItem>();

            XmlDocument doc = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            settings.CloseInput = true;
            settings.IgnoreComments = true;
            settings.IgnoreWhitespace = true;
            doc.Load(XmlReader.Create(new System.IO.StringReader(XmlContent), settings));

            XmlNamespaceManager expr = new XmlNamespaceManager(doc.NameTable);
            expr.AddNamespace("media", "http://search.yahoo.com/mrss");
            expr.AddNamespace("exInfo", "http://www.gametrailers.com/rssexplained.php");
            XmlNode root = doc.SelectSingleNode("//rss/channel/item", expr);
            XmlNodeList nodeList;
            nodeList = root.SelectNodes("//rss/channel/item");
            RssItem loRssItem;
            MediaContent loMediaContent;
            int itemCount = 0;
            foreach (XmlNode chileNode in nodeList)
            {
                if (itemCount >= 100) break;

                itemCount++;
                loRssItem = new RssItem();

                for (int i = 0; i < chileNode.ChildNodes.Count; i++)
                {
                    XmlNode n = chileNode.ChildNodes[i];

                    switch (n.Name)
                    {
                        case "title":
                            loRssItem.title = n.InnerText;
                            break;
                        case "link":
                            loRssItem.link = n.InnerText;
                            break;
                        case "guid":
                            loRssItem.guid = n.InnerText;
                            break;
                        case "pubDate":
                            loRssItem.pubDate = n.InnerText;
                            break;
                        case "description":
                            loRssItem.description = n.InnerText;
                            break;
                        case "author":
                            loRssItem.author = n.InnerText;
                            break;
                        case "exInfo:image":
                            loRssItem.exInfoImage = n.InnerText;
                            break;
                        case "exInfo:gameID":
                            loRssItem.gameID = n.InnerText;
                            break;
                        case "feedburner:origLink":
                            loRssItem.feedBurnerOrigLink = n.InnerText;
                            break;
                        case "media:group":

                            for (int j = 0; j < n.ChildNodes.Count; j++)
                            {
                                XmlNode nin = n.ChildNodes[j];
                                switch (nin.Name)
                                {

                                    case "media:content":
                                        loMediaContent = new MediaContent();
                                        try
                                        {
                                            loMediaContent.url = nin.Attributes["url"].Value;
                                            loMediaContent.type = nin.Attributes["type"].Value;
                                            loMediaContent.medium = nin.Attributes["medium"].Value;
                                            loMediaContent.duration = nin.Attributes["duration"].Value;
                                            loMediaContent.height = nin.Attributes["height"].Value;
                                            loMediaContent.width = nin.Attributes["width"].Value;
                                        }
                                        catch (Exception) { };
                                        loRssItem.contentList.Add(loMediaContent);
                                        break;
                                    case "media:description":
                                        loRssItem.mediaDescription = n.InnerText;
                                        break;
                                    case "media:thumbnail":
                                        loRssItem.mediaThumbnail = nin.Attributes["url"].Value;
                                        break;
                                    case "media:title":
                                        loRssItem.mediaTitle = nin.InnerText;
                                        break;

                                }
                            }
                            break;
                        case "media:content":
                            loMediaContent = new MediaContent();
                            if (n.Attributes["url"] != null)
                            {
                                loMediaContent.url = n.Attributes["url"].Value;
                            }
                            if (n.Attributes["type"] != null)
                            {
                                loMediaContent.type = n.Attributes["type"].Value;
                            }
                            if (n.Attributes["medium"] != null)
                            {
                                loMediaContent.medium = n.Attributes["medium"].Value;
                            }
                            if (n.Attributes["duration"] != null)
                            {
                                loMediaContent.duration = n.Attributes["duration"].Value;
                            }
                            if (n.Attributes["height"] != null)
                            {
                                loMediaContent.height = n.Attributes["height"].Value;
                            }
                            if (n.Attributes["width"] != null)
                            {
                                loMediaContent.width = n.Attributes["width"].Value;
                            }
                            loRssItem.contentList.Add(loMediaContent);
                            for (int j = 0; j < n.ChildNodes.Count; j++)
                            {
                                XmlNode nin = n.ChildNodes[j];

                                switch (nin.Name)
                                {
                                    case "media:description":
                                        loRssItem.mediaDescription = n.InnerText;
                                        break;
                                    case "media:thumbnail":
                                        loRssItem.mediaThumbnail = nin.Attributes["url"].Value;
                                        break;
                                    case "media:title":
                                        loRssItem.mediaTitle = nin.InnerText;
                                        break;
                                }
                            }
                            break;
                        case "media:description":
                            loRssItem.mediaDescription = n.InnerText;
                            break;
                        case "media:thumbnail":
                            loRssItem.mediaThumbnail = n.Attributes["url"].Value;
                            break;
                        case "media:title":
                            loRssItem.mediaTitle = n.InnerText;
                            break;
                        case "enclosure":
                            loRssItem.enclosure = n.Attributes["url"].Value;
                            if (n.Attributes["duration"] != null)
                            {
                                loRssItem.enclosureDuration = n.Attributes["duration"].Value;
                            }
                            break;
                        case "media:category":
                            loRssItem.mediaCategory = n.InnerText;
                            break;
                        default:
                            break;
                    }
                }
                loRssItems.Add(loRssItem);
            }


            return loRssItems;
        }
    }
}
