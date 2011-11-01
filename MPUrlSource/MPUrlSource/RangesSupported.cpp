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

#include "RangesSupported.h"

CRangesSupported::CRangesSupported(void)
{
  this->queryResult = E_FAIL;
  this->rangesSupported = false;
  this->filterConnectedToAnotherPin = false;
}

CRangesSupported::~CRangesSupported(void)
{
}

bool CRangesSupported::AreRangesSupported(void)
{
  return (this->IsQueryCompleted() && (this->rangesSupported));
}

HRESULT CRangesSupported::GetQueryResult(void)
{
  return this->queryResult;
}

bool CRangesSupported::IsQueryPending(void)
{
  return (this->queryResult == E_PENDING);
}

bool CRangesSupported::IsQueryCompleted(void)
{
  return (this->queryResult == S_OK);
}

bool CRangesSupported::IsQueryError(void)
{
  return (!(this->IsQueryCompleted() || this->IsQueryPending()));
}

void CRangesSupported::SetRangesSupported(bool rangesSupported)
{
  this->rangesSupported = rangesSupported;
}

void CRangesSupported::SetQueryResult(HRESULT queryResult)
{
  this->queryResult = queryResult;
}

bool CRangesSupported::IsFilterConnectedToAnotherPin(void)
{
  return this->filterConnectedToAnotherPin;
}

void CRangesSupported::SetFilterConnectedToAnotherPin(bool filterConnectedToAnotherPin)
{
  this->filterConnectedToAnotherPin = filterConnectedToAnotherPin;
}