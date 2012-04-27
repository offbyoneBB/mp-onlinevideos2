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

// <summary>basenametable, holds common names for atom&rss parsing</summary> 
namespace Google.GData.Client {
    /// <summary>BaseNameTable. An initialized nametable for faster XML processing
    /// parses:  
    ///     *  opensearch:totalResults - the total number of search results available (not necessarily all present in the feed).
    ///     *  opensearch:startIndex - the 1-based index of the first result.
    ///     *  opensearch:itemsPerPage - the maximum number of items that appear on one page. This allows clients to generate direct links to any set of subsequent pages.
    ///     *  gData:processed
    ///  </summary>
    public class BaseNameTable {
        /// <summary>the nametable itself, based on XML core</summary> 
        private NameTable atomNameTable;

        /// <summary>opensearch:totalResults</summary> 
        private object totalResults;
        /// <summary>opensearch:startIndex</summary> 
        private object startIndex;
        /// <summary>opensearch:itemsPerPage</summary> 
        private object itemsPerPage;
        /// <summary>xml base</summary> 
        private object baseUri;
        /// <summary>xml language</summary> 
        private object language;

        // batch extensions
        private object batchId;
        private object batchStatus;
        private object batchOperation;
        private object batchInterrupt;
        private object batchContentType;
        private object batchStatusCode;
        private object batchReason;
        private object batchErrors;
        private object batchError;
        private object batchSuccessCount;
        private object batchFailureCount;
        private object batchParsedCount;
        private object batchField;
        private object batchUnprocessed;

        private object type;
        private object value;
        private object name;
        private object eTagAttribute;

        /// <summary>
        /// namespace of the opensearch v1.0 elements
        /// </summary>
        public const string NSOpenSearchRss = "http://a9.com/-/spec/opensearchrss/1.0/";
        /// <summary>
        /// namespace of the opensearch v1.1 elements
        /// </summary>
        public const string NSOpenSearch11 = "http://a9.com/-/spec/opensearch/1.1/";
        /// <summary>static namespace string declaration</summary> 
        public const string NSAtom = "http://www.w3.org/2005/Atom";
        /// <summary>namespace for app publishing control, draft version</summary> 
        public const string NSAppPublishing = "http://purl.org/atom/app#";
        /// <summary>namespace for app publishing control, final version</summary> 
        public const string NSAppPublishingFinal = "http://www.w3.org/2007/app";
        /// <summary>xml namespace</summary> 
        public const string NSXml = "http://www.w3.org/XML/1998/namespace";
        /// <summary>GD namespace</summary> 
        public const string gNamespace = "http://schemas.google.com/g/2005";
        /// <summary>GData batch extension namespace</summary> 
        public const string gBatchNamespace = "http://schemas.google.com/gdata/batch";
        /// <summary>GD namespace prefix</summary> 
        public const string gNamespacePrefix = gNamespace + "#";
        /// <summary>the post definiton in the link collection</summary> 
        public const string ServicePost = gNamespacePrefix + "post";
        /// <summary>the feed definition in the link collection</summary> 
        public const string ServiceFeed = gNamespacePrefix + "feed";
        /// <summary>the batch URI definition in the link collection</summary>
        public const string ServiceBatch = gNamespacePrefix + "batch";
        /// <summary>GData Kind Scheme</summary> 
        public const string gKind = gNamespacePrefix + "kind";
        /// <summary>label scheme</summary>
        public const string gLabels = gNamespace + "/labels";
        /// <summary>the edit definition in the link collection</summary> 
        public const string ServiceEdit = "edit";
        /// <summary>the next chunk URI in the link collection</summary> 
        public const string ServiceNext = "next";
        /// <summary>the previous chunk URI in the link collection</summary> 
        public const string ServicePrev = "previous";
        /// <summary>the self URI in the link collection</summary> 
        public const string ServiceSelf = "self";
        /// <summary>the alternate URI in the link collection</summary> 
        public const string ServiceAlternate = "alternate";
        /// <summary>the alternate URI in the link collection</summary> 
        public const string ServiceMedia = "edit-media";

        /// <summary>prefix for atom if writing</summary> 
        public const string AtomPrefix = "atom";

        /// <summary>prefix for gNamespace if writing</summary> 
        public const string gDataPrefix = "gd";

        /// <summary>prefix for gdata:batch if writing</summary> 
        public const string gBatchPrefix = "batch";

        /// <summary>prefix for gd:errors</summary> 
        public const string gdErrors = "errors";

        /// <summary>prefix for gd:error</summary> 
        public const string gdError = "error";

        /// <summary>prefix for gd:domain</summary> 
        public const string gdDomain = "domain";

        /// <summary>prefix for gd:code</summary> 
        public const string gdCode = "code";

        /// <summary>prefix for gd:location</summary> 
        public const string gdLocation = "location";

        /// <summary>prefix for gd:internalReason</summary> 
        public const string gdInternalReason = "internalReason";

