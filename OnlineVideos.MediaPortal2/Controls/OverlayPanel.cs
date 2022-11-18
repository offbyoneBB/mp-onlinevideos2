using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using SharpDX;
using Point = System.Drawing.Point;

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
#if NETFRAMEWORK
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
#endif
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
            var mainLocation = mainForm.Location;
            var mainWidth = mainForm.Width;
            var mainHeight = mainForm.Height;

            //ServiceRegistration.Get<ILogger>().Debug("OVP: Source BBOX: {0}", boundingBox);
            //ServiceRegistration.Get<ILogger>().Debug("OVP: MainForm   : {0} / {1}x{2}", mainLocation, mainWidth, mainHeight);
            //ServiceRegistration.Get<ILogger>().Debug("OVP: Screen     : {0}", mainFormScreen.Bounds);
            //ServiceRegistration.Get<ILogger>().Debug("OVP: Skin       : {0}x{1}", Screen.SkinWidth, Screen.SkinHeight);

            // When the main window got minimized, position is off-screen. For calculation we use the full screen then
            if (mainLocation.X < 0 || mainLocation.Y < 0)
            {
                mainLocation = Point.Empty;
                mainWidth = mainFormScreen.Bounds.Width;
                mainHeight = mainFormScreen.Bounds.Height;
            }

            // First compensate size: ratio between "control to screen" vs. "screen to mainwindow"
            var ratioScreenToSkinWidth = mainFormScreen.Bounds.Width / (float)Screen.SkinWidth;
            var ratioScreenToSkinHeight = mainFormScreen.Bounds.Height / (float)Screen.SkinHeight;
            var newWidth = boundingBox.Width / mainFormScreen.Bounds.Width * mainWidth * ratioScreenToSkinWidth;
            var newHeight = boundingBox.Height / mainFormScreen.Bounds.Height * mainHeight * ratioScreenToSkinHeight;

            // Then move to compensate window position
            var newX = boundingBox.X * ratioScreenToSkinWidth + mainLocation.X;
            var newY = boundingBox.Y * ratioScreenToSkinHeight + mainLocation.Y;
            RectangleF newBBOX = new RectangleF(newX, newY, newWidth, newHeight);
            ServiceRegistration.Get<ILogger>().Info("OVP: Transformed BBOX: {0}", newBBOX);
            return newBBOX;
        }
    }
}
