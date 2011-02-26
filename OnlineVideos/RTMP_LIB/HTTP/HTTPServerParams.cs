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
using System.Text;

namespace HybridDSP.Net.HTTP
{
    /// <summary>
    /// The server parameters.
    /// </summary>
    public struct HTTPServerParams
    {
        private int _timeout;
        private int _keepAliveTimeout;
        private int _maxRequests;
        private bool _keepAlive;

        /// <summary>
        /// The default server parameters.
        /// </summary>
        public static HTTPServerParams Default
        {
            get
            {
                HTTPServerParams p = new HTTPServerParams();
                p.Timeout = 2000000;
                p.KeepAliveTimeout = 5000000;
                p.MaxRequests = 8;
                p.KeepAlive = true;
                return p;
            }
        }

        /// <summary>
        /// Gets or sets he timeout for the first request to be made after
        /// a connection has been established.
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <summary>
        /// Gets or sets the timeout for subsequent request to be made after
        /// the last request has been completed.
        /// </summary>
        public int KeepAliveTimeout
        {
            get { return _keepAliveTimeout; }
            set { _keepAliveTimeout = value; }
        }

        /// <summary>
        /// Gets ot sets the maximum number of requests that are handled in
        /// one session.
        /// </summary>
        public int MaxRequests
        {
            get { return _maxRequests; }
            set { _maxRequests = value; }
        }

        /// <summary>
        /// Gets or sets the keep alive feature. If this property is false, a
        /// session will close the connection after each completed request.
        /// </summary>
        public bool KeepAlive
        {
            get { return _keepAlive; }
            set { _keepAlive = value; }
        }
    }
}
