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
    public static class DebugLogger
    {
        private static bool _debugMode = false; // Allow for the form to be resized/lose focus in debug mode
        private static string _debugLogPath;
        private static bool _debugEntryWritten = false;

        /// <summary>
        /// Ctor - load values from the config file
        /// </summary>
        static DebugLogger()
        {
            var configValue = ConfigurationManager.AppSettings["DebugMode"];
            if (!string.IsNullOrEmpty(configValue) && configValue.ToUpper() == "TRUE")
                _debugMode = true;

            configValue = ConfigurationManager.AppSettings["DebugLogPath"];
            if (!string.IsNullOrEmpty(configValue)) _debugLogPath = configValue;
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
    }
}
