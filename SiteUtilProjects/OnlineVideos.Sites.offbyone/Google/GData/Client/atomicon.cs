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
using System.Globalization;
using System.ComponentModel;
using System.Xml;


#endregion

//////////////////////////////////////////////////////////////////////
// contains AtomIcon 
// atombase holds the 2 common properties (xml:lang & xml:base)  
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{
    //////////////////////////////////////////////////////////////////////
    /// <summary>atomIcon object representation. 
    ///     The "atom:icon" element's content is an IRI reference [RFC3987] which identifies an image which provides 
    ///     iconic visual identification for a feed.
    ///     The image SHOULD have an aspect ratio of one (horizontal) to one (vertical), and SHOULD be suitable 
    ///     for presentation at a small size.
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [TypeConverterAttribute(typeof(AtomBaseLinkConverter)), DescriptionAttribute("Expand to see the link attributes for the Icon.")]
    public class AtomIcon : AtomBaseLink
    {
        #region Persistence overloads
        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public override string XmlName 
        {
            get { return AtomParserNameTable.XmlIconElement; }
        }
        /////////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////////////////
        #endregion


    }
    /////////////////////////////////////////////////////////////////////////////
} 
/////////////////////////////////////////////////////////////////////////////
