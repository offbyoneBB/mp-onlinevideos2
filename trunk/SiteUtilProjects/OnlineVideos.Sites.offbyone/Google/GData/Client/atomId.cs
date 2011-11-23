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


#endregion

//////////////////////////////////////////////////////////////////////
// contains AtomId
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{
    //////////////////////////////////////////////////////////////////////
    /// <summary>The "atom:id" element conveys a permanent, universally unique identifier for an entry or feed.
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [TypeConverterAttribute(typeof(AtomBaseLinkConverter)), DescriptionAttribute("Expand to see the link attributes for the Id.")]
    public class AtomId : AtomBaseLink, IComparable
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>empty constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public AtomId() : base()
        {
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>public AtomId(string uri)</summary> 
        /// <param name="link">the URI for the ID</param>
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomId(string link) : base()
        {
            this.Uri = new AtomUri(link); 
        }
        /////////////////////////////////////////////////////////////////////////////

   
        #region Persistence overloads
        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public override string XmlName 
        {
            get { return AtomParserNameTable.XmlIdElement; }
        }
        /////////////////////////////////////////////////////////////////////////////
        #endregion

        #region IComparable Members

        /// <summary>
        /// as we do comparisons, we need to override this
        /// we return the hashcode of our string member
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Uri.GetHashCode(); 
        }


        /// <summary>
        /// overloaded IComparable interface method
        /// </summary>
        /// <param name="obj">the object to compare this instance with</param>
        /// <returns>int</returns>
		public int CompareTo(object obj)
		{
		    AtomId other = obj as AtomId;

            if (other == null)
                return -1;

            if (this.Uri != null) 
                return this.Uri.CompareTo(other.Uri);

            if (other.Uri == null)
                return 0;

            return -1;
		}

		#endregion

        /// <summary>
        /// overridden equal method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>bool</returns>
        public override bool Equals(object obj)
        {
            return this.CompareTo(obj) == 0;
        }

        /// <summary>
        /// overridden comparson operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>bool</returns>
        public static bool operator ==(AtomId a, AtomId b)
        {
            if ((object)a == null && (object)b == null) return true;
            if ((object)a != null && (object)b != null)
            {
                return a.Equals(b);
            }
            return false;
        }

        /// <summary>
        /// overridden comparson operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>bool</returns>
        public static bool operator !=(AtomId a, AtomId b)
        {
            return !(a == b);
        }


    }
    /////////////////////////////////////////////////////////////////////////////
} 
/////////////////////////////////////////////////////////////////////////////
