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

            if (uri.Scheme == "mms" || uri.PathAndQuery.Contains(".asf"))
            {
                return new OnlineVideosPlayer();
            }
            else if (uri.PathAndQuery.Contains(".asx"))
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
                            return new OnlineVideosPlayer();
                        }
                    }
                }
                return new AudioPlayerWMP9();
            }            
        }              
    }
}
