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

#ifndef __ASYNCSOURCE_DEFINED
#define __ASYNCSOURCE_DEFINED

#include "AsyncSourceStream.h"
#include "ProtocolInterface.h"
#include "Download.h"

#include <streams.h>

// the class that will handle each pin
class CAsyncSourceStream;

class CAsyncSource : public CBaseFilter, public IBaseProtocol, public IDownloadCallback
{
public:
  // initialise the pin count for the filter
  // the user will create the pins in the derived class
  CAsyncSource(__in_opt LPCSTR pName, __inout_opt LPUNKNOWN lpunk, CLSID clsid, __inout HRESULT *phr);
  CAsyncSource(__in_opt LPCSTR pName, __inout_opt LPUNKNOWN lpunk, CLSID clsid);

  ~CAsyncSource();

  // returns the number of pins this filter has
  int GetPinCount(void);
  // return a non-addref'd pointer to pin n
  // needed by CBaseFilter
  CBasePin *GetPin(int n);

  CCritSec*	GetStateLock(void);

  // add a new pin
  HRESULT AddPin(__in CAsyncSourceStream *);
  // remove a pin - pStream is NOT deleted
  HRESULT RemovePin(__in CAsyncSourceStream *);

  // set *ppPin to the IPin* that has the id Id or to NULL if the Id cannot be matched
  STDMETHODIMP FindPin(LPCWSTR Id, __deref_out IPin ** ppPin);

  // return the number of the pin with this IPin* or -1 if none
  int FindPinNumber(__in IPin *iPin);

protected:
  // the number of pins on this filter
  // updated by CAsyncSourceStream constructors and destructors
  int m_iPins;       

  // the pins on this filter
  CAsyncSourceStream **m_paStreams;

  // lock this to serialize function accesses to the filter state
  CCritSec m_cStateLock;	
};

#endif