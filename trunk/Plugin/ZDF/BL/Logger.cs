namespace ZDF.BL
{
    using Microsoft.Win32;
    using System;
    using System.IO;
    using System.Text;

    public class Logger
    {
        private static string GetLoggingDir()
        {
            string path = null;
            try
            {
                path = (string) Registry.LocalMachine.OpenSubKey(@"Software\ZDF\ZDF TV", false).GetValue("CacheDir");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch
            {
            }
            if (path != null)
            {
                return path;
            }
            return @"C:\tmp";
        }

        public static void Log(Exception ex)
        {
            Log(ex.ToString());
        }

        public static void Log(string message)
        {
            try
            {
                StreamWriter writer = new StreamWriter(Path.Combine(GetLoggingDir(), "errorlog.txt"), true, Encoding.UTF8, 100);
                writer.Write("\r\n");
                writer.Write(DateTime.Now.ToShortDateString());
                writer.Write(' ');
                writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffffff"));
                writer.WriteLine(message);
                writer.Flush();
                writer.Close();
            }
            catch
            {
            }
        }
    }
}

