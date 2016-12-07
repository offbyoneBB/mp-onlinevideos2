//#define TRACE_LOG
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortalWrapper.Utils
{
  public static class Logger
  {
    public static void Info(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Info(format, args);
    }
    public static void Log(int logLevel, string format, params object[] args)
    {
      if (logLevel == 0)
        ServiceRegistration.Get<ILogger>().Debug(format, args);
      if (logLevel == 1)
        ServiceRegistration.Get<ILogger>().Info(format, args);
      if (logLevel == 2)
        ServiceRegistration.Get<ILogger>().Warn(format, args);
      if (logLevel == 3)
        ServiceRegistration.Get<ILogger>().Error(format, args);
    }

    /// <summary>
    /// Wrapper for low level trace logging. To get log entries the assembly has to be compiled with TRACE_LOG and MP2 log level needs to be set to "ALL".
    /// </summary>
    public static void Log(string format, params object[] args)
    {
#if TRACE_LOG
      ServiceRegistration.Get<ILogger>().Debug(format, args);
#endif
    }
  }
}
