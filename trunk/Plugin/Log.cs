using System;

namespace OnlineVideos
{
    public interface ILog
    {
        void Debug(string format, params object[] arg);
        void Error(Exception ex);
        void Error(string format, params object[] arg);
        void Info(string format, params object[] arg);
        void Warn(string format, params object[] arg);
    }

    /// <summary>
    /// This static class simply delegates Log calls to the <see cref="OnlineVideoSettings.Logger"/>.
    /// It's sole purpose is the easy access of logging functions.
    /// </summary>
    public static class Log
    {        
        public static void Debug(string format, params object[] arg)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Debug(format, arg);
        }

        public static void Error(Exception ex)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Error(ex.ToString());
        }

        public static void Error(string format, params object[] arg)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Error(format, arg);
        }

        public static void Info(string format, params object[] arg)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Info(format, arg);
        }

        public static void Warn(string format, params object[] arg)
        {
            if (OnlineVideoSettings.Instance.Logger != null) OnlineVideoSettings.Instance.Logger.Warn(format, arg);
        }
    }
}
