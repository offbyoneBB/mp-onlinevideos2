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
#include "RangesSupported.h"
#include "StreamAvailableLength.h"

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

  // notifies output stream that end of stream was reached
  // @param outputPinName : the name of output pin (the output pin name must be value from values returned from GetStreamNames() method of IProtocol interface
  // @return : STATUS_OK if successful
  virtual int EndOfStreamReached(const TCHAR *outputPinName) = 0;
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

  // request protocol implementation to receive data from specified time to specified time
  // @param startTime : the requested start time (zero is start of stream)
  // @param endTime : the requested end time, if endTime is lower or equal to startTime than endTime is not specified
  // @return : S_OK if successful, error code otherwise
  virtual HRESULT ReceiveDataFromTimestamp(REFERENCE_TIME startTime, REFERENCE_TIME endTime) = 0;

  // request protocol implementation to cancel the stream reading operation
  // @return : S_OK if successful
  virtual HRESULT AbortStreamReceive() = 0;

  // retrieves the progress of the stream reading operation
  // @param total : reference to a variable that receives the length of the entire stream, in bytes
  // @param current : reference to a variable that receives the length of the downloaded portion of the stream, in bytes
  // @return : S_OK if successful, VFW_S_ESTIMATED if returned values are estimates, E_UNEXPECTED if unexpected error
  virtual HRESULT QueryStreamProgress(LONGLONG *total, LONGLONG *current) = 0;
  
  // retrieves available lenght of stream
  // @param available : reference to instance of class that receives the available length of stream, in bytes
  // @return : S_OK if successful, other error codes if error
  virtual HRESULT QueryStreamAvailableLength(CStreamAvailableLength *availableLength) = 0;

  // queries protocol implementation if ranges are supported
  // @param rangesSupported : reference to instance of class that receives if ranges are supported
  // @return : S_OK if successful
  virtual HRESULT QueryRangesSupported(CRangesSupported *rangesSupported) = 0;
};

// defines interface for stream protocol implementation
// each stream protocol implementation will be in separate library and MUST implement this interface
struct IProtocol : public IBaseProtocol
{
public:
  // initialize protocol implementation with configuration parameters
  // @param filter : the url source filter initializing protocol
  // @param : the reference to additional configuration parameters (can be same parameters which were passed while creating instance), can be NULL
  // @return : STATUS_OK if successfull
  virtual int Initialize(IOutputStream *filter, CParameterCollection *configuration) = 0;

  // clear current session before running ParseUrl() method
  // @return : STATUS_OK if successfull
  virtual int ClearSession(void) = 0;

  // parse given url to internal variables for specified protocol
  // errors should be logged to log file
  // @param url : the url to parse
  // @param parameters : the reference to collection of configuration parameters (can be same parameters which were passed while creating instance and initializing), can be NULL
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
  typedef PIProtocol (*CREATEPROTOCOLINSTANCE)(CParameterCollection *configuration);

  void DestroyProtocolInstance(PIProtocol pProtocol);
  typedef void (*DESTROYPROTOCOLINSTANCE)(PIProtocol);
}

#endif