using System;
using Vattenmelon.Nrk.Domain;

namespace Vattenmelon.Nrk.Parser
{
    public class NullLogger : ILog
    {
        public void BackupLogFiles()
        {
        }
        public void Debug(string format, params object[] arg)
        {

        }
        //void Debug(LogType type, string format, params object[] arg);
        public void Error(Exception ex)
        {
        }
        public void Error(string format, params object[] arg)
        {
        }
        //void Error(LogType type, string format, params object[] arg);
        //[Obsolete("This method will disappear because the thread information is always logged now.", true)]
        public void ErrorThread(string format, params object[] arg)
        {
        }
        public void Info(string format, params object[] arg)
        {

        }
        //void Info(LogType type, string format, params object[] arg);
       // [Obsolete("This method will disappear because the thread information is always logged now.", true)]
        public void InfoThread(string format, params object[] arg)
        {
        }
        //public void SetConfigurationMode();
        //public void SetLogLevel(Level logLevel);
        public void Warn(string format, params object[] arg)
        {
        }
    }
}
