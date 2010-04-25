using MediaPortal.GUI.Video;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using MediaPortal.Player;

namespace OnlineVideos.Player
{
    public class GUIOnlineVideoFullscreen : GUIVideoFullscreen
    {
        public const int WINDOW_FULLSCREEN_ONLINEVIDEO = 4758;
        public override int GetID { get { return WINDOW_FULLSCREEN_ONLINEVIDEO; } set { } }
      
        public override bool Init()
        {
            bool bResult = Load(GUIGraphicsContext.Skin + @"\myonlinevideosFullScreen.xml");
            using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                typeof(GUIVideoFullscreen).InvokeMember("_immediateSeekIsRelative", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField, null, this, new object[] {xmlreader.GetValueAsBool("movieplayer", "immediateskipstepsisrelative", true)});
                typeof(GUIVideoFullscreen).InvokeMember("_immediateSeekValue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField, null, this, new object[] { xmlreader.GetValueAsInt("movieplayer", "immediateskipstepsize", 10)});
            }
            return bResult;
        }
        
        public override void OnAction(Action action)
        {
            Action translatedAction = new Action();
            if (ActionTranslator.GetAction((int)Window.WINDOW_FULLSCREEN_VIDEO, action.m_key, ref translatedAction))
            {
                if (translatedAction.wID == Action.ActionType.ACTION_SHOW_OSD)
                { 
                    base.OnAction(translatedAction);
                    return;
                }
            }            
            base.OnAction(action);
        }
        
        public override bool OnMessage(GUIMessage message)
        {
            bool result = base.OnMessage(message);

            if (message.Message == GUIMessage.MessageType.GUI_MSG_WINDOW_INIT)
            {
                GUIVideoOSD osd = (GUIVideoOSD)GUIWindowManager.GetWindow(GUIOnlineVideoOSD.WINDOW_ONLINEVIDEOS_OSD);
                typeof(GUIVideoFullscreen).InvokeMember("_osdWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField, null, this, new object[] { osd });
            }

            return result;
        }
    }
}
