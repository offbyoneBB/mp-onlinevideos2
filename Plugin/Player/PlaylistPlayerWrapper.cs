using System;
using MediaPortal.Player;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Player
{
    public class PlaylistPlayerWrapper : MediaPortal.Playlists.PlayListPlayer.IPlayer
    {
        PlayerType playerType;

        public PlaylistPlayerWrapper(PlayerType playerType)
        {
            this.playerType = playerType;
        }

        public bool Play(string lsUrl)
        {
            bool playing = false;
            // play files using the normal internal MediaPortal player
            if (System.IO.Path.IsPathRooted(lsUrl))
            {
                playing = g_Player.PlayVideoStream(lsUrl);
            }
            else
            {
                IPlayerFactory savedFactory = g_Player.Factory;
                g_Player.Factory = new OnlineVideos.Player.PlayerFactory(playerType);            
                playing = g_Player.Play(lsUrl, g_Player.MediaType.Video); 
                g_Player.Factory = savedFactory;
            }

            if (playing)
            {
                if (GUIWindowManager.ActiveWindow == Player.GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO) 
                    GUIGraphicsContext.IsFullScreenVideo = true;

                new System.Threading.Thread(delegate()
                    {
                        System.Threading.Thread.Sleep(2000);
                        PlayListItemWrapper item = MediaPortal.Playlists.PlayListPlayer.SingletonPlayer.GetCurrentItem() as PlayListItemWrapper;
                        if (item != null) GUIOnlineVideos.SetPlayingGuiProperties(item.Video, item.Description);
                    }) { IsBackground = true, Name = "OnlineVideosInfosSetter" }.Start();
            }
            return playing;
        }

        public bool PlayAudioStream(string strURL)
        {
            return g_Player.PlayAudioStream(strURL);
        }

        public bool PlayVideoStream(string strURL, string streamName)
        {
            return g_Player.PlayVideoStream(strURL, streamName);
        }

        public void Release()
        {
            g_Player.Release();
        }

        public void SeekAbsolute(double dTime)
        {
            g_Player.SeekAbsolute(dTime);
        }

        public void SeekAsolutePercentage(int iPercentage)
        {
            g_Player.SeekAsolutePercentage(iPercentage);
        }

        public bool ShowFullScreenWindow()
        {
            return true;
            // g_Player.ShowFullScreenWindow(); -> don't pass the call on, otherwise overlay playback will always go fullscreen on next item
        }

        public void Stop()
        {
            g_Player.Stop();
        }

        public double CurrentPosition
        {
            get
            {
                return g_Player.CurrentPosition;
            }
        }

        public double Duration
        {
            get
            {
                return g_Player.Duration;
            }
        }

        public bool HasVideo
        {
            get
            {
                return g_Player.HasVideo;
            }
        }

        public bool Playing
        {
            get
            {
                return g_Player.Playing;
            }
        }
    }

    public class PlayListItemWrapper : MediaPortal.Playlists.PlayListItem
    {
        public PlayListItemWrapper(string description, string fileName) : base(description, fileName) { }

        public VideoInfo Video { get; set; }
    }
}