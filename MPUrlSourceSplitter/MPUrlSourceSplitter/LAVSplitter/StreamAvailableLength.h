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

#ifndef __STREAMAVAILABLELENGTH_DEFINED
#define __STREAMAVAILABLELENGTH_DEFINED

#include "MPUrlSourceSplitterExports.h"

class MPURLSOURCESPLITTER_API CStreamAvailableLength
{
public:
  CStreamAvailableLength(void);
  ~CStreamAvailableLength(void);

  // returns query result if ranges are supported
  // @return : S_OK if query was successful, other error code if error
  HRESULT GetQueryResult(void);

  // gets available length
  // @return : the available length value
  LONGLONG GetAvailableLength(void);

  // tests if query result is successfully completed
  // @return : true if query result is successfully completed, false otherwise
  bool IsQueryCompleted(void);

  // tests if query result is error
  // @return : true if query result is error, false otherwise
  bool IsQueryError(void);

  // sets available length
  // @param availableLength : the available length value
  void SetAvailableLength(LONGLONG availableLength);

  // sets query result
  // @param queryResult : the result of query
  void SetQueryResult(HRESULT queryResult);

  // tests if filer is connected to another pin
  // @return : true if connected, false otherwise
  bool IsFilterConnectedToAnotherPin(void);

  // sets if filter is connected to another pin
  // @param filterConnectedToAnotherPin : true if connected, false otherwise
  void SetFilterConnectedToAnotherPin(bool filterConnectedToAnotherPin);

private:
  LONGLONG availableLength;
  HRESULT queryResult;
  bool filterConnectedToAnotherPin;
};

#endif