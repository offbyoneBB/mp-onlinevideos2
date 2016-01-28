using OnlineVideos.Helpers;
using OnlineVideos.Sites.WebAutomation.BrowserHost.Helpers;
using System;
using System.IO;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ProcessHelper.PreventMonitorPowerdown();

                // Process requires path to MediaPortal, Video Id, Web Automation Type, Username, Password
                if (args.Length < 5) return;

                Directory.SetCurrentDirectory(args[0]);

                var result = args[2];
                var host = new BrowserHost();
                IERegistryVersion.SetIEVersion();
                Application.Run(host.PlayVideo(result, args[1], args[3], args[4]));
                IERegistryVersion.RemoveIEVersion();
            }
            catch (Exception ex)
            {
                var message = string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace);
                new DebugLogger().Error(message);
                Console.Error.WriteLine(message);
                Console.Error.Flush();
            }
        }
    }
}
