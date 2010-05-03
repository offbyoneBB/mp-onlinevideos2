using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using Vattenmelon.Nrk.Parser;
using Vattenmelon.Nrk.Domain;
using Vattenmelon.Nrk.Parser.Http;

namespace Vattenmelon.Nrk.Parser.Xml
{
    public class PodkastXmlParser : XmlRSSParser
    {
        private XmlNamespaceManager manager;

//        public PodkastXmlParser(string siteurl, string section)
//            : base(siteurl, section)
//        {
//            
//        }

        public PodkastXmlParser(String urltopodkast)
            : base("", "")
        {
            url = urltopodkast;
        }

        override protected Clip.KlippType GetClipType()
        {
            return Clip.KlippType.PODCAST;
        }


        protected override void PutPublicationDateOnItem(Clip item, XmlNode n)
        {
            item.Klokkeslett = DateTime.Parse(n.InnerText).ToString("f");
        }

        protected override void PutDurationOnItem(Clip item, XmlNode n)
        {
            item.Duration = n.InnerText;
        }
        protected override void LoadXmlDocument()
        {
            if (doc == null)
            {
                InternalLoadXmlDocument();
                XPathNavigator xPathNavigator = doc.CreateNavigator();
                manager = new XmlNamespaceManager(xPathNavigator.NameTable);
                manager.AddNamespace("itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd");
            }
        }

        override protected void PutGuidOnItem(Clip loRssItem, XmlNode n)
        {
            
        }

        protected override string GetPicture(Clip clip)
        {
            return getPodkastPicture();
        }

        protected override void PutEnclosureOnItem(Clip item, XmlNode n)
        {
            string tmpId = n.Attributes["url"].Value;
            item.ID = tmpId.Substring(0, tmpId.LastIndexOf("?"));
            item.MediaType = n.Attributes["type"].Value;
        }

        public string getPodkastPicture()
        {
            return GetSingleNodeValue("//rss/channel/image/url");
        }

        public string getPodkastDescription()
        {
            return GetSingleNodeValue("//rss/channel/description");
        }

        public string getPodkastCopyright()
        {
            return GetSingleNodeValue("//rss/channel/copyright");
        }

        public string getPodkastAuthor()
        {
            LoadXmlDocument();
            return doc.SelectSingleNode("//rss/channel/itunes:author", manager).FirstChild.Value;
        }

        private string GetSingleNodeValue(String path)
        {
            LoadXmlDocument();
            return doc.SelectSingleNode(path).FirstChild.Value;
        }
    }
}
