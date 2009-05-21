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
using System.Xml;
using System.Collections;
using System.Text;
using Google.GData.Client;

namespace Google.GData.Extensions {

    /// <summary>
    /// GData schema extension describing the original recurring event.
    /// </summary>
    public class OriginalEvent : SimpleContainer
    {
        /// <summary>
        /// default constructor for an original event
        /// </summary>
        public OriginalEvent() :
            base(GDataParserNameTable.XmlOriginalEventElement,
                 BaseNameTable.gDataPrefix,
                 BaseNameTable.gNamespace)
        {
            this.ExtensionFactories.Add(new When());
            this.Attributes.Add(GDataParserNameTable.XmlAttributeId, null);
            this.Attributes.Add(GDataParserNameTable.XmlAttributeHref, null);

        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Href</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Href
        {
            get { return this.Attributes[GDataParserNameTable.XmlAttributeHref] as string; }
            set { this.Attributes[GDataParserNameTable.XmlAttributeHref] = value; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public IdOriginal</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string IdOriginal
        {
            get { return this.Attributes[GDataParserNameTable.XmlAttributeId] as string; }
            set { this.Attributes[GDataParserNameTable.XmlAttributeId] = value; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public original Start Time</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public When OriginalStartTime
        {
            get 
            { 
                return FindExtension(GDataParserNameTable.XmlWhenElement,
                                      BaseNameTable.gNamespace) as When;
            }
            set 
            { 
                ReplaceExtension(GDataParserNameTable.XmlWhenElement,
                                      BaseNameTable.gNamespace, value);
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Parses an xml node to create a Who object.</summary> 
        /// <param name="node">the xml parses node, can be NULL</param>
        /// <param name="parser">the xml parser to use if we need to dive deeper</param>
        /// <returns>the created IExtensionElement object</returns>
        //////////////////////////////////////////////////////////////////////
        public override IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser) 
        {
            IExtensionElementFactory ele = base.CreateInstance(node, parser);

            OriginalEvent ev = ele as OriginalEvent;
            if (ev != null)
            {
                if (ev.IdOriginal == null)
                {
                    throw new ArgumentException("g:originalEvent/@id is required.");
                }
    
                if (ev.OriginalStartTime == null)
                {
                    throw new ArgumentException("g:when inside g:originalEvent is required.");
                }
            }

            return ev;
        }
    }
}
