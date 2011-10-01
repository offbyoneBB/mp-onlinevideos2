#include <stdarg.h>
#include <Windows.h>
#include <stdio.h>
#include "Rtmp.h"

typedef void (RTMP_LogCallback)(int level, const char *fmt, va_list argptr);
typedef void (*RTMP_LogSetCallback)(RTMP_LogCallback *cb);

typedef void (MyLogCallback)(int level, const char *message);

MyLogCallback* callback=0;

void Callback(int level, const char *format, va_list vl )
{
	if (callback != 0)
	{
		char str[256]="";
		vsnprintf(str, 256-1, format, vl);
		callback(level,str);
	}
}

extern "C" __declspec(dllexport) void SetLogCallback(MyLogCallback *cb)
{
	callback = cb;

	RTMP_LogSetCallback setCallback;

	HINSTANCE hinstLib = LoadLibrary(TEXT("librtmp.dll"));
    if (hinstLib == NULL) 
	{
		callback(1,"ERROR: unable to load DLL\n");
    }
	else
	{
        setCallback = (RTMP_LogSetCallback)GetProcAddress(hinstLib, "RTMP_LogSetCallback");
        if (setCallback == NULL) 
		{
			callback(1,"ERROR: unable to find DLL function\n");
            FreeLibrary(hinstLib);
        }
		else
			setCallback(Callback);
	}

}

extern "C" __declspec(dllexport) int InitSockets()
{
#ifdef WIN32
  WORD version;
  WSADATA wsaData;

  version = MAKEWORD(1, 1);
  return (WSAStartup(version, &wsaData) == 0);
#else
  return TRUE;
#endif
}

extern "C" __declspec(dllexport) inline void CleanupSockets()
{
#ifdef WIN32
  WSACleanup();
#endif
}

extern "C" __declspec(dllexport) inline RTMP_LNK* GetLink(RTMP *rtmp)
{
	return &(rtmp->Link);
}