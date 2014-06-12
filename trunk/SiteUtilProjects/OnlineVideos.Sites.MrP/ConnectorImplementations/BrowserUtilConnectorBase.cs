using OnlineVideos.Sites.Base;
using OnlineVideos.Sites.WebAutomation.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations
{
    /// <summary>
    /// Base connector which will show a loading dialog
    /// </summary>
    public abstract class BrowserUtilConnectorBase: BrowserUtilConnector
    {
        protected PictureBox _loadingPicture = new PictureBox();

        protected abstract Sites.Entities.EventResult PerformActualLogin(string username, string password);

        /// <summary>
        /// Show the loading dialog, the implementations will need to remove it when the video is fully ready
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override Sites.Entities.EventResult PerformLogin(string username, string password)
        {
            _loadingPicture.Image = Resources.loading;
            _loadingPicture.Dock = DockStyle.Fill;
            _loadingPicture.SizeMode = PictureBoxSizeMode.CenterImage;
            Browser.FindForm().Controls.Add(_loadingPicture);
            _loadingPicture.BringToFront();
            return PerformActualLogin(username, password);
        }
    }
}
