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
using System.Net.Sockets;
using System.IO;
using System.Text;

namespace HybridDSP.Net.HTTP
{
    internal class HTTPServerSession : IDisposable
    {
        public Socket _socket;
        private HTTPServerParams _params;
        private int _maxRequests;

        private bool _firstRequest = true;
        private bool _keepAlive = true;

        public HTTPServerSession(Socket socket, HTTPServerParams parameters)
        {
            _socket = socket;
            _params = parameters;
            _maxRequests = parameters.MaxRequests;
            // we are local, don't wait forever when sending or receiving data
            _socket.SendTimeout = 1000; 
            _socket.ReceiveTimeout = 1000;
        }

        public bool KeepAlive
        {
            get { return _keepAlive; }
            set { _keepAlive = value; }
        }

        public bool CanKeepAlive
        {
            get { return _maxRequests != 0; }
        }

        public bool HasMoreRequests
        {
            get
            {
                if (_firstRequest)
                {
                    _firstRequest = false;
                    return _socket.Poll(_params.Timeout, SelectMode.SelectRead);
                }
                else if (_maxRequests != 0 && _keepAlive)
                {
                    if (_maxRequests > 0)
                        --_maxRequests;
                    return _socket.Poll(_params.KeepAliveTimeout, SelectMode.SelectRead);
                }
                return false;
            }
        }

        public void Abort()
        {
            _socket.Shutdown(SocketShutdown.Send);

            byte[] buffer = new byte[0x1000];
            while (_socket.Poll(50000, SelectMode.SelectRead))
                if (_socket.Receive(buffer, SocketFlags.Partial) == 0)
                    break;

            _socket.Shutdown(SocketShutdown.Receive);
            _socket.Close();
        }

        public int ReadByte()
        {
            byte[] b = new byte[1];
            if (Read(b, 1) == 1)
                return (int)b[0];
            else
                return -1;
        }

        public int Read(byte[] buffer, int count)
        {
            return _socket.Receive(buffer, count, SocketFlags.None);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _socket.Receive(buffer, offset, count, SocketFlags.None);
        }

        public void Write(byte[] buffer, int count)
        {
            int send = 0;
            while (send < count)
                send += _socket.Send(buffer, send, count, SocketFlags.None);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            int send = 0;
            while (send < count)
                send += _socket.Send(buffer, offset + count, SocketFlags.None);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Abort();
        }

        #endregion
    }
}
