/* Copyright (c) 2006-2008 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
/* Change history
* Oct 13 2008  Joe Feser       joseph.feser@gmail.com
* Converted ArrayLists and other .NET 1.1 collections to use Generics
* Combined IExtensionElement and IExtensionElementFactory interfaces
* 
*/
using System;
using System.Collections;
using System.Text;
using System.Xml;
using Google.GData.Client;

namespace Google.GData.Extensions {

    /// <summary>
    /// GData schema extension describing a comments feed.
    /// </summary>
    public class Comments : IExtensionElementFactory
    {

        /// <summary>
        ///  holds the feedLink property
        /// </summary>
        private FeedLink feedLink;

        /// <summary>
        /// Comments feed link.
        /// </summary>
        public FeedLink FeedLink
        {
            get { return feedLink;}
            set { feedLink = value;}
        }

        #region overloaded from IExtensionElementFactory
        //////////////////////////////////////////////////////////////////////
        /// <summary>Parses an xml node to create a Where  object.</summary> 
        /// <param name="node">the node to parse node</param>
        /// <param name="parser">the xml parser to use if we need to dive deeper</param>
        /// <returns>the created Where  object</returns>
        //////////////////////////////////////////////////////////////////////
        public IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser)
        {
            Tracing.TraceCall();
            Comments comments = null;

            if (node != null)
            {
                object localname = node.LocalName;
                if (!localname.Equals(this.XmlName) ||
                    !node.NamespaceURI.Equals(this.XmlNameSpace))
                {
                    return null;
                }
            }

            comments = new Comments();

            if (node != null)
            {
                if (node.HasChildNodes)
                {
                    XmlNode commentsChild = node.FirstChild;
                    while (commentsChild != null && commentsChild is XmlElement)
                    {
                        if (commentsChild.LocalName == GDataParserNameTable.XmlFeedLinkElement &&
                            commentsChild.NamespaceURI == BaseNameTable.gNamespace)
                        {
                            if (comments.FeedLink == null)
                            {
                                comments.FeedLink = FeedLink.ParseFeedLink(commentsChild);
                            }
                            else
                            {
                                throw new ArgumentException("Only one feedLink is allowed inside the gd:comments");
                            }
                        }
                        commentsChild = commentsChild.NextSibling;
                    }
                }

            }
            return comments; 
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlName
        {
            get { return GDataParserNameTable.XmlCommentsElement;}
        }

          //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlNameSpace
        {
            get { return BaseNameTable.gNamespace; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlPrefix
        {
            get { return BaseNameTable.gDataPrefix; }
        }

        /// <summary>
        /// Persistence method for the Comment  object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
            if (FeedLink != null)
            {
                // only save out if there is something to save
                writer.WriteStartElement(XmlPrefix, XmlName, XmlNameSpace);
                FeedLink.Save(writer);
                writer.WriteEndElement();
            }
        }
    }
}
