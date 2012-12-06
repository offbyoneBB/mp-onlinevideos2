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

#ifndef __SOCKET_CONTEXT_DEFINED
#define __SOCKET_CONTEXT_DEFINED

#include <WinSock2.h>
#include <Ws2tcpip.h>

#define BUFFER_LENGTH_DEFAULT                                                 262144

#define SOCKET_STATE_UNDEFINED                                                0
#define SOCKET_STATE_READABLE                                                 1
#define SOCKET_STATE_WRITABLE                                                 2

class CSocketContext
{
public:
  CSocketContext(void);
  ~CSocketContext(void);

  /* get methods */

  // gets socket family (AF_INET, AF_INET6, ...)
  // @return : socket family
  int GetFamily(void);

  // gets socket type (SOCK_STREAM, SOCK_DGRAM, ...)
  // @return : socket type
  int GetType(void);

  // gets socket protocol (IPPROTO_TCP, IPPROTO_UDP, ...)
  // @return : socket protocol
  int GetProtocol(void);

  // gets socket option
  // @param level : lhe level at which the option is defined (SOL_SOCKET, ...)
  // @param optionName : the socket option for which the value is to be retrieved(SO_ACCEPTCONN, ...)
  // @param optionValue : a pointer to the buffer in which the value for the requested option is to be returned
  // @param optionLength : a pointer to the size, in bytes, of the optionValue buffer
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT GetOption(int level, int optionName, char *optionValue, int *optionLength);

  /* set methods */

  // sets socket option
  // @param level : lhe level at which the option is defined (SOL_SOCKET, ...)
  // @param optionName : the socket option for which the value is to be set (SO_BROADCAST, ...)
  // @param optionValue : a pointer to the buffer in which the value for the requested option is specified
  // @param optionLength : the size, in bytes, of the buffer pointed to by the optionValue parameter
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT SetOption(int level, int optionName, const char *optionValue, int optionLength);

  // sets socket blocking mode
  // @param blocking : true if blocking socket, false otherwise
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT SetBlockingMode(bool blocking);

  /* other methods */

  // binds local address with a socket
  // @param address : 
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Bind(ADDRINFOW *address);
  
  // sends data on a connected socket
  // @param buffer : pointer to a buffer containing the data to be transmitted
  // @param length : the length, in bytes, of the data in buffer pointed to by the buffer parameter
  // @param sentLength : reference to total number of bytes sent, which can be less than the number requested to be sent
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Send(const char *buffer, unsigned int length, unsigned int *sentLength);

  // sends data on a connected socket
  // @param buffer : pointer to a buffer containing the data to be transmitted
  // @param length : the length, in bytes, of the data in buffer pointed to by the buffer parameter
  // @param flags : set of flags that specify the way in which the call is made, see remarks of send() method
  // @param sentLength : reference to total number of bytes sent, which can be less than the number requested to be sent
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Send(const char *buffer, unsigned int length, int flags, unsigned int *sentLength);

  // receives data from a connected socket
  // @param buffer : pointer to the buffer to receive the incoming data
  // @param length : the length, in bytes, of the buffer pointed to by the buffer parameter
  // @param receivedLength : reference to the number of bytes received, if the connection has been gracefully closed, value is zero
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Receive(char *buffer, unsigned int length, unsigned int *receivedLength);

  // receives data from a connected socket
  // @param buffer : pointer to the buffer to receive the incoming data
  // @param length : the length, in bytes, of the buffer pointed to by the buffer parameter
  // @param flasg : set of flags that influences the behavior of this function, see remarks of recv() method
  // @param receivedLength : reference to the number of bytes received, if the connection has been gracefully closed, value is zero
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Receive(char *buffer, unsigned int length, int flags, unsigned int *receivedLength);

  // determines the status of socket
  // @param read : determine if socket is in readable state (incoming connection or unread data)
  // @param write : determine if socket is in writable state (connect is successful or can send data)
  // @param timeout : the maximum time for select to wait (in ms)
  // @param state : reference to socket state variable
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT Select(bool read, bool write, unsigned int timeout, unsigned int *state);

  // resolves IP addresses
  // @param family : socket family (AF_INET, AF_INET6, ...)
  // @param type : socket type (SOCK_STREAM, SOCK_DGRAM, ...)
  // @param protocol : socket protocol (IPPROTO_TCP, IPPROTO_UDP, ...)
  // @param flags : flags for resolving IP address
  // @param name : the name to resolve
  // @param port : port to resolve
  // @param addresses : reference to addresses array to store resolved addresses
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  static HRESULT ResolveIpAddresses(int family, int type, int protocol, unsigned int flags, const wchar_t *name, WORD port, ADDRINFOW **addresses);

  // frees addresses acquired by ResolveIpAddresses() method
  // @param addresses : reference to addresses (set to NULL after successful execution)
  static void FreeAddresses(ADDRINFOW **addresses);

  // creates socket with specified family, type and protocol
  // @param address : address structure to create socket (type, family, protocol)
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT CreateSocket(ADDRINFOW *address);

  // closes socket
  // @return : S_OK if successful, error code otherwise (can be system or WSA)
  HRESULT CloseSocket(void);

protected:

  // holds internal socket
  SOCKET internalSocket;

  // specifies if WSA was correctly initialized
  bool wsaInitialized;

  // holds socket family (AF_INET, AF_INET6, ...)
  int family;

  // holds socket type (SOCK_STREAM, SOCK_DGRAM, ...)
  int type;

  // holds socket protocol (IPPROTO_TCP, IPPROTO_UDP, ...)
  int protocol;

  /* methods */
};

#endif