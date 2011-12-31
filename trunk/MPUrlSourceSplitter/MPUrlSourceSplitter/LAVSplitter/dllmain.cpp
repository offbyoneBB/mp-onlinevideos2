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

// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"

#include "Logger.h"

// Initialize the GUIDs
#include <InitGuid.h>

#include <qnetwork.h>
#include "LAVSplitter.h"
#include "moreuuids.h"

#include "registry.h"
#include "IGraphRebuildDelegate.h"


#define g_wszPullSource                                           L"MediaPortal Url Source Splitter"

#define MODULE_NAME                                               L"DLLMAIN"

#define METHOD_DLL_REGISTER_SERVER_NAME                           L"DllRegisterServer()"
#define METHOD_DLL_UNREGISTER_SERVER_NAME                         L"DllUnregisterServer()"
#define METHOD_DLL_MAIN_NAME                                      L"DllMain()"


// Filter setup data
const AMOVIESETUP_MEDIATYPE sudMediaTypes[] =
{
  &MEDIATYPE_Stream,              // Major type
  &MEDIASUBTYPE_NULL              // Minor type
};

const AMOVIESETUP_PIN sudOutputPins[] = 
{
  {
    L"Output",            // pin name
      FALSE,              // is rendered?    
      TRUE,               // is output?
      FALSE,              // zero instances allowed?
      TRUE,               // many instances allowed?
      &CLSID_NULL,        // connects to filter (for bridge pins)
      NULL,               // connects to pin (for bridge pins)
      0,                  // count of registered media types
      NULL                // list of registered media types
  }
};

const AMOVIESETUP_FILTER sudFilterReg =
{
  &__uuidof(CLAVSplitter),          // filter clsid
  g_wszPullSource,                  // filter name
  MERIT_NORMAL,                     // merit
  1,                                // count of registered pins
  sudOutputPins                     // list of pins to register
};

// List of class IDs and creator functions for the class factory. This
// provides the link between the OLE entry point in the DLL and an object
// being created. The class factory will call the static CreateInstance.
// We provide a set of filters in this one DLL.

CFactoryTemplate g_Templates[1] = 
{
  { 
    g_wszPullSource,                      // Name
    sudFilterReg.clsID,                   // CLSID
    CreateInstance<CLAVSplitter>,         // Method to create an instance
    NULL,                                 // Initialization function
    &sudFilterReg                         // Set-up information (for filters)
  }
};

int g_cTemplates = sizeof(g_Templates) / sizeof(g_Templates[0]);

extern "C" BOOL WINAPI DllEntryPoint(HINSTANCE, ULONG, LPVOID);

STDAPI DllRegisterServer()
{
  CLogger logger(NULL);
  logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DLL_REGISTER_SERVER_NAME);

  return AMovieDllRegisterServer2(TRUE);
}

STDAPI DllUnregisterServer()
{
  CLogger logger(NULL);
  logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DLL_UNREGISTER_SERVER_NAME);

  return AMovieDllRegisterServer2(FALSE);
}

BOOL APIENTRY DllMain(HMODULE hModule,
  DWORD  ul_reason_for_call,
  LPVOID lpReserved
  )
{
  CLogger logger(NULL);
  logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DLL_MAIN_NAME);

  switch (ul_reason_for_call)
  {
  case DLL_PROCESS_ATTACH:
    logger.Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_DLL_MAIN_NAME, L"DLL_PROCESS_ATTACH");
    break;
  case DLL_THREAD_ATTACH:
    logger.Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_DLL_MAIN_NAME, L"DLL_THREAD_ATTACH");
    break;
  case DLL_THREAD_DETACH:
    logger.Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_DLL_MAIN_NAME, L"DLL_THREAD_DETACH");
    break;
  case DLL_PROCESS_DETACH:
    logger.Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_DLL_MAIN_NAME, L"DLL_PROCESS_DETACH");
    break;
  }

  BOOL result = DllEntryPoint((HINSTANCE)(hModule), ul_reason_for_call, lpReserved);

  logger.Log(LOGGER_INFO, L"%s: %s: result: %d", MODULE_NAME, METHOD_DLL_MAIN_NAME, result);
  logger.Log(LOGGER_INFO, (result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, MODULE_NAME, METHOD_DLL_MAIN_NAME);

  return result;
}
