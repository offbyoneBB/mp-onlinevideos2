/*
 * Copyright (c) 2007, Hybrid DSP
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of Hybrid DSP nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY HYBRID DSP ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL HYBRID DSP BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HybridDSP.Net.HTTP
{
    /// <summary>
    /// HTTPMessage is the base class for HTTPServerRequest and
    /// HTTPServerResponse. It reads, writes and manages all common
    /// HTTP headers.
    /// </summary>
    public class HTTPMessage
    {
        protected const int EOF = -1;

        public const string HTTP_1_0                   = "HTTP/1.0";
        public const string HTTP_1_1                   = "HTTP/1.1";
        public const string IDENTITY_TRANSFER_ENCODING = "identity";
        public const string CHUNKED_TRANSFER_ENCODING  = "chunked";
        public const int    UNKNOWN_CONTENT_LENGTH     = -1;
        public const string UNKNOWN_CONTENT_TYPE       = "";
        public const string CONTENT_LENGTH             = "Content-Length";
        public const string CONTENT_TYPE               = "Content-Type";
        public const string TRANSFER_ENCODING          = "Transfer-Encoding";
        public const string CONNECTION                 = "Connection";
        public const string CONNECTION_KEEP_ALIVE      = "Keep-Alive";
        public const string CONNECTION_CLOSE           = "Close";

        private int _lastRead;
        private string _version;
        private Dictionary<string, string> _dictionary;

        public Dictionary<string, string>.KeyCollection Headers
        {
            get { return _dictionary.Keys; }
        }

        /// <summary>
        /// Construct a new HTTPMessage.
        /// </summary>
        protected HTTPMessage()
        {
            _lastRead = -1;
            _dictionary = new Dictionary<string, string>();
        }

        /// <summary>
        /// Returns the last read character when an HTTP header has been read.
        /// </summary>
        public int LastRead
        {
            get { return _lastRead; }
        }

        /// <summary>
        /// Sets or gets the HTTP protocol version.
        /// </summary>
        public string Version
        {
            set { _version = value; }
            get { return _version; }
        }

        /// <summary>
        /// Add a header to the set of headers. This will fail if a header
        /// with the same name exists.
        /// </summary>
        /// <param name="name">Name of the header.</param>
        /// <param name="value">Value of the header.</param>
        public void Add(string name, string value)
        {
            _dictionary.Add(name, value);
        }

        /// <summary>
        /// Check if the set of headers contains a particular header.
        /// </summary>
        /// <param name="name">Name of the header to check for.</param>
        /// <returns></returns>
        public bool Has(string name)
        {
            return _dictionary.ContainsKey(name);
        }

        /// <summary>
        /// Get the value for a particular header.
        /// </summary>
        /// <param name="name">Name of the header.</param>
        /// <returns>The value for the header or an empty string if
        /// it doesn't exist.</returns>
        public string Get(string name)
        {
            if (Has(name))
                return _dictionary[name];
            else
                return string.Empty;
        }

        /// <summary>
        /// Set the value for a particular header. This will create the header
        /// if it doesn;t exist yet.
        /// </summary>
        /// <param name="name">Name of the header.</param>
        /// <param name="value">Value of the header.</param>
        public void Set(string name, string value)
        {
            _dictionary[name] = value;
        }

        /// <summary>
        /// Gets or sets the transfer encoding
        /// </summary>
        public string TransferEncoding
        {
            get
            {
                if (Has(TRANSFER_ENCODING))
                    return Get(TRANSFER_ENCODING);
                else
                    return IDENTITY_TRANSFER_ENCODING;
            }
            set { Set(TRANSFER_ENCODING, value); }
        }

        /// <summary>
        /// Checks if the Transfer-Encoding is chunked or sets it to chunked or identity.
        /// </summary>
        public bool ChunkedTransferEncoding
        {
            get { return string.Compare(TransferEncoding, CHUNKED_TRANSFER_ENCODING, true) == 0; }
            set
            {
                if (value)
                    TransferEncoding = CHUNKED_TRANSFER_ENCODING;
                else
                    TransferEncoding = IDENTITY_TRANSFER_ENCODING;
            }
        }

        /// <summary>
        /// Gets or sets the Content-Length header.
        /// </summary>
        public Int64 ContentLength
        {
            get
            {
                if (Has(CONTENT_LENGTH))
                    return Int64.Parse(Get(CONTENT_LENGTH));
                else
                    return UNKNOWN_CONTENT_LENGTH;
            }
            set { Set(CONTENT_LENGTH, value.ToString()); }
        }

        /// <summary>
        /// Gets or sets the Content-Type header.
        /// </summary>
        public string ContentType
        {
            get
            {
                if (Has(CONTENT_TYPE))
                    return Get(CONTENT_TYPE);
                else
                    return UNKNOWN_CONTENT_TYPE;
            }
            set { Set(CONTENT_TYPE, value); }
        }

        /// <summary>
        /// Gets or sets the Connection header appropriate for the current
        /// HTTP version.
        /// </summary>
        public bool KeepAlive
        {
            get 
            {
                if (Has(CONNECTION))
                    return string.Compare(Get(CONNECTION), CONNECTION_KEEP_ALIVE, true) == 0;
                else
                    return string.Compare(_version, HTTP_1_1) == 0;
            }
            set
            {
                if (value)
                    Set(CONNECTION, CONNECTION_KEEP_ALIVE);
                else
                    Set(CONNECTION, CONNECTION_CLOSE);
            }
        }

        /// <summary>
        /// Reads the headers from a Stream.
        /// </summary>
        /// <param name="istr"></param>
        public virtual void Read(Stream istr)
        {
	        int c = istr.ReadByte();
	        while (c != EOF && c != '\r' && c != '\n')
	        {
		        string name = "";
		        string value = "";

		        while (c != EOF && c != ':' && c != '\n') { name += (char)c; c = istr.ReadByte(); }
		        if (c == '\n') { c = istr.ReadByte(); continue; } // ignore invalid header lines
                if (c != ':') throw new HTTPMessageException("Field name too long/no colon found");
		        if (c != EOF) c = istr.ReadByte(); // ':'
		        while (char.IsWhiteSpace((char)c)) c = istr.ReadByte();
		        while (c != EOF && c != '\r' && c != '\n') { value += (char)c; c = istr.ReadByte(); }
		        if (c == '\r') c = istr.ReadByte();
		        if (c == '\n')
			        c = istr.ReadByte();
		        else if (c != EOF)
                    throw new HTTPMessageException("Field value too long/no CRLF found");
		        while (c == ' ' || c == '\t') // folding
		        {
			        while (c != EOF && c != '\r' && c != '\n') { value += (char)c; c = istr.ReadByte(); }
			        if (c == '\r') c = istr.ReadByte();
			        if (c == '\n')
				        c = istr.ReadByte();
			        else if (c != EOF)
                        throw new HTTPMessageException("Folded field value too long/no CRLF found");
		        }
		        Add(name, value);
	        }

            _lastRead = c;
        }

        /// <summary>
        /// Writes the headers to a Stream.
        /// </summary>
        /// <param name="stream"></param>
        public virtual void Write(Stream stream)
        {
            using (StreamWriter sw = new StreamWriter(stream))
            {
                sw.NewLine = "\r\n";
                foreach (KeyValuePair<string, string> kv in _dictionary)
                    sw.WriteLine(kv.Key + ": " + kv.Value);
                sw.Flush();
            }
        }
    }
}
