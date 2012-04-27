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
using System.IO;
using System.Collections;
using System.Text;
using System.Xml;
using Google.GData.Client;

namespace Google.GData.Extensions {

    /// <summary>
    /// GData schema extension describing a nested feed link.
    /// </summary>
    public class FeedLink : IExtensionElementFactory
    {

        /// <summary>holds the href property</summary>
        private string href;
        /// <summary>holds the readOnly property</summary>
        private bool readOnly;
        /// <summary>holds the feed property</summary>
        private AtomFeed feed;
          /// <summary>holds the rel attribute of the EntryLink element</summary> 
        private string rel;


        private bool readOnlySet; 
        private int countHint;

        /// <summary>
        /// constructor
        /// </summary>
        public FeedLink() 
        {
            this.countHint = -1; 
            this.readOnly = true; 
        }

        /// <summary>
        /// Entry  URI
        /// </summary>
        public string Href
        {
            get { return href;}
            set { href = value;}
        }

        /// <summary>
        /// Read only flag.
        /// </summary>
        public bool ReadOnly
        {
            get { return readOnly;}
            set { this.readOnly = value; this.readOnlySet = true;}
        }

        /// <summary>
        /// Count hint.
        /// </summary>
        public int CountHint
        {
            get { return countHint;}
            set { countHint = value;}
        }

        /// <summary>
        ///  Nested entry (optional).
        /// </summary>
        public AtomFeed Feed
        {
            get { return feed;}
            set { feed = value;}
        }

         //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Rel</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Rel
        {
            get {return this.rel;}
            set {this.rel = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


#region FeedLink Parser
        //////////////////////////////////////////////////////////////////////
        /// <summary>Parses an xml node to create an FeedLink object.</summary> 
        /// <param name="node">feedLink node</param>
        /// <returns> the created FeedLink object</returns>
        //////////////////////////////////////////////////////////////////////
        public static FeedLink ParseFeedLink(XmlNode node)
        {
            Tracing.TraceCall();
            FeedLink link = null;
            Tracing.Assert(node != null, "node should not be null");
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            object localname = node.LocalName;
            if (localname.Equals(GDataParserNameTable.XmlFeedLinkElement))
            {
                link = new FeedLink();
                if (node.Attributes != null)
                {
                    if (node.Attributes[GDataParserNameTable.XmlAttributeHref] != null)
                    {
                        link.Href = node.Attributes[GDataParserNameTable.XmlAttributeHref].Value;
                    }

                    if (node.Attributes[GDataParserNameTable.XmlAttributeReadOnly] != null)
                    {
                        link.ReadOnly = node.Attributes[GDataParserNameTable.XmlAttributeReadOnly].Value.Equals(Utilities.XSDTrue);
                    }
                    if (node.Attributes[GDataParserNameTable.XmlAttributeRel] != null)
                    {
                        link.Rel = node.Attributes[GDataParserNameTable.XmlAttributeRel].Value;
                    }
                    if (node.Attributes[GDataParserNameTable.XmlAttributeCountHint] != null)
                    {
                        try
                        {
                            link.CountHint = Int32.Parse(node.Attributes[GDataParserNameTable.XmlAttributeCountHint].Value);
                        }
                        catch (FormatException fe)
                        {
                            throw new ArgumentException("Invalid g:feedLink/@countHint.", fe);
                        }
                    }
                }

                if (node.HasChildNodes)
                {
                    XmlNode feedChild = node.FirstChild;
                    while (feedChild != null && feedChild is XmlElement)
                    {
                        if (feedChild.LocalName == AtomParserNameTable.XmlFeedElement &&
                            feedChild.NamespaceURI == BaseNameTable.NSAtom)
                        {

                            if (link.Feed == null)
                            {
                                link.Feed = new AtomFeed(null, new Service());
                                Stream feedStream =
                                new MemoryStream(ASCIIEncoding.Default.GetBytes(feedChild.OuterXml));

                                link.Feed.Parse(feedStream, AlternativeFormat.Atom);
                            }
                            else
                            {
                                throw new ArgumentException("Only one feed is allowed inside the g:feedLink");
                            }
                        }
                        feedChild = feedChild.NextSibling;
                    }
                }

            }

            return link;
        }

#endregion
      #region overloaded from IExtensionElementFactory
        //////////////////////////////////////////////////////////////////////
        /// <summary>Parses an xml node to create a Where  object.</summary> 
        /// <param name="node">the node to parse node</param>
        /// <param name="parser">the xml parser to use if we need to dive deeper</param>
        /// <returns>the created Where  object</returns>
        //////////////////////////////////////////////////////////////////////
        public IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser)
        {
            return FeedLink.ParseFeedLink(node);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlName
        {
            get { return GDataParserNameTable.XmlFeedLinkElement;}
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

        #endregion

#region overloaded for persistence


        /// <summary>
        /// Persistence method for the FeedLink object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
            if (Utilities.IsPersistable(this.Href) || this.Feed != null)
            {
                writer.WriteStartElement(XmlPrefix, XmlName, XmlNameSpace);
    
                if (Utilities.IsPersistable(this.Href))
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeHref, this.Href);
                }
                // do not save the default
                if (this.readOnlySet)
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeReadOnly, 
                                                Utilities.ConvertBooleanToXSDString(this.ReadOnly));
                }
                if (Utilities.IsPersistable(this.Rel))
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeRel, this.Rel);
                }

                if (this.countHint > -1)
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeCountHint, this.countHint.ToString());
                }
    
                if (Feed != null)
                {
                    Feed.SaveToXml(writer);
                }
    
                writer.WriteEndElement();
            }
        }
#endregion
    }
}
