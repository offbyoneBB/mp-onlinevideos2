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

#ifndef __FILTERINTERFACE_DEFINED
#define __FILTERINTERFACE_DEFINED

#include "ISeeking.h"
#include "Logger.h"

#include <stdint.h>

// define seeking capabilities of protocol
// protocol doesn't support seeking
#define SEEKING_METHOD_NONE                                             0
// protocol supports seeking by position (in bytes)
#define SEEKING_METHOD_POSITION                                         1
// protocol supports seeking by time (in ms)
#define SEEKING_METHOD_TIME                                             2

// defines interface for filter
struct IFilter : public ISeeking
{
  // gets logger instance
  // @return : logger instance or NULL if error
  virtual CLogger *GetLogger(void) = 0;
};

#endif