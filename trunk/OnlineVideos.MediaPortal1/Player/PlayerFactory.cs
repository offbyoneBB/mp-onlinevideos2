using System;
using MediaPortal.Player;

namespace OnlineVideos.MediaPortal1.Player
{
    enum PlayState { Init, Playing, Paused, Ended };

    public interface OVSPLayer
    {
        bool GoFullscreen { get; set; }
        string SubtitleFile { get; set; }
		string PlaybackUrl { get; } // hack to get around the MP 1.3 Alpha bug with non http URLs
    }

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
                // send all supported schemes to internal player
                if (uri.Scheme.StartsWith("rtmp") || uri.Scheme.StartsWith("http") || uri.Scheme == "sop" || uri.Scheme == "mms")
                {
                    PreparedPlayerType = PlayerType.Internal;
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
                case PlayerType.VLC: PreparedPlayer = new VLCPlayer(); break;
                case PlayerType.Browser: PreparedPlayer = new WebBrowserVideoPlayer(); break;
                default: PreparedPlayer = new WMPVideoPlayer(); break;
            }
        }

        public IPlayer Create(string filename)
        {
            return Create(filename, g_Player.MediaType.Video);
        }  

        public IPlayer Create(string filename, g_Player.MediaType type)
        {
			// hack to get around the MP 1.3 Alpha bug with non http URLs
            /*if (filename != PreparedUrl)
                throw new OnlineVideosException("Cannot play a different url than this PlayerFactory was created with!");
            else*/
                return PreparedPlayer;
        }              
    }
}
