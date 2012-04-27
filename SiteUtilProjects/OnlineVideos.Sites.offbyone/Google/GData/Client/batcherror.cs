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
using System;
using System.Xml;
using Google.GData.Client;
using System.Globalization;

namespace Google.GData.Extensions {
    public class BatchError : SimpleContainer {
        /// <summary>
        /// default constructor for gd:error
        /// </summary>
        public BatchError()
            : base(BaseNameTable.gdError,
                BaseNameTable.gDataPrefix,
                BaseNameTable.gNamespace)
        {
            this.ExtensionFactories.Add(new BatchErrorDomain());
            this.ExtensionFactories.Add(new BatchErrorCode());
            this.ExtensionFactories.Add(new BatchErrorLocation());
            this.ExtensionFactories.Add(new BatchErrorInternalReason());
            this.ExtensionFactories.Add(new BatchErrorId());
        }

        /// <summary>
        /// returns the gd:domain
        /// </summary>
        public string Domain
        {
            get
            {
                return GetStringValue<BatchErrorDomain>(BaseNameTable.gdDomain,
                    BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<BatchErrorDomain>(value.ToString(),
                    BaseNameTable.gdDomain,
                    BaseNameTable.gNamespace);
            }
        }

        /// <summary>
        /// returns the gd:code
        /// </summary>
        public string Code
        {
            get
            {
                return GetStringValue<BatchErrorCode>(BaseNameTable.gdCode,
                    BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<BatchErrorCode>(value.ToString(),
                    BaseNameTable.gdCode,
                    BaseNameTable.gNamespace);
            }
        }

        /// <summary>
        /// returns the gd:location
        /// </summary>
        public BatchErrorLocation Location
        {
            get {
                return FindExtension(BaseNameTable.gdLocation,
                   BaseNameTable.gNamespace) as BatchErrorLocation;
            }
            set {
                ReplaceExtension(BaseNameTable.gdLocation,
                    BaseNameTable.gNamespace,
                    value);
            }
        }

        /// <summary>
        /// returns the gd:internalReason
        /// </summary>
        public string InternalReason
        {
            get
            {
                return GetStringValue<BatchErrorInternalReason>(BaseNameTable.gdInternalReason,
                    BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<BatchErrorInternalReason>(value.ToString(),
                    BaseNameTable.gdInternalReason,
                    BaseNameTable.gNamespace);
            }
        }

        /// <summary>
        /// returns the id
        /// </summary>
        public string Id
        {
            get
            {
                return GetStringValue<BatchErrorId>(BaseNameTable.XmlElementBatchId,
                    BaseNameTable.NSAtom);
            }
            set
            {
                SetStringValue<BatchErrorId>(value.ToString(),
                    BaseNameTable.XmlElementBatchId,
                    BaseNameTable.NSAtom);
            }
        }
    }

    public class BatchErrorDomain : SimpleElement
    {
        /// <summary>
        /// default constructor for gd:domain
        /// </summary>
        public BatchErrorDomain()
            : base(BaseNameTable.gdDomain,
            BaseNameTable.gDataPrefix,
            BaseNameTable.gNamespace)
        {
        }
    }

    public class BatchErrorCode : SimpleElement
    {
        /// <summary>
        /// default constructor for gd:code
        /// </summary>
        public BatchErrorCode()
            : base(BaseNameTable.gdCode,
            BaseNameTable.gDataPrefix,
            BaseNameTable.gNamespace)
        {
        }
    }

    public class BatchErrorLocation : SimpleElement
    {
        /// <summary>
        /// default constructor for gd:location
        /// </summary>
        public BatchErrorLocation()
            : base(BaseNameTable.gdLocation,
            BaseNameTable.gDataPrefix,
            BaseNameTable.gNamespace)
        {
        }

        /// <summary>
        /// Type property accessor
        /// </summary>
        public string Type
        {
            get
            {
                return Convert.ToString(Attributes[BaseNameTable.XmlAttributeType]);
            }
            set
            {
                Attributes[BaseNameTable.XmlAttributeType] = value;
            }
        }
    }

    public class BatchErrorInternalReason : SimpleElement
    {
        /// <summary>
        /// default constructor for gd:internalReason
        /// </summary>
        public BatchErrorInternalReason()
            : base(BaseNameTable.gdInternalReason,
            BaseNameTable.gDataPrefix,
            BaseNameTable.gNamespace)
        {
        }
    }

    public class BatchErrorId : SimpleElement
    {
        /// <summary>
        /// default constructor for id
        /// </summary>
        public BatchErrorId()
            : base(BaseNameTable.XmlElementBatchId,
            "",
            BaseNameTable.NSAtom)
        {
        }
    }
}  
