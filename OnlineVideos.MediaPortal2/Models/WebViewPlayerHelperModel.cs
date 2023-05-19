using MediaPortal.Common;
using MediaPortal.UI.Presentation.Players;
using OnlineVideos.MediaPortal2.Player;
using System;
using System.Drawing;

namespace OnlineVideos.MediaPortal2.Models
{
    public class WebViewPlayerHelperModel
    {
        public static Guid MODEL_ID = new Guid("E3C534DF-C1AB-4AB1-AC65-F0F86756CDDC");

        public void UpdateOverlayPosition()
        {
            IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
            for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
            {
                IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
                if (playerContext != null && playerContext.CurrentPlayer is WebViewPlayer)
                {
                    WebViewPlayer webPlayer = (WebViewPlayer)playerContext.CurrentPlayer;
                    webPlayer.TargetBounds = RectangleF.Empty; // Empty dimension hides overlay
                }
            }
        }
    }
}
