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
    /// GData schema extension describing an RFC 2445 recurrence rule.
    /// </summary>
    public class RecurrenceException : SimpleContainer
    {

        public RecurrenceException() :
            base(GDataParserNameTable.XmlRecurrenceExceptionElement,
                 BaseNameTable.gDataPrefix,
                 BaseNameTable.gNamespace)
        {
            this.ExtensionFactories.Add(new OriginalEvent());
            this.ExtensionFactories.Add(new EntryLink());
            this.Attributes.Add(GDataParserNameTable.XmlAttributeSpecialized, null);
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor for "specialized" attribute.</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool Specialized
        {
            get
            {
                return bool.Parse(this.Attributes[GDataParserNameTable.XmlAttributeSpecialized] as string);
            }
            set
            {
                this.Attributes[GDataParserNameTable.XmlAttributeSpecialized] = value;
            }
        }

        /// <summary>
        /// exposes the EntryLink element for this exception
        /// </summary>
        /// <returns></returns>
        public EntryLink EntryLink
        {
            get
            {
                return FindExtension(GDataParserNameTable.XmlEntryLinkElement,
                                     BaseNameTable.gNamespace) as EntryLink;
            }
            set
            {
                ReplaceExtension(GDataParserNameTable.XmlEntryLinkElement,
                                 BaseNameTable.gNamespace,
                                 value);
            }
        }

        /// <summary>
        /// exposes the OriginalEvent element for this exception
        /// </summary>
        /// <returns></returns>
        public OriginalEvent OriginalEvent
        {
            get
            {
                return FindExtension(GDataParserNameTable.XmlOriginalEventElement,
                                     BaseNameTable.gNamespace) as OriginalEvent;
            }
            set
            {
                ReplaceExtension(GDataParserNameTable.XmlOriginalEventElement,
                                 BaseNameTable.gNamespace,
                                 value);
            }
        }

     }
}
