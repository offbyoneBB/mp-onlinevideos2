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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace HybridDSP.Net.HTTP
{
    /// <summary>
    /// This class represents the response from the server to the client.
    /// </summary>
    public class HTTPServerResponse : HTTPMessage
    {
        public enum HTTPStatus
        {
            HTTP_CONTINUE = 100,
            HTTP_SWITCHING_PROTOCOLS = 101,
            HTTP_OK = 200,
            HTTP_CREATED = 201,
            HTTP_ACCEPTED = 202,
            HTTP_NONAUTHORITATIVE = 203,
            HTTP_NO_CONTENT = 204,
            HTTP_RESET_CONTENT = 205,
            HTTP_PARTIAL_CONTENT = 206,
            HTTP_MULTIPLE_CHOICES = 300,
            HTTP_MOVED_PERMANENTLY = 301,
            HTTP_FOUND = 302,
            HTTP_SEE_OTHER = 303,
            HTTP_NOT_MODIFIED = 304,
            HTTP_USEPROXY = 305,
            // UNUSED: 306
            HTTP_TEMPORARY_REDIRECT = 307,
            HTTP_BAD_REQUEST = 400,
            HTTP_UNAUTHORIZED = 401,
            HTTP_PAYMENT_REQUIRED = 402,
            HTTP_FORBIDDEN = 403,
            HTTP_NOT_FOUND = 404,
            HTTP_METHOD_NOT_ALLOWED = 405,
            HTTP_NOT_ACCEPTABLE = 406,
            HTTP_PROXY_AUTHENTICATION_REQUIRED = 407,
            HTTP_REQUEST_TIMEOUT = 408,
            HTTP_CONFLICT = 409,
            HTTP_GONE = 410,
            HTTP_LENGTH_REQUIRED = 411,
            HTTP_PRECONDITION_FAILED = 412,
            HTTP_REQUESTENTITYTOOLARGE = 413,
            HTTP_REQUESTURITOOLONG = 414,
            HTTP_UNSUPPORTEDMEDIATYPE = 415,
            HTTP_REQUESTED_RANGE_NOT_SATISFIABLE = 416,
            HTTP_EXPECTATION_FAILED = 417,
            HTTP_INTERNAL_SERVER_ERROR = 500,
            HTTP_NOT_IMPLEMENTED = 501,
            HTTP_BAD_GATEWAY = 502,
            HTTP_SERVICE_UNAVAILABLE = 503,
            HTTP_GATEWAY_TIMEOUT = 504,
            HTTP_VERSION_NOT_SUPPORTED = 505
        };

        public const string HTTP_REASON_CONTINUE                        = "Continue";
        public const string HTTP_REASON_SWITCHING_PROTOCOLS             = "Switching Protocols";
        public const string HTTP_REASON_OK                              = "OK";
        public const string HTTP_REASON_CREATED                         = "Created";
        public const string HTTP_REASON_ACCEPTED                        = "Accepted";
        public const string HTTP_REASON_NONAUTHORITATIVE                = "Non-Authoritative Information";
        public const string HTTP_REASON_NO_CONTENT                      = "No Content";
        public const string HTTP_REASON_RESET_CONTENT                   = "Reset Content";
        public const string HTTP_REASON_PARTIAL_CONTENT                 = "Partial Content";
        public const string HTTP_REASON_MULTIPLE_CHOICES                = "Multiple Choices";
        public const string HTTP_REASON_MOVED_PERMANENTLY               = "Moved Permanently";
        public const string HTTP_REASON_FOUND                           = "Found";
        public const string HTTP_REASON_SEE_OTHER                       = "See Other";
        public const string HTTP_REASON_NOT_MODIFIED                    = "Not Modified";
        public const string HTTP_REASON_USEPROXY                        = "Use Proxy";
        public const string HTTP_REASON_TEMPORARY_REDIRECT              = "Temporary Redirect";
        public const string HTTP_REASON_BAD_REQUEST                     = "Bad Request";
        public const string HTTP_REASON_UNAUTHORIZED                    = "Unauthorized";
        public const string HTTP_REASON_PAYMENT_REQUIRED                = "Payment Required";
        public const string HTTP_REASON_FORBIDDEN                       = "Forbidden";
        public const string HTTP_REASON_NOT_FOUND                       = "Not Found";
        public const string HTTP_REASON_METHOD_NOT_ALLOWED              = "Method Not Allowed";
        public const string HTTP_REASON_NOT_ACCEPTABLE                  = "Not Acceptable";
        public const string HTTP_REASON_PROXY_AUTHENTICATION_REQUIRED   = "Proxy Authentication Required";
        public const string HTTP_REASON_REQUEST_TIMEOUT                 = "Request Time-out";
        public const string HTTP_REASON_CONFLICT                        = "Conflict";
        public const string HTTP_REASON_GONE                            = "Gone";
        public const string HTTP_REASON_LENGTH_REQUIRED                 = "Length Required";
        public const string HTTP_REASON_PRECONDITION_FAILED             = "Precondition Failed";
        public const string HTTP_REASON_REQUESTENTITYTOOLARGE           = "Request Entity Too Large";
        public const string HTTP_REASON_REQUESTURITOOLONG               = "Request-URI Too Large";
        public const string HTTP_REASON_UNSUPPORTEDMEDIATYPE            = "Unsupported Media Type";
        public const string HTTP_REASON_REQUESTED_RANGE_NOT_SATISFIABLE = "Requested Range Not Satisfiable";
        public const string HTTP_REASON_EXPECTATION_FAILED              = "Expectation Failed";
        public const string HTTP_REASON_INTERNAL_SERVER_ERROR           = "Internal Net.HTTP Error";
        public const string HTTP_REASON_NOT_IMPLEMENTED                 = "Not Implemented";
        public const string HTTP_REASON_BAD_GATEWAY                     = "Bad Gateway";
        public const string HTTP_REASON_SERVICE_UNAVAILABLE             = "Service Unavailable";
        public const string HTTP_REASON_GATEWAY_TIMEOUT                 = "Gateway Time-out";
        public const string HTTP_REASON_VERSION_NOT_SUPPORTED           = "HTTP Version not supported";
        public const string HTTP_REASON_UNKNOWN                         = "???";
        public const string DATE       = "Date";
        public const string SET_COOKIE = "Set-Cookie";

        internal HTTPServerSession _session { get; private set; }
        private Stream _stream = null;

        private HTTPStatus _status;
        private string _reason;

        internal HTTPServerResponse(HTTPServerSession session)
        {
            _session = session;

            _status = HTTPStatus.HTTP_OK;
            _reason = GetReasonForStatus(HTTPStatus.HTTP_OK);
        }

        /// <summary>
        /// Gets or sets the status of the response.
        /// </summary>
        public HTTPStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        /// <summary>
        /// Gets or sets the reason string of the response.
        /// </summary>
        public string Reason
        {
            get { return _reason; }
            set { _reason = value; }
        }

        /// <summary>
        /// Sets the status and the correct reason for it.
        /// </summary>
        public HTTPStatus StatusAndReason
        {
            set
            {
                _status = value;
                _reason = GetReasonForStatus(_status);
            }
        }

        /// <summary>
        /// Gets or sets the date of the response.
        /// </summary>
        public DateTime Date
        {
            set { Set(DATE, value.ToUniversalTime().ToString("R")); }
            get { return DateTime.Parse(Get(DATE)).ToLocalTime(); }
        }

        /// <summary>
        /// Get if the response header has been sent yet..
        /// </summary>
        public bool Sent
        {
            get { return _stream != null; }
        }

        /// <summary>
        /// Send a continue reponse. This is used internally by the session.
        /// </summary>
        public void SendContinue()
        {
            Debug.Assert(_stream == null);

            using (HTTPHeaderOutputStream hs = new HTTPHeaderOutputStream(_session))
            using (TextWriter tw = new StreamWriter(hs))
            {
                tw.Write(Version);
                tw.Write(" 100 Continue\r\n\r\n");
            }
        }

        /// <summary>
        /// Send the response header and get a stream to which the body can be written.
        /// </summary>
        /// <returns>The Stream for the body.</returns>
        public Stream Send()
        {
            Debug.Assert(_stream == null);

            if (ChunkedTransferEncoding)
            {
                _stream = new HTTPOutputStream(_session);
                Write(_stream);
                _stream = new ChunkedStream(_session);
            }
            else if (ContentLength != UNKNOWN_CONTENT_LENGTH)
            {
                CountingOutputStream co = new CountingOutputStream();
                Write(co);
                _stream = new HTTPFixedLengthOutputStream(_session, ContentLength + co.Length);
                Write(_stream);
            }
            else
            {
                KeepAlive = false;
                _stream = new HTTPOutputStream(_session);
                Write(_stream);
            }

            return _stream;
        }

        /// <summary>
        /// Send the content of a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="mediaType">The Content-Type of the file.</param>
        public void SendFile(string path, string mediaType)
        {
            Debug.Assert(_stream == null);

            using (FileStream fs = File.OpenRead(path))
            {
                this.Date = File.GetLastWriteTime(path);
                this.ContentLength = fs.Length;
                this.ContentType = mediaType;
                this.ChunkedTransferEncoding = false;

                _stream = new HTTPOutputStream(_session);
                Write(_stream);

                int rc;
                byte[] bytes = new byte[65536];
                do
                {
                    rc = fs.Read(bytes, 0, 65536);
                    _stream.Write(bytes, 0, rc);
                } while (rc != 0);
            }
        }

        /// <summary>
        /// Send the content of a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="mediaType">The Content-Type of the buffer.</param>
        public void SendBuffer(byte[] buffer, string mediaType)
        {
            Debug.Assert(_stream == null);

            this.Date = DateTime.Now;
            this.ContentLength = buffer.Length;
            this.ContentType = mediaType;
            this.ChunkedTransferEncoding = false;

            _stream = new HTTPOutputStream(_session);
            Write(_stream);

            _stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Redirect the request.
        /// </summary>
        /// <param name="uri">The URI to redirect to.</param>
        public void Redirect(string uri)
        {
            Debug.Assert(_stream == null);

            StatusAndReason = HTTPStatus.HTTP_FOUND;
            Set("Location", uri);

            _stream = new HTTPOutputStream(_session);
            Write(_stream);
        }

        /// <summary>
        /// Write the response header to a Stream.
        /// </summary>
        /// <param name="stream"></param>
        public override void Write(Stream stream)
        {
            using (StreamWriter sw = new StreamWriter(stream))
            {
                sw.NewLine = "\r\n";
                sw.WriteLine(string.Format("{0} {1} {2}", Version, (int)Status, Reason));
            }
            base.Write(stream);
            using (StreamWriter sw = new StreamWriter(stream))
                sw.Write("\r\n");
        }

        private static string GetReasonForStatus(HTTPStatus status)
        {
            switch (status)
            {
                case HTTPStatus.HTTP_CONTINUE:
                    return HTTP_REASON_CONTINUE;
                case HTTPStatus.HTTP_SWITCHING_PROTOCOLS:
                    return HTTP_REASON_SWITCHING_PROTOCOLS;
                case HTTPStatus.HTTP_OK:
                    return HTTP_REASON_OK;
                case HTTPStatus.HTTP_CREATED:
                    return HTTP_REASON_CREATED;
                case HTTPStatus.HTTP_ACCEPTED:
                    return HTTP_REASON_ACCEPTED;
                case HTTPStatus.HTTP_NONAUTHORITATIVE:
                    return HTTP_REASON_NONAUTHORITATIVE;
                case HTTPStatus.HTTP_NO_CONTENT:
                    return HTTP_REASON_NO_CONTENT;
                case HTTPStatus.HTTP_RESET_CONTENT:
                    return HTTP_REASON_RESET_CONTENT;
                case HTTPStatus.HTTP_PARTIAL_CONTENT:
                    return HTTP_REASON_PARTIAL_CONTENT;
                case HTTPStatus.HTTP_MULTIPLE_CHOICES:
                    return HTTP_REASON_MULTIPLE_CHOICES;
                case HTTPStatus.HTTP_MOVED_PERMANENTLY:
                    return HTTP_REASON_MOVED_PERMANENTLY;
                case HTTPStatus.HTTP_FOUND:
                    return HTTP_REASON_FOUND;
                case HTTPStatus.HTTP_SEE_OTHER:
                    return HTTP_REASON_SEE_OTHER;
                case HTTPStatus.HTTP_NOT_MODIFIED:
                    return HTTP_REASON_NOT_MODIFIED;
                case HTTPStatus.HTTP_USEPROXY:
                    return HTTP_REASON_USEPROXY;
                case HTTPStatus.HTTP_TEMPORARY_REDIRECT:
                    return HTTP_REASON_TEMPORARY_REDIRECT;
                case HTTPStatus.HTTP_BAD_REQUEST:
                    return HTTP_REASON_BAD_REQUEST;
                case HTTPStatus.HTTP_UNAUTHORIZED:
                    return HTTP_REASON_UNAUTHORIZED;
                case HTTPStatus.HTTP_PAYMENT_REQUIRED:
                    return HTTP_REASON_PAYMENT_REQUIRED;
                case HTTPStatus.HTTP_FORBIDDEN:
                    return HTTP_REASON_FORBIDDEN;
                case HTTPStatus.HTTP_NOT_FOUND:
                    return HTTP_REASON_NOT_FOUND;
                case HTTPStatus.HTTP_METHOD_NOT_ALLOWED:
                    return HTTP_REASON_METHOD_NOT_ALLOWED;
                case HTTPStatus.HTTP_NOT_ACCEPTABLE:
                    return HTTP_REASON_NOT_ACCEPTABLE;
                case HTTPStatus.HTTP_PROXY_AUTHENTICATION_REQUIRED:
                    return HTTP_REASON_PROXY_AUTHENTICATION_REQUIRED;
                case HTTPStatus.HTTP_REQUEST_TIMEOUT:
                    return HTTP_REASON_REQUEST_TIMEOUT;
                case HTTPStatus.HTTP_CONFLICT:
                    return HTTP_REASON_CONFLICT;
                case HTTPStatus.HTTP_GONE:
                    return HTTP_REASON_GONE;
                case HTTPStatus.HTTP_LENGTH_REQUIRED:
                    return HTTP_REASON_LENGTH_REQUIRED;
                case HTTPStatus.HTTP_PRECONDITION_FAILED:
                    return HTTP_REASON_PRECONDITION_FAILED;
                case HTTPStatus.HTTP_REQUESTENTITYTOOLARGE:
                    return HTTP_REASON_REQUESTENTITYTOOLARGE;
                case HTTPStatus.HTTP_REQUESTURITOOLONG:
                    return HTTP_REASON_REQUESTURITOOLONG;
                case HTTPStatus.HTTP_UNSUPPORTEDMEDIATYPE:
                    return HTTP_REASON_UNSUPPORTEDMEDIATYPE;
                case HTTPStatus.HTTP_REQUESTED_RANGE_NOT_SATISFIABLE:
                    return HTTP_REASON_REQUESTED_RANGE_NOT_SATISFIABLE;
                case HTTPStatus.HTTP_EXPECTATION_FAILED:
                    return HTTP_REASON_EXPECTATION_FAILED;
                case HTTPStatus.HTTP_INTERNAL_SERVER_ERROR:
                    return HTTP_REASON_INTERNAL_SERVER_ERROR;
                case HTTPStatus.HTTP_NOT_IMPLEMENTED:
                    return HTTP_REASON_NOT_IMPLEMENTED;
                case HTTPStatus.HTTP_BAD_GATEWAY:
                    return HTTP_REASON_BAD_GATEWAY;
                case HTTPStatus.HTTP_SERVICE_UNAVAILABLE:
                    return HTTP_REASON_SERVICE_UNAVAILABLE;
                case HTTPStatus.HTTP_GATEWAY_TIMEOUT:
                    return HTTP_REASON_GATEWAY_TIMEOUT;
                case HTTPStatus.HTTP_VERSION_NOT_SUPPORTED:
                    return HTTP_REASON_VERSION_NOT_SUPPORTED;
                default:
                    return HTTP_REASON_UNKNOWN;
            }
        }
    }
}