        // app publishing control strings
        /// <summary>prefix for appPublishing if writing</summary> 
        public const string gAppPublishingPrefix = "app";

        /// <summary>xmlelement for app:control</summary> 
        public const string XmlElementPubControl = "control";

        /// <summary>xmlelement for app:draft</summary> 
        public const string XmlElementPubDraft = "draft";
        /// <summary>xmlelement for app:draft</summary> 
        public const string XmlElementPubEdited = "edited";

        /// <summary>
        /// static string for parsing the etag attribute
        /// </summary>
        /// <returns></returns>
        public const string XmlEtagAttribute = "etag";

        // batch strings:

        /// <summary>xmlelement for batch:id</summary> 
        public const string XmlElementBatchId = "id";
        /// <summary>xmlelement for batch:operation</summary> 
        public const string XmlElementBatchOperation = "operation";
        /// <summary>xmlelement for batch:status</summary> 
        public const string XmlElementBatchStatus = "status";
        /// <summary>xmlelement for batch:interrupted</summary> 
        public const string XmlElementBatchInterrupt = "interrupted";
        /// <summary>xmlattribute for batch:status@contentType</summary> 
        public const string XmlAttributeBatchContentType = "content-type";
        /// <summary>xmlattribute for batch:status@code</summary> 
        public const string XmlAttributeBatchStatusCode = "code";
        /// <summary>xmlattribute for batch:status@reason</summary> 
        public const string XmlAttributeBatchReason = "reason";
        /// <summary>xmlelement for batch:status:errors</summary> 
        public const string XmlElementBatchErrors = "errors";
        /// <summary>xmlelement for batch:status:errors:error</summary> 
        public const string XmlElementBatchError = "error";
        /// <summary>xmlattribute for batch:interrupted@success</summary> 
        public const string XmlAttributeBatchSuccess = "success";
        /// <summary>XmlAttribute for batch:interrupted@parsed</summary> 
        public const string XmlAttributeBatchParsed = "parsed";
        /// <summary>XmlAttribute for batch:interrupted@field</summary> 
        public const string XmlAttributeBatchField = "field";
        /// <summary>XmlAttribute for batch:interrupted@unprocessed</summary> 
        public const string XmlAttributeBatchUnprocessed = "unprocessed";
        /// <summary>XmlConstant for value in enums</summary> 
        public const string XmlValue = "value";
        /// <summary>XmlConstant for name in enums</summary> 
        public const string XmlName = "name";
        /// <summary>XmlAttribute for type in enums</summary> 
        public const string XmlAttributeType = "type";
        /// <summary>XmlAttribute for key in enums</summary> 
        public const string XmlAttributeKey = "key";

        /// <summary>initializes the name table for use with atom parsing. This is the
        /// only place where strings are defined for parsing</summary> 
        public virtual void InitAtomParserNameTable() {
            // create the nametable object
            Tracing.TraceCall("Initializing basenametable support");
            this.atomNameTable = new NameTable();
            // <summary>add the keywords for the Feed
            this.totalResults = this.atomNameTable.Add("totalResults");
            this.startIndex = this.atomNameTable.Add("startIndex");
            this.itemsPerPage = this.atomNameTable.Add("itemsPerPage");
            this.baseUri = this.atomNameTable.Add("base");
            this.language = this.atomNameTable.Add("lang");

            // batch keywords
            this.batchId = this.atomNameTable.Add(BaseNameTable.XmlElementBatchId);
            this.batchOperation = this.atomNameTable.Add(BaseNameTable.XmlElementBatchOperation);
            this.batchStatus = this.atomNameTable.Add(BaseNameTable.XmlElementBatchStatus);
            this.batchInterrupt = this.atomNameTable.Add(BaseNameTable.XmlElementBatchInterrupt);
            this.batchContentType = this.atomNameTable.Add(BaseNameTable.XmlAttributeBatchContentType);
            this.batchStatusCode = this.atomNameTable.Add(BaseNameTable.XmlAttributeBatchStatusCode);
            this.batchReason = this.atomNameTable.Add(BaseNameTable.XmlAttributeBatchReason);
            this.batchErrors = this.atomNameTable.Add(BaseNameTable.XmlElementBatchErrors);
            this.batchError = this.atomNameTable.Add(BaseNameTable.XmlElementBatchError);
            this.batchSuccessCount = this.atomNameTable.Add(BaseNameTable.XmlAttributeBatchSuccess);
            this.batchFailureCount = this.batchError;
            this.batchParsedCount = this.atomNameTable.Add(BaseNameTable.XmlAttributeBatchParsed);
            this.batchField = this.atomNameTable.Add(BaseNameTable.XmlAttributeBatchField);
            this.batchUnprocessed = this.atomNameTable.Add(BaseNameTable.XmlAttributeBatchUnprocessed);

            this.type = this.atomNameTable.Add(BaseNameTable.XmlAttributeType);
            this.value = this.atomNameTable.Add(BaseNameTable.XmlValue);
            this.name = this.atomNameTable.Add(BaseNameTable.XmlName);
            this.eTagAttribute = this.atomNameTable.Add(BaseNameTable.XmlEtagAttribute);
        }

