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
using System.IO; 

#endregion

//////////////////////////////////////////////////////////////////////
// Contains AtomCategory, an object to represent an atom:category
// element.
//
// atomCategory = element atom:category {
//    atomCommonAttributes,
//    attribute term { text },
//    attribute scheme { atomUri }?,
//    attribute label { text }?,
//    empty
// }
//
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{
    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Category elements contain information about a category to which an Atom feed or entry is associated.
    /// atomCategory = element atom:category {
    ///    atomCommonAttributes,
    ///    attribute term { text },
    ///    attribute scheme { atomUri }?,
    ///    attribute label { text }?,
    ///    empty
    /// }
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class AtomCategory : AtomBase, IEquatable<AtomCategory>
    {
        /// <summary>holds the term</summary> 
        private string term;
        /// <summary>holds the scheme as an Uri</summary> 
        private AtomUri scheme;
        /// <summary>holds the category label</summary> 
        private string label;

        //////////////////////////////////////////////////////////////////////
        /// <summary>empty Category constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public AtomCategory()
        {
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>Category constructor</summary> 
        /// <param name="term">the term of the category</param>
        //////////////////////////////////////////////////////////////////////
        public AtomCategory(string term)
        {
            this.Term = term;
            this.Scheme = null; 
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Category constructor</summary> 
        /// <param name="term">the term of the category</param>
        /// <param name="scheme">the scheme of the category</param>
        //////////////////////////////////////////////////////////////////////
        public AtomCategory(string term, AtomUri scheme)
        {
            this.Term = term;
            this.Scheme = scheme; 
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Category constructor</summary> 
        /// <param name="term">the term of the category</param>
        /// <param name="scheme">the scheme of the category</param>
        /// <param name="label"> the label for the category</param>
        //////////////////////////////////////////////////////////////////////
        public AtomCategory(string term, AtomUri scheme, string label)
        {
            this.Term = term;
            this.Scheme = scheme;
            this.label = label;
        }
        /////////////////////////////////////////////////////////////////////////////


        #region overloaded for persistence

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public override string XmlName 
        {
            get { return AtomParserNameTable.XmlCategoryElement; }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>overridden to save attributes for this(XmlWriter writer)</summary> 
        /// <param name="writer">the xmlwriter to save into </param>
        //////////////////////////////////////////////////////////////////////
        protected override void SaveXmlAttributes(XmlWriter writer)
        {
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlAttributeTerm, this.Term);
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlAttributeScheme, this.Scheme);
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlAttributeLabel, this.Label);
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
            if (!base.ShouldBePersisted())
            {
                return Utilities.IsPersistable(this.term) || Utilities.IsPersistable(this.label) || Utilities.IsPersistable(this.scheme); 
            }
            return true;
        }
        /////////////////////////////////////////////////////////////////////////////


        #endregion


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Term</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Term
        {
            get {return this.term;}
            set {this.Dirty = true;  this.term = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Label</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Label
        {
            get {return this.label;}
            set {this.Dirty = true;  this.label = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Uri Scheme</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri Scheme
        {
            get {return this.scheme;}
            set {this.Dirty = true;  this.scheme = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>creates a string rep of a category useful for a query URI</summary> 
        /// <returns>the category as a string for a query </returns>
        //////////////////////////////////////////////////////////////////////
        public string UriString
        {
            get 
            {
                String result = String.Empty; 

                if (this.scheme != null)
                {
                    result = "{" + this.Scheme + "}";
                }
                if (Utilities.IsPersistable(this.Term))
                {
                    result += this.Term; 
                    return result; 
                }
                return null; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        #region added by Noam Gal (ATGardner gmail.com)

        public bool Equals(AtomCategory other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(other.term, this.term) && (other.scheme == null || this.scheme == null || Equals(other.scheme, this.scheme));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != typeof(AtomCategory))
            {
                return false;
            }
            return Equals((AtomCategory)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.term != null ? this.term.GetHashCode() : 0) * 397) ^ (this.scheme != null ? this.scheme.GetHashCode() : 0);
            }
        }

        #endregion

    }
    /////////////////////////////////////////////////////////////////////////////
} 
/////////////////////////////////////////////////////////////////////////////
