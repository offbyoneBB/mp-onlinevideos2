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
// Contains AtomBaseLink.
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

#if WindowsCE || PocketPC
#else
    //////////////////////////////////////////////////////////////////////
    /// <summary>TypeConverter, so that AtomBaseLink shows up in the property pages
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [ComVisible(false)]
    public class AtomBaseLinkConverter : ExpandableObjectConverter
    {
        
        ///<summary>Standard type converter method</summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType) 
        {
            if (destinationType == typeof(AtomBaseLink)
                || destinationType == typeof(AtomId)
                || destinationType == typeof(AtomIcon)
                || destinationType == typeof(AtomLogo)
                )
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>standard ConvertTo typeconverter code</summary> 
        ///<summary>Standard type converter method</summary>
        public override object ConvertTo(ITypeDescriptorContext context,CultureInfo culture, object value, System.Type destinationType) 
        {
            AtomBaseLink link = value as AtomBaseLink; 

            if (destinationType == typeof(System.String) && link != null)
            {
                return "Uri: " + link.Uri;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
    /////////////////////////////////////////////////////////////////////////////
#endif
    //////////////////////////////////////////////////////////////////////
    /// <summary>AtomBaselink is an intermediate object that adds the URI property
    /// used as the parent class for a lot of other objects (like atomlink, atomicon, etc)
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public abstract class AtomBaseLink : AtomBase
    {
        /// <summary>holds the string rep</summary> 
        private AtomUri uriString;


        
        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Uri</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri Uri
        {
            get {return this.uriString;}
            set {this.Dirty = true;  this.uriString = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>public Uri AbsoluteUri</summary> 
        //////////////////////////////////////////////////////////////////////
        public string AbsoluteUri
        {
            get
            {
                return GetAbsoluteUri(this.Uri.ToString()); 
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        #region Persistence overloads
        //////////////////////////////////////////////////////////////////////
        /// <summary>saves the inner state of the element</summary> 
        /// <param name="writer">the xmlWriter to save into </param>
        //////////////////////////////////////////////////////////////////////
        protected override void SaveInnerXml(XmlWriter writer)
        {
            base.SaveInnerXml(writer);
            WriteEncodedString(writer, this.Uri);
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
                return Utilities.IsPersistable(this.uriString);
            }
            return true;
        }
        /////////////////////////////////////////////////////////////////////////////

        #endregion
    }
    /////////////////////////////////////////////////////////////////////////////
} 
/////////////////////////////////////////////////////////////////////////////