        #region Read only accessors 8/10/2005

        /// <summary>Read only accessor for atomNameTable</summary> 
        internal NameTable Nametable {
            get { return this.atomNameTable; }
        }

        /// <summary>Read only accessor for BatchId</summary> 
        public object BatchId {
            get { return this.batchId; }
        }

        /// <summary>Read only accessor for BatchOperation</summary> 
        public object BatchOperation {
            get { return this.batchOperation; }
        }

        /// <summary>Read only accessor for BatchStatus</summary> 
        public object BatchStatus {
            get { return this.batchStatus; }
        }

        /// <summary>Read only accessor for BatchInterrupt</summary> 
        public object BatchInterrupt {
            get { return this.batchInterrupt; }
        }

        /// <summary>Read only accessor for BatchContentType</summary>
        public object BatchContentType {
            get { return this.batchContentType; }
        }

        /// <summary>Read only accessor for BatchStatusCode</summary>
        public object BatchStatusCode {
            get { return this.batchStatusCode; }
        }

        /// <summary>Read only accessor for BatchErrors</summary> 
        public object BatchErrors {
            get { return this.batchErrors; }
        }

        /// <summary>Read only accessor for BatchError</summary> 
        public object BatchError {
            get { return this.batchError; }
        }

        /// <summary>Read only accessor for BatchReason</summary> 
        public object BatchReason {
            get { return this.batchReason; }
        }

        /// <summary>Read only accessor for BatchReason</summary> 
        public object BatchField {
            get { return this.batchField; }
        }

        /// <summary>Read only accessor for BatchUnprocessed</summary> 
        public object BatchUnprocessed {
            get { return this.batchUnprocessed; }
        }

        /// <summary>Read only accessor for BatchSuccessCount</summary> 
        public object BatchSuccessCount {
            get { return this.batchSuccessCount; }
        }

        /// <summary>Read only accessor for BatchFailureCount</summary> 
        public object BatchFailureCount {
            get { return this.batchFailureCount; }
        }

        /// <summary>Read only accessor for BatchParsedCount</summary> 
        public object BatchParsedCount {
            get { return this.batchParsedCount; }
        }

        /// <summary>Read only accessor for totalResults</summary> 
        public object TotalResults {
            get { return this.totalResults; }
        }

        /// <summary>Read only accessor for startIndex</summary>
        public object StartIndex {
            get { return this.startIndex; }
        }

        /// <summary>Read only accessor for itemsPerPage</summary>
        public object ItemsPerPage {
            get { return this.itemsPerPage; }
        }

        /// <summary>Read only accessor for parameter</summary>
        static public string Parameter {
            get { return "parameter"; }
        }

        /// <summary>Read only accessor for baseUri</summary> 
        public object Base {
            get { return this.baseUri; }
        }

        /// <summary>Read only accessor for language</summary> 
        public object Language {
            get { return this.language; }
        }

        /// <summary>Read only accessor for value</summary>
        public object Value {
            get { return this.value; }
        }

        /// <summary>Read only accessor for value</summary> 
        public object Type {
            get { return this.type; }
        }

        /// <summary>Read only accessor for name</summary> 
        public object Name {
            get { return this.name; }
        }

        /// <summary>Read only accessor for etag</summary> 
        public object ETag {
            get { return this.eTagAttribute; }
        }
        
        #endregion end of Read only accessors

        /// <summary>
        /// returns the correct opensearchnamespace to use based
        /// on the version information passed in. All protocols with 
        /// version > 1 use opensearch1.1 where version 1 uses
        /// opensearch 1.0
        /// </summary>
        /// <param name="v">The versioninformation</param>
        /// <returns></returns>
        public static string OpenSearchNamespace(IVersionAware v) {
            int major = VersionDefaults.Major;
            if (v != null) {
                major = v.ProtocolMajor;
            }

            if (major == 1) {
                return BaseNameTable.NSOpenSearchRss;
            }

            return BaseNameTable.NSOpenSearch11;
        }

        /// <summary>
        /// returns the correct app:publishing namespace to use based
        /// on the version information passed in. All protocols with 
        /// version > 1 use the final version of the namespace, where 
        /// version 1 uses the draft version. 
        /// </summary>
        /// <param name="v">The versioninformation</param>
        /// <returns></returns>
        public static string AppPublishingNamespace(IVersionAware v) {
            int major = VersionDefaults.Major;

            if (v != null) {
                major = v.ProtocolMajor;
            }

            if (major == 1) {
                return BaseNameTable.NSAppPublishing;
            }

            return BaseNameTable.NSAppPublishingFinal;
        }
    }
}
