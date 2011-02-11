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
// <summary>Contains AtomGenerator, an object to represent the
// atom:generator element</summary> 
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{
#if WindowsCE || PocketPC
#else 
    //////////////////////////////////////////////////////////////////////
    /// <summary>TypeConverter, so that AtomGenerator shows up in the property pages
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [ComVisible(false)]
    public class AtomGeneratorConverter : ExpandableObjectConverter
    {
        ///<summary>Standard type converter method</summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType) 
        {
            if (destinationType == typeof(AtomGenerator))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        ///<summary>Standard type converter method</summary>
        public override object ConvertTo(ITypeDescriptorContext context,CultureInfo culture, object value, System.Type destinationType) 
        {
            AtomGenerator generator = value as AtomGenerator; 
            if (destinationType == typeof(System.String) && generator != null)
            {
                return generator.Text;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
    /////////////////////////////////////////////////////////////////////////////

    //////////////////////////////////////////////////////////////////////
    /// <summary>Represents the Generator element /feed/generator in Atom. In RSS, only the name property is used.
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [TypeConverterAttribute(typeof(AtomGeneratorConverter)), DescriptionAttribute("Expand to see the feed generator object.")]
#endif
    public class AtomGenerator : AtomBase
    {

        /// <summary>text part of the Generator element</summary> 
        private string text;
        /// <summary>Uri attribute of the Generator element</summary> 
        private AtomUri uri;
        /// <summary>version attribute of the Generator element</summary> 
        private string version;


        //////////////////////////////////////////////////////////////////////
        /// <summary>standard constructor, not used right now
        /// atomGenerator = element atom:generator {
        ///    atomCommonAttributes,
        ///    attribute url { atomUri }?,
        ///    attribute version { text }?,
        ///    text
        /// }
        /// </summary> 
        //////////////////////////////////////////////////////////////////////
        public AtomGenerator()
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>public AtomGenerator(string text)</summary> 
        /// <param name="text">the human readable representation of the generator</param>
        //////////////////////////////////////////////////////////////////////
        public AtomGenerator(string text)
        {
            this.Text = text; 
        }
        /////////////////////////////////////////////////////////////////////////////


        #region Persistence overloads
        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public override string XmlName 
        {
            get { return AtomParserNameTable.XmlGeneratorElement; }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>overridden to save attributes for this(XmlWriter writer)</summary> 
        /// <param name="writer">the xmlwriter to save into </param>
        //////////////////////////////////////////////////////////////////////
        protected override void SaveXmlAttributes(XmlWriter writer)
        {
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlUriElement, this.Uri);
            WriteEncodedAttributeString(writer, AtomParserNameTable.XmlAttributeVersion, this.Version);
            // call base later as base takes care of writing out extension elements that might close the attribute list
            base.SaveXmlAttributes(writer);
        }
        /////////////////////////////////////////////////////////////////////////////




        //////////////////////////////////////////////////////////////////////
        /// <summary>saves the inner state of the element</summary> 
        /// <param name="writer">the xmlWriter to save into </param>
        //////////////////////////////////////////////////////////////////////
        protected override void SaveInnerXml(XmlWriter writer)
        {
            base.SaveInnerXml(writer);
            WriteEncodedString(writer, this.text);
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
                if (this.uri != null && Utilities.IsPersistable(this.uri.ToString()))
                {
                    return true;
                }
                if (Utilities.IsPersistable(this.version) || Utilities.IsPersistable(this.text))
                {
                    return true;
                }
                return false;
            }
            return true;
        }
        /////////////////////////////////////////////////////////////////////////////

        #endregion


        #region property accessors

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Text</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Text
        {
            get {return this.text;}
            set {this.Dirty = true;  this.text = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Uri Uri</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri Uri
        {
            get {return this.uri;}
            set {this.Dirty = true;  this.uri = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Version</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Version
        {
            get {return this.version;}
            set {this.Dirty = true;  this.version = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        #endregion end of property accessors

    }
    /////////////////////////////////////////////////////////////////////////////
}
