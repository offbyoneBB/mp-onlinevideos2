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

using System;
using System.Collections;
using System.Text;
using System.Xml;
using Google.GData.Client;

namespace Google.GData.Extensions {

    /// <summary>
    /// Extensible enum type used in many places.
    /// </summary>
    public abstract class EnumConstruct : SimpleAttribute
    {

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="xmlElement">the XmlElement that is used</param>
        protected EnumConstruct(string xmlElement)
            :
            base(xmlElement,
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace)
        {
        }

        /// <summary>
        /// Creates a new EnumConstruct instance with a specific type and value.
        /// When this constructor is used the instance has a constant value and
        /// may not be modified by the setValue() API.
        /// </summary>
        /// <param name="xmlElement">the XmlElement that is used</param>
        /// <param name="initialValue">the initial value of the type</param>
        protected EnumConstruct(string xmlElement, string initialValue)
            :
            base(xmlElement,
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace,
               initialValue)
        {
        }

        /// <summary>
        /// Creates a new EnumConstruct instance with a specific type and namespace
        /// </summary>
        /// <param name="xmlElement">the XmlElement that is used</param>
        /// <param name="prefix">the prefix to use</param>
        /// <param name="nameSpace">the namespace to use</param>
        protected EnumConstruct(string xmlElement, string prefix, string nameSpace)
            :
            base(xmlElement,
               prefix,
               nameSpace)
        {
        }

        /// <summary>
        /// Creates a new EnumConstruct instance with a specific type, namespace and value.
        /// When this constructor is used the instance has a constant value and
        /// may not be modified by the setValue() API.
        /// </summary>
        /// <param name="xmlElement">the XmlElement that is used</param>
        /// <param name="prefix">the prefix to use</param>
        /// <param name="nameSpace">the namespace to use</param>
        /// <param name="initialValue">the initial value</param>
        protected EnumConstruct(string xmlElement, string prefix, string nameSpace, string initialValue)
            :
            base(xmlElement,
               prefix,
               nameSpace,
               initialValue)
        {
            readOnly = true;
        }


        /// <summary>
        /// Construct value cannot be changed
        /// </summary>
        private bool readOnly;


        /// <summary>
        ///  Accessor Method for the enumType
        /// </summary>
        public string Type
        {
            get { return this.XmlName; }
        }

        /// <summary>
        ///  Accessor Method for the value
        /// </summary>
        public override string Value
        {
            get { return base.Value; }
            set
            {
                if (readOnly)
                {
                    throw new ArgumentException(this.XmlName + " instance is read only");
                }
                base.Value = value;
            }
        }

        /// <summary>
        ///  Equal operator overload
        /// </summary>
        /// <param name="obj">the object to compare to</param>
        /// <returns>bool</returns>
        public override bool Equals(Object obj)
        {
            //
            // Two EnumConstant instances are considered equal of they are of the
            // same concrete subclass and have the same type/value strings.  If
            // a subtype adds additional member elements that effect the equivalence  
            // test, it *must* override this implemention.
            //
            if (obj == null || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            EnumConstruct ec = (EnumConstruct)obj;

            if (!Type.Equals(ec.Type))
                return false;

            if (Value != null)
                return Value.Equals(ec.Value);

            return ec.Value == null;
        }

        /// <summary>
        ///  GetHashCode overload
        /// </summary>
        /// <returns>a hash based on the string value</returns>
        public override int GetHashCode()
        {
            // the hashcode for an enum will be derived by it's value          
            return Value != null ? Value.GetHashCode() : 0;
        }
    }
}   
