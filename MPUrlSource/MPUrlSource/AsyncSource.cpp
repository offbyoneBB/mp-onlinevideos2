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
#include "AsyncSource.h"

CAsyncSource::CAsyncSource(__in_opt LPCSTR pName, __inout_opt LPUNKNOWN lpunk, CLSID clsid)
  : CBaseFilter(pName, lpunk, &m_cStateLock, clsid),
  m_iPins(0),
  m_paStreams(NULL)
{
}

CAsyncSource::CAsyncSource(__in_opt LPCSTR pName, __inout_opt LPUNKNOWN lpunk, CLSID clsid, __inout HRESULT *phr)
  : CBaseFilter(pName, lpunk, &m_cStateLock, clsid),
  m_iPins(0),
  m_paStreams(NULL)
{
  UNREFERENCED_PARAMETER(phr);
}

CAsyncSource::~CAsyncSource()
{
  // free our pins and pin array
  while (m_iPins != 0)
  {
    // deleting the pins causes them to be removed from the array...
    delete m_paStreams[m_iPins - 1];
  }

  ASSERT(m_paStreams == NULL);
}

HRESULT CAsyncSource::AddPin(__in CAsyncSourceStream *pStream)
{
  CAutoLock lock(&m_cStateLock);

  //  Allocate space for this pin and the old ones
  CAsyncSourceStream **paStreams = new CAsyncSourceStream *[m_iPins + 1];
  if (paStreams == NULL)
  {
    return E_OUTOFMEMORY;
  }

  if (m_paStreams != NULL)
  {
    CopyMemory((PVOID)paStreams, (PVOID)m_paStreams, m_iPins * sizeof(m_paStreams[0]));
    paStreams[m_iPins] = pStream;
    delete [] m_paStreams;
  }
  m_paStreams = paStreams;
  m_paStreams[m_iPins] = pStream;
  m_iPins++;

  return S_OK;
}

HRESULT CAsyncSource::RemovePin(__in CAsyncSourceStream *pStream)
{
  int i;
  for (i = 0; i < m_iPins; i++)
  {
    if (m_paStreams[i] == pStream)
    {
      if (m_iPins == 1)
      {
        delete [] m_paStreams;
        m_paStreams = NULL;
      }
      else
      {
        // no need to reallocate
        while (++i < m_iPins)
        {
          m_paStreams[i - 1] = m_paStreams[i];
        }
      }
      m_iPins--;
      return S_OK;
    }
  }
  return S_FALSE;
}

STDMETHODIMP CAsyncSource::FindPin(LPCWSTR Id, __deref_out IPin **ppPin)
{
  CheckPointer(ppPin, E_POINTER);
  ValidateReadWritePtr(ppPin, sizeof(IPin *));
  // The -1 undoes the +1 in QueryId and ensures that totally invalid
  // strings (for which WstrToInt delivers 0) give a deliver a NULL pin.
  int i = WstrToInt(Id) -1;
  *ppPin = GetPin(i);
  if (*ppPin != NULL)
  {
    (*ppPin)->AddRef();
    return NOERROR;
  }
  else
  {
    return VFW_E_NOT_FOUND;
  }
}

int CAsyncSource::FindPinNumber(__in IPin *iPin)
{
  for(int i = 0; i < m_iPins; ++i)
  {
    if ((IPin *)(m_paStreams[i])==iPin)
    {
      return i;
    }
  }
  return -1;
}

int CAsyncSource::GetPinCount(void)
{
  CAutoLock lock(&m_cStateLock);
  return m_iPins;
}

CBasePin *CAsyncSource::GetPin(int n)
{
  CAutoLock lock(&m_cStateLock);

  // n must be in the range 0..m_iPins-1
  // if m_iPins > n  && n >= 0 it follows that m_iPins > 0
  // which is what used to be checked (i.e. checking that we have a pin)
  if ((n >= 0) && (n < m_iPins))
  {
    ASSERT(m_paStreams[n]);
    return m_paStreams[n];
  }
  return NULL;
}

CCritSec*	CAsyncSource::GetStateLock(void)
{
  return &this->m_cStateLock;
}
