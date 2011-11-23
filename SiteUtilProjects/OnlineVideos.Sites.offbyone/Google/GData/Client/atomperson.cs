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
using System.ComponentModel;
using System.Runtime.InteropServices;


#endregion

//////////////////////////////////////////////////////////////////////
// <summary>Handles atom:person element.</summary> 
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{
    //////////////////////////////////////////////////////////////////////
    /// <summary>TypeConverter, so that AtomHead shows up in the property pages
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [ComVisible(false)]
    public class AtomPersonConverter : ExpandableObjectConverter
    {
        ///<summary>Standard type converter method</summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType) 
        {
            if (destinationType == typeof(AtomPerson))
                return true;
        
            return base.CanConvertTo(context, destinationType);
        }

        ///<summary>Standard type converter method</summary>
        public override object ConvertTo(ITypeDescriptorContext context,CultureInfo culture, object value, System.Type destinationType) 
        {
            AtomPerson person = value as AtomPerson; 
            if (destinationType == typeof(System.String) && person != null)
            {
                return "Person: " + person.Name;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>enum to describe the different person types
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public enum AtomPersonType
    {
        /// <summary>is an author</summary>
        Author,                       
        /// <summary>is an contributor</summary>
        Contributor,                        /// 
        /// <summary>parsing error</summary>
        Unknown                        
    }
    /////////////////////////////////////////////////////////////////////////////



    //////////////////////////////////////////////////////////////////////
    /// <summary>generic Person object, used for the feed and for the entry
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [TypeConverterAttribute(typeof(AtomPersonConverter)), DescriptionAttribute("Expand to see the person object for the feed/entry.")]
    public class AtomPerson : AtomBase
    {
        /// <summary>name holds the Name property as a string</summary> 
        private string name;
        /// <summary>email holds the email property as a string</summary> 
        private string email; 
        /// <summary>link holds an Uri, representing the link atribute</summary> 
        private AtomUri uri;
        /// <summary>holds  the type for persistence</summary> 
        private AtomPersonType type; 

        /// <summary>public default constructor, usefull only for property pages</summary> 
        public AtomPerson()
        {
            this.type = AtomPersonType.Author; 
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Constructor taking a type to indicate whether person is author or contributor.</summary> 
        /// <param name="type">indicates if author or contributor</param>
        //////////////////////////////////////////////////////////////////////
        public AtomPerson(AtomPersonType type)
        {
            this.type = type; 
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>Constructor taking a type to indicate whether person is author or contributor, plus the person's name</summary> 
        /// <param name="type">indicates if author or contributor</param>
        /// <param name="name">person's name</param>
        //////////////////////////////////////////////////////////////////////
        public AtomPerson(AtomPersonType type, string name) : this(type)
        {
            this.name = name; 
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Name</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Name
        {
            get {return this.name;}
            set {this.Dirty =true; this.name = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Uri Uri</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri Uri
        {
            get 
            {
                if (this.uri == null)
                {
                    this.uri = new AtomUri("");
                }
                return this.uri;
            }
            set {this.Dirty = true;  this.uri = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Uri Email</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Email
        {
            get {return this.email;}
            set {this.Dirty = true;  this.email = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        #region Persistence overloads
        //////////////////////////////////////////////////////////////////////
        /// <summary>Just returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public override string XmlName 
        {
            get { return this.type == AtomPersonType.Author ? AtomParserNameTable.XmlAuthorElement : AtomParserNameTable.XmlContributorElement; }
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>saves the inner state of the element</summary> 
        /// <param name="writer">the xmlWriter to save into </param>
        //////////////////////////////////////////////////////////////////////
        protected override void SaveInnerXml(XmlWriter writer)
        {
            base.SaveInnerXml(writer);
            // now save our state...
            WriteEncodedElementString(writer, BaseNameTable.XmlName, this.Name); 
            WriteEncodedElementString(writer, AtomParserNameTable.XmlEmailElement, this.Email); 
            WriteEncodedElementString(writer, AtomParserNameTable.XmlUriElement, this.Uri); 
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>figures out if this object should be persisted</summary> 
        /// <returns> true, if it's worth saving</returns>
        //////////////////////////////////////////////////////////////////////
        public override bool ShouldBePersisted()
        {
            if (!base.ShouldBePersisted())
            {
                if (Utilities.IsPersistable(this.name))
                {
                    return true;
                }
                if (Utilities.IsPersistable(this.email))
                {
                    return true;
                }
                if (Utilities.IsPersistable(this.uri))
                {
                    return true;
                }
                return false;
            }
            return true;
        }
        /////////////////////////////////////////////////////////////////////////////


        /////////////////////////////////////////////////////////////////////////////
        #endregion

    }
    /////////////////////////////////////////////////////////////////////////////

}
/////////////////////////////////////////////////////////////////////////////
