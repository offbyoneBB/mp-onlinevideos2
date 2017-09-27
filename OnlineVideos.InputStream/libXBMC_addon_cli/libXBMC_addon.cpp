/*
*      Copyright (C) 2012-2013 Team XBMC
*      http://xbmc.org
*
*  This Program is free software; you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation; either version 2, or (at your option)
*  any later version.
*
*  This Program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with XBMC; see the file COPYING.  If not, see
*  <http://www.gnu.org/licenses/>.
*
*/

#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string>
#include <sys/stat.h>
#include "stdafx.h"
#include "libXBMC_addon.h"

#ifdef _WIN32
#include <windows.h>
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT
#endif

#using "..\MediaPortalWrapper\bin\MediaPortalWrapper.dll"

using namespace std;
using namespace ADDON;
using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Generic;

using namespace MediaPortalWrapper;

extern "C"
{

  DLLEXPORT void* XBMC_register_me(void *hdl)
  {
    Utils::Logger::Log("XBMC_register_me");
    return (void*)1;
  }

  DLLEXPORT void XBMC_unregister_me(void *hdl, void* cb)
  {
    Utils::Logger::Log("XBMC_unregister_me");
  }

  DLLEXPORT void XBMC_log(void *hdl, void* cb, const addon_log_t loglevel, const char *msg)
  {
    Utils::Logger::Log(gcnew String(msg));
  }

  DLLEXPORT bool XBMC_get_setting(void *hdl, void* cb, const char* settingName, void *settingValue)
  {
    String^ strName = gcnew String(settingName);
    Utils::Logger::Log("XBMC_get_setting: " + strName);
    return Settings::SettingsWrapper::GetSetting(strName, (IntPtr)settingValue);
  }

  DLLEXPORT char* XBMC_translate_special(void *hdl, void* cb, const char* source)
  {
    Utils::Logger::Log("XBMC_translate_special: " + gcnew String(source));
    String^ path = Settings::SettingsWrapper::GetSettingString("DECRYPTERPATH");
    return (char*)Marshal::StringToHGlobalAnsi((String^)path).ToPointer();
  }

  DLLEXPORT void XBMC_queue_notification(void *hdl, void* cb, const queue_msg_t type, const char *msg)
  {
    Utils::Logger::Log("XBMC_queue_notification");
    Utils::Logger::Info(gcnew String(msg));
  }

  DLLEXPORT bool XBMC_wake_on_lan(void* hdl, void* cb, char* mac)
  {
    Utils::Logger::Log("XBMC_wake_on_lan");
    return false;
  }

  DLLEXPORT char* XBMC_unknown_to_utf8(void *hdl, void* cb, const char* str)
  {
    Utils::Logger::Log("XBMC_unknown_to_utf8: " + gcnew String(str));
    return (char*)(Marshal::StringToHGlobalAnsi(gcnew String(str))).ToPointer();
  }

  DLLEXPORT char* XBMC_get_localized_string(void *hdl, void* cb, int dwCode)
  {
    Utils::Logger::Log("XBMC_get_localized_string");
    return "";
  }

  DLLEXPORT char* XBMC_get_dvd_menu_language(void *hdl, void* cb)
  {
    Utils::Logger::Log("XBMC_get_dvd_menu_language");
    return "";
  }

  DLLEXPORT void XBMC_free_string(void* hdl, void* cb, char* str)
  {
    Utils::Logger::Log("XBMC_free_string");
    Marshal::FreeHGlobal((IntPtr)str);
  }

  DLLEXPORT void* XBMC_open_file(void *hdl, void* cb, const char* strFileName, unsigned int flags)
  {
    Utils::Logger::Log("XBMC_open_file");
    return NULL;
  }

  DLLEXPORT void* XBMC_open_file_for_write(void *hdl, void* cb, const char* strFileName, bool bOverWrite)
  {
    Utils::Logger::Log("XBMC_open_file_for_write");
    return NULL;
  }

  //DLLEXPORT ssize_t XBMC_read_file(void *hdl, void* cb, void* file, void* lpBuf, size_t uiBufSize)
  DLLEXPORT size_t XBMC_read_file(void *hdl, void* cb, void* file, void* lpBuf, size_t uiBufSize)
  {
    Utils::Logger::Log("XBMC_read_file");
    Filesystem::AbstractFile^ f;
    if (Filesystem::FileFactory::TryGetValue((IntPtr)file, f))
    {
      int numRead = f->Read((IntPtr)lpBuf, uiBufSize);
      if (numRead == 0)
        Utils::Logger::Log(" - total read: {0}", f->TotalBytesRead);
      return numRead;
    }
    return -1;
  }

  DLLEXPORT bool XBMC_read_file_string(void *hdl, void* cb, void* file, char *szLine, int iLineLength)
  {
    Utils::Logger::Log("XBMC_read_file_string");
    return false;
  }

  DLLEXPORT ssize_t XBMC_write_file(void *hdl, void* cb, void* file, const void* lpBuf, size_t uiBufSize)
  {
    Utils::Logger::Log("XBMC_write_file");
    return -1;
  }

  DLLEXPORT void XBMC_flush_file(void *hdl, void* cb, void* file)
  {
    Utils::Logger::Log("XBMC_flush_file");
    return;
  }

  DLLEXPORT int64_t XBMC_seek_file(void *hdl, void* cb, void* file, int64_t iFilePosition, int iWhence)
  {
    Utils::Logger::Log("XBMC_seek_file");
    return 0;
  }

  DLLEXPORT int XBMC_truncate_file(void *hdl, void* cb, void* file, int64_t iSize)
  {
    Utils::Logger::Log("XBMC_truncate_file");
    return 0;
  }

  DLLEXPORT int64_t XBMC_get_file_position(void *hdl, void* cb, void* file)
  {
    Utils::Logger::Log("XBMC_get_file_position");
    return 0;
  }

  DLLEXPORT int64_t XBMC_get_file_length(void *hdl, void* cb, void* file)
  {
    Utils::Logger::Log("XBMC_get_file_length");
    return 0;
  }

  DLLEXPORT double XBMC_get_file_download_speed(void *hdl, void* cb, void* file)
  {
    Filesystem::AbstractFile^ f;
    double downloadSpeed = 0.0f;
    if (Filesystem::FileFactory::TryGetValue((IntPtr)file, f))
      downloadSpeed = f->DownloadSpeed;

    Utils::Logger::Log("XBMC_get_file_download_speed: {0}", downloadSpeed);
    return downloadSpeed;
  }

  DLLEXPORT void XBMC_close_file(void *hdl, void* cb, void* file)
  {
    Utils::Logger::Log("XBMC_close_file");
    Filesystem::FileFactory::Remove((IntPtr)file);
    return;
  }

  DLLEXPORT int XBMC_get_file_chunk_size(void *hdl, void* cb, void* file)
  {
    Utils::Logger::Log("XBMC_get_file_chunk_size");
    return 0;
  }

  DLLEXPORT bool XBMC_file_exists(void *hdl, void* cb, const char *strFileName, bool bUseCache)
  {
    String^ path = gcnew String(strFileName);
    Utils::Logger::Log("XBMC_file_exists: {0}", path);
    return false;
  }

  DLLEXPORT int XBMC_stat_file(void *hdl, void* cb, const char *strFileName, struct __stat64* buffer)
  {
    String^ path = gcnew String(strFileName);
    Utils::Logger::Log("XBMC_stat_file: {0}", path);
    return -1;
  }

  DLLEXPORT bool XBMC_delete_file(void *hdl, void* cb, const char *strFileName)
  {
    String^ path = gcnew String(strFileName);
    Utils::Logger::Log("XBMC_delete_file: {0}", path);
    return true;
  }

  DLLEXPORT bool XBMC_can_open_directory(void *hdl, void* cb, const char* strPath)
  {
    String^ path = gcnew String(strPath);
    Utils::Logger::Log("XBMC_can_open_directory: {0}", path);
    return true;
  }

  DLLEXPORT bool XBMC_create_directory(void *hdl, void* cb, const char *strPath)
  {
    String^ path = gcnew String(strPath);
    Utils::Logger::Log("XBMC_create_directory: {0}", path);
    if (!Directory::Exists(path))
      Directory::CreateDirectory(path);
    return true;
  }

  DLLEXPORT bool XBMC_directory_exists(void *hdl, void* cb, const char *strPath)
  {
    String^ path = gcnew String(strPath);
    Utils::Logger::Log("XBMC_directory_exists: {0}", path);
    return Directory::Exists(path);
  }

  DLLEXPORT bool XBMC_remove_directory(void *hdl, void* cb, const char *strPath)
  {
    String^ path = gcnew String(strPath);
    Utils::Logger::Log("XBMC_remove_directory: {0}", path);
    return false;
  }

  DLLEXPORT bool XBMC_get_directory(void *hdl, void* cb, const char *strPath, const char* mask, VFSDirEntry** items, unsigned int* num_items)
  {
    String^ path = gcnew String(strPath);
    Utils::Logger::Log("XBMC_get_directory: {0}", path);
    return Filesystem::DirectoryHelper::GetDirectory(path, gcnew String(mask), (IntPtr)items, (IntPtr)num_items);
  }

  DLLEXPORT void XBMC_free_directory(void *hdl, void* cb, VFSDirEntry* items, unsigned int num_items)
  {
    Utils::Logger::Log("XBMC_free_directory");
    Filesystem::DirectoryHelper::FreeDirectory((IntPtr)items, num_items);
    return;
  }

  DLLEXPORT void* XBMC_curl_create(void *hdl, void* cb, const char* strURL)
  {
    Utils::Logger::Log("XBMC_curl_create: {0}", gcnew String(strURL));
    Filesystem::UrlSourceHttpClient^ urlSource = gcnew Filesystem::UrlSourceHttpClient();
    if (!urlSource->UrlCreate(gcnew String(strURL)))
    {
      delete urlSource;
      return nullptr;
    }
    return (void*)Filesystem::FileFactory::AddFile(urlSource);
  }

  DLLEXPORT bool XBMC_curl_add_option(void *hdl, void* cb, void *file, XFILE::CURLOPTIONTYPE type, const char* name, const char *value)
  {
    Utils::Logger::Log("XBMC_curl_add_option: {0} : {1}", gcnew String(name), gcnew String(value));
    Filesystem::AbstractFile^ f;
    if (Filesystem::FileFactory::TryGetValue((IntPtr)file, f))
      return f->AddOption((Filesystem::CURLOPTIONTYPE)type, gcnew String(name), gcnew String(value));
    return false;
  }

  DLLEXPORT bool XBMC_curl_open(void *hdl, void* cb, void *file, unsigned int flags)
  {
    Utils::Logger::Log("XBMC_curl_open");
    Filesystem::AbstractFile^ f;
    if (Filesystem::FileFactory::TryGetValue((IntPtr)file, f))
      return f->Open(flags);
    return false;
  }
};
