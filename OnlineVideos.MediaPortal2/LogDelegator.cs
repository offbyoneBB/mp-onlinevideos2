using System;
using MediaPortal.Common.Logging;
using MediaPortal.Common;

namespace OnlineVideos.MediaPortal2
{
    public class LogDelegator : ILog
    {
        const string PREFIX = "[OnlineVideos] ";

        public void Debug(string format, params object[] arg)
        {
            ServiceRegistration.Get<ILogger>().Debug(PREFIX + format, arg);
        }

        public void Error(Exception ex)
        {
            ServiceRegistration.Get<ILogger>().Error(ex);
        }

        public void Error(string format, params object[] arg)
        {
            ServiceRegistration.Get<ILogger>().Error(PREFIX + format, arg);
        }

        public void Info(string format, params object[] arg)
        {
            ServiceRegistration.Get<ILogger>().Info(PREFIX + format, arg);
        }

        public void Warn(string format, params object[] arg)
        {
            ServiceRegistration.Get<ILogger>().Warn(PREFIX + format, arg);
        }
    }
}
