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
using System.Xml;
using System.Collections;
using System.Text;
using Google.GData.Client;

namespace Google.GData.Extensions 
{

    /// <summary>
    /// GData schema extension describing an extended property/value pair
    /// </summary>
    public class ExtendedProperty : SimpleNameValueAttribute
    {

        /// <summary>
        /// default constructor for an extended property
        /// </summary>
        public ExtendedProperty() : base(GDataParserNameTable.XmlExtendedPropertyElement,
                                         BaseNameTable.gDataPrefix,
                                         BaseNameTable.gNamespace)
        {
        }

        /// <summary>
        /// default constructor with an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public ExtendedProperty(string initValue) : base(GDataParserNameTable.XmlExtendedPropertyElement,
                                         BaseNameTable.gDataPrefix,
                                         BaseNameTable.gNamespace)
        {
            this.Value = initValue; 
        }

        /// <summary>
        /// default constructor with a value and a key name
        /// </summary>
        /// <param name="initValue">initial value</param>
        /// <param name="initName">name for the key</param>
        public ExtendedProperty(string initValue, string initName) : base(GDataParserNameTable.XmlExtendedPropertyElement,
                                         BaseNameTable.gDataPrefix,
                                         BaseNameTable.gNamespace)
        {
            this.Value = initValue; 
            this.Name = initName;
        }
    }
}
