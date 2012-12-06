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

#include "StdAfx.h"

#include "ServerSocketContext.h"

CServerSocketContext::CServerSocketContext(void)
  : CSocketContext()
{
  this->port = 0;
  this->connections = 0;
}

CServerSocketContext::~CServerSocketContext(void)
{
}

/* get methods */

WORD CServerSocketContext::GetPort(void)
{
  return this->port;
}

int CServerSocketContext::GetConnections(void)
{
  return this->connections;
}

/* set methods */

/* other methods */

HRESULT CServerSocketContext::Listen()
{
  HRESULT result = S_OK;

  if (listen(this->internalSocket, this->connections) == SOCKET_ERROR)
  {
    result = WSAGetLastError();
  }

  return result;
}

HRESULT CServerSocketContext::Accept(CServerSocketContext **acceptedContext)
{
  HRESULT result = S_OK;
  CHECK_POINTER_HRESULT(result, acceptedContext, result, E_INVALIDARG);

  if (SUCCEEDED(result))
  {
    SOCKET acceptedSocket = accept(this->internalSocket, NULL, NULL);
    if (acceptedSocket == INVALID_SOCKET)
    {
      result = HRESULT_FROM_WIN32(WSAGetLastError());
    }

    if (SUCCEEDED(result))
    {
      *acceptedContext = new CServerSocketContext();
      CHECK_POINTER_HRESULT(result, (*acceptedContext), result, E_OUTOFMEMORY);

      (*acceptedContext)->family = this->family;
      (*acceptedContext)->type = this->type;
      (*acceptedContext)->protocol = this->protocol;
      (*acceptedContext)->internalSocket = acceptedSocket;
    }
  }

  return result;
}

HRESULT CServerSocketContext::Initialize(WORD port, int connections)
{
  this->port = port;
  this->connections = connections;

  return S_OK;
}

HRESULT CServerSocketContext::IsClosed(void)
{
  unsigned int state = SOCKET_STATE_UNDEFINED;
  HRESULT result = this->Select(true, false, 0, &state);
  result = (result == HRESULT_FROM_WIN32(WSAETIMEDOUT)) ? S_OK : result;

  if (SUCCEEDED(result))
  {
    if (state == SOCKET_STATE_READABLE)
    {
      // if socket is closed, then readable state is signaled
      // Receive() method returns 0 received bytes when connection is closed
      ALLOC_MEM_DEFINE_SET(buffer, char, DEFAULT_BUFFER_REQUEST_SIZE, 0);
      CHECK_POINTER_HRESULT(result, buffer, result, E_OUTOFMEMORY);

      if (SUCCEEDED(result))
      {
        unsigned int receivedLength = 0;
        result = this->Receive(buffer, DEFAULT_BUFFER_REQUEST_SIZE, MSG_PEEK, &receivedLength);

        if (SUCCEEDED(result))
        {
          result = (receivedLength == 0) ? S_FALSE : result;
        }
      }

      FREE_MEM(buffer);
    }
  }

  return result;
}
