using System;
using MePo = MediaPortal.GUI.Library;
using log4net.Repository.Hierarchy;
using log4net;
using log4net.Layout;
using log4net.Appender;

namespace OnlineVideos.MediaPortal1
{    
    /// <summary>
    /// This static class simply delegates Log calls to the MediaPortal Logging facility and prefixes the Output with [OnlineVideos].
    /// </summary>
    public class Log : MarshalByRefObject, ILog
    {
		#region MarshalByRefObject overrides
		public override object InitializeLifetimeService()
		{
			// In order to have the lease across appdomains live forever, we return null.
			return null;
		}
		#endregion

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
		#endregion

		log4net.ILog logger;

		private Log() 
		{
			log4net.Core.Level minLevel = log4net.Core.Level.All;
			using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.MPSettings())
			{
				var MPminLevel = (MediaPortal.Services.Level)Enum.Parse(typeof(MediaPortal.Services.Level), xmlreader.GetValueAsString("general", "loglevel", "2"));
				switch(MPminLevel)
				{
					case MediaPortal.Services.Level.Information: minLevel = log4net.Core.Level.Info; break;
					case MediaPortal.Services.Level.Warning: minLevel = log4net.Core.Level.Warn; break;
					case MediaPortal.Services.Level.Error: minLevel = log4net.Core.Level.Error; break;
				}
			}

			Hierarchy hierarchy = (Hierarchy)LogManager.CreateRepository("OnlineVideos");
			PatternLayout patternLayout = new PatternLayout();
			patternLayout.ConversionPattern = "[%date{MM-dd HH:mm:ss,fff}] [%-12thread] [%-5level] %message%newline";
			patternLayout.ActivateOptions();

			RollingFileAppender roller = new RollingFileAppender();
			roller.Layout = patternLayout;
			roller.AppendToFile = true;
			roller.RollingStyle = RollingFileAppender.RollingMode.Once;
			roller.MaxSizeRollBackups = 1;
			roller.MaximumFileSize = "10MB";
			roller.StaticLogFileName = true;
			roller.File = MediaPortal.Configuration.Config.GetFile(MediaPortal.Configuration.Config.Dir.Log, "OnlineVideos.log");
			roller.ActivateOptions();
			hierarchy.Root.AddAppender(roller);

			hierarchy.Root.Level = minLevel;
			hierarchy.Configured = true;

			logger = log4net.LogManager.GetLogger("OnlineVideos", "OnlineVideos");
		}		
        
        public void Debug(string format, params object[] arg)
        {
            logger.Debug(string.Format(format, arg));
        }

        public void Info(string format, params object[] arg)
        {
			logger.Info(string.Format(format, arg));
        }

        public void Warn(string format, params object[] arg)
        {
			logger.Warn(string.Format(format, arg));
        }

		public void Error(Exception ex)
		{
			MePo.Log.Error(ex.ToString());
		}

		public void Error(string format, params object[] arg)
		{
			MePo.Log.Error(string.Format(format, arg));
		}
    }
}
