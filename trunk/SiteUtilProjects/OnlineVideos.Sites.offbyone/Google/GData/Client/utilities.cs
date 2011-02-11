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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Reflection;
using Google.GData.Extensions;

#endregion

namespace Google.GData.Client
{
    //////////////////////////////////////////////////////////////////////
    /// <summary>String utilities
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public sealed class Utilities
    {

        /// <summary>
        /// xsd version of bool:true
        /// </summary>
        public const string XSDTrue = "true";
        /// <summary>
        /// xsd version of bool:false
        /// </summary>
        public const string XSDFalse = "false";
        /// <summary>
        /// default user string
        /// </summary>
        public const string DefaultUser = "default";


        //////////////////////////////////////////////////////////////////////
        /// <summary>private constructor to prevent the compiler from generating a default one</summary> 
        //////////////////////////////////////////////////////////////////////
        private Utilities()
        {
        }
        /////////////////////////////////////////////////////////////////////////////
        /// <summary>Little helper that checks if a string is XML persistable</summary> 
        public static bool IsPersistable(string toPersist)
        {
            if (!string.IsNullOrEmpty(toPersist) && toPersist.Trim().Length != 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>Little helper that checks if a string is XML persistable</summary> 
        public static bool IsPersistable(AtomUri uriString)
        {
            return uriString == null ? false : Utilities.IsPersistable(uriString.ToString());
        }

        /// <summary>Little helper that checks if an int is XML persistable</summary> 
        public static bool IsPersistable(int number)
        {
            return number == 0 ? false : true;
        }

        /// <summary>Little helper that checks if a datevalue is XML persistable</summary> 
        public static bool IsPersistable(DateTime testDate)
        {
            return testDate == Utilities.EmptyDate ? false : true;
        }

        /// <summary>
        /// .NET treats bool as True/False as the default
        /// string representation. XSD requires true/false
        /// this method encapsulates this
        /// </summary>
        /// <param name="flag">the boolean to convert</param>
        /// <returns>"true" or "false"</returns>
        public static string ConvertBooleanToXSDString(bool flag)
        {
            return flag ? Utilities.XSDTrue : Utilities.XSDFalse;
        }


        /// <summary>
        /// .NET treats bool as True/False as the default
        /// string representation. XSD requires true/false
        /// this method encapsulates this
        /// </summary>
        /// <param name="obj">the object to convert</param>
        /// <returns>the string representation</returns>
        public static string ConvertToXSDString(Object obj)
        {
            if (obj is bool)
            {
                return ConvertBooleanToXSDString((bool) obj);
            }
            return Convert.ToString(obj, CultureInfo.InvariantCulture);
        }





        //////////////////////////////////////////////////////////////////////
        /// <summary>helper to read in a string and Encode it</summary> 
        /// <param name="content">the xmlreader string</param>
        /// <returns>UTF8 encoded string</returns>
        //////////////////////////////////////////////////////////////////////
        public static string EncodeString(string content)
        {
            // get the encoding
            Encoding utf8Encoder = Encoding.UTF8; 

            Byte[] utf8Bytes = EncodeStringToUtf8(content);

            char[] utf8Chars = new char[utf8Encoder.GetCharCount(utf8Bytes, 0, utf8Bytes.Length)];
            utf8Encoder.GetChars(utf8Bytes, 0, utf8Bytes.Length, utf8Chars, 0);
      
            String utf8String = new String(utf8Chars); 

            return utf8String; 
        }

        /// <summary>
        /// returns you a bytearray of UTF8 bytes from the string passed in
        /// the passed in string is assumed to be UTF16
        /// </summary>
        /// <param name="content">UTF16 string</param>
        /// <returns>utf 8 byte array</returns>
        public static Byte[] EncodeStringToUtf8(string content)
        {
            // get the encoding
            Encoding utf8Encoder = Encoding.UTF8;
            Encoding utf16Encoder = Encoding.Unicode;

            Byte[] bytes = utf16Encoder.GetBytes(content);

            Byte[] utf8Bytes = Encoding.Convert(utf16Encoder, utf8Encoder, bytes);
            return utf8Bytes;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>helper to read in a string and Encode it according to 
        /// RFC 5023 rules for slugheaders</summary> 
        /// <param name="slug">the Unicode string for the slug header</param>
        /// <returns>ASCII  encoded string</returns>
        //////////////////////////////////////////////////////////////////////
        public static string EncodeSlugHeader(string slug)
        {
            if (slug == null)
                return "";

            Byte[] bytes =EncodeStringToUtf8(slug);

            if (bytes == null)
                return "";

            StringBuilder returnString = new StringBuilder(256);

            foreach (byte b in bytes)
            {
                if ((b < 0x20) ||
                    (b == 0x25) ||
                    (b > 0x7E))
                {
                    returnString.AppendFormat(CultureInfo.InvariantCulture, "%{0:X}", b);
                }
                else
                {
                    returnString.Append((char) b);
                }
            }

            return returnString.ToString(); 
        }

        

        /// <summary>
        /// used as a cover method to hide the actual decoding implementation
        /// decodes an html decoded string
        /// </summary>
        /// <param name="value">the string to decode</param>
        public static string DecodedValue(string value) 
        {
            return HttpUtility.HtmlDecode(value);
        }

        /// <summary>
        /// used as a cover method to hide the actual decoding implementation
        /// decodes an URL decoded string
        /// </summary>
        /// <param name="value">the string to decode</param>
        public static string UrlDecodedValue(string value)
        {
            return HttpUtility.UrlDecode(value);
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>helper to read in a string and replace the reserved URI 
        /// characters with hex encoding</summary> 
        /// <param name="content">the parameter string</param>
        /// <returns>hex encoded string</returns>
        //////////////////////////////////////////////////////////////////////
        public static string UriEncodeReserved(string content) 
        {
            if (content == null)
                return null;

            StringBuilder returnString = new StringBuilder(256);

            foreach (char ch in content) 
            {
                if (ch == ';' || 
                    ch == '/' ||
                    ch == '?' ||
                    ch == ':' ||
                    ch == '@' ||
                    ch == '&' ||
                    ch == '=' ||
                    ch == '+' ||
                    ch == '$' ||
                    ch == ','  ||
                    ch == '%' )
                {
                    returnString.Append(Uri.HexEscape(ch));
                }
                else 
                {
                    returnString.Append(ch);
                }
            }

            return returnString.ToString(); 
            
        }

        /// <summary>
        ///  tests an etag for weakness. returns TRUE for weak etags and for null strings
        /// </summary>
        /// <param name="eTag"></param>
        /// <returns></returns>
        public static bool IsWeakETag(string eTag)
        {
            if (eTag == null)
            {
                return true;
            }
			return eTag.StartsWith("W/");
        }

        /// <summary>
        ///  tests an etag for weakness. returns TRUE for weak etags and for null strings
        /// </summary>
        /// <param name="ise">the element that supports an etag</param>
        /// <returns></returns>
        public static bool IsWeakETag(ISupportsEtag ise)
        {
            string eTag = null; 

            if (ise != null)
            {
                eTag = ise.Etag;
            }
            return IsWeakETag(eTag);
        }


         //////////////////////////////////////////////////////////////////////
        /// <summary>helper to read in a string and replace the reserved URI 
        /// characters with hex encoding</summary> 
        /// <param name="content">the parameter string</param>
        /// <returns>hex encoded string</returns>
        //////////////////////////////////////////////////////////////////////
        public static string UriEncodeUnsafe(string content) 
        {
            if (content == null)
                return null;

            StringBuilder returnString = new StringBuilder(256);

            foreach (char ch in content) 
            {
                if (ch == ';' || 
                    ch == '/' ||
                    ch == '?' ||
                    ch == ':' ||
                    ch == '@' ||
                    ch == '&' ||
                    ch == '=' ||
                    ch == '+' ||
                    ch == '$' ||
                    ch == ',' || 
                    ch == ' ' || 
                    ch == '\'' || 
                    ch == '"' || 
                    ch == '>' || 
                    ch == '<' || 
                    ch == '#' || 
                    ch == '%' ) 
                {
                    returnString.Append(Uri.HexEscape(ch));
                }
                else 
                {
                    returnString.Append(ch);
                }
            }
            return returnString.ToString(); 
        }

       

        //////////////////////////////////////////////////////////////////////
        /// <summary>Method to output just the date portion as string</summary>
        /// <param name="dateTime">the DateTime object to output as a string</param>
        /// <returns>an rfc-3339 string</returns>
        //////////////////////////////////////////////////////////////////////
        public static string LocalDateInUTC(DateTime dateTime)
        {
            // Add "full-date T partial-time"
            return dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Method to output DateTime as string</summary>
        /// <param name="dateTime">the DateTime object to output as a string</param>
        /// <returns>an rfc-3339 string</returns>
        //////////////////////////////////////////////////////////////////////
        public static string LocalDateTimeInUTC(DateTime dateTime)
        {
            TimeSpan diffFromUtc = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);

            // Add "full-date T partial-time"
            string strOutput = dateTime.ToString("s", CultureInfo.InvariantCulture);

            // Add "time-offset"
            return strOutput + FormatTimeOffset(diffFromUtc);
        }



        /// <summary>
        /// returns the next child element of the xml reader, based on the
        /// depth passed in.
        /// </summary>
        /// <param name="reader">the xml reader to use</param>
        /// <param name="depth">the depth to start with</param>
        /// <returns></returns>
        public static bool NextChildElement(XmlReader reader, ref int depth)
        {
            Tracing.Assert(reader != null, "reader should not be null");
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (reader.Depth == depth)
            {
                // assume we gone around circle, a child read and moved to the next element of the same KIND
                return false;
            }

            if (reader.NodeType == XmlNodeType.Element && depth >= 0 && reader.Depth > depth)
            {
                // assume we gone around circle, a child read and moved to the next element of the same KIND
                // but now we are in the parent/containing element, hence we return TRUE without reading further
                return true;
            }

            if (depth == -1)
            {
                depth = reader.Depth;
            }


            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                {
                    return false;
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Depth > depth)
                {
                    return true;
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth)
                {
                    // assume that we had no children. We read once and we are at the 
                    // next element, same level as the previous one.
                    return false;
                }
            }
            return !reader.EOF;
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Helper method to format a TimeSpan as a string compliant with the "time-offset" format defined in RFC-3339</summary>
        /// <param name="spanFromUtc">the TimeSpan to format</param>
        /// <returns></returns>
        //////////////////////////////////////////////////////////////////////
        public static string FormatTimeOffset(TimeSpan spanFromUtc)
        {
            // Simply return "Z" if there is no offset
            if (spanFromUtc == TimeSpan.Zero)
                return "Z";

            // Return the numeric offset
            TimeSpan absoluteSpan = spanFromUtc.Duration();
            if (spanFromUtc > TimeSpan.Zero)
            {
                return "+" + FormatNumOffset(absoluteSpan);
            }
            else
            {
                return "-" + FormatNumOffset(absoluteSpan);
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Helper method to format a TimeSpan to {HH}:{MM}</summary>
        /// <param name="timeSpan">the TimeSpan to format</param>
        /// <returns>a string in "hh:mm" format.</returns>
        //////////////////////////////////////////////////////////////////////
        internal static string FormatNumOffset(TimeSpan timeSpan)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", timeSpan.Hours, timeSpan.Minutes);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>public static string CalculateUri(string base, string inheritedBase, string local)</summary> 
        /// <param name="localBase">the baseUri from xml:base </param>
        /// <param name="inheritedBase">the pushed down baseUri from an outer element</param>
        /// <param name="localUri">the Uri value</param>
        /// <returns>the absolute Uri to use... </returns>
        //////////////////////////////////////////////////////////////////////
        internal static string CalculateUri(AtomUri localBase, AtomUri inheritedBase, string localUri)
        {
            try 
            {
                Uri uriBase = null;
                Uri uriSuperBase= null;
                Uri uriComplete = null;
    
                if (inheritedBase != null && inheritedBase.ToString() != null)
                {
                    uriSuperBase = new Uri(inheritedBase.ToString()); 
                }
                if (localBase != null && localBase.ToString() != null)
                {
                    if (uriSuperBase != null)
                    {
                        uriBase = new Uri(uriSuperBase, localBase.ToString());
                    }
                    else
                    {
                        uriBase = new Uri(localBase.ToString());
                    }
                }
                else
                {
                    // if no local xml:base, take the passed down one
                    uriBase = uriSuperBase;
                }
                if (localUri != null)
                {
                    if (uriBase != null)
                    {
                        uriComplete = new Uri(uriBase, localUri.ToString());
                    }
                    else
                    {
                        uriComplete = new Uri(localUri.ToString());
                    }
                }
                else 
                {
                    uriComplete = uriBase;
                }
    
                return uriComplete != null ? uriComplete.AbsoluteUri : null;
            }
            catch (System.UriFormatException)
            {
                return "Unsupported URI format"; 
            }

        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Sets the Atom namespace, if it's not already set.
        /// </summary> 
        /// <param name="writer"> the xmlwriter to use</param>
        /// <returns> the namespace prefix</returns>
        //////////////////////////////////////////////////////////////////////
        static public string EnsureAtomNamespace(XmlWriter writer)
        {
            Tracing.Assert(writer != null, "writer should not be null");
            if (writer == null)
            {
                throw new ArgumentNullException("writer"); 
            }
            string strPrefix = writer.LookupPrefix(BaseNameTable.NSAtom);
            if (strPrefix == null)
            {
                writer.WriteAttributeString("xmlns", null, BaseNameTable.NSAtom);
            }
            return strPrefix;
           
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Sets the gData namespace, if it's not already set.
        /// </summary> 
        /// <param name="writer"> the xmlwriter to use</param>
        /// <returns> the namespace prefix</returns>
        //////////////////////////////////////////////////////////////////////
        static public string EnsureGDataNamespace(XmlWriter writer)
        {
            Tracing.Assert(writer != null, "writer should not be null");
            if (writer == null)
            {
                throw new ArgumentNullException("writer"); 
            }
            string strPrefix = writer.LookupPrefix(BaseNameTable.gNamespace);
            if (strPrefix == null)
            {
                writer.WriteAttributeString("xmlns", BaseNameTable.gDataPrefix, null, BaseNameTable.gNamespace);
            }
            return strPrefix;
        }
        /////////////////////////////////////////////////////////////////////////////

       
        //////////////////////////////////////////////////////////////////////
        /// <summary>Sets the gDataBatch namespace, if it's not already set.
        /// </summary> 
        /// <param name="writer"> the xmlwriter to use</param>
        /// <returns> the namespace prefix</returns>
        //////////////////////////////////////////////////////////////////////
        static public string EnsureGDataBatchNamespace(XmlWriter writer)
        {
            Tracing.Assert(writer != null, "writer should not be null");
            if (writer == null)
            {
                throw new ArgumentNullException("writer"); 
            }
            string strPrefix = writer.LookupPrefix(BaseNameTable.gBatchNamespace);
            if (strPrefix == null)
            {
                writer.WriteAttributeString("xmlns", BaseNameTable.gBatchPrefix, null, BaseNameTable.gBatchNamespace);
            }
            return strPrefix;
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>searches tokenCollection for a specific NEXT value. 
        ///  The collection is assume to be a key/value pair list, so if A,B,C,D is the list
        ///   A and C are keys, B and  D are values
        /// </summary> 
        /// <param name="tokens">the TokenCollection to search</param>
        /// <param name="key">the key to search for</param>
        /// <returns> the value string</returns>
        //////////////////////////////////////////////////////////////////////
        static public string FindToken(TokenCollection tokens,  string key)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException("tokens");
            }
        
            string returnValue = null; 
            bool fNextOne=false; 

            foreach (string token in tokens)
            {
                if (fNextOne)
                {
                    returnValue = token; 
                    break;
                }
                if (key == token)
                {
                    // next one is it
                    fNextOne = true; 
                }
            }

            return returnValue; 
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>converts a form response stream to a TokenCollection,
        ///  by parsing the contents of the stream for newlines and equal signs
        ///  the input stream is assumed to be an ascii encoded form resonse
        /// </summary> 
        ///  <param name="inputStream">the stream to read and parse</param>
        /// <returns> the resulting TokenCollection </returns>
        //////////////////////////////////////////////////////////////////////
        static public TokenCollection ParseStreamInTokenCollection(Stream inputStream)
        {
             // get the body and parse it
            ASCIIEncoding encoder = new ASCIIEncoding();
            StreamReader readStream = new StreamReader(inputStream, encoder);
            String body = readStream.ReadToEnd(); 
            readStream.Close(); 
            Tracing.TraceMsg("got the following body back: " + body);
            // all we are interested is the token, so we break the string in parts
            TokenCollection tokens = new TokenCollection(body, '=', true, 2); 
            return tokens;
         }
        /////////////////////////////////////////////////////////////////////////////

          //////////////////////////////////////////////////////////////////////
        /// <summary>parses a form response stream in token form for a specific value
        /// </summary> 
        /// <param name="inputStream">the stream to read and parse</param>
        /// <param name="key">the key to search for</param>
        /// <returns> the string in the tokenized stream </returns>
        //////////////////////////////////////////////////////////////////////
        static public string ParseValueFormStream(Stream inputStream,  string key)
        {
            TokenCollection tokens = ParseStreamInTokenCollection(inputStream);
            return FindToken(tokens, key);
         }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>returns a blank emptyDate. That's the default for an empty string date</summary> 
        //////////////////////////////////////////////////////////////////////
        public static DateTime EmptyDate
        {
            get {
                // that's the blank value you get when setting a DateTime to an empty string inthe property browswer
                return new DateTime(1,1,1);
            }

        }
        /////////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// Finds a specific ExtensionElement based on it's local name
        /// and it's namespace. If namespace is NULL, the first one where
        /// the localname matches is found. If there are extensionelements that do 
        /// not implment ExtensionElementFactory, they will not be taken into account
        /// Primary use of this is to find XML nodes
        /// </summary>
        /// <param name="arrList">the array to search through</param>
        /// <param name="localName">the xml local name of the element to find</param>
        /// <param name="ns">the namespace of the elementToPersist</param>
        /// <returns>Object</returns>
        public static IExtensionElementFactory FindExtension(ExtensionList arrList, string localName, string ns) 
        {
            if (arrList == null)
            {
                return null;
            }
            foreach (IExtensionElementFactory ele in arrList)
            {
                if (compareXmlNess(ele.XmlName, localName, ele.XmlNameSpace, ns))
                {
                    return ele;
                }
            }
            return null;
        }
       
        /// <summary>
        /// Finds all ExtensionElement based on it's local name
        /// and it's namespace. If namespace is NULL, allwhere
        /// the localname matches is found. If there are extensionelements that do 
        /// not implment ExtensionElementFactory, they will not be taken into account
        /// Primary use of this is to find XML nodes
        /// </summary>
        /// <param name="arrList">the array to search through</param>
        /// <param name="localName">the xml local name of the element to find</param>
        /// <param name="ns">the namespace of the elementToPersist</param>
        /// <param name="arr">the array to fill</param>
        /// <returns>none</returns>
        public static ExtensionList FindExtensions(ExtensionList arrList, string localName, string ns, ExtensionList arr) 
        {
           if (arrList == null)
           {
               throw new ArgumentNullException("arrList");
           }
           if (arr == null)
           {
               throw new ArgumentNullException("arr");
           }

           foreach (IExtensionElementFactory ob in arrList)
           {
               XmlNode node = ob as XmlNode;
               if (node != null)
               {
                   if (compareXmlNess(node.LocalName, localName, node.NamespaceURI, ns))
                   {
                       arr.Add(ob);
                   }
               }
               else
               {
                   // only if the elements do implement the ExtensionElementFactory
                   // do we know if it's xml name/namespace
                   IExtensionElementFactory ele = ob as IExtensionElementFactory;
                   if (ele != null)
                   {
                       if (compareXmlNess(ele.XmlName, localName, ele.XmlNameSpace, ns))
                       {
                           arr.Add(ob);
                       }
                   }
               }
           }
           return arr;
        }

        /// <summary>
        /// Finds all ExtensionElement based on it's local name
        /// and it's namespace. If namespace is NULL, allwhere
        /// the localname matches is found. If there are extensionelements that do 
        /// not implment ExtensionElementFactory, they will not be taken into account
        /// Primary use of this is to find XML nodes
        /// </summary>
        /// <param name="arrList">the array to search through</param>
        /// <param name="localName">the xml local name of the element to find</param>
        /// <param name="ns">the namespace of the elementToPersist</param>
        /// <returns>none</returns>
        public static List<T> FindExtensions<T>(ExtensionList arrList, string localName, string ns) where T : IExtensionElementFactory
        {
            List<T> list = new List<T>(); 
            if (arrList == null)
            {
                throw new ArgumentNullException("arrList");
            }

            foreach (IExtensionElementFactory obj in arrList)
            {
                if (obj is T)
                {
                    T ele = (T) obj;
                    // only if the elements do implement the ExtensionElementFactory
                    // do we know if it's xml name/namespace
                    if (ele != null)
                    {
                        if (compareXmlNess(ele.XmlName, localName, ele.XmlNameSpace, ns))
                        {
                            list.Add(ele);
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// save method to get an attribute value from an xmlnode
        /// </summary>
        /// <param name="attributeName"></param>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        public static string GetAttributeValue(string attributeName, XmlNode xmlNode) 
        {
            
            if (xmlNode != null &&
                attributeName != null && 
                xmlNode.Attributes != null && 
                xmlNode.Attributes[attributeName] != null)
            {
                    return xmlNode.Attributes[attributeName].Value;
            }
            
            return null;
        }
        
        /// <summary>
        /// returns the current assembly version using split() instead of the version 
        /// attribute to avoid security issues
        /// </summary>
        /// <returns>the current assembly version as a string</returns>
        public static string GetAssemblyVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            if (asm != null) 
            {
                string[] parts = asm.FullName.Split(',');
                if (parts != null && parts.Length > 1)
                    return parts[1].Trim();
            }     
            return "1.0.0";
        }
        
        
        
        /// <summary>
        /// returns the useragent string, including a version number
        /// </summary>
        /// <returns>the constructed userAgend in a standard form</returns>
        public static string ConstructUserAgent(string applicationName, string serviceName)
        {
            return "G-" + applicationName + "/" + serviceName + "-CS-" + GetAssemblyVersion();
        }

        private static bool compareXmlNess(string l1, string l2, string ns1, string ns2) 
        {
            if (String.Compare(l1,l2)==0)
            {
                if (ns1 == null)
                {
                    return true;
                } 
                else if (String.Compare(ns1, ns2)==0)
                {
                    return true;
                }
            }
            return false;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>goes to the Google auth service, and gets a new auth token</summary> 
        /// <returns>the auth token, or NULL if none received</returns>
        //////////////////////////////////////////////////////////////////////
        public static string QueryClientLoginToken(GDataCredentials gc, 
                                              string serviceName,
                                              string applicationName, 
                                              bool fUseKeepAlive,
                                              Uri clientLoginHandler
                                     )
        {
            Tracing.Assert(gc != null, "Do not call QueryAuthToken with no network credentials"); 
            if (gc == null)
            {
                throw new System.ArgumentNullException("nc", "No credentials supplied");
            }
             
            HttpWebRequest authRequest = WebRequest.Create(clientLoginHandler) as HttpWebRequest; 

            authRequest.KeepAlive = fUseKeepAlive;     
         
            string accountType = GoogleAuthentication.AccountType;
            if (!String.IsNullOrEmpty(gc.AccountType))
            {
                accountType += gc.AccountType;
            } 
            else 
            {
                accountType += GoogleAuthentication.AccountTypeDefault;
            }
          
            WebResponse authResponse = null;
            HttpWebResponse response = null;

            string authToken = null; 
            try
            {
                authRequest.ContentType = HttpFormPost.Encoding; 
                authRequest.Method = HttpMethods.Post;
                ASCIIEncoding encoder = new ASCIIEncoding();

                string user = gc.Username == null ? "" : gc.Username;
                string pwd = gc.getPassword() == null ? "" : gc.getPassword();

                // now enter the data in the stream
                string postData = GoogleAuthentication.Email + "=" + Utilities.UriEncodeUnsafe(user) + "&"; 
                postData += GoogleAuthentication.Password + "=" + Utilities.UriEncodeUnsafe(pwd) + "&";  
                postData += GoogleAuthentication.Source + "=" + Utilities.UriEncodeUnsafe(applicationName) + "&"; 
                postData += GoogleAuthentication.Service + "=" + Utilities.UriEncodeUnsafe(serviceName) + "&"; 
                if (gc.CaptchaAnswer != null)
                {
                    postData += GoogleAuthentication.CaptchaAnswer + "=" + Utilities.UriEncodeUnsafe(gc.CaptchaAnswer) + "&"; 
                }
                if (gc.CaptchaToken != null)
                {
                    postData += GoogleAuthentication.CaptchaToken + "=" + Utilities.UriEncodeUnsafe(gc.CaptchaToken) + "&"; 
                }
                postData += accountType; 

                byte[] encodedData = encoder.GetBytes(postData);
                authRequest.ContentLength = encodedData.Length; 

                Stream requestStream = authRequest.GetRequestStream() ;
                requestStream.Write(encodedData, 0, encodedData.Length); 
                requestStream.Close();        
                authResponse = authRequest.GetResponse();
                response = authResponse as HttpWebResponse;
            } 
            catch (WebException e)
            {
                response = e.Response as HttpWebResponse;
                if (response == null)
                {
                    Tracing.TraceMsg("QueryAuthtoken failed " + e.Status + " " + e.Message);
                    throw;
                }
            }
            if (response != null)
            {
                 // check the content type, it must be text
                if (!response.ContentType.StartsWith(HttpFormPost.ReturnContentType))
                {
                    throw new GDataRequestException("Execution of authentication request returned unexpected content type: " + response.ContentType,  response); 
                }
                TokenCollection tokens = Utilities.ParseStreamInTokenCollection(response.GetResponseStream());
                authToken = Utilities.FindToken(tokens, GoogleAuthentication.AuthToken); 

                if (authToken == null)
                {
                    throw Utilities.getAuthException(tokens, response);
                }
                // failsafe. if getAuthException did not catch an error...
                int code= (int)response.StatusCode;
                if (code != 200)
                {
                    throw new GDataRequestException("Execution of authentication request returned unexpected result: " +code,  response); 
                }

            }
            Tracing.Assert(authToken != null, "did not find an auth token in QueryAuthToken");
            if (authResponse != null)
            {
                authResponse.Close();
            }

           return authToken;
        }
        /////////////////////////////////////////////////////////////////////////////
                /// <summary>
        ///  Returns the respective GDataAuthenticationException given the return
        /// values from the login URI handler.
        /// </summary>
        /// <param name="tokens">The tokencollection of the parsed return form</param>
        /// <param name="response">the  webresponse</param> 
        /// <returns>AuthenticationException</returns>
        static LoggedException getAuthException(TokenCollection tokens,  HttpWebResponse response) 
        {
            String errorName = Utilities.FindToken(tokens, "Error");
            int code = (int)response.StatusCode;
            if (errorName == null || errorName.Length == 0)
            {
               // no error given by Gaia, return a standard GDataRequestException
                throw new GDataRequestException("Execution of authentication request returned unexpected result: " +code,  response); 
            }
            if ("BadAuthentication".Equals(errorName))
            {
                return new InvalidCredentialsException("Invalid credentials");
            }
            else if ("AccountDeleted".Equals(errorName))
            {
                return new AccountDeletedException("Account deleted");
            }
            else if ("AccountDisabled".Equals(errorName))
            {
                return new AccountDisabledException("Account disabled");
            }
            else if ("NotVerified".Equals(errorName))
            {
                return new NotVerifiedException("Not verified");
            }
            else if ("TermsNotAgreed".Equals(errorName))
            {
                return new TermsNotAgreedException("Terms not agreed");
            }
            else if ("ServiceUnavailable".Equals(errorName))
            {
                return new ServiceUnavailableException("Service unavailable");
            }
            else if ("CaptchaRequired".Equals(errorName))
            {
                String captchaPath = Utilities.FindToken(tokens, "CaptchaUrl");
                String captchaToken = Utilities.FindToken(tokens, "CaptchaToken");

                StringBuilder captchaUrl = new StringBuilder();
                captchaUrl.Append(GoogleAuthentication.DefaultProtocol).Append("://");
                captchaUrl.Append(GoogleAuthentication.DefaultDomain);
                captchaUrl.Append(GoogleAuthentication.AccountPrefix);
                captchaUrl.Append('/').Append(captchaPath);
                return new CaptchaRequiredException("Captcha required",
                                                    captchaUrl.ToString(),
                                                    captchaToken);

            }
            else
            {
                return new AuthenticationException("Error authenticating (check service name): " + errorName);
            }
        }

    }
    /////////////////////////////////////////////////////////////////////////////

    //////////////////////////////////////////////////////////////////////
    /// <summary>standard string tokenizer class. Pretty much cut/copy/paste out of 
    /// MSDN. 
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class TokenCollection : IEnumerable
    {
       private string[] elements;
  
       /// <summary>Constructor, takes a string and a delimiter set</summary> 
       public TokenCollection(string source, char[] delimiters)
       {

           if (source != null)
           {
               this.elements = source.Split(delimiters);
           }
       }

         /// <summary>Constructor, takes a string and a delimiter set</summary> 
       public TokenCollection(string source, char delimiter, 
                              bool separateLines, int resultsPerLine)
       {
           if (source != null)
           {
			   if (separateLines)
               {
                   // first split the source into a line array
                   string [] lines = source.Split(new char[] {'\n'});
                   int size = lines.Length * resultsPerLine;
                   this.elements = new string[size]; 
                   size = 0; 
                   foreach (String s in lines)
                   {
                       // do not use Split(char,int) as that one
                       // does not exist on .NET CF
                       string []temp = s.Split(delimiter);
                       int counter = temp.Length < resultsPerLine ? temp.Length : resultsPerLine;
                    
                       for (int i = 0; i <counter; i++) 
                       {
                           this.elements[size++] = temp[i];
                       }
                       for (int i = resultsPerLine; i < temp.Length; i++) 
                       {
                           this.elements[size-1] += delimiter + temp[i];
                       }
             
                   }
               } 
               else 
               {
                   string[] temp = source.Split(delimiter);
                   resultsPerLine = temp.Length < resultsPerLine ? temp.Length : resultsPerLine;
                   this.elements = new string[resultsPerLine];

                   for (int i = 0; i <resultsPerLine; i++) 
                   {
                       this.elements[i] = temp[i];
                   }
                   for (int i = resultsPerLine; i < temp.Length; i++) 
                   {
                       this.elements[resultsPerLine-1] += delimiter + temp[i];
                   }
               } 
           }
       }

        /// <summary>
        /// creates a dictionary of tokens based on this tokencollection
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> CreateDictionary()
        {
           Dictionary<string, string> dict = new Dictionary<string,string>(); 

           for (int i=0; i< this.elements.Length; i+=2)
           {
               string key = this.elements[i];
               string val = this.elements[i+1];
               dict.Add(key, val);
           }
           return dict; 
}

       /// <summary>IEnumerable Interface Implementation, for the noninterface</summary> 
       public TokenEnumerator GetEnumerator() // non-IEnumerable version
       {
          return new TokenEnumerator(this);
       }
       /// <summary>IEnumerable Interface Implementation</summary> 
       IEnumerator IEnumerable.GetEnumerator() 
       {
          return (IEnumerator) new TokenEnumerator(this);
       }
    
       /// <summary>Inner class implements IEnumerator interface</summary> 
       public class TokenEnumerator: IEnumerator
       {
          private int position = -1;
          private TokenCollection tokens;

          /// <summary>Standard constructor</summary> 
          public TokenEnumerator(TokenCollection tokens)
          {
             this.tokens = tokens;
          }

          /// <summary>IEnumerable::MoveNext implementation</summary> 
          public bool MoveNext()
          {
             if (this.tokens.elements != null && position < this.tokens.elements.Length - 1)
             {
                position++;
                return true;
             }
             else
             {
                return false;
             }
          }

          /// <summary>IEnumerable::Reset implementation</summary> 
          public void Reset()
          {
             position = -1;
          }

          /// <summary>Current implementation, non interface, type-safe</summary> 
          public string Current
          {
             get
             {
                return this.tokens.elements != null ? this.tokens.elements[position] : null;
             }
          }

          /// <summary>Current implementation, interface, not type-safe</summary> 
          object IEnumerator.Current 
          {
             get
             {
                return this.tokens.elements != null ? this.tokens.elements[position] : null;
             }
          }
       }
    }
    /////////////////////////////////////////////////////////////////////////////

}   /////////////////////////////////////////////////////////////////////////////
