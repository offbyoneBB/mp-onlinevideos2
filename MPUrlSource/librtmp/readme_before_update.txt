
The purpose of this document is to remark all changes made in librtmp by any developer. These same changes
(or at least their meaning) must be made in next releases of librtmp.
 
Changes in Commit:30fcf46fc82f96ca41b710fc38bbc15f2489795e:

File: \amf.h, \dh.h, \handshake.h, \http.h
Comment: add to each method as first parameter 'struct RTMP *'
Code:

e.g. from:
  char *AMF_EncodeString(char *output, char *outend, const AVal * str);
to:
  char *AMF_EncodeString(struct RTMP *r, char *output, char *outend, const AVal * str);

--------------------------------------------

File: \log.h
Comment: include 'rtmp.h'
Code:

#include "rtmp.h"

--------------------------------------------

Comment: replace code
Code to replace:

extern RTMP_LogLevel RTMP_debuglevel;

typedef void (RTMP_LogCallback)(int level, const char *fmt, va_list);
void RTMP_LogSetCallback(RTMP_LogCallback *cb);
void RTMP_LogSetOutput(FILE *file);
void RTMP_LogPrintf(const char *format, ...);
void RTMP_LogStatus(const char *format, ...);
void RTMP_Log(int level, const char *format, ...);
void RTMP_LogHex(int level, const uint8_t *data, unsigned long len);
void RTMP_LogHexString(int level, const uint8_t *data, unsigned long len);
void RTMP_LogSetLevel(RTMP_LogLevel lvl);
RTMP_LogLevel RTMP_LogGetLevel(void);

Code:

//extern RTMP_LogLevel RTMP_debuglevel;

typedef void (RTMP_LogCallback)(struct RTMP *r, int level, const char *fmt, va_list);
void RTMP_LogSetCallback(struct RTMP *r, RTMP_LogCallback *cb);
//void RTMP_LogSetOutput(FILE *file);
//void RTMP_LogPrintf(const char *format, ...);
//void RTMP_LogStatus(const char *format, ...);
void RTMP_Log(struct RTMP *r, int level, const char *format, ...);
void RTMP_LogHex(struct RTMP *r, int level, const uint8_t *data, unsigned long len);
void RTMP_LogHexString(struct RTMP *r, int level, const uint8_t *data, unsigned long len);
//void RTMP_LogSetLevel(struct RTMP *r, RTMP_LogLevel lvl);
//RTMP_LogLevel RTMP_LogGetLevel(struct RTMP *r);


--------------------------------------------

File: \rtmp.h
Comment: include 'log.h'
Code:

#include "log.h"

--------------------------------------------

Comment:
Code to replace:

  void RTMPPacket_Reset(RTMPPacket *p);
  void RTMPPacket_Dump(RTMPPacket *p);
  int RTMPPacket_Alloc(RTMPPacket *p, int nSize);
  void RTMPPacket_Free(RTMPPacket *p);

Code:

  void RTMPPacket_Reset(struct RTMP *r, RTMPPacket *p);
  void RTMPPacket_Dump(struct RTMP *r, RTMPPacket *p);
  int RTMPPacket_Alloc(struct RTMP *r, RTMPPacket *p, int nSize);
  void RTMPPacket_Free(struct RTMP *r, RTMPPacket *p);

--------------------------------------------

Comment: add to struct RTMP
Code:

    void *m_logUserData;
    RTMP_LogCallback *m_logCallback;

--------------------------------------------

Comment: add 'RTMP *r' as first parameter to these methods
Code:

  int RTMP_ParseURL(RTMP *r, const char *url, int *protocol, AVal *host, unsigned int *port, AVal *playpath, AVal *app);
  void RTMP_ParsePlaypath(RTMP *r, AVal *in, AVal *out);
  int RTMP_FindFirstMatchingProperty(RTMP *r, AMFObject *obj, const AVal *name, AMFObjectProperty * p);
  int RTMPSockBuf_Fill(RTMP *r, RTMPSockBuf *sb);
  int RTMPSockBuf_Send(RTMP *r, RTMPSockBuf *sb, const char *buf, int len);
  int RTMPSockBuf_Close(RTMP *r, RTMPSockBuf *sb);
  int RTMP_HashSWF(RTMP *r, const char *url, unsigned int *size, unsigned char *hash, int age);

--------------------------------------------

File: \amf.c, \hashswf.c, \parseurl.c
Comment: to all methods add 'RTMP *r' as first parameter also add 'r' as first parameter to calling methods (best method is compiling)
Code:

--------------------------------------------

File: \log.c
Comment: comment lines
Code:

//RTMP_LogLevel RTMP_debuglevel = RTMP_LOGERROR;

//static RTMP_LogCallback rtmp_log_default, *cb = rtmp_log_default;

--------------------------------------------

Comment: comment methods
Code:

//static void rtmp_log_default(int level, const char *format, va_list vl)

//void RTMP_LogSetOutput(FILE *file)

//void RTMP_LogSetLevel(RTMP *r, RTMP_LogLevel level)

//RTMP_LogLevel RTMP_LogGetLevel(RTMP *r)

//void RTMP_LogPrintf(const char *format, ...)

//void RTMP_LogStatus(const char *format, ...)

--------------------------------------------

Comment: change methods
Code:

void RTMP_LogSetCallback(RTMP *r, RTMP_LogCallback *cbp)
{
  if (r != NULL)
  {
    r->m_logCallback = cbp;
  }
}

void RTMP_Log(RTMP *r, int level, const char *format, ...)
{
  va_list args;
  va_start(args, format);

  if (r != NULL)
  {
    if (r->m_logCallback != NULL)
    {
      r->m_logCallback(r, level, format, args);
    }
  }

  va_end(args);
}

--------------------------------------------

Comment: change parameters of methods
Code:

void RTMP_LogHex(RTMP *r, int level, const uint8_t *data, unsigned long len)

void RTMP_LogHexString(RTMP *r, int level, const uint8_t *data, unsigned long len)

--------------------------------------------

File: \rtmp.c
Comment: all methods must have first parameter 'RTMP *r'
Code:

--------------------------------------------
