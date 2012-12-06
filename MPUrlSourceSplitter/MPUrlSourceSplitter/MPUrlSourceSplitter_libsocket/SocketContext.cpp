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

#include "SocketContext.h"

CSocketContext::CSocketContext(void)
{
  this->internalSocket = INVALID_SOCKET;
  this->wsaInitialized = false;

  WORD versionRequested;
  WSADATA wsaData;

  /* Use the MAKEWORD(lowbyte, highbyte) macro declared in Windef.h */
  versionRequested = MAKEWORD(2, 2);

  this->wsaInitialized = (SUCCEEDED(HRESULT_FROM_WIN32(WSAStartup(versionRequested, &wsaData))));

  this->family = AF_UNSPEC;
  this->type = 0;
  this->protocol = 0;
}

CSocketContext::~CSocketContext(void)
{
  this->CloseSocket();

  if (this->wsaInitialized)
  {
    WSACleanup();
  }
}

/* get methods */

int CSocketContext::GetFamily(void)
{
  return this->family;
}

int CSocketContext::GetType(void)
{
  return this->type;
}

int CSocketContext::GetProtocol(void)
{
  return this->protocol;
}

HRESULT CSocketContext::GetOption(int level, int optionName, char *optionValue, int *optionLength)
{
  HRESULT result = S_OK;

  if (getsockopt(this->internalSocket, level, optionName, optionValue, optionLength) == SOCKET_ERROR)
  {
    result = HRESULT_FROM_WIN32(WSAGetLastError());
  }

  return result;
}

/* set methods */

HRESULT CSocketContext::SetOption(int level, int optionName, const char *optionValue, int optionLength)
{
  HRESULT result = S_OK;

  if (setsockopt(this->internalSocket, level, optionName, optionValue, optionLength) == SOCKET_ERROR)
  {
    result = HRESULT_FROM_WIN32(WSAGetLastError());
  }

  return result;
}

HRESULT CSocketContext::SetBlockingMode(bool blocking)
{
  HRESULT result = S_OK;
  unsigned long nonblocking = (blocking == 0);

  if (ioctlsocket(this->internalSocket, FIONBIO, &nonblocking) == SOCKET_ERROR) 
  {
    result = HRESULT_FROM_WIN32(WSAGetLastError());
  }

  return result;
}

/* other methods */

HRESULT CSocketContext::CreateSocket(ADDRINFOW *address)
{
  HRESULT result = (address != NULL) ? S_OK : E_INVALIDARG;

  if (this->internalSocket == INVALID_SOCKET)
  {
    this->family = address->ai_family;
    this->type = address->ai_socktype;
    this->protocol = address->ai_protocol;

    this->internalSocket = socket(this->family, this->type, this->protocol);

    if (this->internalSocket == INVALID_SOCKET)
    {
      result = HRESULT_FROM_WIN32(WSAGetLastError());
    }
    else
    {
      // set socket buffer size
      DWORD dw = BUFFER_LENGTH_DEFAULT;
      int dwLen = sizeof(dw);
      
      this->SetOption(SOL_SOCKET, SO_RCVBUF, (const char*)&dw, dwLen);
      this->SetOption(SOL_SOCKET, SO_SNDBUF, (const char*)&dw, dwLen);
    }
  }

  return result;
}

HRESULT CSocketContext::CloseSocket(void)
{
  HRESULT result = S_OK;

  if (this->internalSocket != INVALID_SOCKET)
  {
    int err = closesocket(this->internalSocket);
    this->internalSocket = INVALID_SOCKET;
  }

  return result;
}

HRESULT CSocketContext::ResolveIpAddresses(int family, int type, int protocol, unsigned int flags, const wchar_t *name, WORD port, ADDRINFOW **addresses)
{
  HRESULT wsaLastError = S_OK;

  ADDRINFOW hints;
  ADDRINFOW *result = NULL;

  wchar_t *portStr = FormatString(L"%d", port);

  CHECK_POINTER_HRESULT(wsaLastError, addresses, wsaLastError, E_INVALIDARG);
  CHECK_POINTER_HRESULT(wsaLastError, name, wsaLastError, E_INVALIDARG);
  CHECK_POINTER_HRESULT(wsaLastError, portStr, wsaLastError, E_OUTOFMEMORY);

  if (SUCCEEDED(wsaLastError))
  {
    // setup the hints address info structure
    // which is passed to the getaddrinfo() function
    ZeroMemory(&hints, sizeof(ADDRINFOW));

    const int safe_flags =
      AI_PASSIVE |
      AI_CANONNAME |
      AI_NUMERICHOST |
      AI_NUMERICSERV |
#ifdef AI_ALL
      AI_ALL |
#endif
#ifdef AI_ADDRCONFIG
      AI_ADDRCONFIG |
#endif
#ifdef AI_V4MAPPED
      AI_V4MAPPED |
#endif
      0;

    hints.ai_family = family;
    hints.ai_socktype = type;
    hints.ai_protocol = protocol;

    // unfortunately, some flags change the layout of struct addrinfo, so
    // they cannot be copied blindly from p_hints to &hints. Therefore, we
    // only copy flags that we know for sure are "safe".
    hints.ai_flags = flags & safe_flags;

    // we only ever use port *numbers*
    hints.ai_flags |= AI_NUMERICSERV;

    if ((hints.ai_flags & AI_NUMERICHOST) == 0)
    {
      hints.ai_flags |= AI_NUMERICHOST;
      wsaLastError = HRESULT_FROM_WIN32(GetAddrInfo(name, portStr, &hints, &result));
      if (FAILED(wsaLastError))
      {
        hints.ai_flags &= ~AI_NUMERICHOST;
      }
    } 

    wsaLastError = HRESULT_FROM_WIN32(GetAddrInfo(name, portStr, &hints, &result));
    if (SUCCEEDED(wsaLastError))
    {
      *addresses = result;
    }
  }
  FREE_MEM(portStr);

  return wsaLastError;
}

