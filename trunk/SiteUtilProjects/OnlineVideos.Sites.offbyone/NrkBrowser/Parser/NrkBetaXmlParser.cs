using System;
using System.Collections.Generic;
using System.Xml;
using Vattenmelon.Nrk.Parser;
using Vattenmelon.Nrk.Domain;
using Vattenmelon.Nrk.Parser.Http;

namespace Vattenmelon.Nrk.Parser.Xml
{
    public class NrkBetaXmlParser : XmlRSSParser
    {

        private IHttpClient httpClient;

        public NrkBetaXmlParser(string siteurl, string section) : base(siteurl, section)
        {
            
        }

        public NrkBetaXmlParser()
            : this("", "")
        {
            
        }

        public void SearchFor(string keyword)
        {
            url = string.Format(NrkParserConstants.NRK_BETA_FEEDS_SOK_URL, keyword);
        }

        public NrkBetaXmlParser FindLatestClips()
        {
            url = NrkParserConstants.NRK_BETA_FEEDS_LATEST_CLIPS_URL;
            return this;
        }

        public NrkBetaXmlParser FindHDClips()
        {
            url = NrkParserConstants.NRK_BETA_FEEDS_HD_CLIPS_URL;
            return this;
        }

        override protected void LoadXmlDocument()
        {
            doc = new XmlDocument();
            httpClient = GetHttClient();
            String xmlAsString = httpClient.GetUrl(url);
            doc.LoadXml(xmlAsString);
        }

        private IHttpClient GetHttClient()
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient(new System.Net.CookieContainer());
            }
            return httpClient;
        }
        override protected string GetPicture(Clip clip)
        {
            String bilde = NrkParserConstants.NRK_BETA_THUMBNAIL_URL;
            String videoFileName = clip.ID.Substring(clip.ID.LastIndexOf("/") + 1);
            return string.Format(bilde, videoFileName);
        }

        override protected Clip.KlippType GetClipType()
        {
            return Clip.KlippType.NRKBETA;
        }

        override protected XmlNodeList GetNodeList()
        {
            return doc.GetElementsByTagName("entry");
        }

        override protected void PutLinkOnItem(Clip clip, XmlNode node)
        {
            clip.ID = node.Attributes["href"].Value;
        }

        override protected void PutSummaryOnItem(Clip clip, XmlNode node)
        {
            clip.Description = node.InnerText;
        }

        public IHttpClient HttpClient
        {
            get { return httpClient; }
            set { httpClient = value; }
        }
    }
}
