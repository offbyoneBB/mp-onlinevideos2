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

#include "AkamaiFlashInstance.h"
#include "conversions.h"
#include "base64.h"

#include <wchar.h>

CAkamaiFlashInstance::CAkamaiFlashInstance(CLogger *logger, const wchar_t *instanceName, const wchar_t *swfFilePath)
  : CFlashInstance(logger, instanceName, swfFilePath)
{
  this->error = NULL;
  this->decryptorErrorCode = UINT_MAX;
  this->decryptorError = NULL;
}

CAkamaiFlashInstance::~CAkamaiFlashInstance(void)
{
  FREE_MEM(this->error);
  FREE_MEM(this->decryptorError);
}

/* get methods */

AkamaiDecryptorState CAkamaiFlashInstance::GetState(void)
{
  wchar_t *queryResult = this->GetResult(L"<invoke name=\"GetState\" returntype=\"xml\"></invoke>");
  AkamaiDecryptorState result = AkamaiDecryptorState_Undefined;

  if (queryResult != NULL)
  {
    if (wcscmp(queryResult, L"NotInitialized") == 0)
    {
      result = AkamaiDecryptorState_NotInitialized;
    }
    else if (wcscmp(queryResult, L"Pending") == 0)
    {
      result = AkamaiDecryptorState_Pending;
    }
    else if (wcscmp(queryResult, L"Ready") == 0)
    {
      result = AkamaiDecryptorState_Ready;
    }
    else if (wcscmp(queryResult, L"Error") == 0)
    {
      result = AkamaiDecryptorState_Error;

      // we are in error state, get error text
      FREE_MEM(this->error);
      this->error = this->GetResult(L"<invoke name=\"GetError\" returntype=\"xml\"></invoke>");
    }
  }

  FREE_MEM(queryResult);
  return result;
}

const wchar_t *CAkamaiFlashInstance::GetError(void)
{
  return this->error;
}

wchar_t *CAkamaiFlashInstance::GetDecryptedData(const wchar_t *key, const wchar_t *encryptedData)
{
  FREE_MEM(this->decryptorError);

  wchar_t *result = NULL;

  // get decrypted data
  wchar_t *query = FormatString(L"<invoke name=\"GetDecryptedData\" returntype=\"xml\"><arguments><string>%s</string><string>%s</string></arguments></invoke>", key, encryptedData);
  result = this->GetResult(query);
  FREE_MEM(query);

  // get decryptor error code
  wchar_t *queryResult = this->GetResult(L"<invoke name=\"GetDecryptorErrorCode\" returntype=\"xml\"><arguments></arguments></invoke>");
  // parse string to uint32_t
  this->decryptorErrorCode = GetValueUnsignedIntW(queryResult, UINT_MAX);
  FREE_MEM(queryResult);

  // get decryptor error
  this->decryptorError = this->GetResult(L"<invoke name=\"GetDecryptorErrorString\" returntype=\"xml\"><arguments></arguments></invoke>");

  return result;
}

int SpecialIndexOf(const wchar_t *buffer, unsigned int start, wchar_t c)
{
  int index = 0;
  while ((buffer[start + index] != L'\0') && (buffer[start + index] != c))
  {
    index++;
  }
  return (buffer[start + index] != L'\0') ? index : (-1);
}

