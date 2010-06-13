using MediaPortal.GUI.Video;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using MediaPortal.Player;

namespace OnlineVideos.Player
{
    public class GUIOnlineVideoFullscreen : GUIVideoFullscreen
    {
#if !MP102
        public override string GetModuleName()
        {
            return OnlineVideoSettings.PLUGIN_NAME + " Fullscreen";
        }
#endif

        public const int WINDOW_FULLSCREEN_ONLINEVIDEO = 4758;
        public override int GetID { get { return WINDOW_FULLSCREEN_ONLINEVIDEO; } set { } }
      
        public override bool Init()
        {
            bool bResult = Load(GUIGraphicsContext.Skin + @"\myonlinevideosFullScreen.xml");
#if !MP102
            using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    typeof(GUIVideoFullscreen).InvokeMember("_immediateSeekIsRelative", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField, null, this, new object[] { xmlreader.GetValueAsBool("movieplayer", "immediateskipstepsisrelative", true) });
                    typeof(GUIVideoFullscreen).InvokeMember("_immediateSeekValue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField, null, this, new object[] { xmlreader.GetValueAsInt("movieplayer", "immediateskipstepsize", 10) });
                }
#endif
            return bResult;
        }
        
        public override void OnAction(Action action)
        {
            if (action.wID == Action.ActionType.ACTION_NEXT_ITEM)
            {
                // send a GUI Message to Onlinevideos GUI that it can now play the next item if any
                GUIWindowManager.SendMessage(new GUIMessage() { TargetWindowId = GUIOnlineVideos.WindowId, SendToTargetWindow = true, Object = this, Param1 = 1 });
                return;
            }
            else if (action.wID == Action.ActionType.ACTION_PREV_ITEM)
            {
                // send a GUI Message to Onlinevideos GUI that it can now play the previous item if any
                GUIWindowManager.SendMessage(new GUIMessage() { TargetWindowId = GUIOnlineVideos.WindowId, SendToTargetWindow = true, Object = this, Param1 = -1 });
                return;
            }
            else
            {
                if (action.wID == Action.ActionType.ACTION_VOLUME_UP ||
                    action.wID == Action.ActionType.ACTION_VOLUME_DOWN ||
                    action.wID == Action.ActionType.ACTION_VOLUME_MUTE)
                {
                    // MediaPortal core sends this message to the Fullscreenwindow, we need to do it ourselves to make the Volume OSD show up
                    base.OnAction(new Action(Action.ActionType.ACTION_SHOW_VOLUME, 0, 0));
                    return;
                }
                else
                {
                    Action translatedAction = new Action();
                    if (ActionTranslator.GetAction((int)Window.WINDOW_FULLSCREEN_VIDEO, action.m_key, ref translatedAction))
                    {
                        if (translatedAction.wID == Action.ActionType.ACTION_SHOW_OSD)
                        {
                            base.OnAction(translatedAction);
                            if (GUIWindowManager.VisibleOsd == Window.WINDOW_OSD)
                            {
                                GUIWindowManager.VisibleOsd = (Window)GUIOnlineVideoOSD.WINDOW_ONLINEVIDEOS_OSD;
                            }
                            return;
                        }
                    }
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
