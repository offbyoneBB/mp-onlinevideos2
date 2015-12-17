using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime
{
    /// <summary>
    /// Log to file if the debug flags are set
    /// </summary>
    public class DebugLogger : ILog
    {
        private static bool _debugMode = true; // Allow for the form to be resized/lose focus in debug mode
        private static string _debugLogPath = "C:\\ProgramData\\Team MediaPortal\\MediaPortal\\log\\OnlineVideos.Amazon-DEBUG.log";
        private static bool _debugEntryWritten = false;

        /// <summary>
        /// Ctor - load values from the config file
        /// </summary>
        public DebugLogger()
        {
            _debugMode = true;
            _debugLogPath = "C:\\ProgramData\\Team MediaPortal\\MediaPortal\\log\\OnlineVideos.Amazon-DEBUG.log";
        }

        /// <summary>
        /// Write a debug message if enabled in the app config
        /// </summary>
        /// <param name="message"></param>
        public static void WriteDebugLog(string message)
        {
            if (_debugMode)
            {
                if (!string.IsNullOrEmpty(_debugLogPath) && Directory.Exists(Path.GetDirectoryName(_debugLogPath)))
                {
                    var msg = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + " " + message + "\r\n";
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
            WriteDebugLog("Debug: " + string.Format(format, arg));
        }
        public void Error(Exception ex)
        {
            WriteDebugLog("Error: " + ex.Message + " " + ex.StackTrace);
        }
        public void Error(string format, params object[] arg)
        {
            WriteDebugLog("Error: " + string.Format(format, arg));
        }
        public void Info(string format, params object[] arg)
        {
            WriteDebugLog("Info: " + string.Format(format, arg));
        }
        public void Warn(string format, params object[] arg)
        {
        }
    }
}
