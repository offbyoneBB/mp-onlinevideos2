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

#ifndef __MPURLSOURCESPLITTER_AFHS_DECRYPTION_AKAMAI_DEFINED
#define __MPURLSOURCESPLITTER_AFHS_DECRYPTION_AKAMAI_DEFINED

#include "MPUrlSourceSplitter_Afhs_Decryption_akamai_Exports.h"

#include "IAfhsDecryptionPlugin.h"

#define DECRYPTION_NAME                                                       L"AFHS_AKAMAI"

// This class is exported from the MPUrlSourceSplitter_Afhs_Decryption_Akamai.dll
class MPURLSOURCESPLITTER_AFHS_DECRYPTION_AKAMAI_API CMPUrlSourceSplitter_Afhs_Decryption_Akamai : public IAfhsDecryptionPlugin
{
public:
  // constructor
  // create instance of CMPUrlSourceSplitter_Afhs_Decryption_Akamai class
  CMPUrlSourceSplitter_Afhs_Decryption_Akamai(CParameterCollection *configuration);

  // destructor
  ~CMPUrlSourceSplitter_Afhs_Decryption_Akamai(void);

  // IAfhsDecryptionPlugin interface

  // check if decryption plugin supports decrypting segments and fragments
  // @param segmentsFragments : collection of segments and fragments
  // @result : one of DecryptionResult values
  DecryptionResult Supported(CSegmentFragmentCollection *segmentsFragments);

  // IAfhsSimpleDecryptionPlugin interface

  // clears decryption plugin session
  // @return : S_OK if successfull
  HRESULT ClearSession(void);

  // process segments and fragments
  // @param segmentsFragments : collection of segments and fragments
  // @result : S_OK if successful, error code otherwise
  HRESULT ProcessSegmentsAndFragments(CSegmentFragmentCollection *segmentsFragments);

  // IPlugin interface

  // return reference to null-terminated string which represents plugin name
  // errors should be logged to log file and returned NULL
  // @return : reference to null-terminated string
  const wchar_t *GetName(void);

  // get plugin instance ID
  // @return : GUID, which represents instance identifier or GUID_NULL if error
  GUID GetInstanceId(void);

  // initialize plugin implementation with configuration parameters
  // @param configuration : the reference to additional configuration parameters (created by plugin's hoster class)
  // @return : S_OK if successfull
  HRESULT Initialize(PluginConfiguration *configuration);

protected:
  CLogger *logger;

  // holds various parameters supplied by caller
  CParameterCollection *configurationParameters;
};

#endif