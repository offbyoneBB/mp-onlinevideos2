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

//////////////////////////////////////////////////////////////////////
// <summary>Contains AtomParserNameTable.</summary> 
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>AtomParserNameTable. An initialized nametable for faster XML processing
    /// parses:  4  Element Definitions
    ///         4.1  The "atom:feed" Element
    ///         4.1.1  The "version" Attribute
    ///         4.2  The "atom:head" Element
    ///         4.2.1  Usage of "atom:head" within "atom:entry"
    ///         4.3  The "atom:entry" Element
    ///         4.4  The "atom:title" Element
    ///         4.5  The "atom:id" Element
    ///         4.6  The "atom:link" Element
    ///         4.6.1  The "href" Attribute
    ///         4.6.2  The "rel" Attribute
    ///         4.6.3  The "type" Attribute
    ///         4.6.4  The "hreflang" Attribute
    ///         4.6.5  The "title" Attribute
    ///         4.6.6  The "length" Attribute
    ///         4.7  The "atom:updated" Element
    ///         4.8  The "atom:published" Element
    ///         4.9  The "atom:author" Element
    ///         4.10  The "atom:contributor" Element
    ///         4.12  The "atom:copyright" Element
    ///         4.13  The "atom:category" Element
    ///         4.13.1  The "term" Attribute
    ///         4.13.2  The "scheme" Attribute
    ///         4.13.3  The "label" attribute
    ///         4.14  The "atom:summary" Element
    ///         4.15  The "atom:content" Element
    ///         4.15.1  The "type" attribute
    ///         4.15.2  The "src" attribute
    ///         4.15.3  Processing Model
    ///         4.16  The "atom:introspection" Element
    ///         4.17  The "atom:post" Element
    ///         4.18  The "atom:edit" Element
    ///         4.20  The "atom:generator" Element
    ///         the atom:icon element
    ///         the atom:logo element
    ///  </summary>
    //////////////////////////////////////////////////////////////////////
    public class AtomParserNameTable : BaseNameTable
    {
        /// <summary>atom:feed</summary> 
        private object feed;
        /// <summary>atom:version</summary> 
        private object version;
        /// <summary>atom:title</summary> 
        private object title;
        /// <summary>atom:id</summary> 
        private object id;
        /// <summary>atom:link</summary> 
        private object link;
        /// <summary>link attributes: href, rel, type, hreflang, title (use the defined string), length</summary> 
        private object href;
        /// <summary>property holder exposed over get/set</summary> 
        private object rel;
        /// <summary>property holder exposed over get/set</summary> 
        private object hreflang;
        /// <summary>property holder exposed over get/set</summary> 
        private object length;

        /// <summary>atom:updated</summary> 
        private object updated;
        /// <summary>atom:published</summary> 
        private object published;
        /// <summary>atom:author</summary> 
        private object author;
        /// <summary>atom:contributor</summary> 
        private object contributor;
        /// <summary>atom:rights</summary> 
        private object rights;
        /// <summary>atom:category</summary> 
        private object category;
        /// <summary>attributes term, scheme, label</summary> 
        private object term;
        /// <summary>property holder exposed over get/set</summary> 
        private object scheme;
        /// <summary>property holder exposed over get/set</summary> 
        private object label;
        /// <summary>atom:summary</summary> 
        private object summary;
        /// <summary>atom:content</summary> 
        private object content;
        /// <summary>attributes type (use the defined string), src</summary> 
        private object src; 

        /// <summary>atom:subtitle</summary> 
        private object subTitle; 
        /// <summary>atom:generator</summary> 
        private object generator;
        /// <summary>atom:source</summary> 
        private object source;

        // <summary>generic atom entry elements/attributes - only additional ones
        //
        //         atomEntry =
        //       element atom:entry {
        //       atomCommonAttributes,
        //       atomVersionAttribute?,
        //       (atomTitle
        //        & atomId
        //        & atomLink*
        //        & atomUpdated
        //        & atomPublished?
        //        & atomAuthor?
        //        & atomContributor*
        //        & atomRights?
        //        & atomCategory*
        //        & atomEdit?
        //        & atomSummary?
        //        & atomContent?
        //        & atomHead?
        //        & anyElement*) }
        //    </summary>

        /// <summary>atom:entry</summary> 
        private object entry;

        // 3.2  AtomPerson Constructs
        // 3.2.1  The "atom:name" Element
        // 3.2.2  The "atom:uri" Element
        // 3.2.3  The "atom:email" Element

        /// <summary>property holder exposed over get/set</summary> 
        private object uri;
        /// <summary>property holder exposed over get/set</summary> 
        private object email;
    
        /// <summary>holds the icon </summary> 
        private object icon;
        /// <summary>holds the logo</summary> 
        private object logo;

        private object categories; 

        /// <summary>static string for parsing</summary> 
        public const string XmlCategoryElement = "category"; 
        /// <summary>static string for parsing</summary> 
        public const string XmlContentElement = "content";
        /// <summary>static string for parsing</summary> 
        public const string XmlAtomEntryElement = "entry";
        /// <summary>static string for parsing</summary> 
        public const string XmlGeneratorElement = "generator";
        /// <summary>static string for parsing</summary> 
        public const string XmlIconElement = "icon";
        /// <summary>static string for parsing</summary> 
        public const string XmlLogoElement = "logo";
        /// <summary>static string for parsing</summary> 
        public const string XmlIdElement = "id"; 
        /// <summary>static string for parsing</summary> 
        public const string XmlLinkElement = "link"; 
        /// <summary>static string for parsing</summary>    
        public const string XmlFeedElement = "feed";
        /// <summary>static string for parsing</summary>    
        public const string XmlAuthorElement = "author";
        /// <summary>static string for parsing</summary>    
        public const string XmlContributorElement = "contributor";
        /// <summary>static string for parsing</summary>    
        public const string XmlSourceElement = "source";

        /// <summary>static string for parsing</summary>    
        public const string XmlRightsElement = "rights";
        /// <summary>static string for parsing</summary>    
        public const string XmlSubtitleElement = "subtitle";
        /// <summary>static string for parsing</summary>    
        public const string XmlTitleElement = "title";
        /// <summary>static string for parsing</summary>    
        public const string XmlSummaryElement = "summary";
        /// <summary>static string for parsing</summary>    
        public const string XmlUpdatedElement = "updated";
        
        /// <summary>static string for parsing</summary>    
        public const string XmlEmailElement = "email";
        /// <summary>static string for parsing - same for attribute</summary>    
        public const string XmlUriElement = "uri";

        /// <summary>static string for parsing - same for attribute</summary>    
        public const string XmlPublishedElement = "published";

        // attribute strings

        /// <summary>static string for parsing</summary>    
        public const string XmlAttributeTerm = "term";
        /// <summary>static string for parsing</summary>    
        public const string XmlAttributeScheme = "scheme";
        /// <summary>static string for parsing</summary>    
        public const string XmlAttributeLabel = "label";
        /// <summary>static string for parsing</summary>    
        public const string XmlAttributeVersion = "version";
        /// <summary>static string for parsing</summary>    
        public const string XmlAttributeLength = "length";
        /// <summary>static string for parsing</summary>    
        public const string XmlAttributeRel = "rel";
        /// <summary>static string for parsing</summary>    
        public const string XmlAttributeHRefLang = "hreflang";
        /// <summary>static string for parsing</summary>    
        public const string XmlAttributeHRef = "href";

        /// <summary>static string for parsing</summary>    
        public const string XmlAttributeSrc = "src";


        /// <summary>static string for parsing the App:categories element</summary>    
        public const string XmlCategoriesElement = "categories"; 

        


        //////////////////////////////////////////////////////////////////////
        /// <summary>initializes the name table for use with atom parsing. This is the
        /// only place where strings are defined for parsing</summary> 
        //////////////////////////////////////////////////////////////////////
        public override void InitAtomParserNameTable()
        {
            // create the nametable object by calling the base
            base.InitAtomParserNameTable();

            Tracing.TraceCall("Initializing atom nametable support");
            // <summary>add the keywords for the Feed</summary>
            this.feed = this.Nametable.Add(AtomParserNameTable.XmlFeedElement);
            this.version = this.Nametable.Add(AtomParserNameTable.XmlAttributeVersion);
            this.source = this.Nametable.Add(AtomParserNameTable.XmlSourceElement);
            this.entry = this.Nametable.Add(AtomParserNameTable.XmlAtomEntryElement);
            this.title = this.Nametable.Add(AtomParserNameTable.XmlTitleElement);
            this.link = this.Nametable.Add(AtomParserNameTable.XmlLinkElement);
            this.id = this.Nametable.Add(AtomParserNameTable.XmlIdElement);
            this.href = this.Nametable.Add(AtomParserNameTable.XmlAttributeHRef);

            this.rel = this.Nametable.Add(AtomParserNameTable.XmlAttributeRel);
            this.hreflang = this.Nametable.Add(AtomParserNameTable.XmlAttributeHRefLang);
            this.length = this.Nametable.Add(AtomParserNameTable.XmlAttributeLength);
            this.updated = this.Nametable.Add(AtomParserNameTable.XmlUpdatedElement);
            this.published = this.Nametable.Add(AtomParserNameTable.XmlPublishedElement);
            this.author = this.Nametable.Add(AtomParserNameTable.XmlAuthorElement);
            this.contributor = this.Nametable.Add(AtomParserNameTable.XmlContributorElement);
            this.rights = this.Nametable.Add(AtomParserNameTable.XmlRightsElement);
            this.category = this.Nametable.Add(AtomParserNameTable.XmlCategoryElement);
            this.term = this.Nametable.Add(AtomParserNameTable.XmlAttributeTerm);
            this.scheme = this.Nametable.Add(AtomParserNameTable.XmlAttributeScheme);
            this.label = this.Nametable.Add(AtomParserNameTable.XmlAttributeLabel);
            this.summary = this.Nametable.Add(AtomParserNameTable.XmlSummaryElement);
            this.content = this.Nametable.Add(AtomParserNameTable.XmlContentElement);
            this.src = this.Nametable.Add(AtomParserNameTable.XmlAttributeSrc);

            this.uri = this.Nametable.Add(AtomParserNameTable.XmlUriElement);
            this.generator = this.Nametable.Add(AtomParserNameTable.XmlGeneratorElement);
            this.email = this.Nametable.Add(AtomParserNameTable.XmlEmailElement);
            this.icon = this.Nametable.Add(AtomParserNameTable.XmlIconElement);
            this.logo = this.Nametable.Add(AtomParserNameTable.XmlLogoElement);
            this.subTitle = this.Nametable.Add(AtomParserNameTable.XmlSubtitleElement);

            this.categories = this.Nametable.Add(AtomParserNameTable.XmlCategoriesElement);
        }
        /////////////////////////////////////////////////////////////////////////////

        #region Read only accessors 7/7/2005
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for feed</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Feed
        {
            get {return this.feed;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for Categories</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Categories
        {
            get {return this.categories;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for version</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Version
        {
            get {return this.version;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for source</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Source
        {
            get {return this.source;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for entry</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Entry
        {
            get {return this.entry;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for title</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Title
        {
            get {return this.title;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for link</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Link
        {
            get {return this.link;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for id</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Id
        {
            get {return this.id;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for href</summary> 
        //////////////////////////////////////////////////////////////////////
        public object HRef
        {
            get {return this.href;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for rel</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Rel
        {
            get {return this.rel;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for hreflang</summary> 
        //////////////////////////////////////////////////////////////////////
        public object HRefLang
        {
            get {return this.hreflang;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for length</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Length
        {
            get {return this.length;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for category</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Category
        {
            get {return this.category;}
        }
        /////////////////////////////////////////////////////////////////////////////
        
        
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for updated</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Updated
        {
            get {return this.updated;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for published</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Published
        {
            get {return this.published;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for author</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Author
        {
            get {return this.author;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for contributor</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Contributor
        {
            get {return this.contributor;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for rights</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Rights
        {
            get {return this.rights;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for term</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Term
        {
            get {return this.term;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for scheme</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Scheme
        {
            get {return this.scheme;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for label</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Label
        {
            get {return this.label;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for summary</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Summary
        {
            get {return this.summary;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for content</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Content
        {
            get {return this.content;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for src</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Src
        {
            get {return this.src;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for subtitle</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Subtitle
        {
            get {return this.subTitle;}
        }
        /////////////////////////////////////////////////////////////////////////////
        
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for uri</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Uri
        {
            get {return this.uri;}
        }
        /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for generator</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Generator
        {
            get {return this.generator;}
        }
        /////////////////////////////////////////////////////////////////////////////


          //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for email</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Email
        {
            get {return this.email;}
        }
        /////////////////////////////////////////////////////////////////////////////




        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for icon</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Icon
        {
            get {return this.icon;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for logo</summary> 
        //////////////////////////////////////////////////////////////////////
        public object Logo
        {
            get {return this.logo;}
        }
        /////////////////////////////////////////////////////////////////////////////
        
        
        

        #endregion end of Read only accessors

    }
    /////////////////////////////////////////////////////////////////////////////

}
/////////////////////////////////////////////////////////////////////////////

