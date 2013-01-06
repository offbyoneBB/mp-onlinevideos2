using MediaPortal.GUI.Video;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using MediaPortal.Player;
using MediaPortal.Profile;

namespace OnlineVideos.MediaPortal1.Player
{
    public class GUIOnlineVideoFullscreen : GUIVideoFullscreen
    {
        public override string GetModuleName()
        {
            return PluginConfiguration.Instance.BasicHomeScreenName + " Fullscreen";
        }

        public const int WINDOW_FULLSCREEN_ONLINEVIDEO = 4758;
        public override int GetID { get { return WINDOW_FULLSCREEN_ONLINEVIDEO; } set { } }
        
        public override bool Load(string _skinFileName)
        {
            return base.Load(GUIGraphicsContext.Skin + @"\myonlinevideosFullScreen.xml");
        }

		public override void OnAction(Action action)
		{
			if (action.wID == Action.ActionType.ACTION_KEY_PRESSED)
			{
				// translate the action as if our window was the WINDOW_FULLSCREEN_VIDEO
				if (ActionTranslator.GetAction((int)Window.WINDOW_FULLSCREEN_VIDEO, action.m_key, ref action))
				{
					if (action.wID == Action.ActionType.ACTION_SHOW_OSD) // handle the OSD action differently - we need to show our OSD
					{
						base.OnAction(action);
						if (GUIWindowManager.VisibleOsd == Window.WINDOW_OSD)
						{
							GUIWindowManager.VisibleOsd = (Window)GUIOnlineVideoOSD.WINDOW_ONLINEVIDEOS_OSD;
						}
						return;
					}
					else if (action.wID == Action.ActionType.ACTION_SHOW_GUI)
					{
						return;
					}
					else if (action.wID == Action.ActionType.ACTION_VOLUME_UP ||
							 action.wID == Action.ActionType.ACTION_VOLUME_DOWN ||
							 action.wID == Action.ActionType.ACTION_VOLUME_MUTE)
					{
						// MediaPortal core sends this message to the Fullscreenwindow, we need to do it ourselves to make the Volume OSD show up
						GUIWindowManager.SendThreadCallback((p1, p2, o) => 
						{
							Action showVolume = new Action(Action.ActionType.ACTION_SHOW_VOLUME, 0, 0);
							GUIWindowManager.OnAction(showVolume);
							return 0; 
						}, 0, 0, null);
						return;
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
