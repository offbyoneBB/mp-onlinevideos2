using OnlineVideos.Sites.WebAutomation.BrowserHost.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost
{
    public class BrowserHost
    {
        /// <summary>
        /// Play the specified video
        /// </summary>
        /// <param name="videoId"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public Form PlayVideo(string connector, string videoId, string username, string password)
        {
            return new BrowserForm(connector, videoId, username, password);
        }
    }
}
