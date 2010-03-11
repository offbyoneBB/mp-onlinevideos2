using System;

namespace OnlineVideos
{
    /// <summary>
    /// This static class simply delegates Log calls to the MediaPortal Logging facility and prefixes the Output with [OnlineVideos].
    /// This will allow satellite SiteUtil dll Projects, to log into MediaPortal files without having to reference any MediaPortal dlls.
    /// </summary>
    public static class Log
    {
        const string PREFIX = "[OnlineVideos]";

        public static void Debug(string format, params object[] arg)
        {
            MediaPortal.GUI.Library.Log.Debug(PREFIX + format, arg);
        }

        public static void Error(Exception ex)
        {
            MediaPortal.GUI.Library.Log.Error(PREFIX + ex.ToString());
        }

        public static void Error(string format, params object[] arg)
        {
            MediaPortal.GUI.Library.Log.Error(PREFIX + format, arg);
        }

        public static void Info(string format, params object[] arg)
        {
            MediaPortal.GUI.Library.Log.Info(PREFIX + format, arg);
        }

        public static void Warn(string format, params object[] arg)
        {
            MediaPortal.GUI.Library.Log.Warn(PREFIX + format, arg);
        }
    }
}
