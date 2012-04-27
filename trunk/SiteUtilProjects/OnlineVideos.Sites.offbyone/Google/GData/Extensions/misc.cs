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
using System.Globalization;
using Google.GData.Client;

namespace Google.GData.Extensions {
    /// <summary>
    /// The resource ID is an identifier for a resource that an entry might 
    /// be refering to
    /// </summary>
    public class ResourceId : SimpleElement {
        /// <summary>
        /// default constructor for gd:resourceid 
        /// </summary>
        public ResourceId()
            : base(GDataParserNameTable.XmlResourceIdElement,
            GDataParserNameTable.gDataPrefix,
            GDataParserNameTable.gNamespace) {
        }
    }

    /// <summary>
    /// Identifies when an entry was last viewed
    /// </summary>
    public class LastViewed : SimpleElement {
        /// <summary>
        /// default constructor for gd:resourceid 
        /// </summary>
        public LastViewed()
            : base(GDataParserNameTable.XmlLastViewedElement,
            GDataParserNameTable.gDataPrefix,
            GDataParserNameTable.gNamespace) {
        }
    }

    /// <summary>
    /// identifies the person who last modified an entry
    /// </summary>
    /// <returns></returns>
    public class LastModifiedBy : SimpleContainer {
        /// <summary>
        /// default constructor
        /// </summary>
        public LastModifiedBy()
            : base(GDataParserNameTable.XmlLastModifiedByElement,
            GDataParserNameTable.gDataPrefix,
            GDataParserNameTable.gNamespace) {
            this.ExtensionFactories.Add(new LastModifiedByName());
            this.ExtensionFactories.Add(new LastModifiedByEMail());
        }

        /// <summary>
        /// the name portion of the element
        /// </summary>
        /// <returns></returns>
        public string Name {
            get {
                return GetStringValue<LastModifiedByName>(GDataParserNameTable.XmlName,
                    GDataParserNameTable.NSAtom);
            }
        }

        /// <summary>
        /// the email portion of the element
        /// </summary>
        /// <returns></returns>
        public string EMail {
            get {
                return GetStringValue<LastModifiedByEMail>(GDataParserNameTable.XmlEmailElement,
                    GDataParserNameTable.NSAtom);
            }
        }
    }

    /// <summary>
    /// simple subclass to hold the name subportion for the lastmodified container
    /// </summary>
    public class LastModifiedByName : SimpleElement {
        /// <summary>
        /// default constructor for a name subobject
        /// </summary>
        public LastModifiedByName()
            : base(GDataParserNameTable.XmlName,
            GDataParserNameTable.AtomPrefix,
            GDataParserNameTable.NSAtom) {
        }
    }

    /// <summary>
    /// simple subclass to hold the email subportion of the lastmodified container
    /// </summary>
    public class LastModifiedByEMail : SimpleElement {
        /// <summary>
        /// default constructor for a email subobject
        /// </summary>
        public LastModifiedByEMail()
            : base(GDataParserNameTable.XmlEmailElement,
            GDataParserNameTable.AtomPrefix,
            GDataParserNameTable.NSAtom) {
        }
    }

    /// <summary>
    /// The amount of quota consumed by the entry. 
    /// </summary>
    public class QuotaBytesUsed : SimpleElement {
        /// <summary>
        /// default constructor for gd:resourceid 
        /// </summary>
        public QuotaBytesUsed()
            : base(GDataParserNameTable.XmlQuotaBytesUsedElement,
            GDataParserNameTable.gDataPrefix,
            GDataParserNameTable.gNamespace) {
        }
    }
}
