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

#include "SimpleServer.h"

CSimpleServer::CSimpleServer(void)
{
  this->addresses = NULL;
  this->serverContext = NULL;
  this->acceptedConnections = NULL;
}

CSimpleServer::~CSimpleServer(void)
{
  this->StopListening();

  FREE_MEM_CLASS(this->serverContext);
  CSocketContext::FreeAddresses(&this->addresses);
}

/* get methods */

WORD CSimpleServer::GetPort(void)
{
  return (this->serverContext != NULL) ? this->serverContext->GetPort() : 0;
}

int CSimpleServer::GetConnections(void)
{
  return (this->serverContext != NULL) ? this->serverContext->GetConnections() : 0;
}

CServerSocketContextCollection *CSimpleServer::GetAcceptedConnections(void)
{
  return this->acceptedConnections;
}

/* set methods */

/* other methods */

HRESULT CSimpleServer::Initialize(int family, int type, int protocol, WORD port, int connections)
{
  HRESULT result = S_OK;

  FREE_MEM_CLASS(this->serverContext);
  CSocketContext::FreeAddresses(&this->addresses);

  result = CSocketContext::ResolveIpAddresses(family, type, protocol, AI_PASSIVE, L"0.0.0.0", port, &this->addresses);

  if (SUCCEEDED(result))
  {
    this->serverContext = new CServerSocketContext();
    CHECK_POINTER_HRESULT(result, this->serverContext, result, E_OUTOFMEMORY);

    this->acceptedConnections = new CServerSocketContextCollection();
    CHECK_POINTER_HRESULT(result, this->acceptedConnections, result, E_OUTOFMEMORY);

    if (SUCCEEDED(result))
    {
      result = this->serverContext->Initialize(port, connections);
    }
  }

  return result;
}

HRESULT CSimpleServer::StartListening(void)
{
  HRESULT result = S_OK;
  CHECK_POINTER_HRESULT(result, this->addresses, result, E_INVALIDARG);
  CHECK_POINTER_HRESULT(result, this->serverContext, result, E_INVALIDARG);

  if (SUCCEEDED(result))
  {
    // create server socket
    result = this->serverContext->CreateSocket(this->addresses);
  }

  if (SUCCEEDED(result))
  {
    // set non-blocking mode
    result = this->serverContext->SetBlockingMode(false);
  }

  if (SUCCEEDED(result))
  {
    // bind socket to local address and port
    result = this->serverContext->Bind(this->addresses);
  }

  if (SUCCEEDED(result))
  {
    // listen on socket
    result = this->serverContext->Listen();
  }

  return result;
}

HRESULT CSimpleServer::StopListening(void)
{
  HRESULT result = S_OK;
  CHECK_POINTER_HRESULT(result, this->serverContext, result, E_INVALIDARG);
  CHECK_POINTER_HRESULT(result, this->acceptedConnections, result, E_INVALIDARG);

  if (SUCCEEDED(result))
  {
    FREE_MEM_CLASS(this->acceptedConnections);
    FREE_MEM_CLASS(this->serverContext);
  }

  return result;
}

HRESULT CSimpleServer::IsPendingIncomingConnection(void)
{
  HRESULT result = S_OK;
  CHECK_POINTER_HRESULT(result, this->serverContext, result, E_INVALIDARG);

  if (SUCCEEDED(result))
  {
    unsigned int state = SOCKET_STATE_UNDEFINED;
    result = this->serverContext->Select(true, false, 0, &state);
    result = (result == HRESULT_FROM_WIN32(WSAETIMEDOUT)) ? S_OK : result;

    if (SUCCEEDED(result))
    {
      result = ((state & SOCKET_STATE_READABLE) != 0) ? S_FALSE : S_OK;
    }
  }

  return result;
}

HRESULT CSimpleServer::AcceptPendingIncomingConnection(void)
{
  HRESULT result = S_OK;
  CHECK_POINTER_HRESULT(result, this->serverContext, result, E_INVALIDARG);
  CHECK_POINTER_HRESULT(result, this->acceptedConnections, result, E_INVALIDARG);

  if (SUCCEEDED(result))
  {
    CServerSocketContext *acceptedContext = NULL;
    result = this->serverContext->Accept(&acceptedContext);

    if (SUCCEEDED(result))
    {
      result = (this->acceptedConnections->Add(acceptedContext)) ? result : E_FAIL;
    }

    if (FAILED(result))
    {
      FREE_MEM_CLASS(acceptedContext);
    }
  }

  return result;
}