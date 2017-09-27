//#define TRACE_LOG

namespace MediaPortalWrapper.Utils
{
  public static class Logger
  {
    public static void Info(string format, params object[] args)
    {
      OnlineVideos.Log.Info(format, args);
    }
    public static void Log(int logLevel, string format, params object[] args)
    {
      if (logLevel == 0)
        OnlineVideos.Log.Debug(format, args);
      if (logLevel == 1)
        OnlineVideos.Log.Info(format, args);
      if (logLevel == 2)
        OnlineVideos.Log.Warn(format, args);
      if (logLevel == 3)
        OnlineVideos.Log.Error(format, args);
    }

    /// <summary>
    /// Wrapper for low level trace logging. To get log entries the assembly has to be compiled with TRACE_LOG and MP2 log level needs to be set to "ALL".
    /// </summary>
    public static void Log(string format, params object[] args)
    {
#if TRACE_LOG
      OnlineVideos.Log.Debug(format, args);
#endif
    }
  }
}
