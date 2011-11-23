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
using System.Net;
using System.IO;
using System.Xml;
using System.Collections;

#endregion

//////////////////////////////////////////////////////////////////////
// <summary>contains Service, the base interface that 
//  allows to query a service for different feeds
//  </summary>
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client {
    //////////////////////////////////////////////////////////////////////
    /// <summary>base Service interface definition
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public interface IService {
        /// <summary>get/set for credentials to the service calls. Gets passed through to GDatarequest</summary> 
        GDataCredentials Credentials {
            get;
            set;
        }
        /// <summary>get/set for the GDataRequestFactory object to use</summary> 
        IGDataRequestFactory RequestFactory {
            get;
            set;
        }

        /// <summary>
        /// returns the name of the service identifier, like wise for spreadsheets services
        /// </summary>
        string ServiceIdentifier {
            get;
        }

        /// <summary>the minimal Get OpenSearchRssDescription function</summary> 
        Stream QueryOpenSearchRssDescription(Uri serviceUri);

        /// <summary>the minimal query implementation</summary> 
        AtomFeed Query(FeedQuery feedQuery);
        /// <summary>the minimal query implementation with conditional GET</summary> 
        AtomFeed Query(FeedQuery feedQuery, DateTime ifModifiedSince);
        /// <summary>simple update for atom resources</summary> 
        AtomEntry Update(AtomEntry entry);
        /// <summary>simple insert for atom entries, based on a feed</summary> 
        AtomEntry Insert(AtomFeed feed, AtomEntry entry);
        /// <summary>delete an entry</summary> 
        void Delete(AtomEntry entry);
        /// <summary>delete an entry</summary> 
        void Delete(Uri uriTarget);
        /// <summary>batch operation, posting of a set of entries</summary>
        AtomFeed Batch(AtomFeed feed, Uri batchUri);
        /// <summary>simple update for media resources</summary> 
        AtomEntry Update(Uri uriTarget, Stream input, string contentType, string slugHeader);
        /// <summary>simple insert for media resources</summary> 
        AtomEntry Insert(Uri uriTarget, Stream input, string contentType, string slugHeader);
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>the one that creates GDatarequests on the service
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public interface IGDataRequestFactory {
        /// <summary>creation method for GDatarequests</summary> 
        IGDataRequest CreateRequest(GDataRequestType type, Uri uriTarget);
        /// <summary>set wether or not to use gzip for new requests</summary>
        bool UseGZip {
            get;
            set;
        }

        /// <summary>
        /// indicates that the service should use SSL exclusively
        /// </summary>
        bool UseSSL {
            get;
            set;
        }
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>enum to describe the different operations on the GDataRequest
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public enum GDataRequestType {
        /// <summary>The request is used for query</summary>
        Query,
        /// <summary>The request is used for an insert</summary>
        Insert,
        /// <summary>The request is used for an update</summary>
        Update,
        /// <summary>The request is used for a delete</summary>
        Delete,
        /// <summary>This request is used for a batch operation</summary>
        Batch
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>Thin layer to abstract the request/response
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public interface IGDataRequest {
        /// <summary>get/set for credentials to the service calls. Gets passed through to GDatarequest</summary> 
        GDataCredentials Credentials {
            get;
            set;
        }
        /// <summary>set wether or not to use gzip for this request</summary>
        bool UseGZip {
            get;
            set;
        }
        /// <summary>set a timestamp for conditional GET</summary>
        DateTime IfModifiedSince {
            get;
            set;
        }

        /// <summary>gets the request stream to write into</summary> 
        Stream GetRequestStream();
        /// <summary>Executes the request</summary> 
        void Execute();
        /// <summary>gets the response stream to read from</summary> 
        Stream GetResponseStream();
    }

    /// <summary>
    /// interface to indicate that an element supports an Etag. Currently implemented on AbstractEntry,
    /// AbstractFeed and GDataRequest
    /// </summary>
    public interface ISupportsEtag {
        /// <summary>set the etag for updates</summary>
        string Etag {
            get;
            set;
        }

    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>Thin layer to create an action on an item/response
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public interface IBaseWalkerAction {
        /// <summary>the only relevant method here</summary> 
        bool Go(AtomBase atom);
    }

    /// <summary>
    /// Wrapper interface used to replace the ExtensionList.
    /// </summary>
    public interface IExtensionElementFactory {
        /// <summary>
        /// returns the XML local name that is used
        /// </summary>
        string XmlName {
            get;
        }
        /// <summary>
        /// returns the XML namespace that is processed
        /// </summary>
        string XmlNameSpace {
            get;
        }
        /// <summary>
        /// returns the xml prefix used 
        /// </summary>
        string XmlPrefix {
            get;
        }
        /// <summary>
        /// instantiates the correct extension element
        /// </summary>
        /// <param name="node">the xmlnode to parse</param>
        /// <param name="parser">the atomfeedparser to use if deep parsing of subelements is required</param>
        /// <returns></returns>
        IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser);

        /// <summary>the only relevant method here</summary> 
        void Save(XmlWriter writer);
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>interface for commone extension container functionallity
    /// used for AtomBase and SimpleContainer
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public interface IExtensionContainer {
        //////////////////////////////////////////////////////////////////////
        /// <summary>the list of extensions for this container
        /// the elements in that list MUST implement IExtensionElementFactory 
        /// and IExtensionElement</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        ExtensionList ExtensionElements {
            get;
        }

        /// <summary>
        /// Finds a specific ExtensionElement based on its local name
        /// and its namespace. If namespace is NULL, the first one where
        /// the localname matches is found. If there are extensionelements that do 
        /// not implment ExtensionElementFactory, they will not be taken into account
        /// </summary>
        /// <param name="localName">the xml local name of the element to find</param>
        /// <param name="ns">the namespace of the elementToPersist</param>
        /// <returns>Object</returns>
        IExtensionElementFactory FindExtension(string localName, string ns);

        /// <summary>
        /// all extension elements that match a namespace/localname
        /// given will be removed and the new one will be inserted
        /// </summary> 
        /// <param name="localName">the local name to find</param>
        /// <param name="ns">the namespace to match, if null, ns is ignored</param>
        /// <param name="obj">the new element to put in</param>
        void ReplaceExtension(string localName, string ns, IExtensionElementFactory obj);

        /// <summary>
        /// Finds all ExtensionElement based on its local name
        /// and its namespace. If namespace is NULL, allwhere
        /// the localname matches is found. If there are extensionelements that do 
        /// not implment ExtensionElementFactory, they will not be taken into account
        /// Primary use of this is to find XML nodes
        /// </summary>
        /// <param name="localName">the xml local name of the element to find</param>
        /// <param name="ns">the namespace of the elementToPersist</param>
        /// <returns>none</returns>
        ExtensionList FindExtensions(string localName, string ns);

        /// <summary>
        /// Deletes all Extensions from the Extension list that match
        /// a localName and a Namespace. 
        /// </summary>
        /// <param name="localName">the local name to find</param>
        /// <param name="ns">the namespace to match, if null, ns is ignored</param>
        /// <returns>int - the number of deleted extensions</returns>
        int DeleteExtensions(string localName, string ns);

        //////////////////////////////////////////////////////////////////////
        /// <summary>the list of extensions for this container
        /// the elements in that list MUST implement IExtensionElementFactory 
        /// and IExtensionElement</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        ExtensionList ExtensionFactories {
            get;
        }
    }
}
