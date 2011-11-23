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

namespace Google.GData.Extensions 
{

    /// <summary>
    /// A place (such as an event location) associated with the containing entity. The type of 
    /// the association is determined by the rel attribute; the details of the location are 
    /// contained in an embedded or linked-to Contact entry.
    /// A gd:where element is more general than a gd:geoPt element. The former identifies a place
    ///  using a text description and/or a Contact entry, while the latter identifies a place 
    /// using a specific geographic location.
    ///     Properties
    ///    Property 	    Type 	    Description
    ///     @label? 	    xs:string 	Specifies a user-readable label to distinguish this location from other locations.
    ///     @rel? 	        xs:string 	Specifies the relationship between the containing entity and the contained location. Possible values
    ///     (see below) are defined by other elements. For example, gd:when defines http://schemas.google.com/g/2005#event.
    ///     @valueString? 	xs:string 	A simple string value that can be used as a representation of this location.
    ///     gd:entryLink? 	entryLink 	Entry representing location details. This entry should implement the Contact kind.
    ///     rel values
    ///     Value 	                                                    Description
    ///    http://schemas.google.com/g/2005#event or not specified 	 Place where the enclosing event takes place.
    ///    http://schemas.google.com/g/2005#event.alternate 	          A secondary location. For example, a remote 
    ///                                                                  site with a videoconference link to the main site.
    ///    http://schemas.google.com/g/2005#event.parking 	              A nearby parking lot.
    /// </summary>
    public class Where : IExtensionElementFactory
    {

        /// <summary>
        /// Relation type. Describes the meaning of this location.
        /// </summary>
        public class RelType
        {
            /// <summary>
            /// The standard relationship EVENT_ALTERNATE
            /// </summary>
            public const string EVENT = null; 
            /// <summary>
            /// the alternate EVENT location
            /// </summary>
            public const string EVENT_ALTERNATE = BaseNameTable.gNamespacePrefix + "event.alternate";
            /// <summary>
            ///  the parking location
            /// </summary>
            public const string EVENT_PARKING = BaseNameTable.gNamespacePrefix + "event.parking";
        }

        /// <summary>
        /// Constructs an empty Where instance
        /// </summary>
        public Where()
        {
        }

        /// <summary>
        /// default constructor, takes 3 parameters
        /// </summary>
        /// <param name="value">the valueString property value</param>
        /// <param name="label">label property value</param>
        /// <param name="rel">default for the Rel property value</param>
        public Where(String rel,
                     String label, 
                     String value)
        {
            this.Rel = rel;
            this.Label = label;
            this.ValueString = value;
        }

        private string rel;
        private string label;
        private string valueString;
        private EntryLink entryLink;

        /// <summary>
        /// Rel property accessor
        /// </summary>
        public string Rel
        {
            get { return rel; }
            set { rel = value; }
        }

        /// <summary>
        /// User-readable label that identifies this location.
        /// </summary>
        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        /// <summary>
        /// String description of the event places.
        /// </summary>
        public string ValueString
        {
            get { return valueString; }
            set { valueString = value; }
        }

        /// <summary>
        ///  Nested entry (optional).
        /// </summary>
        public EntryLink EntryLink
        {
            get { return entryLink; }
            set { entryLink = value; }
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
            Where where = null;

            if (node != null)
            {
                object localname = node.LocalName;
                if (!localname.Equals(this.XmlName) ||
                    !node.NamespaceURI.Equals(this.XmlNameSpace))
                {
                    return null;
                }
            }

            where = new Where();
            if (node != null) {

                if (node.Attributes != null)
                {
                    if (node.Attributes[GDataParserNameTable.XmlAttributeRel] != null)
                    {
                        where.Rel = node.Attributes[GDataParserNameTable.XmlAttributeRel].Value;
                    }
    
                    if (node.Attributes[GDataParserNameTable.XmlAttributeLabel] != null)
                    {
                        where.Label = node.Attributes[GDataParserNameTable.XmlAttributeLabel].Value;
                    }
    
                    if (node.Attributes[GDataParserNameTable.XmlAttributeValueString] != null)
                    {
                        where.ValueString = node.Attributes[GDataParserNameTable.XmlAttributeValueString].Value;
                    }
                }
    
                if (node.HasChildNodes)
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        if (childNode.LocalName == GDataParserNameTable.XmlEntryLinkElement)
                        {
                            if (where.EntryLink == null)
                            {
                                where.EntryLink = EntryLink.ParseEntryLink(childNode, parser);
                            }
                            else
                            {
                                throw new ArgumentException("Only one entryLink is allowed inside the g:where");
                            }
                        }
                    }
                }
            }
            return where;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlName
        {
            get { return GDataParserNameTable.XmlWhereElement; }
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
        /// Persistence method for the Where object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer) {
            writer.WriteStartElement(BaseNameTable.gDataPrefix, XmlName, BaseNameTable.gNamespace);

            writer.WriteAttributeString(GDataParserNameTable.XmlAttributeValueString, this.valueString);

            if (Utilities.IsPersistable(this.Label)) {
                writer.WriteAttributeString(GDataParserNameTable.XmlAttributeLabel, this.Label);
            }

            if (Utilities.IsPersistable(this.Rel)) {
                writer.WriteAttributeString(GDataParserNameTable.XmlAttributeRel, this.Rel);
            }

            if (entryLink != null) {
                entryLink.Save(writer);
            }

            writer.WriteEndElement();
        }

        #endregion
    }
}
