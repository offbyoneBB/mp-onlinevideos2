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

        public static void LogHex(byte[] array, int offset, int count)
        {
            string result = "";
            for (int i = offset; i < offset + count; i++) result += array[i].ToString("X2") + " ";
            MediaPortal.GUI.Library.Log.Debug(result);
        }
    }
}