CDecryptedDataCollection *CAkamaiFlashInstance::GetDecryptedData(const wchar_t *key, CEncryptedDataCollection *encryptedDataCollection)
{
  CDecryptedDataCollection *result = new CDecryptedDataCollection();
  FREE_MEM(this->decryptorError);

  // get total request length
  unsigned int keyLength = wcslen(key);
  unsigned int length = 68 + 17 + keyLength + 30;

  // each request is separated by '|'
  for (unsigned int i = 0; i < encryptedDataCollection->Count(); i++)
  {
    length += encryptedDataCollection->GetItem(i)->GetEncryptedLength() + 1;
  }
  // for memory space we need one extra null character at end
  length++;

  // fastest string concatenation is direct memory copying

  ALLOC_MEM_DEFINE_SET(request, wchar_t, length, 0);
  if (request != NULL)
  {
    // add first part of request
    unsigned int position = 0;
    wmemcpy(request + position, L"<invoke name=\"GetDecryptedData\" returntype=\"xml\"><arguments><string>", 68);
    position += 68;

    wmemcpy(request + position, key, keyLength);
    position += keyLength;

    wmemcpy(request + position, L"</string><string>", 17);
    position += 17;

    // concatenate collection
    for (unsigned int i = 0; i < encryptedDataCollection->Count(); i++)
    {
      wmemcpy(request + position, encryptedDataCollection->GetItem(i)->GetEncryptedData(), encryptedDataCollection->GetItem(i)->GetEncryptedLength());
      position += encryptedDataCollection->GetItem(i)->GetEncryptedLength();

      wmemcpy(request + position, L"|", 1);
      position++;
    }

    // add end of request
    wmemcpy(request + position, L"</string></arguments></invoke>", 30);
    position += 30;

    wchar_t *queryResult = this->GetResult(request);

    // result data are delimited by '|'
    // each response consist of 4 fields : decrypted data (BASE64 encoding), error code, length of error text, error text
    // at end are 4 empty fields

    position = 0;
    bool continueProcessing = true;
    for (unsigned int i = 0; (continueProcessing && (i < encryptedDataCollection->Count())); i++)
    {
      int index = SpecialIndexOf(queryResult, position, L'|');
      continueProcessing &= (index != -1);
      if (continueProcessing)
      {
        index += position;

        uint8_t *decodedData = NULL;
        unsigned int decodedDataLength = 0;
        unsigned int errorCode = 0;
        unsigned int errorLength = 0;
        wchar_t *error = NULL;

        // get first part = decoded data in BASE64 encoding
        if (continueProcessing)
        {
          length = index - position + 1;
          ALLOC_MEM_DEFINE_SET(decodedDataBase64, wchar_t, length, 0);
          continueProcessing &= (decodedDataBase64 != NULL);

          if (continueProcessing)
          {
            wmemcpy(decodedDataBase64, queryResult + position, (length - 1));

            continueProcessing &= SUCCEEDED(base64_decode(decodedDataBase64, &decodedData, &decodedDataLength));
          }

          FREE_MEM(decodedDataBase64);
          position = index + 1;
        }

        // get second part = error code
        if (continueProcessing)
        {
          index = SpecialIndexOf(queryResult, position, L'|');
          continueProcessing &= (index != -1);
        }

        if (continueProcessing)
        {
          index += position;
          length = index - position + 1;
          ALLOC_MEM_DEFINE_SET(errorCodeString, wchar_t, length, 0);
          continueProcessing &= (errorCodeString != NULL);

          if (continueProcessing)
          {
            wmemcpy(errorCodeString, queryResult + position, (length - 1));
            errorCode = GetValueUnsignedInt(errorCodeString, 0);
          }

          FREE_MEM(errorCodeString);
          position = index + 1;
        }

        // get third part = length of error
        if (continueProcessing)
        {
          index = SpecialIndexOf(queryResult, position, L'|');
          continueProcessing &= (index != -1);
        }

        if (continueProcessing)
        {
          index += position;
          length = index - position + 1;
          ALLOC_MEM_DEFINE_SET(errorLengthString, wchar_t, length, 0);
          continueProcessing &= (errorLengthString != NULL);

          if (continueProcessing)
          {
            wmemcpy(errorLengthString, queryResult + position, (length - 1));
            errorLength = GetValueUnsignedInt(errorLengthString, 0);
          }

          FREE_MEM(errorLengthString);
          position = index + 1;
        }

        // get fourth path = error
        if (continueProcessing)
        {
          error = ALLOC_MEM_SET(error, wchar_t, (errorLength + 1), 0);
          continueProcessing &= (error != NULL);

          if (continueProcessing)
          {
            wmemcpy(error, queryResult + position, errorLength);
          }

          position += errorLength + 1;
        }

        if (continueProcessing)
        {
          // now we have all data
          continueProcessing &= result->Add(decodedData, decodedDataLength, errorCode, error);
        }

        FREE_MEM(decodedData);
        FREE_MEM(error);
      }
    }

    if (!continueProcessing)
    {
      result->Clear();
    }

    FREE_MEM(queryResult);
  }
  FREE_MEM(request);

  return result;
}

uint32_t CAkamaiFlashInstance::GetDecryptorErrorCode(void)
{
  return this->decryptorErrorCode;
}

const wchar_t *CAkamaiFlashInstance::GetDecryptorError(void)
{
  return this->decryptorError;
}

/* set methods */

void CAkamaiFlashInstance::SetDecryptionModuleUrl(const wchar_t *url)
{
  // set decryption module url
  wchar_t *query = FormatString(L"<invoke name=\"SetDecryptionModuleUrl\" returntype=\"xml\"><arguments><string>%s</string></arguments></invoke>", url);
  wchar_t *queryResult = this->GetResult(query);
  FREE_MEM(queryResult);
  FREE_MEM(query);
  
  // initialize decryption module
  queryResult = this->GetResult(L"<invoke name=\"Init\" returntype=\"xml\"></invoke>");
  FREE_MEM(queryResult);
}

/* other methods */

HRESULT CAkamaiFlashInstance::Initialize(void)
{
  return __super::Initialize();
}

