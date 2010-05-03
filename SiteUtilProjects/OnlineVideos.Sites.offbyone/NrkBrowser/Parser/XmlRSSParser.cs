using System;
using System.Collections.Generic;
using System.Xml;
using Vattenmelon.Nrk.Parser;
using Vattenmelon.Nrk.Domain;

namespace Vattenmelon.Nrk.Parser.Xml
{
    public class XmlRSSParser : XmlParser
    {
        public XmlRSSParser(string siteurl, string section)
        {
            url = siteurl + section;
        }


        public List<Item> getClips()
        {
            LoadXmlDocument();
            XmlNodeList nodeList = GetNodeList();
            List<Item> clips = new List<Item>();
            int itemCount = 0;
            foreach (XmlNode childNode in nodeList)
            {
                Clip loRssItem = CreateClipFromChildNode(childNode);
                if (NrkParser.isNotShortVignett(loRssItem))
                {
                    loRssItem.Type = GetClipType();
                    loRssItem.Bilde = GetPicture(loRssItem);
                    clips.Add(loRssItem);
                }
                
                itemCount++;
                if (isItemCount100orOver(itemCount))
                {
                    //Log.Info(string.Format("{0}: Over 100 clips in document, breaking.", NrkParserConstants.PLUGIN_NAME));
                    break;
                }
                
            }
            return clips;
        }

        virtual protected void LoadXmlDocument()
        {
            InternalLoadXmlDocument();
        }

        virtual protected Clip.KlippType GetClipType()
        {
            return Clip.KlippType.RSS;
        }

        virtual protected string GetPicture(Clip clip)
        {
            return String.Empty;
        }
        
        private static bool isItemCount100orOver(int itemCount)
        {
            return itemCount >= 100;
        }

        virtual protected XmlNodeList GetNodeList()
        {
            return doc.SelectNodes("//rss/channel/item");
        }
        virtual protected void PutLinkOnItem(Clip clip, XmlNode node)
        {

        }
        virtual protected void PutSummaryOnItem(Clip clip, XmlNode node)
        {

        }
        protected Clip CreateClipFromChildNode(XmlNode childNode)
        {
            Clip loRssItem = new Clip("", "");
            for (int i = 0; i < childNode.ChildNodes.Count; i++)
            {
                XmlNode n = childNode.ChildNodes[i];

                switch (n.Name)
                {
                    case "title":
                        loRssItem.Title = n.InnerText;
                        break;
                    case "link":
                        PutLinkOnItem(loRssItem, n);
                        break;
                    case "guid":
                        PutGuidOnItem(loRssItem, n);
                        break;
                    case "pubDate":
                        PutPublicationDateOnItem(loRssItem, n);
                        break;
                    case "description":
                        loRssItem.Description = n.InnerText;
                        break;
                    case "summary":
                        PutSummaryOnItem(loRssItem, n);
                        break;
                    case "author":

                        break;
                    case "exInfo:image":

                        break;
                    case "exInfo:gameID":

                        break;
                    case "feedburner:origLink":

                        break;
                    case "media:group":

                        for (int j = 0; j < n.ChildNodes.Count; j++)
                        {
                            XmlNode nin = n.ChildNodes[j];
                            switch (nin.Name)
                            {
                                case "media:content":
                                    //                                        loMediaContent = new MediaContent();
                                    try
                                    {
                                        //                                            loMediaContent.url = nin.Attributes["url"].Value;
                                        //                                            loMediaContent.type = nin.Attributes["type"].Value;
                                        //                                            loMediaContent.medium = nin.Attributes["medium"].Value;
                                        //                                            loMediaContent.duration = nin.Attributes["duration"].Value;
                                        //                                            loMediaContent.height = nin.Attributes["height"].Value;
                                        //                                            loMediaContent.width = nin.Attributes["width"].Value;
                                    }
                                    catch (Exception e)
                                    {
                                        //Log.Error("catccccccccccched exception: " + e.Message);
                                        Console.Error.WriteLine(e.StackTrace);
                                    }
                                    ;
                                    break;
                                case "media:description":

                                    break;
                                case "media:thumbnail":

                                    break;
                                case "media:title":

                                    break;
                            }
                        }
                        break;
                    case "itunes:duration":
                        PutDurationOnItem(loRssItem, n);
                        break;
                    case "media:description":
                        //                            loRssItem.mediaDescription = n.InnerText;
                        break;
                    case "media:thumbnail":
                        //                            loRssItem.mediaThumbnail = n.Attributes["url"].Value;
                        break;
                    case "media:title":
                        //                            loRssItem.mediaTitle = n.InnerText;
                        break;
                    case "enclosure":
                        PutEnclosureOnItem(loRssItem, n);
                        break;
                    case "media:category":
                        //  loRssItem.mediaCategory = n.InnerText;
                        break;
                    default:
                        break;
                }
            }
            return loRssItem;
        }

        protected virtual void PutDurationOnItem(Clip item, XmlNode n)
        {
            
        }

        protected virtual void PutPublicationDateOnItem(Clip item, XmlNode n)
        {
            
        }

        virtual protected void PutEnclosureOnItem(Clip item, XmlNode n)
        {
            
        }

        virtual protected void PutGuidOnItem(Clip loRssItem, XmlNode n)
        {
            loRssItem.ID = n.InnerText;
        }
    }
}
