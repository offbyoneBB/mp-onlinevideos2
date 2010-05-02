using System;
using MediaPortal.Player;

namespace OnlineVideos.Player
{
    public class PlayerFactory : IPlayerFactory
    {
        PlayerType playerType = PlayerType.Auto;        

        public PlayerFactory(PlayerType playerType)
        {
            this.playerType = playerType;
        }
        
        string preparedUrl;
        PlayerType preparedPlayerType = PlayerType.Auto;

        public PlayerType Prepare(string url)
        {
            preparedUrl = url;

            switch (playerType)
            {
                case PlayerType.Internal:
                    preparedPlayerType = PlayerType.Internal;
                    break;
                case PlayerType.WMP:
                    preparedPlayerType = PlayerType.WMP;
                    break;
                default:
                    Uri uri = new Uri(url);

                    if (uri.Scheme == "rtsp" || uri.Scheme == "mms" || uri.PathAndQuery.Contains(".asf"))
                    {
                        preparedPlayerType = PlayerType.Internal;
                    }
                    else if (uri.PathAndQuery.Contains(".asx"))
                    {
                        preparedPlayerType = PlayerType.WMP;
                    }
                    else
                    {
                        foreach (string anExt in OnlineVideoSettings.Instance.VideoExtensions.Keys)
                        {
                            if (uri.PathAndQuery.Contains(anExt))
                            {
                                if (anExt == ".wmv" && !string.IsNullOrEmpty(uri.Query))
                                {
                                    preparedPlayerType = PlayerType.WMP;
                                    break;
                                }
                                else
                                {
                                    preparedPlayerType = PlayerType.Internal;
                                    break;
                                }
                            }
                        }
                        if (preparedPlayerType == PlayerType.Auto) preparedPlayerType = PlayerType.WMP;
                    }
                    break;
            }
            return preparedPlayerType;
        }
        
        public IPlayer Create(string filename)
        {
            return Create(filename, g_Player.MediaType.Video);
        }  

        public IPlayer Create(string filename, g_Player.MediaType type)
        {
            if (filename != preparedUrl) Prepare(filename);

            switch (preparedPlayerType)
            {
                case PlayerType.Internal: return new OnlineVideosPlayer();
                default: return new WMPVideoPlayer();
            }
        }              
    }
}
