using System;
using MediaPortal.Player;

namespace OnlineVideos.Player
{
    public class PlaylistPlayerWrapper : MediaPortal.Playlists.PlayListPlayer.IPlayer
    {
        PlayerType playerType;

        public PlaylistPlayerWrapper(PlayerType playerType)
        {
            this.playerType = playerType;
        }

        public bool Play(string strFile)
        {
            if (g_Player.Playing) g_Player.Stop(true);

            IPlayerFactory savedFactory = g_Player.Factory;
            g_Player.Factory = new OnlineVideos.Player.PlayerFactory(playerType);
            bool result = g_Player.Play(strFile, g_Player.MediaType.Video);
            g_Player.Factory = savedFactory;

            return result;
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
            return g_Player.ShowFullScreenWindow();
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
}