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
                                if (nin.Name == "media:content")
                                {
                                    GetContent(nin, loRssItem); 
                                    break;
                                }
                            }
                            break;
                        case "media:content":
                            GetContent(n, loRssItem);
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
                        case "exInfo:fileType":
                            MediaContent loMediaContent = new MediaContent();
                            for (int j = 0; j < n.ChildNodes.Count; j++)
                            {
                                XmlNode nin = n.ChildNodes[j];
                                switch (nin.Name)
                                {
                                    case "type": loMediaContent.type = nin.InnerText; break;
                                    case "link": loMediaContent.url = nin.InnerText; break;                                    
                                }
                            }
                            loRssItem.contentList.Add(loMediaContent);                            
                            break;
                        default:
                            break;
                    }
                }
                loRssItems.Add(loRssItem);
            }

            return loRssItems;
        }

        private static void GetContent(XmlNode contentNode, RssItem loRssItem)
        {
            MediaContent loMediaContent = new MediaContent();

            if (contentNode.Attributes["url"] != null) loMediaContent.url = contentNode.Attributes["url"].Value;
            if (contentNode.Attributes["type"] != null) loMediaContent.type = contentNode.Attributes["type"].Value;
            if (contentNode.Attributes["medium"] != null) loMediaContent.medium = contentNode.Attributes["medium"].Value;
            if (contentNode.Attributes["duration"] != null) loMediaContent.duration = contentNode.Attributes["duration"].Value;
            if (contentNode.Attributes["height"] != null) loMediaContent.height = contentNode.Attributes["height"].Value;
            if (contentNode.Attributes["width"] != null) loMediaContent.width = contentNode.Attributes["width"].Value;
            
            for (int j = 0; j < contentNode.ChildNodes.Count; j++)
            {
                XmlNode nin = contentNode.ChildNodes[j];

                switch (nin.Name)
                {
                    case "media:description":
                        loRssItem.mediaDescription = nin.InnerText;
                        break;
                    case "media:thumbnail":
                        loRssItem.mediaThumbnail = nin.Attributes["url"].Value;
                        break;
                    case "media:title":
                        loRssItem.mediaTitle = nin.InnerText;
                        break;
                }
            }

            loRssItem.contentList.Add(loMediaContent);
        }
    }
}
