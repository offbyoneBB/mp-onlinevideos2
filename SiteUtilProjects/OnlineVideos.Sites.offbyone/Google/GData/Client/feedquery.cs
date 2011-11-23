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
using System.Text; 
using System.Globalization;
using System.Diagnostics;

#endregion

namespace Google.GData.Client
{


    //////////////////////////////////////////////////////////////////////
    /// <summary>Enum to describe the different category boolean operations.
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public enum QueryCategoryOperator
    {
        /// <summary>A logical AND operation.</summary>
        AND,                       
        /// <summary>A logical OR operation.</summary>
        OR
    }
    /////////////////////////////////////////////////////////////////////////////




    //////////////////////////////////////////////////////////////////////
    /// <summary>Base class to hold an Atom category plus the boolean
    /// to create the query category.
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class QueryCategory 
    {
        /// <summary>AtomCategory holder.</summary> 
        private AtomCategory category;
        /// <summary>Boolean operator (can be OR or AND).</summary> 
        private QueryCategoryOperator categoryOperator; 
        /// <summary>Boolean negator (can be true or false).</summary> 
        private bool isExcluded; 

        
        //////////////////////////////////////////////////////////////////////
        /// <summary>Constructor, given a category.</summary>
        //////////////////////////////////////////////////////////////////////
        public QueryCategory(AtomCategory category)
        {
            this.category = category;
            this.categoryOperator = QueryCategoryOperator.AND; 
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>Constructor, given a category as a string from the URI.</summary>
        //////////////////////////////////////////////////////////////////////
        public QueryCategory(string strCategory, QueryCategoryOperator op)
        {
            Tracing.TraceMsg("Depersisting category from: " + strCategory); 
            this.categoryOperator = op; 
            strCategory = FeedQuery.CleanPart(strCategory); 

            // let's parse the string
            if (strCategory[0] == '-')
            {
                // negator
                this.isExcluded = true; 
                // remove him
                strCategory = strCategory.Substring(1, strCategory.Length-1); 
            }

            // let's extract the scheme if there is one...
            int iStart = strCategory.IndexOf('{') ; 
            int iEnd = strCategory.IndexOf('}') ; 
            AtomUri scheme = null; 
            if (iStart != -1 && iEnd != -1)
            {
                // 
                iEnd++;
                iStart++;
                scheme = new AtomUri(strCategory.Substring(iStart, iEnd- iStart-1)); 
                // the rest is then
                strCategory = strCategory.Substring(iEnd, strCategory.Length - iEnd); 

            }

            Tracing.TraceMsg("Category found: " + strCategory + " - scheme: " + scheme); 

            this.category = new AtomCategory(strCategory, scheme);
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor method public AtomCategory Category</summary> 
        /// <returns></returns>
        //////////////////////////////////////////////////////////////////////
        public AtomCategory Category
        {
            get {return this.category;}
            set {this.category = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor method public QueryCategoryOperator Operator</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public QueryCategoryOperator Operator
        {
            get {return this.categoryOperator;}
            set {this.categoryOperator = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor method public bool Excluded</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool Excluded
        {
            get {return this.isExcluded;}
            set {this.isExcluded = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>Base class to create a GData query URI. Provides public 
    /// properties that describe the different aspects of the URI
    /// as well as a composite URI.
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class FeedQuery : ISupportsEtag
    {
        #region member variables
        /// <summary>baseUri property holder</summary> 
        private string query;
        /// <summary>category part as string, comma seperated</summary> 
        private QueryCategoryCollection categories;
        /// <summary>author part as string</summary> 
        private string author;
        /// <summary>extra parameters as string</summary> 
        private string extraParameters;
        /// <summary>mininum date/time as DateTime</summary> 
        private DateTime datetimeMin;
        /// <summary>maximum date/time as DateTime</summary> 
        private DateTime datetimeMax;
        /// <summary>mininum date/time for the publicationdate as DateTime</summary> 
        private DateTime publishedMin;
        /// <summary>maximum date/time for the publicationdate as DateTime</summary> 
        private DateTime publishedMax;
        /// <summary>start-index as integer</summary> 
        private int startIndex;
        /// <summary>number of entries to retrieve as integer</summary> 
        private int numToRetrieve;
        /// <summary>alternative format as AlternativeFormat</summary> 
        private AlternativeFormat altFormat;

        private DateTime ifModifiedSince;

        private bool defaultSSL; 

        private bool useCategoryQueriesAsParameter;

        /// <summary>the oauth requestor id</summary>
        private string oauthRequestorId;

        /// <summary>the base URI</summary> 
        protected string baseUri;
        #endregion
        //////////////////////////////////////////////////////////////////////
        /// <summary>Default constructor.</summary> 
        //////////////////////////////////////////////////////////////////////
        public FeedQuery()
        {
            // set some defaults...
            this.FeedFormat = AlternativeFormat.Atom;
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>constructor taking a base URI constructor. Note, that
        /// if this form is used, the string will not be parsed. So a uri that
        /// would have additional query parameters will not have this reflected. 
        /// this version is primarily there to be used on mobile devices, as on 
        /// mobile we don't have URI parsing code available.
        /// </summary> 
        //////////////////////////////////////////////////////////////////////
        public FeedQuery(string baseUri)
        {
            // set some defaults...
            this.FeedFormat = AlternativeFormat.Atom;
            this.baseUri = baseUri; 
            this.UseSSL = this.baseUri.StartsWith("https://");
        }
        /////////////////////////////////////////////////////////////////////////////
        
        
        /// <summary>
        ///  this will simply return/set the baseUri without any parsing as a string
        ///  this is the same as using the constructor for most cases, it is here to allow the creation
        /// of template methods.
        /// </summary>
        /// <returns></returns>
        public string BaseAddress
        {
            get
            {
                return this.baseUri;
            }
            set
            {
                this.baseUri = value;
                this.UseSSL = this.baseUri.StartsWith("https://");
            }
        }


        /// <summary>
        /// helper method to setup a query object with some parameters 
        /// based on a requestsettings
        /// </summary>
        /// <param name="q"></param>
        /// <param name="settings"></param>
        internal static void PrepareQuery(FeedQuery q, RequestSettings settings)
        {
            if (settings.PageSize != -1)
            {
                q.NumberToRetrieve = settings.PageSize;
            }
            if (settings.OAuthUser != null)
            {
                q.OAuthRequestorId = settings.OAuthUser;
                if (settings.OAuthDomain != null)
                {
                    q.OAuthRequestorId += "@" + settings.OAuthDomain;
                }
            }
        }


     
        //////////////////////////////////////////////////////////////////////
        /// <summary>We do not hold on to the precalculated Uri.
        /// It's safer and cheaper to calculate this on the fly.
        /// Setting this loses the base Uri.
        /// Note that the result of this is effected by the UseSSL flag. 
        /// so if you created this with a NON ssl string, but the flag states you 
        /// want to use SSL, this will result in an HTTPS URI
        /// </summary> 
        /// <returns>returns the complete UriPart that is used to execute the query</returns>
        //////////////////////////////////////////////////////////////////////
        public Uri Uri
        {
            get {
                String computedBaseUri = GetBaseUri();
                String uriToUse = computedBaseUri == null ? String.Empty : computedBaseUri.Replace(this.UnusedProtocol, this.DefaultProtocol);
                return new Uri(CalculateQuery(uriToUse));
                }
            set 
                {
                ParseUri(value);
                }
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the base Uri for the feed.
        /// </summary>
        protected virtual string GetBaseUri() {
            return this.baseUri;
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public bool CategoryQueriesAsParameter</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool CategoryQueriesAsParameter
        {
            get {return this.useCategoryQueriesAsParameter;}
            set {this.useCategoryQueriesAsParameter = value;}
        }
        // end of accessor public bool CategoryQueriesAsParameter

        //////////////////////////////////////////////////////////////////////
        /// <summary>Passing in a complete URI, we strip all the
        /// GData query-related things and then treat the rest
        /// as the base URI. For this we create a service.</summary> 
        /// <param name="uri">a complete URI</param>
        /// <param name="service">the new GData service for this URI</param>
        /// <param name="query">the parsed query object for this URI</param>
        //////////////////////////////////////////////////////////////////////
        public static void Parse(Uri uri, out Service service, out FeedQuery query)
        {
            query = new FeedQuery();

            query.ParseUri(uri);

            service = new Service();
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// indicates if constructed feed URIs should use http or https
        /// - if you pass in a full URI, this one will get changed from http to https
        /// or the other way round. This is mostly relevant for hosted domains. 
        /// </summary>
        /// <returns></returns>
        [Obsolete("This is deprecated and replaced by UseSSL on the service and the requestsettings")]
        public bool UseSSL
        {
            get { return this.defaultSSL; }
            set { this.defaultSSL = value; }
        }

        private string DefaultProtocol
        {
            get 
            {
                return this.UseSSL ? "https://" : "http://"; 
            }
            
        }

        private string UnusedProtocol
        {
            get 
            {
                return this.UseSSL ? "http://" : "https://"; 
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor method public string Query.</summary> 
        /// <returns>returns the query string portion of the URI</returns>
        //////////////////////////////////////////////////////////////////////
        public string Query
        {
            get {return this.query;}
            set {this.query = value; }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>set's the OAuth Requestor Identifier. Only useful if 
        /// you are using the OAuthFactory as well. 
        /// </summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string OAuthRequestorId
        {
            get { return this.oauthRequestorId; }
            set { this.oauthRequestorId = value; }
        }
        /////////////////////////////////////////////////////////////////////////////        

        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor method public string Category.</summary> 
        /// <returns>the category filter</returns>
        //////////////////////////////////////////////////////////////////////
        public QueryCategoryCollection Categories
        {
            get 
            {
                if (this.categories == null)
                {
                    this.categories = new QueryCategoryCollection(); 
                }
                return this.categories;
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// supports the etag for a query. Setting this etag here will create an if-notmatch
        /// query with the etag given. 
        /// </summary>
        private string etag=null;
        /// <summary>
        /// the Etag value that should be used in the query. Setting this will create an if-match or if-not match
        /// header
        /// </summary>
        public string Etag
        {
            get
            {
                return this.etag;
            }
            set
            {
                this.etag = value;
            }
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>ExtraParameters holds a string that just get's added to the
        /// query string per se. The parameter should honor URL encoding, the library
        /// will not touch it's value, but just append it to the existing query. The 
        /// URL parameter characters will be inserted by the FeedQuery object.</summary> 
        /// <returns></returns>
        //////////////////////////////////////////////////////////////////////
        public string ExtraParameters
        {
            get {return this.extraParameters;}
            set {this.extraParameters = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor method public string Author.</summary> 
        /// <returns>the requested author</returns>
        //////////////////////////////////////////////////////////////////////
        public string Author
        {
            get { return this.author;}
            set { this.author = value; }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>set's the mininum daterange value for the updated element</summary> 
        /// <returns>the min (inclusive) date/time</returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime StartDate
        {
            get {return this.datetimeMin;}
            set {this.datetimeMin = value; }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>set's the maximum daterange value for the updated element</summary> 
        /// <returns>the max (exclusive) date/time</returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime EndDate
        {
            get {return this.datetimeMax;}
            set {this.datetimeMax = value; }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>set's the mininum daterange value for the publication element</summary> 
        /// <returns>the min (inclusive) date/time</returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime MinPublication
        {
            get {return this.publishedMin;}
            set {this.publishedMin = value; }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>set's the maximum daterange value for the publication element</summary> 
        /// <returns>the max (exclusive) date/time</returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime MaxPublication
        {
            get {return this.publishedMax;}
            set {this.publishedMax = value; }
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// set's the ModifiedSince date. If this is set to something different than
        /// DateTime.MinValue, and the FeedQuery object is used for a Service.Query
        /// call, this will cause an ifmodified Since header to be created.
        /// </summary>
        /// <returns></returns>
        public DateTime ModifiedSince
        {
            get
            {
                return this.ifModifiedSince;
            }
            set
            {
                this.ifModifiedSince = value;
            }
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor method public int StartIndex.</summary> 
        /// <returns>the start-index query parameter, a 1-based index
        /// indicating the first result to be retrieved.</returns>
        //////////////////////////////////////////////////////////////////////
        public virtual int StartIndex
        {
            get {return this.startIndex;}
            set {this.startIndex = value; }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor method public int NumberToRetrieve.</summary> 
        /// <returns>the number of entries to retrieve</returns>
        //////////////////////////////////////////////////////////////////////
        public virtual int NumberToRetrieve
        {
            get {return this.numToRetrieve;}
            set {this.numToRetrieve = value; }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Accessor method public AlternativeFormat FeedFormat.
        /// </summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AlternativeFormat FeedFormat
        {
            get {return this.altFormat;}
            set {this.altFormat = value; }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>protected void ParseUri</summary> 
        /// <param name="targetUri">takes an incoming Uri string and parses all the properties out of it</param>
        /// <returns>throws a query exception when it finds something wrong with the input, otherwise returns a baseuri</returns>
        //////////////////////////////////////////////////////////////////////
        protected virtual Uri ParseUri(Uri targetUri)
        {
            Reset(); 
            StringBuilder newPath = null;
            UriBuilder newUri = null; 

            if (targetUri != null)
            {
                TokenCollection tokens; 
                // want to check some basic things on this guy first...
                ValidateUri(targetUri);
                newPath = new StringBuilder("", 2048);  
                newUri = new UriBuilder(targetUri);
                newUri.Path = null; 
                newUri.Query = null; 

                // now parse the query string and take the properties out
                string [] parts = targetUri.Segments;
                bool fCategory = false;

                foreach (string part in parts)
                {
                    string segment = CleanPart(part);
                    if (segment.Equals("-"))
                    {
                        // found the category simulator
                        fCategory = true; 
                    }
                    else if (fCategory)
                    {
                        ParseCategoryString(segment);
                    }
                    else
                    {
                        newPath.Append(part);
                    }
                }

                char [] deli = {'?','&'};

                string source = HttpUtility.UrlDecode(targetUri.Query);
                tokens = new TokenCollection(source, deli); 
                foreach (String token in tokens )
                {
                    if (token.Length > 0)
                    {
                        char [] otherDeli = {'='};
                        String [] parameters = token.Split(otherDeli,2); 
                        switch (parameters[0])
                        {
                            case "q":
                                this.Query = parameters[1];
                                break;
                            case "author":
                                this.Author = parameters[1];
                                break;
                            case "start-index":
                                this.StartIndex = int.Parse(parameters[1], CultureInfo.InvariantCulture);
                                break;
                            case "max-results":
                                this.NumberToRetrieve = int.Parse(parameters[1], CultureInfo.InvariantCulture);
                                break;
                            case "updated-min":
                                this.StartDate = DateTime.Parse(parameters[1], CultureInfo.InvariantCulture);
                                break;
                            case "updated-max":
                                this.EndDate = DateTime.Parse(parameters[1], CultureInfo.InvariantCulture);
                                break;
                            case "published-min":
                                this.MinPublication = DateTime.Parse(parameters[1], CultureInfo.InvariantCulture);
                                break;
                            case "published-max":
                                this.MaxPublication = DateTime.Parse(parameters[1], CultureInfo.InvariantCulture);
                                break;
                            case "category":
                                ParseCategoryQueryString(parameters[1]);
                                break;
                            case "xoauth_requestor_id":
                            	this.OAuthRequestorId = parameters[1];
                            	break;
                            default:
                                break;
                        }
                    }
                }
            }

            if (newPath != null)
            {
                if (newPath[newPath.Length-1] == '/')
                    newPath.Length = newPath.Length -1 ;

                newUri.Path = newPath.ToString(); 
                this.baseUri = newUri.Uri.AbsoluteUri;
                this.UseSSL = this.baseUri.StartsWith("https://");

            }
            return null; 
        }
        /////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// this will take the complete parameter string and split it into parts
        /// </summary>
        /// <param name="categories"></param>
        /// <returns></returns>
        private void ParseCategoryQueryString(string categories)
        {
            // split the string in parts
            char [] deli = {','}; 
            TokenCollection tokens = new TokenCollection(categories, deli); 

            foreach (String token in tokens)
            {
                ParseCategoryString(token);
            }
        }

        /// <summary>
        /// this will take one category part and parse it
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private void ParseCategoryString(string category)
        {
            // take the string, and create some category objects out of it...

            // replace the curly braces and the or operator | so that we can tokenize better
            category = category.Replace("%7B", "{"); 
            category = category.Replace("%7D", "}");
            category = category.Replace("%7C", "|");
            category = Utilities.UrlDecodedValue(category);

            // let's see if it's the only one...
            TokenCollection tokens = new TokenCollection(category, new char[1] {'|'}); 
            QueryCategoryOperator op = QueryCategoryOperator.AND; 
            foreach (String token in tokens)
            {
                // each one is a category
                QueryCategory cat = new QueryCategory(token, op); 
                this.Categories.Add(cat);
                op = QueryCategoryOperator.OR; 
            }
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>protected void ParseUri</summary> 
        /// <param name="target">takes an incoming string and parses all the properties out of it</param>
        /// <returns>throws a query exception when it finds something wrong with the input, otherwise returns a baseuri</returns>
        //////////////////////////////////////////////////////////////////////
        protected Uri ParseUri(string target)
        {
            Reset(); 
            if (target != null)
            {
                return ParseUri(new Uri(target));
            }
            return null; 
            
        }
        /////////////////////////////////////////////////////////////////////////////
 
        //////////////////////////////////////////////////////////////////////
        /// <summary>Takes an incoming URI segment and removes leading/trailing slashes.</summary> 
        /// <param name="part">the URI segment to clean</param>
        /// <returns>the cleaned string</returns>
        //////////////////////////////////////////////////////////////////////
        static internal string CleanPart(string part)
        {
            Tracing.Assert(part != null, "part should not be null");
            if (part == null)
            {
                throw new ArgumentNullException("part"); 
            }
            
            string cleaned = part.Trim();
            if (cleaned.EndsWith("/"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length-1);
            }
            if (cleaned.StartsWith("/"))
            {
                cleaned = cleaned.Substring(1, cleaned.Length-1);
            }

            return cleaned;
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>Checks to see if the URI is valid to be used for an Atom query.</summary> 
        /// <returns>Throws a client exception if not</returns>
        //////////////////////////////////////////////////////////////////////
        static protected void ValidateUri(Uri uriToTest)
        {
            Tracing.Assert(uriToTest != null, "uriToTest should not be null");
            if (uriToTest == null)
            {
                throw new ArgumentNullException("uriToTest"); 
            }

            if (uriToTest.Scheme == Uri.UriSchemeFile || uriToTest.Scheme == Uri.UriSchemeHttp  || uriToTest.Scheme == Uri.UriSchemeHttps)
            {
                return;
            }
            throw new ClientQueryException("Only HTTP/HTTPS 1.1 protocol is currently supported");
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>Resets object state to default, as if newly created.
        /// </summary> 
        //////////////////////////////////////////////////////////////////////
        protected virtual void Reset()
        {
            this.query = this.author = this.oauthRequestorId = String.Empty; 
            this.categories = null; 
            this.datetimeMax = this.datetimeMin = Utilities.EmptyDate; 
            this.MinPublication = this.MaxPublication = Utilities.EmptyDate; 
            this.ifModifiedSince = DateTime.MinValue; 
            this.startIndex = this.numToRetrieve = 0; 
            this.altFormat = AlternativeFormat.Atom;
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Creates the complete URI query string based on all set properties.</summary> 
        /// <returns> string => the query part of the URI</returns>
        //////////////////////////////////////////////////////////////////////
        protected virtual string CalculateQuery(string basePath)
        {
            Tracing.TraceCall("creating target Uri");

            StringBuilder newPath = new StringBuilder(basePath, 2048);  
            char paramInsertion = InsertionParameter(basePath);

            paramInsertion = CreateCategoryString(newPath, paramInsertion);

            if (this.FeedFormat != AlternativeFormat.Atom)
            {
                newPath.Append(paramInsertion);
                newPath.AppendFormat(CultureInfo.InvariantCulture, "alt={0}", FormatToString(this.FeedFormat));
                paramInsertion = '&'; 
            }

            paramInsertion = AppendQueryPart(this.Query, "q", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.Author, "author", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.StartDate, "updated-min", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.EndDate, "updated-max", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.MinPublication, "published-min", paramInsertion, newPath);            
            paramInsertion = AppendQueryPart(this.MaxPublication, "published-max", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.StartIndex, 0,  "start-index", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.NumberToRetrieve, 0,  "max-results", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.OAuthRequestorId, "xoauth_requestor_id", paramInsertion, newPath);    

            if (Utilities.IsPersistable(this.ExtraParameters))
            {
                newPath.Append(paramInsertion);
                newPath.Append(ExtraParameters);
            }

            return newPath.ToString();
        }
        /////////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// checks if the passed in string contains a "?" and if so returns the &amp; as the insertion char
        /// </summary>
        /// <param name="basePath"></param>
        /// <returns></returns>
        protected char InsertionParameter(string basePath)
        {
            char r = '?'; 
            if (basePath.IndexOf('?') != -1)
            {
                r = '&';
            }
            return r; 
        }
       


        private char CreateCategoryString(StringBuilder builder, char connect)
        {
            bool firstTime = true;

            int iLen = builder.Length;

            string prePendString = this.CategoryQueriesAsParameter ? connect + "category=" : "/-/";
            string seperator =  this.CategoryQueriesAsParameter ? "," : "/";

            foreach (QueryCategory category in this.Categories )
            {
                string strCategory = Utilities.UriEncodeReserved(category.Category.UriString); 

                if (Utilities.IsPersistable(strCategory))
                {
                    if (firstTime)
                    {
                        builder.Append(prePendString); 
                    }
                    else
                    {
                        switch (category.Operator)
                        {
                            case QueryCategoryOperator.AND:
                                // we get another AND, so it's a new path
                                builder.Append(seperator);
                                break;
                            case QueryCategoryOperator.OR:
                                builder.Append("|");
                                break;
                        }
                    }
                    firstTime = false; 
                    if (category.Excluded)
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, "-{0}", strCategory);
                    }
                    else
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", strCategory);
                    }   
                }
                else
                {
                    throw new ClientQueryException("One of the categories could not be persisted to a string");
                }
            }

            if (builder.Length > iLen && this.CategoryQueriesAsParameter)
            {
                return '&';
            }
            return connect;

        }

        /// <summary>
        /// helper to format a string parameter into the query
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parameterName"></param>
        /// <param name="connect"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        protected static char AppendQueryPart(string value, string parameterName, char connect, StringBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");

            if (builder.ToString().IndexOf(parameterName + "=") == -1)
            {
                if (Utilities.IsPersistable(value))
                {
                    builder.Append(connect);
                    builder.AppendFormat(CultureInfo.InvariantCulture, parameterName+"={0}", Utilities.UriEncodeReserved(value)); 
                    connect = '&'; 
                }
            }
            return connect;
        }

        /// <summary>
        /// helper to format an integer parameter into the query
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defValue">default value</param>
        /// <param name="parameterName"></param>
        /// <param name="connect"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        protected static char AppendQueryPart(int value, int defValue, string parameterName, char connect, StringBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");

            if (builder.ToString().IndexOf(parameterName + "=") == -1)
            {
                if (value != defValue)
                {
                    builder.Append(connect);
                    builder.AppendFormat(CultureInfo.InvariantCulture, parameterName+"={0:d}", value); 
                    connect = '&'; 
                }
            }
            return connect;
        }

         /// <summary>
        /// helper to format an unsigned integer parameter into the query
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defValue">default value</param>
        /// <param name="parameterName"></param>
        /// <param name="connect"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        protected static char AppendQueryPart(uint value, uint defValue, string parameterName, char connect, StringBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");

            // this used to check for connect + parameterName, i do not recall why
            // using just the parametername should catch cases where the parameter to be 
            // appended is already in the URI as the first parameter
            if (builder.ToString().IndexOf(parameterName+"=") == -1)
            {
                if (value != defValue)
                {
                    builder.Append(connect);
                    builder.AppendFormat(CultureInfo.InvariantCulture, parameterName+"={0:d}", value); 
                    connect = '&'; 
                }
            }
            return connect;
        }

        /// <summary>
        /// helper to format a DateTime parameter into the query
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parameterName"></param>
        /// <param name="connect"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        protected static char AppendQueryPart(DateTime value, string parameterName, char connect, StringBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");

            if (builder.ToString().IndexOf(parameterName + "=") == -1)
            {
                if (Utilities.IsPersistable(value))
                {
                    return FeedQuery.AppendQueryPart(Utilities.LocalDateTimeInUTC(value), parameterName, connect, builder);
                }
            }
            return connect;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Converts an AlternativeFormat to a string for use in
        /// the query string.</summary> 
        /// <param name="format">the format that we want to be converted to string </param>
        /// <returns>string version of the format</returns>
        //////////////////////////////////////////////////////////////////////
        static protected string FormatToString(AlternativeFormat format)
        {
            switch (format)
            {
                case AlternativeFormat.Atom:
                    return "atom";
                case AlternativeFormat.Rss:
                    return "rss";
                case AlternativeFormat.OpenSearchRss:
                    return "osrss";
            }
            return null;
        }
        /////////////////////////////////////////////////////////////////////////////
    }
    /////////////////////////////////////////////////////////////////////////////
    
}
