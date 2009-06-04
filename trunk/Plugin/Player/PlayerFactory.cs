using System;
using MediaPortal.Player;

namespace OnlineVideos.Player
{
    public class PlayerFactory : IPlayerFactory
    {
        public IPlayer Create(string filename)
        {
            return Create(filename, g_Player.MediaType.Unknown);
        }  

        public IPlayer Create(string filename, g_Player.MediaType type)
        {
            Uri uri = new Uri(filename);

            if (uri.AbsolutePath.EndsWith(".swf") && uri.AbsolutePath.Contains("yahoo"))
            {
                return new YahooMusicVideosPlayer();
            }
            else if (uri.Scheme == "mms" || uri.PathAndQuery.Contains(".asx"))
            {
                return new AudioPlayerWMP9();
            }
            else
            {
                foreach (string anExt in OnlineVideoSettings.getInstance().videoExtensions.Keys)
                {
                    if (uri.PathAndQuery.Contains(anExt))
                    {
                        if (anExt == ".wmv" && !string.IsNullOrEmpty(uri.Query))
                        {
                            return new AudioPlayerWMP9();
                        }
                        else
                        {
                            return new VideoPlayerVMR9(); // safer to use default until codec specific playback is done - OnlineVideosPlayer();
                        }
                    }
                }
                return new AudioPlayerWMP9();
            }
        }              
    }
}
