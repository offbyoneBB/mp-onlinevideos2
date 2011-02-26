/*=======================================================================
  Copyright (C) Microsoft Corporation.  All rights reserved.

  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
  PARTICULAR PURPOSE.
=======================================================================*/

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Xml;
using System.Xml.XPath;
using RssToolkit.Opml;

namespace RssToolkit.Rss
{
    /// <summary>
    /// Used for Aggregating Rss links inside an OPML file
    /// </summary>
    public class RssAggregator
    {
        private AutoResetEvent[] rssEvents;
        private List<string> aggregateRss = new List<string>();
        private string _rssXml;

        /// <summary>
        /// Initializes a new instance of the <see cref="RssAggregator"/> class.
        /// </summary>
        public RssAggregator() 
        {
        }

        /// <summary>
        /// Event Handler for Aggregation errors
        /// </summary>
        public event EventHandler<RssAggregationEventArgs> RssAggregationEvent;

        /// <summary>
        /// Gets the RSS XML.
        /// </summary>
        /// <value>The RSS XML.</value>
        public string RssXml
        {
            get
            {
                return _rssXml;
            }
        }

        /// <summary>
        /// Loads from URL.
        /// </summary>
        /// <param name="opmlUrl">The opml URL.</param>
        public void Load(System.Uri opmlUrl)
        {
            if (opmlUrl == null)
            {
                throw new ArgumentNullException("opmlUrl");
            }

            OpmlDocument opmlDoc = OpmlDocument.Load(opmlUrl);

            Load(opmlDoc);
        }

        /// <summary>
        /// Loads from XML.
        /// </summary>
        /// <param name="xml">The XML.</param>
        public void Load(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The argument '{0}' is Null or Empty", "xml"));
            }

            OpmlDocument opmlDoc = OpmlDocument.Load(xml);

            Load(opmlDoc);
        }

        private void Load(OpmlDocument opmlDocument)
        {
            rssEvents = new AutoResetEvent[opmlDocument.Body.Outlines.Count];

            LoadRssFeeds(opmlDocument);

            MergeRss(opmlDocument.Head.Title);
        }

        private void LoadRssFeeds(OpmlDocument opmlDocument)
        {
            IList<OpmlOutline> outlines = opmlDocument.Body.Outlines;
            for (int index = 0; index < outlines.Count; index++)
            {
                if (rssEvents[index] == null)
                {
                    rssEvents[index] = new AutoResetEvent(false);
                }

                ThreadPool.QueueUserWorkItem(new WaitCallback(GetRssFeeds), new OutlineInfo(outlines[index], index));
            }

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                // WaitAll for multiple handles on an STA thread is not supported. 
                // ...so wait on each handle individually. 
                foreach (WaitHandle waitHandle in rssEvents)
                {
                    WaitHandle.WaitAny(new WaitHandle[] { waitHandle });
                }
            }
            else
            {
                WaitHandle.WaitAll(rssEvents);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void GetRssFeeds(object state)
        {
            OutlineInfo outline = null;
            try
            {
                outline = (OutlineInfo)state;
                string xmlString = string.Empty;

                using (Stream rssStream = DownloadManager.GetFeed(outline.Outline.XmlUrl))
                {
                    using (XmlTextReader reader = new XmlTextReader(rssStream))
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                xmlString = reader.ReadOuterXml();
                                break;
                            }
                        }
                    }
                }

                ////convert to rss
                xmlString = RssXmlHelper.ConvertToRssXml(xmlString);

                if (!string.IsNullOrEmpty(xmlString))
                {
                    RssXmlSchemaValidator validator = new RssXmlSchemaValidator();
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    using (Stream stream = assembly.GetManifestResourceStream(Constants.Rss20Xsd))
                    {
                        lock (aggregateRss)
                        {
                            validator.ValidXmlDoc(xmlString, new XmlTextReader(stream));

                            if (validator.IsValidXml)
                            {
                                aggregateRss.Add(xmlString);
                            }
                        }

                        if (!validator.IsValidXml)
                        {
                            NotifySubscribers(null, "Rss not valid in OPML for - " + outline.Outline.XmlUrl, RssSeverityType.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotifySubscribers(ex, ex.Message, RssSeverityType.Error);
            }
            finally
            {
                rssEvents[outline.Index].Set();
            }
        }

        private void NotifySubscribers(Exception ex, string message, RssSeverityType severityType)
        {
            RssAggregationEventArgs eventArgs = new RssAggregationEventArgs();
            eventArgs.Exception = ex;
            eventArgs.Message = message;
            eventArgs.SeverityType = severityType;

            if (this.RssAggregationEvent != null)
                RssAggregationEvent(this, eventArgs);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void MergeRss(string title)
        {
            string emptyXml = string.Format(
                   CultureInfo.InvariantCulture,
                   @"<?xml version=""1.0"" encoding=""utf-8""?><rss version=""2.0""><channel>{0}</channel></rss>",
                   string.IsNullOrEmpty(title) ? string.Empty : "<title>" + title + "</title>");

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(emptyXml);
            try
            {
                foreach (string inputXml in aggregateRss)
                {
                    XmlNodeList nodes;
                    XmlDocument rssDocument = new XmlDocument();
                    XmlNode channelNode = xmlDocument.SelectSingleNode("rss/channel");

                    if (!string.IsNullOrEmpty(inputXml) && channelNode != null)
                    {
                        rssDocument.LoadXml(inputXml);
                        nodes = rssDocument.SelectNodes("rss/channel/item");
                        SortedList<string, XmlElement> nodesList = new SortedList<string, XmlElement>();
                        foreach (XmlElement node in nodes)
                        {
                            DateTime date = DateTime.MinValue;
                            try
                            {
                                XmlNodeList pubDates = node.GetElementsByTagName("pubDate");
                                if (pubDates.Count > 0)
                                {
                                    XmlNode pubDate = pubDates[0];
                                    date = RssXmlHelper.Parse(pubDate.InnerText);
                                }
                            }
                            catch (Exception ex)
                            {
                                NotifySubscribers(ex, ex.Message, RssSeverityType.Warning);
                            }

                            ////add to list, it will sort by the date
                            try
                            {
                                string key = String.Format(
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    "{0}_{1}", 
                                    date.ToString("u", System.Globalization.CultureInfo.InvariantCulture), 
                                    node.ChildNodes[0].InnerText);
                                nodesList.Add(key, node);
                            }
                            catch (ArgumentException argumentException)
                            {
                                NotifySubscribers(argumentException, argumentException.Message, RssSeverityType.Warning);
                            }
                        }

                        foreach (string key in nodesList.Keys)
                        {
                            channelNode.AppendChild(xmlDocument.ImportNode(nodesList[key], true));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotifySubscribers(ex, ex.Message, RssSeverityType.Error);
            }

            _rssXml = xmlDocument.OuterXml;
        }
    }
}
