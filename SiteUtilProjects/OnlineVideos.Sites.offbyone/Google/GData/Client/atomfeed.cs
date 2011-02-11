/* Copyright (c) 2006-2008 Google Inc.7
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
using System.Collections;
using System.Net;
using System.IO; 
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;



#endregion

//////////////////////////////////////////////////////////////////////
// <summary>Contains AtomFeed, an object to represent the atom:feed
// element.</summary> 
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>Base class to read gData feeds in Atom</summary>
    /// <remarks>
    /// <para>
    /// Version 1.0 changed to:
    /// <code>
    /// AtomFeed =
    ///    element atom:feed {
    ///       atomCommonAttributes,
    ///       (atomAuthor*
    ///        atomCategory*
    ///        atomContributor*
    ///        atomGenerator?
    ///        atomIcon?
    ///        atomId
    ///        atomLink*
    ///        atomLogo?
    ///        atomRights?
    ///        atomSubtitle?
    ///        atomTitle
    ///        atomUpdated
    ///        extensionElement*),
    ///       atomEntry*
    ///    }
    /// </code>
    /// In addition it holds:
    /// <list type="bullet">
    ///     <item>
    ///         <term><c>opensearch:totalResults</c></term>
    ///         <description>Total number of search results available (not necessarily all present in the feed).</description>
    ///     </item>
    ///     <item>
    ///         <term><c>opensearch:startIndex</c></term>
    ///         <description>The 1-based index of the first result.</description>
    ///     </item>
    ///     <item>
    ///         <term><c>opensearch:itemsPerPage</c></term>
    ///         <description>The maximum number of items that appear on one page. This allows clients to generate direct links to any set of subsequent pages.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// In addition to these OpenSearch tags, the response also includes the following Atom and gData tags:
    /// <list type="bullet">
    ///     <item>
    ///         <term><c>atom:link rel="service.feed" type="application/atom+xml" href="..."/></c></term>
    ///         <description>Specifies the URI where the complete Atom feed can be retrieved.</description>
    ///     </item>
    ///     <item>
    ///         <term><c>atom:link rel="service.feed" type="application/rss+xml" href="..."/></c></term>
    ///         <description>Specifies the URI where the complete RSS feed can be retrieved.</description>
    ///     </item>
    ///     <item>
    ///         <term><c>atom:link rel="service.post" type="application/atom+xml" href="..."/></c></term>
    ///         <description>Specifies the Atom feed PostURI (where new entries can be posted).</description>
    ///     </item>
    ///     <item>
    ///         <term><c>atom:link rel="self" type="..." href="..."/></c></term>
    ///         <description>Contains the URI of this search request. The type attribute depends on the requested format. If no data changes, issuing a <c>GET</c> to this URI returns the same response.</description>
    ///     </item>
    ///     <item>
    ///         <term><c>atom:link rel="previous" type="application/atom+xml" href="..."/></c></term>
    ///         <description>Specifies the URI of the previous chunk of this query resultset, if it is chunked.</description>
    ///     </item>
    ///     <item>
    ///         <term><c>atom:link rel="next" type="application/atom+xml" href="..."/></c></term>
    ///         <description>Specifies the URI of the next chunk of this query resultset, if it is chunked.</description>
    ///     </item>
    ///     <item>
    ///         <term><c>gdata:processed parameter="..."/></c></term>
    ///         <description>One of these tags is inserted for each parameter understood and processed by the service, e.g. <c>gdata:processed parameter="author"</c>.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </remarks> 
    //////////////////////////////////////////////////////////////////////
#if WindowsCE || PocketPC

#else 

    [TypeConverterAttribute(typeof(AtomSourceConverter)), DescriptionAttribute("Expand to see the options for the feed")]
#endif
    public class AtomFeed : AtomSource
    {


        /// <summary>collection of feed entries</summary> 
        private AtomEntryCollection feedEntries; 
        /// <summary>eventhandler, when the parser creates a new feed entry-> mirrored from underlying parser</summary> 
        public event FeedParserEventHandler NewAtomEntry;
        /// <summary>eventhandler, when the parser finds a new extension element-> mirrored from underlying parser</summary> 
        public event ExtensionElementEventHandler NewExtensionElement;

#region properties
        /// <summary>holds the total results</summary> 
        private int totalResults;
        /// <summary>holds the start-index parameter</summary> 
        private int startIndex;
        /// <summary>holds number of items per page</summary> 
        private int itemsPerPage;
        /// <summary>holds the service interface to use</summary> 
        private IService service;

        private GDataBatchFeedData batchData;
#endregion



        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        private AtomFeed() : base()
        {
            Tracing.Assert(false, "privately Constructing AtomFeed - should not happen");
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        /// <param name="uriBase">the location the feed was loaded from</param>        
        /// <param name="service">the service used to create this feed</param>        
        //////////////////////////////////////////////////////////////////////
        public AtomFeed(Uri uriBase, IService service) : base()
        {
            Tracing.TraceCall("Constructing AtomFeed");
            if (uriBase != null)
            {
                this.ImpliedBase = new AtomUri(uriBase.AbsoluteUri);
            }
            this.Service = service;
            NewExtensionElement += new ExtensionElementEventHandler(this.OnNewExtensionsElement);
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        /// <param name="originalFeed">if you want to create a copy feed, for batch use, e.g</param>        
        //////////////////////////////////////////////////////////////////////
        public AtomFeed(AtomFeed originalFeed) : base()
        {
            if (originalFeed == null)
            {
                throw new ArgumentNullException("originalFeed");
            }
  
            Tracing.TraceCall("Constructing AtomFeed");
            this.Batch = originalFeed.Batch;
            this.Post  = originalFeed.Post;
            this.Self  = originalFeed.Self; 
            this.Feed  = originalFeed.Feed;

            this.Service=originalFeed.Service; 
            this.ImpliedBase=originalFeed.ImpliedBase;
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>tries to determine if the two feeds derive from the same source</summary> 
        /// <param name="feedOne">the first feed</param>
        /// <param name="feedTwo">the second feed</param>
        /// <returns>true if believed to be the same source</returns>
        //////////////////////////////////////////////////////////////////////
        public static bool IsFeedIdentical(AtomFeed feedOne, AtomFeed feedTwo)
        {

            if (feedOne == feedTwo)
            {
                Tracing.TraceMsg("TRUE : testing for identical feeds, feedpointers equal: "); 
                return true;
            }
            if (feedOne == null || feedTwo == null)
            {
                Tracing.TraceMsg("FALSE : testing for identical feeds, one feed is NULL: "); 
                return false;
            }

            if (String.Compare(feedOne.Post, feedTwo.Post)!=0)
            {
                Tracing.TraceMsg("FALSE : testing for identical feeds: " + feedOne.Post + " vs. : "+ feedTwo.Post); 
                return false;
            }

            if (String.Compare(feedOne.Feed, feedTwo.Feed)!=0)
            {
                Tracing.TraceMsg("FALSE : testing for identical feeds: " + feedOne.Feed + " vs. : "+ feedTwo.Feed); 
                return false;
            }
            Tracing.TraceMsg("TRUE : testing for identical feeds: " + feedOne.Post + " vs. : "+ feedTwo.Post); 
            return true;
        }
        /////////////////////////////////////////////////////////////////////////////





#region Property Accessors

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Post</summary> 
        /// <returns>the Uri as string to the Post Service</returns>
        //////////////////////////////////////////////////////////////////////
        public string Post
        {
            get 
            {
                // scan the link collection
                AtomLink link = this.Links.FindService(BaseNameTable.ServicePost, AtomLink.ATOM_TYPE);
                return link == null ? null : Utilities.CalculateUri(this.Base, this.ImpliedBase, link.HRef.ToString());
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServicePost, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.ATOM_TYPE, BaseNameTable.ServicePost);
                    this.Links.Add(link);
                }
                link.HRef = new AtomUri(value);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

  
        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor to the batchdata for the entry</summary> 
        /// <returns> GDataBatch object </returns>
        //////////////////////////////////////////////////////////////////////
        public GDataBatchFeedData BatchData
        {
            get {return this.batchData;}
            set {this.batchData = value;}
        }
        // end of accessor public GDataBatch BatchData


        //////////////////////////////////////////////////////////////////////
        /// <summary>Retrieves the batch link for the current feed.</summary> 
        /// <returns>The URI of the batch support for the feed. </returns>
        //////////////////////////////////////////////////////////////////////
        public string Batch
        {
            get 
            {
                // scan the link collection
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceBatch, AtomLink.ATOM_TYPE);
                return link == null ? null : Utilities.CalculateUri(this.Base, this.ImpliedBase, link.HRef.ToString());
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceBatch, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.ATOM_TYPE, BaseNameTable.ServiceBatch);
                    this.Links.Add(link);
                }
                link.HRef = new AtomUri(value);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// returns a new batchfeed with all the currently dirty entries in it
        /// </summary>
        /// <param name="defaultOperation">the default operation to execute</param>
        /// <returns>AtomFeed</returns>
        public AtomFeed CreateBatchFeed(GDataBatchOperationType defaultOperation)
        {
            AtomFeed batchFeed = null;
            
            if (this.Batch != null)
            {

                batchFeed = new AtomFeed(this);
                // set the default operation. 
                batchFeed.BatchData = new GDataBatchFeedData();
                batchFeed.BatchData.Type = defaultOperation;
    
                int id = 1; 
                foreach (AtomEntry entry in this.Entries)
                {
                    if (entry.Dirty)
                    {
                        AtomEntry batchEntry = batchFeed.Entries.CopyOrMove(entry); 
                        batchEntry.BatchData = new GDataBatchEntryData();
                        batchEntry.BatchData.Id = id.ToString(CultureInfo.InvariantCulture); 
                        id++;
                        entry.Dirty = false;
                    }
                }
    
            }
            return batchFeed;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>returns whether or not the entry is read-only </summary> 
        //////////////////////////////////////////////////////////////////////
        public bool ReadOnly
        {
            get {
                return this.Post == null ? true : false; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string NextChunk</summary> 
        /// <returns>the Uri as string to the next chunk of the result</returns>
        //////////////////////////////////////////////////////////////////////
        public string NextChunk
        {
            get 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceNext, AtomLink.ATOM_TYPE);
                // scan the link collection
                return link == null ? null : Utilities.CalculateUri(this.Base, this.ImpliedBase, link.HRef.ToString());
            }
            set 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceNext, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.ATOM_TYPE, BaseNameTable.ServiceNext);
                    this.Links.Add(link);
                }
                link.HRef = new AtomUri(value);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string PrevChunk</summary> 
        /// <returns>the Uri as a string to the previous chunk of the result</returns>
        //////////////////////////////////////////////////////////////////////
        public string PrevChunk
        {
            get 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServicePrev, AtomLink.ATOM_TYPE);
                // scan the link collection
                return link == null ? null : Utilities.CalculateUri(this.Base, this.ImpliedBase, link.HRef.ToString());
            }
            set 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServicePrev, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.ATOM_TYPE, BaseNameTable.ServicePrev);
                    this.Links.Add(link);
                }
                link.HRef = new AtomUri(value);
            }

        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Feed</summary> 
        /// <returns>returns the Uri as string for the feed service </returns>
        //////////////////////////////////////////////////////////////////////
        public string Feed
        {
            get 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceFeed, AtomLink.ATOM_TYPE);
                // scan the link collection
                return link == null ? null : Utilities.CalculateUri(this.Base, this.ImpliedBase, link.HRef.ToString());
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceFeed, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.ATOM_TYPE, BaseNameTable.ServiceFeed);
                    this.Links.Add(link);
                }
                link.HRef = new AtomUri(value);
            }
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Self</summary> 
        /// <returns>returns the Uri as string for the feed with the Query Parameters </returns>
        //////////////////////////////////////////////////////////////////////

        public string Self {

            get {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceSelf, AtomLink.ATOM_TYPE);
                // scan the link collection
                return link == null ? null : Utilities.CalculateUri(this.Base, this.ImpliedBase, link.HRef.ToString());

            }

            set {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceSelf, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.ATOM_TYPE, BaseNameTable.ServiceSelf);
                    this.Links.Add(link);
                }
                link.HRef = new AtomUri(value);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method for the gData Service to use</summary> 
        //////////////////////////////////////////////////////////////////////
        public IService Service
        {
            get {return this.service;}
            set {this.Dirty = true;  this.service = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public int TotalResults</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int TotalResults
        {
            get {return this.totalResults;}
            set {this.Dirty = true;  this.totalResults = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public int StartIndex</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int StartIndex
        {
            get {return this.startIndex;}
            set {this.Dirty = true;  this.startIndex = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public int ItemsPerPage</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int ItemsPerPage
        {
            get {return this.itemsPerPage;}
            set {this.Dirty = true;  this.itemsPerPage = value;}
        }
        /////////////////////////////////////////////////////////////////////////////




        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public ExtensionList Entries</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomEntryCollection Entries
        {
            get 
            {
                if (this.feedEntries == null)
                {
                    this.feedEntries = new AtomEntryCollection(this);
                }

                return this.feedEntries;
            }
        }
        /////////////////////////////////////////////////////////////////////////////

#endregion

#region Persistence overloads


        //////////////////////////////////////////////////////////////////////
        /// <summary>checks to see if we are a batch feed, if so, adds the batchNS</summary> 
        /// <param name="writer">the xmlwriter, where we want to add default namespaces to</param>
        //////////////////////////////////////////////////////////////////////
        protected override void AddOtherNamespaces(XmlWriter writer) 
        {
            base.AddOtherNamespaces(writer); 
            if (this.BatchData != null)
            {
                Utilities.EnsureGDataBatchNamespace(writer); 
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>checks if this is a namespace 
        /// decl that we already added</summary> 
        /// <param name="node">XmlNode to check</param>
        /// <returns>true if this node should be skipped </returns>
        //////////////////////////////////////////////////////////////////////
        protected override bool SkipNode(XmlNode node)
        {
            if (base.SkipNode(node))
            {
                return true; 
            }

            Tracing.TraceMsg("in skipnode for node: " + node.Name + "--" + node.Value); 
            if (this.BatchData != null)
            {
                if (node.NodeType == XmlNodeType.Attribute && 
                    node.Name.StartsWith("xmlns") && 
                    (String.Compare(node.Value,BaseNameTable.gBatchNamespace)==0))
                    return true;

            }
            return false; 
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>just returns the constant representing this xml element</summary> 
        //////////////////////////////////////////////////////////////////////
        public override string XmlName 
        {
            get { return AtomParserNameTable.XmlFeedElement;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>saves the inner state of the element</summary> 
        /// <param name="writer">the xmlWriter to save into </param>
        //////////////////////////////////////////////////////////////////////
        protected override void SaveInnerXml(XmlWriter writer)
        {
            // first let the source save it self
            base.SaveInnerXml(writer);
            // now we need to save the entries

            if (this.batchData != null)
            {
                this.batchData.Save(writer);
            }

            foreach (AtomEntry entry in this.Entries)
            {
                entry.SaveToXml(writer);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

#endregion




        //////////////////////////////////////////////////////////////////////
        /// <summary>given a stream, parses it to construct the Feed object out of it</summary> 
        /// <param name="stream"> a stream representing hopefully valid XML</param>
        /// <param name="format"> indicates if the stream is Atom or Rss</param>
        //////////////////////////////////////////////////////////////////////
        public void Parse(Stream stream, AlternativeFormat format)
        {
            Tracing.TraceCall("parsing stream -> Start:" + format.ToString());
            BaseFeedParser feedParser= null; 

            // make sure we reset our collections
            this.Authors.Clear();
            this.Contributors.Clear();
            this.Links.Clear();
            this.Categories.Clear();
            
            feedParser = new AtomFeedParser(this);

            // create a new delegate for the parser
            feedParser.NewAtomEntry += new FeedParserEventHandler(this.OnParsedNewEntry); 
            feedParser.NewExtensionElement += new ExtensionElementEventHandler(this.OnNewExtensionElement);
            feedParser.Parse(stream, this);

            Tracing.TraceInfo("Parsing stream -> Done"); 
            // done parsing
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>Event chaining. We catch this by the baseFeedParsers, which 
        /// would not do anything with the gathered data. We pass the event up
        /// to the user; if the user doesn't discard it, we add the entry to our
        /// collection</summary> 
        /// <param name="sender"> the object which send the event</param>
        /// <param name="e">FeedParserEventArguments, holds the feed entry</param> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        protected void OnParsedNewEntry(object sender, FeedParserEventArgs e)
        {
            // by default, if our event chain is not hooked, add it to the collection
            Tracing.TraceCall("received new item notification");
            Tracing.Assert(e != null, "e should not be null");
            if (e == null)
            {
                throw new ArgumentNullException("e"); 
            }
            if (this.NewAtomEntry != null)
            {
                Tracing.TraceMsg("\t calling event dispatcher"); 
                this.NewAtomEntry(this, e);
            }
            // now check the return
            if (!e.DiscardEntry)
            {
                if (!e.CreatingEntry)
                {
                    if (e.Entry != null)
                    {
                        // add it to the collection
                        Tracing.TraceMsg("\t new AtomEntry found, adding to collection");
                        e.Entry.Service = this.Service;
                        this.Entries.Add(e.Entry);
                    }
                    else if (e.Feed != null)
                    {
                        // parsed a feed, set ourselves to it...
                        Tracing.TraceMsg("\t Feed parsed found, parsing is done...");
                    }
                }
                else
                {
                    IVersionAware v = e.Entry as IVersionAware;
                    if (v != null)
                    {
                        v.ProtocolMajor = this.ProtocolMajor;
                        v.ProtocolMinor = this.ProtocolMinor;
                    }
                }
            }

            if (e.DoneParsing)
            {
                this.BaseUriChanged(this.ImpliedBase);
            }

        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>Event chaining. We catch this by the baseFeedParsers, which 
        /// would not do anything with the gathered data. We pass the event up
        /// to the user; if the user doesn't discard it, we add the entry to our
        /// collection</summary> 
        /// <param name="sender"> the object which send the event</param>
        /// <param name="e">FeedParserEventArguments, holds the feed entry</param> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        protected void OnNewExtensionElement(object sender, ExtensionElementEventArgs e)
        {
            // by default, if our event chain is not hooked, the underlying parser will add it
            Tracing.TraceCall("received new extension element notification");
            Tracing.Assert(e != null, "e should not be null");
            if (e == null)
            {
                throw new ArgumentNullException("e"); 
            }
            if (this.NewExtensionElement != null)
            {
                Tracing.TraceMsg("\t calling event dispatcher"); 
                this.NewExtensionElement(sender, e);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

#region overloaded for property changes, xml:base
        //////////////////////////////////////////////////////////////////////
        /// <summary>just go down the child collections</summary> 
        /// <param name="uriBase"> as currently calculated</param>
        //////////////////////////////////////////////////////////////////////
        internal override void BaseUriChanged(AtomUri uriBase)
        {
            base.BaseUriChanged(uriBase);

            // now walk over the entries and forward...
            uriBase = new AtomUri(Utilities.CalculateUri(this.Base, uriBase, null));

            foreach (AtomEntry entry in this.Entries )
            {
                entry.BaseUriChanged(uriBase);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

#endregion


#region Editing APIs

        //////////////////////////////////////////////////////////////////////
        /// <summary>uses the set service to insert a new entry. </summary> 
        /// <param name="newEntry">the atomEntry to insert into the feed</param>
        /// <returns>the entry as echoed back from the server. The entry is NOT added
        ///          to the feeds collection</returns>
        //////////////////////////////////////////////////////////////////////
        public TEntry Insert<TEntry>(TEntry newEntry) where TEntry : AtomEntry
        {
            Tracing.Assert(newEntry != null, "newEntry should not be null");
            if (newEntry == null)
            {
                throw new ArgumentNullException("newEntry");
            }
            AtomEntry echoedEntry = null;
            if (newEntry.Feed == this)
            {
                // same object, already in here. 
                throw new ArgumentException("The entry is already part of this colleciton");
            }

            // now we need to see if this is the same feed. If not, copy
            if (newEntry.Feed == null)
            {
                newEntry.setFeed(this);
            }
            else if (!AtomFeed.IsFeedIdentical(newEntry.Feed, this))
            {
                newEntry = AtomEntry.ImportFromFeed(newEntry) as TEntry;
                newEntry.setFeed(this);
            }

            if (this.Service != null)
            {
                echoedEntry = Service.Insert(this, newEntry);
            }
            return echoedEntry as TEntry;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>goes over all entries, and updates the ones that are dirty</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public virtual void Publish()
        {
            if (this.Service != null)
            {
                for (int i=0; i<this.Entries.Count;i++)
                {
                    AtomEntry entry = this.Entries[i];
                    if (entry.IsDirty())
                    {
                        if (entry.Id.Uri == null)
                        {
                            // new guy
                            Tracing.TraceInfo("adding new entry: " + entry.Title.Text);
                            this.Entries[i] = Service.Insert(this, entry); 
                        }
                        else
                        {
                            // update the entry
                            Tracing.TraceInfo("updating entry: " + entry.Title.Text);
                            entry.Update(); 
                        }
                    }
                }
            }
            this.MarkElementDirty(false);
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>calls the action on this object and all children</summary> 
        /// <param name="action">an IAtomBaseAction interface to call </param>
        /// <returns>true or false, pending outcome</returns>
        //////////////////////////////////////////////////////////////////////
        public override bool WalkTree(IBaseWalkerAction action)
        {
            if (base.WalkTree(action)==true)
            {
                return true;
            }
            foreach (AtomEntry entry in this.Entries )
            {
                if (entry.WalkTree(action)==true)
                    return true;
            }
            return false; 
        }
#endregion


        /// <summary>eventhandler - called for event extension element
        /// </summary>
        /// <param name="sender">the object which send the event</param>
        /// <param name="e">FeedParserEventArguments, holds the feedEntry</param> 
        /// <returns> </returns>
        protected void OnNewExtensionsElement(object sender, ExtensionElementEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            AtomFeedParser parser = sender as AtomFeedParser;

            if (e.Base.XmlName == AtomParserNameTable.XmlAtomEntryElement)
            {
                 // the base is the Entry of the feed, let's call our parsing on the Entry
                AtomEntry entry = e.Base as AtomEntry;
                if (entry != null)
                {
                    entry.Parse(e, parser);
                }
            }
            else 
            {    
                HandleExtensionElements(e, parser);
            }
        }

        /// <summary>
        /// event on the Feed to handle extension elements during parsing
        /// </summary>
        /// <param name="e">the event arguments</param>
        /// <param name="parser">the parser that caused this</param>
        protected virtual void HandleExtensionElements(ExtensionElementEventArgs e, AtomFeedParser parser) 
        {
            Tracing.TraceMsg("Entering HandleExtensionElements on AbstractFeed");
            XmlNode node = e.ExtensionElement;
            if (this.ExtensionFactories != null && this.ExtensionFactories.Count > 0)
            {
                Tracing.TraceMsg("Entring default Parsing for AbstractFeed");
                foreach (IExtensionElementFactory f in this.ExtensionFactories)
                {
                    Tracing.TraceMsg("Found extension Factories");
                    if (String.Compare(node.NamespaceURI, f.XmlNameSpace, true, CultureInfo.InvariantCulture) == 0)
                    {
                        if (String.Compare(node.LocalName, f.XmlName, true, CultureInfo.InvariantCulture) == 0)
                        {
                            e.Base.ExtensionElements.Add(f.CreateInstance(node, parser));
                            e.DiscardEntry = true;
                            break;
                        }
                    }
                }
            }
            return;

        }



    }
    /////////////////////////////////////////////////////////////////////////////
}
/////////////////////////////////////////////////////////////////////////////


