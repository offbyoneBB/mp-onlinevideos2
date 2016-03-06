using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using SharpDX;

namespace OnlineVideos.MediaPortal2.Controls
{
    public class OverlayPanel : StackPanel
    {
        protected override void ArrangeOverride()
        {
            base.ArrangeOverride();
            SetPlayerBounds();
        }

        private void SetPlayerBounds()
        {
            IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
            for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
            {
                IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
                if (playerContext != null && playerContext.CurrentPlayer is WebBrowserVideoPlayer)
                {
                    WebBrowserVideoPlayer webPlayer = (WebBrowserVideoPlayer)playerContext.CurrentPlayer;
                    webPlayer.TargetBounds = TransformBoundingBox(BoundingBox);
                }
            }
        }

        /// <summary>
        /// Transforms the given bounding box in real screen coordinates. The screen size and window position will be considered.
        /// </summary>
        /// <param name="boundingBox"></param>
        /// <returns></returns>
        private RectangleF TransformBoundingBox(RectangleF boundingBox)
        {
            var mainForm = ServiceRegistration.Get<IScreenControl>() as Form;
            if (mainForm == null)
                return boundingBox;

            var mainFormScreen = System.Windows.Forms.Screen.FromControl(mainForm);

            //ServiceRegistration.Get<ILogger>().Debug("OVP: Source BBOX: {0}", boundingBox);
            //ServiceRegistration.Get<ILogger>().Debug("OVP: MainForm   : {0} / {1}", mainForm.Location, mainForm.Size);
            //ServiceRegistration.Get<ILogger>().Debug("OVP: Screen     : {0}", mainFormScreen.Bounds);
            //ServiceRegistration.Get<ILogger>().Debug("OVP: Skin       : {0}x{1}", Screen.SkinWidth, Screen.SkinHeight);

            // First compensate size: ratio between "control to screen" vs. "screen to mainwindow"
            var ratioScreenToSkinWidth = mainFormScreen.Bounds.Width / (float)Screen.SkinWidth;
            var ratioScreenToSkinHeight = mainFormScreen.Bounds.Height / (float)Screen.SkinHeight;
            var newWidth = boundingBox.Width / mainFormScreen.Bounds.Width * mainForm.Width * ratioScreenToSkinWidth;
            var newHeight = boundingBox.Height / mainFormScreen.Bounds.Height * mainForm.Height * ratioScreenToSkinHeight;

            // Then move to compensate window position
            var newX = boundingBox.X * ratioScreenToSkinWidth + mainForm.Location.X;
            var newY = boundingBox.Y * ratioScreenToSkinHeight + mainForm.Location.Y;
            RectangleF newBBOX = new RectangleF(newX, newY, newWidth, newHeight);
            ServiceRegistration.Get<ILogger>().Info("OVP: Transformed BBOX: {0}", newBBOX);
            return newBBOX;
        }
    }
}
