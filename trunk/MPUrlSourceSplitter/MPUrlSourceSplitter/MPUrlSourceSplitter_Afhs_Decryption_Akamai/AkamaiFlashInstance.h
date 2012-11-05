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

#ifndef __AKAMAI_FLASH_INSTANCE_DEFINED
#define __AKAMAI_FLASH_INSTANCE_DEFINED

#include "FlashInstance.h"
#include "EncryptedDataCollection.h"
#include "DecryptedDataCollection.h"

enum AkamaiDecryptorState
{
  AkamaiDecryptorState_NotInitialized,
  AkamaiDecryptorState_Pending,
  AkamaiDecryptorState_Ready,
  AkamaiDecryptorState_Error,
  AkamaiDecryptorState_Undefined
};

// define akamai decryptor error codes
#define AKAMAI_DECRYPTOR_ERROR_CODE_SUCCESS                                   1

class CAkamaiFlashInstance : CFlashInstance
{
public:
  CAkamaiFlashInstance(CLogger *logger, const wchar_t *instanceName, const wchar_t *swfFilePath);
  virtual ~CAkamaiFlashInstance(void);

  /* get methods */

  // gets instance state
  // @return : one of AkamaiDecryptorState values
  AkamaiDecryptorState GetState(void);

  // gets error text if instance is in AkamaiDecryptorState_Error state
  const wchar_t *GetError(void);

  // get decrypted data

  // gets decryptor error code
  // @return : error code or UINT_MAX if unknown
  uint32_t GetDecryptorErrorCode(void);

  // gets decryptor error text
  // @return : error text
  const wchar_t *GetDecryptorError(void);

  // gets decrypted data
  // @param key : decryption key in BASE64 encoding
  // @param encryptedData : encrypted data in BASE64 encoding
  // @return : decrypted data in BASE64 encoding
  wchar_t *GetDecryptedData(const wchar_t *key, const wchar_t *encryptedData);

  // gets decrypted data
  // @param key : decryption key in BASE64 encoding
  // @param encryptedDataCollection : encrypted data collection in BASE64 encoding
  // @return : decrypted data collection in BASE64 encoding
  CDecryptedDataCollection *GetDecryptedData(const wchar_t *key, CEncryptedDataCollection *encryptedDataCollection);

  /* set methods */

  // sets decryption module url
  // @param url : decryption module url
  void SetDecryptionModuleUrl(const wchar_t *url);

  /* other methods */

  // initializes akamai flash instance
  // @return : S_OK if successful, false otherwise
  HRESULT Initialize(void);

protected:
  // holds error if instance is in AkamaiDecryptorState_Error state
  wchar_t *error;

  // holds decryptor error code after decryption
  uint32_t decryptorErrorCode;
  // holds decryptor error text after decryption
  wchar_t *decryptorError;
};

#endif