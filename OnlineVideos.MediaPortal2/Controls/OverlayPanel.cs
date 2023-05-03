using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using OnlineVideos.MediaPortal2.Player;
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
            IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
            for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
            {
                IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
                if (playerContext != null && playerContext.CurrentPlayer is WebViewPlayer)
                {
                    WebViewPlayer webPlayer = (WebViewPlayer)playerContext.CurrentPlayer;
                    webPlayer.TargetBounds = TransformBoundingBox(BoundingBox);
                }
            }
        }

        /// <summary>
        /// Transforms the given bounding box in real screen coordinates. The screen size and window position will be considered.
        /// </summary>
        /// <param name="boundingBox"></param>
        /// <returns></returns>
        private System.Drawing.RectangleF TransformBoundingBox(RectangleF boundingBox)
        {
            var mainForm = ServiceRegistration.Get<IScreenControl>() as Form;
            if (mainForm == null)
                return new System.Drawing.RectangleF(boundingBox.X, boundingBox.Y, boundingBox.Width, boundingBox.Height);

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

            var screenWidthToSkin = boundingBox.Width * mainWidth / Screen.SkinWidth;
            var screenHeightToSkin = boundingBox.Height * mainHeight / Screen.SkinHeight;
            var boxLeft = mainLocation.X + boundingBox.Left * mainWidth / Screen.SkinWidth;
            var boxTop = mainLocation.Y + boundingBox.Top * mainHeight / Screen.SkinHeight;

            System.Drawing.RectangleF newBBOX = new System.Drawing.RectangleF(boxLeft, boxTop, screenWidthToSkin, screenHeightToSkin);
            ServiceRegistration.Get<ILogger>().Info("OVP: Transformed BBOX: {0}", newBBOX);
            return newBBOX;
        }
    }
}
