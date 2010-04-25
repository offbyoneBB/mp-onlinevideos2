using MediaPortal.GUI.Video;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Player
{
    public class GUIOnlineVideoOSD : GUIVideoOSD
    {
        public const int WINDOW_ONLINEVIDEOS_OSD = 4759;
        public override int GetID { get { return WINDOW_ONLINEVIDEOS_OSD; } set { } }

        public override bool Init()
        {
            bool bResult = Load(GUIGraphicsContext.Skin + @"\myonlinevideosOSD.xml");
            return bResult;
        }        
    }
}
