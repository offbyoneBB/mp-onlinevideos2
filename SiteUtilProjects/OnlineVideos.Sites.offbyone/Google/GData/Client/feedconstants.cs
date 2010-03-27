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

#endregion

//////////////////////////////////////////////////////////////////////
// <summary>explain the file</summary> 
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{
    /// <summary>a simple static collection of HTTP method strings </summary> 
    public static class HttpMethods
    {
        /// <summary>the delete method</summary> 
        public const string Delete = "DELETE";
        /// <summary>the post method</summary> 
        public const string Post = "POST"; 
        /// <summary>the put method</summary> 
        public const string Put = "PUT"; 
        /// <summary>the get method</summary> 
        public const string Get = "GET";
    }

    /// <summary>a simple static collection of HTTP form post strings </summary> 
    public static class HttpFormPost 
    {
        /// <summary>form encoding</summary> 
        public const string Encoding = "application/x-www-form-urlencoded";
        /// <summary>expected return form contenttype</summary> 
        public const string ReturnContentType = "text";
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>enum to describe the different formats that query might return
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public enum AlternativeFormat
    {
        /// <summary>returns an atom format</summary>
        Atom,                       
        /// <summary>returns RSS 2.0</summary>
        Rss,                        /// 
        /// <summary>returns the Open RSS 2.0s</summary>
        OpenSearchRss,                    
        /// <summary>parsing error</summary>
        Unknown                        
    }
    /////////////////////////////////////////////////////////////////////////////

    
}
/////////////////////////////////////////////////////////////////////////////
