/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#pragma once

#ifndef __SIMPLE_SERVER_DEFINED
#define __SIMPLE_SERVER_DEFINED

#include "ServerSocketContextCollection.h"

class CSimpleServer
{
public:
  CSimpleServer(void);
  ~CSimpleServer(void);

  /* get methods */

  // gets server listening port
  // @return : server listening port or zero if not specified
  WORD GetPort(void);

  // gets maximum length of the queue of pending connections
  // @return : maximum length of the queue of pending connections or zero if not specified
  int GetConnections(void);

  // gets accepted incoming connections
  // @return : accepted incoming connections
  CServerSocketContextCollection *GetAcceptedConnections(void);

  /* set methods */

  /* other methods */

  // initializes simple server
  // @param family : socket family (AF_INET, AF_INET6, ...)
  // @param type : socket type (SOCK_STREAM, SOCK_DGRAM, ...)
  // @param protocol : socket protocol (IPPROTO_TCP, IPPROTO_UDP, ...)
  // @param port : port to bind server
  // @param connections : the maximum length of the queue of pending connections
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Initialize(int family, int type, int protocol, WORD port, int connections);

  // starts listening to incoming connections
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT StartListening(void);

  // stops listening to incoming connections
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT StopListening(void);

  // tests if there is some pending incoming connection
  // @return : S_OK if no connection request, S_FALSE if there is incoming connection request, error code otherwise (can be system or WSA)
  HRESULT IsPendingIncomingConnection(void);

  // accepts incoming socket connection
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT AcceptPendingIncomingConnection(void);

protected:
  // holds resolved addresses
  ADDRINFOW *addresses;
  // holds server socket context
  CServerSocketContext *serverContext;
  // holds accepted incoming connections
  CServerSocketContextCollection *acceptedConnections;
};

#endif