HRESULT CSocketContext::Bind(ADDRINFOW *address)
{
  HRESULT result = S_OK;
  CHECK_POINTER_HRESULT(result, address, result, E_INVALIDARG);

  if (SUCCEEDED(result))
  {
    if (bind(this->internalSocket, address->ai_addr, address->ai_addrlen) == SOCKET_ERROR)
    {
      result = HRESULT_FROM_WIN32(WSAGetLastError());
    }
  }

  return result;
}

void CSocketContext::FreeAddresses(ADDRINFOW **addresses)
{
  if ((addresses != NULL) && ((*addresses) != NULL))
  {
    FreeAddrInfoW(*addresses);
    *addresses = NULL;
  }
}

HRESULT CSocketContext::Send(const char *buffer, unsigned int length, unsigned int *sentLength)
{
  return this->Send(buffer, length, 0, sentLength);
}

HRESULT CSocketContext::Send(const char *buffer, unsigned int length, int flags, unsigned int *sentLength)
{
  HRESULT result = S_OK;
  CHECK_POINTER_HRESULT(result, buffer, result, E_INVALIDARG);
  CHECK_POINTER_HRESULT(result, sentLength, result, E_INVALIDARG);

  if (SUCCEEDED(result))
  {
    *sentLength = send(this->internalSocket, buffer, length, flags);
    if ((*sentLength) == SOCKET_ERROR)
    {
      result = HRESULT_FROM_WIN32(WSAGetLastError());
      *sentLength = 0;
    }
  }

  return result;
}

HRESULT CSocketContext::Receive(char *buffer, unsigned int length, unsigned int *receivedLength)
{
  return this->Receive(buffer, length, 0, receivedLength);
}

HRESULT CSocketContext::Receive(char *buffer, unsigned int length, int flags, unsigned int *receivedLength)
{
  HRESULT result = S_OK;
  CHECK_POINTER_HRESULT(result, buffer, result, E_INVALIDARG);
  CHECK_POINTER_HRESULT(result, receivedLength, result, E_INVALIDARG);

  if (SUCCEEDED(result))
  {
    *receivedLength = recv(this->internalSocket, buffer, length, flags);
    if ((*receivedLength) == SOCKET_ERROR)
    {
      result = HRESULT_FROM_WIN32(WSAGetLastError());
      *receivedLength = 0;
    }
  }

  return result;
}

HRESULT CSocketContext::Select(bool read, bool write, unsigned int timeout, unsigned int *state)
{
  HRESULT result = S_OK;
  CHECK_POINTER_HRESULT(result, state, result, E_INVALIDARG);

  if (SUCCEEDED(result))
  {
    fd_set readFD;
    fd_set writeFD;
    fd_set exceptFD;

    FD_ZERO(&readFD);
    FD_ZERO(&writeFD);
    FD_ZERO(&exceptFD);

    if (read)
    {
      // want to read from socket
      FD_SET(this->internalSocket, &readFD);
    }
    if (write)
    {
      // want to write to socket
      FD_SET(this->internalSocket, &writeFD);
    }
    // we want to receive errors
    FD_SET(this->internalSocket, &exceptFD);

    timeval sendTimeout;
    sendTimeout.tv_sec = timeout;
    sendTimeout.tv_usec = 0;

    int selectResult = select(0, &readFD, &writeFD, &exceptFD, &sendTimeout);
    if (selectResult == 0)
    {
      // timeout occured
      result = HRESULT_FROM_WIN32(WSAETIMEDOUT);
    }
    else if (selectResult == SOCKET_ERROR)
    {
      // socket error occured
      result = HRESULT_FROM_WIN32(WSAGetLastError());
    }
    else
    {
      // result is 0, correct return
      if (FD_ISSET(this->internalSocket, &exceptFD))
      {
        // error occured on socket, select function was successful
        int err;
        int errlen = sizeof(err);

        result = HRESULT_FROM_WIN32(this->GetOption(SOL_SOCKET, SO_ERROR, (char *)&err, &errlen));
        if (SUCCEEDED(result))
        {
          result = HRESULT_FROM_WIN32(err);
        }
      }

      if (SUCCEEDED(result))
      {
        *state = SOCKET_STATE_UNDEFINED;

        if (read && (FD_ISSET(this->internalSocket, &readFD) != 0))
        {
          (*state) |= SOCKET_STATE_READABLE;
        }

        if (write && (FD_ISSET(this->internalSocket, &writeFD) != 0))
        {
          (*state) |= SOCKET_STATE_WRITABLE;
        }

      }
    }
  }

  return result;
}