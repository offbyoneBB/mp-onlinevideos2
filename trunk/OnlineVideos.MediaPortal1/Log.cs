using System;
using MePo = MediaPortal.GUI.Library;

namespace OnlineVideos.MediaPortal1
{    
    /// <summary>
    /// This static class simply delegates Log calls to the MediaPortal Logging facility and prefixes the Output with [OnlineVideos].
    /// This will allow satellite SiteUtil dll Projects, to log into MediaPortal files without having to reference any MediaPortal dlls.
    /// </summary>
    public class Log : ILog
    {
        #region Singleton
        private static Log _Instance = null;
        public static Log Instance
        {
            get
            {
                if (_Instance == null) _Instance = new Log();
                return _Instance;
            }
        }
        private Log() { }
        #endregion
        
        const string PREFIX = "[OnlineVideos]";
        
        public void Debug(string format, params object[] arg)
        {
            MePo.Log.Debug(PREFIX + format, arg);
        }

        public void Error(Exception ex)
        {
            MePo.Log.Error(PREFIX + ex.ToString());
        }

        public void Error(string format, params object[] arg)
        {
            MePo.Log.Error(PREFIX + format, arg);
        }

        public void Info(string format, params object[] arg)
        {
            MePo.Log.Info(PREFIX + format, arg);
        }

        public void Warn(string format, params object[] arg)
        {
            MePo.Log.Warn(PREFIX + format, arg);
        }
    }
}
