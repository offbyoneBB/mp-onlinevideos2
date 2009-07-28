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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HybridDSP.Net.HTTP
{
    /// <summary>
    /// This class represents the actual HTTP server.
    /// </summary>
    public class HTTPServer : IDisposable
    {
        private IHTTPRequestHandlerFactory _factory = null;
        private Socket _socket = null;
        private HTTPServerParams _params;
        private Thread _thread = null;
        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);

        /// <summary>
        /// Create an HTTPServer with default parameters.
        /// </summary>
        /// <param name="factory">The RequestHandlerFactory that will instantiate the
        /// Request handler(s) for this server.</param>
        /// <param name="socket">The socket on which this server will listen for
        /// connections. The socket must be bound to an endpoint prior to creating
        /// the server.</param>
        public HTTPServer(IHTTPRequestHandlerFactory factory, Socket socket)
            : this(factory, socket, HTTPServerParams.Default)
        { }

        /// <summary>
        /// Create an HTTPServer.
        /// </summary>
        /// <param name="factory">The RequestHandlerFactory that will instantiate the
        /// Request handler(s) for this server.</param>
        /// <param name="socket">The socket on which this server will listen for
        /// connections. The socket must be bound to an endpoint prior to creating
        /// the server.</param>
        /// <param name="parameters">The parameters used for this server.</param>
        public HTTPServer(IHTTPRequestHandlerFactory factory, Socket socket, HTTPServerParams parameters)
        {
            if (!socket.IsBound)
                throw new HTTPException("The socket must be bound.");

            _factory = factory;
            _socket = socket;
            _params = parameters;

            _socket.Listen(64);
        }

        /// <summary>
        /// Create an HTTPServer with default parameters.
        /// </summary>
        /// <param name="factory">The RequestHandlerFactory that will instantiate the
        /// Request handler(s) for this server.</param>
        /// <param name="port">The port on which to listen for connections. The socket
        /// will be created and bound to all interfaces by the HTTPServer</param>
        public HTTPServer(IHTTPRequestHandlerFactory factory, int port)
            : this(factory, port, HTTPServerParams.Default)
        { }

        /// <summary>
        /// Create an HTTPServer.
        /// </summary>
        /// <param name="factory">The RequestHandlerFactory that will instantiate the
        /// Request handler(s) for this server.</param>
        /// <param name="port">The port on which to listen for connections. The socket
        /// will be created and bound to all interfaces by the HTTPServer</param>
        /// <param name="parameters">The parameters used for this server.</param>
        public HTTPServer(IHTTPRequestHandlerFactory factory, int port, HTTPServerParams parameters)
        {
            _factory = factory;
            _params = parameters;

            AddressFamily addressFamily;
            IPAddress bindAddress;
            //if (Socket.OSSupportsIPv6)
            //{
            //    addressFamily = AddressFamily.InterNetworkV6;
            //    bindAddress = IPAddress.IPv6Any;
            //}
            //else
            //{
                addressFamily = AddressFamily.InterNetwork;
                bindAddress = IPAddress.Any;
            //}

            _socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(bindAddress, port));
            _socket.Listen(64);
        }

        /// <summary>
        /// Start the server (in a background thread).
        /// </summary>
        public void Start()
        {
            if (_thread != null)
                throw new InvalidOperationException("The server can not be started again.");

            _thread = new Thread(this.Run);
            _thread.IsBackground = true;
            _thread.Start();
        }

        /// <summary>
        /// Stop the server. Once a server is stopped it can not be
        /// started again.
        /// </summary>
        public void Stop()
        {
            if (_thread != null)
            {
                _shutdownEvent.Set();
                _thread.Join();
                _thread = null;
            }

            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }
        }

        /// <summary>
        /// Gets the port on which the server is listening.
        /// </summary>
        public int Port
        {
            get { return ((IPEndPoint)_socket.LocalEndPoint).Port; }
        }

        /// <summary>
        /// Gets if the server is still running.
        /// </summary>
        public bool IsRunning
        {
            get { return _thread != null && _thread.IsAlive; }
        }

        public delegate void ServerStarted();
        public delegate void ServerStopped();
        public delegate void ServerCaughtException(Exception ex);

        /// <summary>
        /// Invoked when the server is started.
        /// </summary>
        public event ServerStarted OnServerStart;

        /// <summary>
        /// Invoked just before the server stops.
        /// </summary>
        public event ServerStopped OnServerStop;

        /// <summary>
        /// Invoked when the server catches an exception;
        /// </summary>
        public event ServerCaughtException OnServerException;

        private void ServerStart()
        {
            if (OnServerStart != null)
                OnServerStart();
        }

        private void ServerStop()
        {
            if (OnServerStop != null)
                OnServerStop();
        }

        private void ServerException(Exception ex)
        {
            if (OnServerException != null)
                OnServerException(ex);
        }

        private void Run()
        {
            try
            {
                AsyncCallback callback = new AsyncCallback(this.AcceptClient);
                ManualResetEvent evt = new ManualResetEvent(false);
                WaitHandle[] waitHandles = new WaitHandle[] { _shutdownEvent, evt };

                ServerStart();
                while (!_shutdownEvent.WaitOne(0, false))
                {
                    evt.Reset();
                    _socket.BeginAccept(callback, evt);

                    WaitHandle.WaitAny(waitHandles);
                }
            }
            catch (ThreadInterruptedException) { }
            catch (ThreadAbortException) { }
            catch (Exception ex) 
            {
                ServerException(ex);
            }
            finally
            {
                ServerStop();
            }
        }

        private void AcceptClient(IAsyncResult ar)
        {
            try
            {
                using (HTTPServerSession session = new HTTPServerSession(_socket.EndAccept(ar), _params))
                {
                    ManualResetEvent evt = ar.AsyncState as ManualResetEvent;
                    evt.Set();

                    while (session.HasMoreRequests)
                    {
                        try
                        {
                            HTTPServerResponse response = new HTTPServerResponse(session);
                            HTTPServerRequest request = new HTTPServerRequest(session);

                            response.Version = request.Version;
                            response.KeepAlive = session.CanKeepAlive && request.KeepAlive && _params.KeepAlive;

                            try
                            {
                                IHTTPRequestHandler handler = _factory.CreateRequestHandler(request);
                                if (handler != null)
                                {
                                    if (request.ExpectsContinue)
                                        response.SendContinue();

                                    handler.HandleRequest(request, response);
                                    session.KeepAlive = response.KeepAlive && session.CanKeepAlive && _params.KeepAlive;

                                    while (session.HasMoreRequests)
                                    {
                                        System.Threading.Thread.Sleep(1000);
                                    }

                                }
                                else
                                    SendErrorResponse(session, HTTPServerResponse.HTTPStatus.HTTP_NOT_IMPLEMENTED);
                            }
                            catch (Exception ex)
                            {
                                if (!response.Sent)
                                    SendErrorResponse(session, HTTPServerResponse.HTTPStatus.HTTP_INTERNAL_SERVER_ERROR);

                                OnServerException(ex);
                                break;
                            }
                        }
                        catch (HTTPNoMessageException) { break; }
                        catch (HTTPMessageException)
                        {
                            SendErrorResponse(session, HTTPServerResponse.HTTPStatus.HTTP_BAD_REQUEST);
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            { }
        }

        private void SendErrorResponse(HTTPServerSession session, HTTPServerResponse.HTTPStatus status)
        {
            HTTPServerResponse response = new HTTPServerResponse(session);
            response.Version = HTTPMessage.HTTP_1_1;
            response.StatusAndReason = status;
            response.KeepAlive = false;
            response.Send();
            session.KeepAlive = false;
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}
