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

#ifndef __RANGESSUPPORTED_DEFINED
#define __RANGESSUPPORTED_DEFINED

#include "MPUrlSourceSplitterExports.h"

#include <streams.h>

class MPURLSOURCESPLITTER_API CRangesSupported
{
public:
  CRangesSupported(void);
  ~CRangesSupported(void);

  // tests if ranges are supported
  // @return : true if ranges are supported, false otherwise
  bool AreRangesSupported(void);

  // returns query result if ranges are supported
  // @return : S_OK if query was successful, E_PENDING if query was successful but it is unknown if ranges are supported, other error code if error
  HRESULT GetQueryResult(void);

  // tests if query result is pending
  // @return : true if query result is pending, false otherwise
  bool IsQueryPending(void);

  // tests if query result is successfully completed
  // @return : true if query result is successfully completed, false otherwise
  bool IsQueryCompleted(void);

  // tests if query result is error
  // @return : true if query result is error, false otherwise
  bool IsQueryError(void);

  // sets if ranges are supported
  // @param rangesSupported : true if ranges are supported, false otherwise
  void SetRangesSupported(bool rangesSupported);

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
  bool rangesSupported;
  HRESULT queryResult;
  bool filterConnectedToAnotherPin;
};

#endif