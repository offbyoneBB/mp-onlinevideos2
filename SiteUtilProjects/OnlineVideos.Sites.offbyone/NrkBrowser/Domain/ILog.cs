using System;

namespace Vattenmelon.Nrk.Domain
{
    /// <summary>
    /// This is mostly all methods from Mediaportals Log class that i used. serious cleanup needed.
    /// </summary>
    public interface ILog
    { 
        //void BackupLogFile(LogType logType);
        void BackupLogFiles();
        void Debug(string format, params object[] arg);
        //void Debug(LogType type, string format, params object[] arg);
        void Error(Exception ex);
        void Error(string format, params object[] arg);
        //void Error(LogType type, string format, params object[] arg);
        [Obsolete("This method will disappear because the thread information is always logged now.", true)]
        void ErrorThread(string format, params object[] arg);
        void Info(string format, params object[] arg);
        //void Info(LogType type, string format, params object[] arg);
        [Obsolete("This method will disappear because the thread information is always logged now.", true)]
        void InfoThread(string format, params object[] arg);
        //public void SetConfigurationMode();
        //public void SetLogLevel(Level logLevel);
        void Warn(string format, params object[] arg);
        //public void Warn(LogType type, string format, params object[] arg);
        //[Obsolete("This method will disappear because the thread information is always logged now.", true)]
        //public void WarnThread(string format, params object[] arg);
        //[Obsolete("This method will disappear.  Use one of the Info, Warn, Debug or Error variants instead.", true)]
        //public void Write(Exception ex);
        //[Obsolete("This method will disappear.  Use one of the Info, Warn, Debug or Error variants instead.", true)]
        //public void Write(string format, params object[] arg);
        //public void WriteFile(LogType type, string format, params object[] arg);
        //public void WriteFile(LogType type, bool isError, string format, params object[] arg);
        //[Obsolete("This method will disappear because the thread information is always logged now.", true)]
        //public void WriteFileThreadId(LogType type, bool isError, string format, params object[] arg);
    }
}
