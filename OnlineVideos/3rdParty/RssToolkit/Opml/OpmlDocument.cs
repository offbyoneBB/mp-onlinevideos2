/*=======================================================================
  Copyright (C) Microsoft Corporation.  All rights reserved.
 
  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
  PARTICULAR PURPOSE.
=======================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using RssToolkit;
using RssToolkit.Rss;
using System.Globalization;

namespace RssToolkit.Opml
{
    /// <summary>
    /// OpmlDocument
    /// </summary>
    [Serializable]
    [XmlRoot("opml")]
    public class OpmlDocument
    {
        private String version;
        private OpmlHead head;
        private OpmlBody body;

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [XmlAttribute("version")]
        public String Version
        {
            get
            { 
                return version; 
            }

            set 
            { 
                version = value; 
            }
        }

        /// <summary>
        /// Gets or sets the head.
        /// </summary>
        /// <value>The head.</value>
        [XmlElement("head")]
        public OpmlHead Head
        {
            get 
            { 
                return head; 
            }

            set 
            { 
                head = value; 
            }
        }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        /// <value>The body.</value>
        [XmlElement("body")]
        public OpmlBody Body
        {
            get 
            { 
                return body; 
            }

            set 
            { 
                body = value; 
            }
        }

        /// <summary>
        /// Loads from URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>OpmlDocument</returns>
        public static OpmlDocument Load(System.Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            string opmlXml = string.Empty;
            using (Stream opmlStream = DownloadManager.GetFeed(url.ToString()))
            {
                using (XmlTextReader reader = new XmlTextReader(opmlStream))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element &&
                            reader.LocalName.Equals("opml", StringComparison.OrdinalIgnoreCase))
                        {
                            opmlXml = reader.ReadOuterXml();
                            break;
                        }
                    }
                }
            }

            return Load(opmlXml);
        }

        /// <summary>
        /// Loads from XML.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <returns>OpmlDocument</returns>
        public static OpmlDocument Load(string xml)
        {
            if (String.IsNullOrEmpty(xml))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The argument '{0}' is Null or Empty", "xml"));
            }

            OpmlDocument doc = new OpmlDocument();
            using (StringReader stringReader = new StringReader(xml))
            {
                XmlSerializer x = new XmlSerializer(typeof(OpmlDocument));
                using (XmlTextReader xmlReader = new XmlTextReader(stringReader))
                {
                    doc = (OpmlDocument)x.Deserialize(xmlReader);
                }
            }

            return doc;
        }
    }
}
