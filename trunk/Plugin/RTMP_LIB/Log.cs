using System;
using System.Collections.Generic;
using System.Text;

namespace RTMP_LIB
{
    public static class Logger
    {
        public static void Log(string message)
        {
            MediaPortal.GUI.Library.Log.Debug(message);            
        }
    }
}
