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

#ifndef __PROTOCOLINTERFACE_DEFINED
#define __PROTOCOLINTERFACE_DEFINED

#include "StringCollection.h"
#include "ParameterCollection.h"
#include "MediaPacket.h"

// logging constants

// methods' names
#define METHOD_CONSTRUCTOR_NAME                                         _T("ctor()")
#define METHOD_DESTRUCTOR_NAME                                          _T("dtor()")
#define METHOD_CLEAR_SESSION_NAME                                       _T("ClearSession()")
#define METHOD_INITIALIZE_NAME                                          _T("Initialize()")
#define METHOD_PARSE_URL_NAME                                           _T("ParseUrl()")
#define METHOD_OPEN_CONNECTION_NAME                                     _T("OpenConnection()")
#define METHOD_CLOSE_CONNECTION_NAME                                    _T("CloseConnection()")
#define METHOD_RECEIVE_DATA_NAME                                        _T("ReceiveData()")
#define METHOD_PUSH_DATA_NAME                                           _T("PushData()")
#define METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME                         _T("ReceiveDataFromTimestamp()")

// methods' common string formats
#define METHOD_START_FORMAT                                             _T("%s: %s: Start")
#define METHOD_END_FORMAT                                               _T("%s: %s: End")
#define METHOD_END_FAIL_FORMAT                                          _T("%s: %s: End, Fail")
#define METHOD_END_FAIL_HRESULT_FORMAT                                  _T("%s: %s: End, Fail, result: 0x%08X")
#define METHOD_MESSAGE_FORMAT                                           _T("%s: %s: %s")

// return values of protocol methods
// no error
#define STATUS_OK                                       0
// error
#define STATUS_ERROR                                    -1
// error, do not retry call method
#define STATUS_ERROR_NO_RETRY                           -2

// defines interface for stream
struct IOutputStream
{
  // sets total length of stream to output pin
  // caller is responsible for deleting output pin name
  // @param outputPinName : the name of output pin (the output pin name must be value from values returned from GetStreamNames() method of IProtocol interface
  // @param total : total length of stream in bytes
  // @param estimate : specifies if length is estimate
  // @return : STATUS_OK if successful
  virtual int SetTotalLength(const TCHAR *outputPinName, LONGLONG total, bool estimate) = 0;

  // pushes media packet to output pin
  // caller is responsible for deleting output pin name, media packet will be destroyed after processing
  // @param outputPinName : the name of output pin (the output pin name must be value from values returned from GetStreamNames() method of IProtocol interface
  // @param mediaPacket : reference to media packet to push to output pin
  // @return : STATUS_OK if successful
  virtual int PushMediaPacket(const TCHAR *outputPinName, CMediaPacket *mediaPacket) = 0;
};

// defines interface for base protocol implementation
struct IBaseProtocol
{
  // return reference to null-terminated string which represents protocol name
  // function have to allocate enough memory for protocol name string
  // errors should be logged to log file and returned NULL
  // @return : reference to null-terminated string
  virtual TCHAR *GetProtocolName(void) = 0;

  // test if connection is opened
  // @return : true if connected, false otherwise
  virtual bool IsConnected(void) = 0;

  // get timeout (in ms) for receiving data
  // @return : timeout (in ms) for receiving data
  virtual unsigned int GetReceiveDataTimeout(void) = 0;

  // get protocol instance ID
  // @return : GUID, which represents instance identifier or GUID_NULL if error
  virtual GUID GetInstanceId(void) = 0;

  // get protocol maximum open connection attempts
  // @return : maximum attempts of opening connections or UINT_MAX if error
  virtual unsigned int GetOpenConnectionMaximumAttempts(void) = 0;

  // gets stream name provided by protocol
  // this method is called after successfully opened connection
  // caller is responsible for deleting collection after use
  // @return : reference to CStringCollection or NULL if error
  virtual CStringCollection *GetStreamNames(void) = 0;

  // request protocol implementation to receive data from specified time
  // @param time : the requested start time (zero is start of stream)
  // @return : S_OK if successful, error code otherwise
  virtual HRESULT ReceiveDataFromTimestamp(REFERENCE_TIME time) = 0;
};

// defines interface for stream protocol implementation
// each stream protocol implementation will be in separate library and MUST implement this interface
struct IProtocol : public IBaseProtocol
{
public:
  // initialize protocol implementation with configuration parameters
  // @param filter : the url source filter initializing protocol
  // @param : the reference to configuration parameters
  // @return : STATUS_OK if successfull
  virtual int Initialize(IOutputStream *filter, CParameterCollection *configuration) = 0;

  // clear current session before running ParseUrl() method
  // @return : STATUS_OK if successfull
  virtual int ClearSession(void) = 0;

  // parse given url to internal variables for specified protocol
  // errors should be logged to log file
  // @param url : the url to parse
  // @param parameters : the reference to collection of parameters
  // @return : STATUS_OK if successfull
  virtual int ParseUrl(const TCHAR *url, const CParameterCollection *parameters) = 0;

  // open connection
  // errors should be logged to log file
  // @return : STATUS_OK if successfull
  virtual int OpenConnection(void) = 0;

  // close connection
  // errors should be logged to log file
  virtual void CloseConnection(void) = 0;

  // receive data and stores them into internal buffer
  // @param shouldExit : the reference to variable specifying if method have to be finished immediately
  virtual void ReceiveData(bool *shouldExit) = 0;
};

typedef IProtocol* PIProtocol;

extern "C"
{
  PIProtocol CreateProtocolInstance(CParameterCollection *configuration);
  typedef PIProtocol (*CREATEPROTOCOLINSTANCE)(void);

  void DestroyProtocolInstance(PIProtocol pProtocol);
  typedef void (*DESTROYPROTOCOLINSTANCE)(PIProtocol);
}

#endif