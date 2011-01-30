using System;
using MediaPortal.Player;

namespace OnlineVideos.MediaPortal1.Player
{
    public class PlayerFactory : IPlayerFactory
    {
        public string PreparedUrl { get; protected set; }
        public PlayerType PreparedPlayerType { get; protected set; }
        public IPlayer PreparedPlayer { get; protected set; }

        public PlayerFactory(PlayerType playerType, string url)
        {
            PreparedPlayerType = playerType;
            PreparedUrl = url;
            SelectPlayerType();
            PreparePlayer();
        }

        void SelectPlayerType()
        {
            if (PreparedPlayerType == PlayerType.Auto)
            {
                Uri uri = new Uri(PreparedUrl);

                if (uri.Scheme == "rtsp" || uri.Scheme == "sop" || uri.Scheme == "mms" || uri.PathAndQuery.Contains(".asf"))
                {
                    PreparedPlayerType = PlayerType.Internal;
                }
                else if (uri.PathAndQuery.Contains(".asx"))
                {
                    PreparedPlayerType = PlayerType.WMP;
                }
                else
                {
                    foreach (string anExt in OnlineVideoSettings.Instance.VideoExtensions.Keys)
                    {
                        if (uri.PathAndQuery.Contains(anExt))
                        {
                            if (anExt == ".wmv" && !string.IsNullOrEmpty(uri.Query))
                            {
                                PreparedPlayerType = PlayerType.WMP;
                                break;
                            }
                            else
                            {
                                PreparedPlayerType = PlayerType.Internal;
                                break;
                            }
                        }
                    }
                    if (PreparedPlayerType == PlayerType.Auto) PreparedPlayerType = PlayerType.WMP;
                }
            }
        }

        void PreparePlayer()
        {
            switch (PreparedPlayerType)
            {
                case PlayerType.Internal: PreparedPlayer = new OnlineVideosPlayer(PreparedUrl); break;
                default: PreparedPlayer = new WMPVideoPlayer(); break;
            }
        }

        public IPlayer Create(string filename)
        {
            return Create(filename, g_Player.MediaType.Video);
        }  

        public IPlayer Create(string filename, g_Player.MediaType type)
        {
            if (filename != PreparedUrl)
                throw new OnlineVideosException("Cannot play a different url than this PlayerFactory was created with!");
            else
                return PreparedPlayer;
        }              
    }
}
