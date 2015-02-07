using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost.Helpers
{
    /// <summary>
    /// Log to file if the debug flags are set
    /// </summary>
    public class DebugLogger : ILog
    {
        private bool _writeDebugLog = false; // Allow for debug logging to be independant of debug mode
        private string _debugLogPath;
        private bool _debugEntryWritten = false;

        /// <summary>
        /// Ctor - load values from the config file
        /// </summary>
        public DebugLogger()
        {
            var configValue = ConfigurationManager.AppSettings["WriteDebugLog"];
            if (!string.IsNullOrEmpty(configValue) && configValue.ToUpper() == "TRUE")
                _writeDebugLog = true;
            configValue = ConfigurationManager.AppSettings["DebugLogPath"];
            if (!string.IsNullOrEmpty(configValue)) _debugLogPath = configValue;
        }

        /// <summary>
        /// Write a debug message if enabled in the app config
        /// </summary>
        /// <param name="message"></param>
        private void WriteDebugLog(string message, string level)
        {
            if (_writeDebugLog)
            {
                if (!string.IsNullOrEmpty(_debugLogPath) && Directory.Exists(Path.GetDirectoryName(_debugLogPath)))
                {
                    var msg = string.Format("[{0}] [Log    ] [BrowserHost] [{1,-5}] - {2}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff"), level, message);
                    // Create a new file for first line to be written
                    if (!_debugEntryWritten)
                        File.WriteAllText(_debugLogPath, msg);
                    else
                        File.AppendAllText(_debugLogPath, msg);
                }
                _debugEntryWritten = true;
            }
        }

        public void Debug(string format, params object[] arg)
        {
            WriteDebugLog(string.Format(format, arg), "DEBUG");
        }
        public void Error(Exception ex)
        {
            WriteDebugLog(ex.Message + "\r\n" + ex.StackTrace, "ERROR");
        }
        public void Error(string format, params object[] arg)
        {
            WriteDebugLog(string.Format(format, arg), "ERROR");
        }
        public void Info(string format, params object[] arg)
        {
            WriteDebugLog(string.Format(format, arg), "INFO");
        }
        public void Warn(string format, params object[] arg)
        {
            WriteDebugLog(string.Format(format, arg), "WARN");
        }
    }
}
