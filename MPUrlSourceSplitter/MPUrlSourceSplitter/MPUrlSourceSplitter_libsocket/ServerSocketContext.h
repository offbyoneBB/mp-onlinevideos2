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

#ifndef __SERVER_SOCKET_CONTEXT_DEFINED
#define __SERVER_SOCKET_CONTEXT_DEFINED

#include "SocketContext.h"

#define DEFAULT_BUFFER_REQUEST_SIZE                                           32768

class CServerSocketContext : public CSocketContext
{
public:
  CServerSocketContext(void);
  ~CServerSocketContext(void);

  /* get methods */

  // gets server listening port
  // @return : server listening port
  WORD GetPort(void);

  // gets maximum length of the queue of pending connections
  // @return : maximum length of the queue of pending connections
  int GetConnections(void);

  /* set methods */

  /* other methods */

  // creates and places a socket in a state in which it is listening for an incoming connection
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Listen(void);
  
  // accepts incoming socket connection
  // @param acceptedContext : reference to new created accepted socket context
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Accept(CServerSocketContext **acceptedContext);

  // initializes server socket context
  // @param port : port to bind server
  // @param connections : the maximum length of the queue of pending connections
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Initialize(WORD port, int connections);

  // tests if client closed connection
  // @return : S_OK if connection is opened, S_FALSE if connection is closed, error code otherwise
  HRESULT IsClosed(void);

protected:
  // holds server listening port
  WORD port;
  // holds maximum length of the queue of pending connections
  int connections;
};

#endif