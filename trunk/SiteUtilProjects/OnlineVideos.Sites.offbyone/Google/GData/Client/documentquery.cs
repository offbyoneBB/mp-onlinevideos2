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
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Google.GData.Client
{
    /// <summary>
    /// A subclass of FeedQuery, to create a document based query URI.
    /// Provides public properties that describe the different
    /// aspects of the URI, as well as a composite URI.
    /// </summary>
    public class DocumentQuery : FeedQuery
    {
        private string title;
        private bool exact;

        /// <summary>
        /// Constructor - Sets the base URI
        /// </summary>
        /// <param name="baseUri"></param>
        public DocumentQuery(string baseUri) : base(baseUri)
        {
        	Title = null;
        	Exact = false;
        }
        
        /// <summary>
        /// The exact or unexact title to query for.
        /// </summary>
        public string Title
        {
            get
            {
                return title;
            }

            set
            {
                title = value;
            }
        }

        /// <summary>
        /// If true, then only the exact title string will be looked for.
        /// </summary>
        public bool Exact
        {
            get
            {
                return exact;
            }

            set
            {
                exact = value;
            }
        }

        /// <summary>
        /// Parses an incoming URI string and sets the instance variables
        /// of this object.
        /// </summary>
        /// <param name="targetUri">Takes an incoming Uri string and parses all the properties of it</param>
        /// <returns>Throws a query exception when it finds something wrong with the input, otherwise returns a baseuri.</returns>
        protected override Uri ParseUri(Uri targetUri)
        {
            base.ParseUri(targetUri);
            if (targetUri != null)
            {
                char[] delimiters = { '?', '&' };

                string source = HttpUtility.UrlDecode(targetUri.Query);
                TokenCollection tokens = new TokenCollection(source, delimiters);
                foreach (String token in tokens)
                {
                    if (token.Length > 0)
                    {
                        char[] otherDelimiters = { '=' };
                        String[] parameters = token.Split(otherDelimiters, 2);
                        switch (parameters[0])
                        {
                            case "title":
                                Title = parameters[1];
                                Exact = false;
                                break;
                            case "title-exact":
                                Title = parameters[1];
                                Exact = true;
                                break;
                        }
                    }
                }
            }
            return this.Uri;
        }
        
        /// <summary>
        /// Resets object state to default, as if newly created.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();
            Title = null;
            Exact = false;
        }

        /// <summary>
        /// Creates the partial URI query string based on all set properties.
        /// </summary>
        /// <returns> string => the query part of the URI </returns>
        protected override string CalculateQuery(string basePath)
        {
            string path = base.CalculateQuery(basePath);
            StringBuilder newPath = new StringBuilder(path, 2048);
            char paramInsertion = InsertionParameter(path); 

            if (Title != null)
            {
                newPath.Append(paramInsertion);
                newPath.AppendFormat(CultureInfo.InvariantCulture, "title={0}", Utilities.UriEncodeReserved(Title));
                paramInsertion = '&';
                if (Exact)
                {
                    newPath.Append(paramInsertion);
                    newPath.AppendFormat(CultureInfo.InvariantCulture, "title-exact=true");
                }
            }
            return newPath.ToString();
        }
    }
} 
