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
/* Change history
* Oct 13 2008  Joe Feser       joseph.feser@gmail.com
* Converted ArrayLists and other .NET 1.1 collections to use Generics
* Combined IExtensionElement and IExtensionElementFactory interfaces
* 
*/
#region Using directives

#define USE_TRACING

using System;
using System.Xml;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;


#endregion

//////////////////////////////////////////////////////////////////////
// contains AtomId
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    /// <summary>enum to define the GDataBatchOperationType...</summary> 
    public enum GDataBatchOperationType
    {
        /// <summary>this is an insert operatoin</summary> 
        insert,
        /// <summary>this is an update operation</summary> 
        update,
        /// <summary>this is a delete operation</summary> 
        delete,
        /// <summary>this is a query operation</summary>
        query,
        /// <summary>the default (a no-op)</summary>
        Default
    }

    /// <summary>
    /// holds the batch status information
    /// </summary>
    public class GDataBatchStatus : IExtensionElementFactory
    {
        private int code;
        private string reason;
        private string contentType;
        private List<GDataBatchError> errorList;

        /// <summary>default value for the status code</summary>
        public const int CodeDefault = -1;

        /// <summary>
        /// set's the defaults for code
        /// </summary>
        public GDataBatchStatus()
        {
            this.Code = CodeDefault;
        }
        //////////////////////////////////////////////////////////////////////
        /// <summary>returns the status code of the operation</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int Code
        {
            get
            {
                return this.code;
            }
            set
            {
                this.code = value;
            }
        }
        // end of accessor public string Code

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Reason</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Reason
        {
            get
            {
                return this.reason;
            }
            set
            {
                this.reason = value;
            }
        }
        // end of accessor public string Reason


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string ContentType</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string ContentType
        {
            get
            {
                return this.contentType;
            }
            set
            {
                this.contentType = value;
            }
        }
        // end of accessor public string ContentType


        //////////////////////////////////////////////////////////////////////
        /// <summary>the error list</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public List<GDataBatchError> Errors
        {
            get
            {
                if (this.errorList == null)
                {
                    this.errorList = new List<GDataBatchError>();
                }
                return this.errorList;
            }
        }


        #region Persistence overloads

        /// <summary>
        /// Persistence method for the GDataBatchStatus object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new System.ArgumentNullException("writer");
            }
            writer.WriteStartElement(BaseNameTable.gBatchPrefix, BaseNameTable.XmlElementBatchStatus, BaseNameTable.gBatchPrefix);

            if (this.Code != GDataBatchStatus.CodeDefault)
            {
                writer.WriteAttributeString(BaseNameTable.XmlAttributeBatchStatusCode, this.Code.ToString(CultureInfo.InvariantCulture));
            }
            if (Utilities.IsPersistable(this.ContentType))
            {
                writer.WriteAttributeString(BaseNameTable.XmlAttributeBatchContentType, this.ContentType);
            }
            if (Utilities.IsPersistable(this.Reason))
            {
                writer.WriteAttributeString(BaseNameTable.XmlAttributeBatchReason, this.Reason);
            }
            writer.WriteEndElement();
        }
        #endregion

        #region IExtensionElementFactory Members

        /// <summary>
        /// reads the current positioned reader and creates a batchstatus element
        /// </summary>
        /// <param name="reader">XmlReader positioned at the start of the status element</param>
        /// <param name="parser">The Feedparser to be used</param>
        /// <returns>GDataBatchStatus</returns>
        public static GDataBatchStatus ParseBatchStatus(XmlReader reader, AtomFeedParser parser)
        {
            Tracing.Assert(reader != null, "reader should not be null");
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            GDataBatchStatus status = null;

            object localname = reader.LocalName;
            if (localname.Equals(parser.Nametable.BatchStatus))
            {
                status = new GDataBatchStatus();
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        localname = reader.LocalName;
                        if (localname.Equals(parser.Nametable.BatchReason))
                        {
                            status.Reason = Utilities.DecodedValue(reader.Value);
                        }
                        else if (localname.Equals(parser.Nametable.BatchContentType))
                        {
                            status.ContentType = Utilities.DecodedValue(reader.Value);
                        }
                        else if (localname.Equals(parser.Nametable.BatchStatusCode))
                        {
                            status.Code = int.Parse(Utilities.DecodedValue(reader.Value), CultureInfo.InvariantCulture);
                        }
                    }
                }

                reader.MoveToElement();

                // FIX: THIS CODE SEEMS TO MAKE AN INFINITE LOOP WITH NextChildElement()

                int lvl = -1;
                // status can have one child element, errors
                while (Utilities.NextChildElement(reader, ref lvl))
                {
                    localname = reader.LocalName;

                    if (localname.Equals(parser.Nametable.BatchErrors))
                    {
                        GDataBatchError.ParseBatchErrors(reader, parser, status);
                    }
                }
            }
            return status;
        }

        /// <summary>
        /// the xmlname of the element
        /// </summary>
        public string XmlName
        {
            get
            {
                return BaseNameTable.XmlElementBatchStatus;
            }
        }

        /// <summary>
        ///  the xmlnamespace for a batchstatus
        /// </summary>
        public string XmlNameSpace
        {
            get
            {
                return BaseNameTable.gBatchNamespace;
            }
        }

        /// <summary>
        /// the prefered xmlprefix to use
        /// </summary>
        public string XmlPrefix
        {
            get
            {
                return BaseNameTable.gBatchPrefix;
            }
        }

        /// <summary>
        /// creates a new batchstatus element
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        public IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser)
        {
            return ParseBatchStatus(new XmlNodeReader(node), parser);
        }

        #endregion

    }
    /// <summary>
    ///  represents the Error element in the GDataBatch response
    /// </summary>
    public class GDataBatchError : IExtensionElementFactory
    {
        private string errorType;
        private string errorReason;
        private string field;

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method Type</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Type
        {
            get
            {
                return this.errorType;
            }
            set
            {
                this.errorType = value;
            }
        }
        // end of accessor Type


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Field</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Field
        {
            get
            {
                return this.field;
            }
            set
            {
                this.field = value;
            }
        }
        // end of accessor public string Field

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Reason</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Reason
        {
            get
            {
                return this.errorReason;
            }
            set
            {
                this.errorReason = value;
            }
        }
        // end of accessor public string Reason

        #region Persistence overloads
        /// <summary>
        /// Persistence method for the GDataBatchError object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
        }
        #endregion

        #region IExtensionElementFactory Members

        /// <summary>
        ///  parses a list of errors
        /// </summary>
        /// <param name="reader">XmlReader positioned at the start of the status element</param>
        /// <param name="status">the batch status element to add the errors tohe</param>
        /// <param name="parser">the feedparser to be used</param>
        public static void ParseBatchErrors(XmlReader reader, AtomFeedParser parser, GDataBatchStatus status)
        {
            if (reader == null)
            {
                throw new System.ArgumentNullException("reader");
            }

            object localname = reader.LocalName;
            if (localname.Equals(parser.Nametable.BatchErrors))
            {
                int lvl = -1;
                while (Utilities.NextChildElement(reader, ref lvl))
                {
                    localname = reader.LocalName;
                    if (localname.Equals(parser.Nametable.BatchError))
                    {
                        status.Errors.Add(ParseBatchError(reader, parser));
                    }
                }
            }
            return;
        }

        /// <summary>
        /// parses a single error element
        /// </summary>
        /// <param name="reader">XmlReader positioned at the start of the status element</param>
        /// <param name="parser">the feedparser to be used</param>
        /// <returns>GDataBatchError</returns>
        public static GDataBatchError ParseBatchError(XmlReader reader, AtomFeedParser parser)
        {
            if (reader == null)
            {
                throw new System.ArgumentNullException("reader");
            }

            object localname = reader.LocalName;
            GDataBatchError error = null;
            if (localname.Equals(parser.Nametable.BatchError))
            {
                error = new GDataBatchError();
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        localname = reader.LocalName;
                        if (localname.Equals(parser.Nametable.BatchReason))
                        {
                            error.Reason = Utilities.DecodedValue(reader.Value);
                        }
                        else if (localname.Equals(parser.Nametable.Type))
                        {
                            error.Type = Utilities.DecodedValue(reader.Value);
                        }
                        else if (localname.Equals(parser.Nametable.BatchField))
                        {
                            error.Field = Utilities.DecodedValue(reader.Value);
                        }
                    }
                }
            }
            return error;
        }

        /// <summary>
        ///  the name to use
        /// </summary>
        public string XmlName
        {
            get
            {
                return BaseNameTable.XmlElementBatchError;
            }
        }

        /// <summary>
        /// the namespace to use
        /// </summary>
        public string XmlNameSpace
        {
            get
            {
                return BaseNameTable.gBatchNamespace;
            }
        }

        /// <summary>
        /// the prefered prefix
        /// </summary>
        public string XmlPrefix
        {
            get
            {
                return BaseNameTable.gBatchPrefix;
            }
        }

        /// <summary>
        /// creates a GDataBatchError element 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        public IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser)
        {
            return ParseBatchError(new XmlNodeReader(node), parser);
        }

        #endregion
    }
    /// <summary>
    /// holds the batch status information
    /// </summary>
    public class GDataBatchInterrupt : IExtensionElementFactory
    {
        private string reason;
        private int success;
        private int failures;
        private int parsed;
        private int unprocessed;


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Reason</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Reason
        {
            get
            {
                return this.reason;
            }
            set
            {
                this.reason = value;
            }
        }
        // end of accessor public string Reason

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public int Successes</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int Successes
        {
            get
            {
                return this.success;
            }
            set
            {
                this.success = value;
            }
        }
        // end of accessor public int Success


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public int Failures</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int Failures
        {
            get
            {
                return this.failures;
            }
            set
            {
                this.failures = value;
            }
        }
        // end of accessor public int Failures

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public int Unprocessed</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int Unprocessed
        {
            get
            {
                return this.unprocessed;
            }
            set
            {
                this.unprocessed = value;
            }
        }
        // end of accessor public int Unprocessed

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public int Parsed</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int Parsed
        {
            get
            {
                return this.parsed;
            }
            set
            {
                this.parsed = value;
            }
        }
        // end of accessor public int Parsed

        #region Persistence overloads
        /// <summary>
        /// Persistence method for the GDataBatchInterrupt object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
        }
        #endregion

        #region IExtensionElementFactory Members

        /// <summary>
        /// parses a batchinterrupt element from a correctly positioned reader
        /// </summary>
        /// <param name="reader">XmlReader at the start of the element</param>
        /// <param name="parser">the feedparser to be used</param>
        /// <returns>GDataBatchInterrupt</returns>
        public static GDataBatchInterrupt ParseBatchInterrupt(XmlReader reader, AtomFeedParser parser)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            object localname = reader.LocalName;
            GDataBatchInterrupt interrupt = null;
            if (localname.Equals(parser.Nametable.BatchInterrupt))
            {
                interrupt = new GDataBatchInterrupt();
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        localname = reader.LocalName;
                        if (localname.Equals(parser.Nametable.BatchReason))
                        {
                            interrupt.Reason = Utilities.DecodedValue(reader.Value);
                        }
                        else if (localname.Equals(parser.Nametable.BatchSuccessCount))
                        {
                            interrupt.Successes = int.Parse(Utilities.DecodedValue(reader.Value), CultureInfo.InvariantCulture);
                        }
                        else if (localname.Equals(parser.Nametable.BatchFailureCount))
                        {
                            interrupt.Failures = int.Parse(Utilities.DecodedValue(reader.Value), CultureInfo.InvariantCulture);
                        }
                        else if (localname.Equals(parser.Nametable.BatchParsedCount))
                        {
                            interrupt.Parsed = int.Parse(Utilities.DecodedValue(reader.Value), CultureInfo.InvariantCulture);
                        }
                        else if (localname.Equals(parser.Nametable.BatchUnprocessed))
                        {
                            interrupt.Unprocessed = int.Parse(Utilities.DecodedValue(reader.Value), CultureInfo.InvariantCulture);
                        }

                    }
                }
            }
            return interrupt;

        }

        /// <summary>
        /// returns the xmlname to sue
        /// </summary>
        public string XmlName
        {
            get
            {
                return BaseNameTable.XmlElementBatchInterrupt;
            }
        }

        /// <summary>
        /// returns the xmlnamespace
        /// </summary>
        public string XmlNameSpace
        {
            get
            {
                return BaseNameTable.gBatchNamespace;
            }
        }

        /// <summary>
        /// the xmlprefix
        /// </summary>
        public string XmlPrefix
        {
            get
            {
                return BaseNameTable.gBatchPrefix;
            }
        }

        /// <summary>
        /// factory method to create an instance of a batchinterrupt during parsing
        /// </summary>
        /// <param name="node">the xmlnode that is going to be parsed</param>
        /// <param name="parser">the feedparser that is used right now</param>
        /// <returns></returns>
        public IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser)
        {
            return ParseBatchInterrupt(new XmlNodeReader(node), parser);
        }

        #endregion
    }
    //////////////////////////////////////////////////////////////////////
    /// <summary>The GDataFeedBatch object holds batch related information
    /// for the AtomFeed
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class GDataBatchFeedData : IExtensionElementFactory
    {
        private GDataBatchOperationType operationType;
        /// <summary>
        /// constructor, set's the default for the operation type
        /// </summary>
        public GDataBatchFeedData()
        {
            this.operationType = GDataBatchOperationType.Default;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public GDataBatchOperationType Type</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public GDataBatchOperationType Type
        {
            get
            {
                return this.operationType;
            }
            set
            {
                this.operationType = value;
            }
        }
        // end of accessor public GDataBatchOperationType Type


        #region Persistence overloads
        /// <summary>
        /// Persistence method for the GDataBatch object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new System.ArgumentNullException("writer");
            }

            if (this.Type != GDataBatchOperationType.Default)
            {
                writer.WriteStartElement(XmlPrefix, XmlName, XmlNameSpace);
                writer.WriteAttributeString(BaseNameTable.XmlAttributeType, this.operationType.ToString());
                writer.WriteEndElement();
            }
        }
        #endregion
        #region IExtensionElementFactory Members

        /// <summary>
        /// the xmlname to use
        /// </summary>
        public string XmlName
        {
            get
            {
                return BaseNameTable.XmlElementBatchOperation;
            }
        }

        /// <summary>
        /// the xml namespace to use
        /// </summary>
        public string XmlNameSpace
        {
            get
            {
                return BaseNameTable.gBatchNamespace;
            }
        }

        /// <summary>
        /// the xmlprefix to use
        /// </summary>
        public string XmlPrefix
        {
            get
            {
                return BaseNameTable.gBatchPrefix;
            }
        }

        /// <summary>
        /// factory method to create an instance of a batchinterrupt during parsing
        /// </summary>
        /// <param name="node">the xmlnode that is going to be parsed</param>
        /// <param name="parser">the feedparser that is used right now</param>
        /// <returns></returns>
        public IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
    /////////////////////////////////////////////////////////////////////////////

    //////////////////////////////////////////////////////////////////////
    /// <summary>The GDataEntryBatch object holds batch related information\
    /// for an AtomEntry
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class GDataBatchEntryData : IExtensionElementFactory
    {
        private GDataBatchOperationType operationType;
        private string id;
        private GDataBatchStatus status;
        private GDataBatchInterrupt interrupt;

        /// <summary>
        /// constructor, sets the default for the operation type
        /// </summary>
        public GDataBatchEntryData()
        {
            this.operationType = GDataBatchOperationType.Default;
        }

        /// <summary>
        /// Constructor for the batch data
        /// </summary>
        /// <param name="type">The batch operation to be performed</param>
        public GDataBatchEntryData(GDataBatchOperationType type)
        {
            this.Type = type;
        }


        /// <summary>
        /// Constructor for batch data
        /// </summary>
        /// <param name="id">The batch ID of this entry</param>
        /// <param name="type">The batch operation to be performed</param>
        public GDataBatchEntryData(string id, GDataBatchOperationType type)
            : this(type)
        {
            this.Id = id;
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public GDataBatchOperationType Type</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public GDataBatchOperationType Type
        {
            get
            {
                return this.operationType;
            }
            set
            {
                this.operationType = value;
            }
        }
        // end of accessor public GDataBatchOperationType Type

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Id</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }
        // end of accessor public string Id


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor for the GDataBatchInterrrupt element</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public GDataBatchInterrupt Interrupt
        {
            get
            {
                return this.interrupt;
            }
            set
            {
                this.interrupt = value;
            }
        }
        // end of accessor public Interrupt


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public GDataBatchStatus Status</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public GDataBatchStatus Status
        {
            get
            {
                if (this.status == null)
                {
                    this.status = new GDataBatchStatus();
                }
                return this.status;
            }
            set
            {
                this.status = value;
            }
        }
        // end of accessor public GDataBatchStatus Status



        #region Persistence overloads
        /// <summary>
        /// Persistence method for the GDataEntryBatch object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new System.ArgumentNullException("writer");
            }

            if (this.Id != null)
            {
                writer.WriteElementString(BaseNameTable.XmlElementBatchId, BaseNameTable.gBatchNamespace, this.id);
            }
            if (this.Type != GDataBatchOperationType.Default)
            {
                writer.WriteStartElement(XmlPrefix, XmlName, XmlNameSpace);
                writer.WriteAttributeString(BaseNameTable.XmlAttributeType, this.operationType.ToString());
                writer.WriteEndElement();
            }
            if (this.status != null)
            {
                this.status.Save(writer);
            }
        }
        #endregion

        #region IExtensionElementFactory Members

        /// <summary>
        /// xml local name to use
        /// </summary>
        public string XmlName
        {
            get
            {
                //TODO This doesn't seem correct.
                return BaseNameTable.XmlElementBatchOperation;
            }
        }

        /// <summary>
        /// xml namespace to use
        /// </summary>
        public string XmlNameSpace
        {
            get
            {
                return BaseNameTable.gBatchNamespace;
            }
        }

        /// <summary>
        /// xml prefix to use
        /// </summary>
        public string XmlPrefix
        {
            get
            {
                return BaseNameTable.gBatchPrefix;
            }
        }

        /// <summary>
        /// creates a new GDataBatchEntryData
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        public IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser)
        {
            //we really don't know how to create an instance of ourself.
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
    /////////////////////////////////////////////////////////////////////////////

}
/////////////////////////////////////////////////////////////////////////////
