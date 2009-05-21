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
// <summary>Contains RssFeedParser.</summary> 
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>RssFeedParser
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class RssFeedParser : BaseFeedParser
    {
        // our name table
        //////////////////////////////////////////////////////////////////////
        /// <summary>standard empty constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public RssFeedParser() : base()
        {
            // this.nameTable.InitRssNameTable();
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>starts the parsing process</summary> 
        /// <param name="streamInput">input stream to parse </param>
        /// <param name="feed">the feed object to construct</param>
        //////////////////////////////////////////////////////////////////////
        public override void Parse(Stream streamInput, AtomFeed feed)
        {
            

            
        }
        /////////////////////////////////////////////////////////////////////////////


    }
    /////////////////////////////////////////////////////////////////////////////

}
/////////////////////////////////////////////////////////////////////////////

