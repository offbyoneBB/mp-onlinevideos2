/* Copyright (c) 2006 Google Inc.
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
#region Using directives

#define USE_TRACING

using System;
using System.Xml;
using System.Globalization;

#endregion

//////////////////////////////////////////////////////////////////////
// <summary>Contains AtomLink, an object to represent the atom:link
// element.</summary> 
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>AtomLink represents an atom:link element
    /// atomLink = element atom:link {
    ///    atomCommonAttributes,
    ///    attribute href { atomUri },
    ///    attribute rel { atomNCName | atomUri }?,
    ///    attribute type { atomMediaType }?,
    ///    attribute hreflang { atomLanguageTag }?,
    ///    attribute title { text }?,
    ///    attribute length { text }?,
    ///    empty
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class AtomLink : AtomBase
    {
        /// <summary>property holder exposed over get/set</summary> 
        private AtomUri href;
        /// <summary>property holder exposed over get/set</summary> 
        private string rel;
        /// <summary>property holder exposed over get/set</summary> 
        private string type;
        /// <summary>property holder exposed over get/set</summary> 
        private string hreflang;
        /// <summary>property holder exposed over get/set</summary> 
        private string title;
        /// <summary>property holder exposed over get/set</summary> 
        private int length;


        /// <summary>HTML Link Type</summary> 
        public const string HTML_TYPE = "text/html";

        /// <summary>ATOM Link Type</summary>
        public const string ATOM_TYPE = "application/atom+xml";

        //////////////////////////////////////////////////////////////////////
        /// <summary>default empty constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public AtomLink()
        {
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>public AtomLink(string uri)</summary> 
        /// <param name="link">the uri for the link </param>
        //////////////////////////////////////////////////////////////////////
        public AtomLink(string link)
        {
            this.HRef = new AtomUri(link);
        }
        /// <summary>
        /// constructor used in atomfeed to create new links
        /// </summary>
        /// <param name="type">the type of link to create</param>
        /// <param name="rel">the rel value</param>
        public AtomLink(string type, string rel) 
        {
            this.Type = type;
            this.Rel = rel;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Uri HRef</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri HRef
        {
            get {return this.href;}
            set {this.Dirty = true;  this.href = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>public string AbsoluteUri</summary> 
        //////////////////////////////////////////////////////////////////////
        public string AbsoluteUri
        {
            get
            {
                if (this.HRef != null)
                    return GetAbsoluteUri(this.HRef.ToString());
                return null;
            }
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Rel</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Rel
        {
            get {return this.rel;}
            set {this.Dirty = true;  this.rel = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Type</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Type
        {
            get {return this.type;}
            set {this.Dirty = true;  this.type = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string HrefLang</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string HRefLang
        {
            get {return this.hreflang;}
            set {this.Dirty = true;  this.hreflang = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public int Lenght</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int Length
        {
            get {return this.length;}
            set {this.Dirty = true;  this.length = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Title</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Title
        {
            get {return this.title;}
            set {this.Dirty = true;  this.title = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        #region Persistence overloads
        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public override string XmlName 
        {
            get { return AtomParserNameTable.XmlLinkElement; }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>overridden to save attributes for this(XmlWriter writer)</summary> 
        /// <param name="writer">the xmlwriter to save into </param>
        //////////////////////////////////////////////////////////////////////
        protected override void SaveXmlAttributes(XmlWriter writer)
        {
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlAttributeHRef, this.HRef);
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlAttributeHRefLang, this.HRefLang);
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlAttributeRel, this.Rel);
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlAttributeType, this.Type);
            
			if (this.length > 0)
            {
                WriteEncodedAttributeString(writer, AtomParserNameTable.XmlAttributeLength, this.Length.ToString(CultureInfo.InvariantCulture));
            }
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlTitleElement, this.Title);
            // call base later as base takes care of writing out extension elements that might close the attribute list
            base.SaveXmlAttributes(writer);
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>figures out if this object should be persisted</summary> 
        /// <returns> true, if it's worth saving</returns>
        //////////////////////////////////////////////////////////////////////
        public override bool ShouldBePersisted()
        {
            if (base.ShouldBePersisted())
            {
                return true;
            }
            if (Utilities.IsPersistable(this.href))
            {
                return true;
            }
            if (Utilities.IsPersistable(this.hreflang))
            {
                return true;
            }
            if (Utilities.IsPersistable(this.rel))
            {
                return true;
            }
            if (Utilities.IsPersistable(this.type))
            {
                return true;
            }
            if (Utilities.IsPersistable(this.Length))
            {
                return true;
            }

            if (Utilities.IsPersistable(this.title))
            {
                return true;
            }
            return false;
        }
        /////////////////////////////////////////////////////////////////////////////


        #endregion




    }
    /////////////////////////////////////////////////////////////////////////////

} /////////////////////////////////////////////////////////////////////////////
 
