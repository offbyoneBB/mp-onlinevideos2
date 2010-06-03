using System;
using MediaPortal.Playlists;

namespace OnlineVideos.Player
{
    public class PlayListItemWrapper : MediaPortal.Playlists.PlayListItem
    {
        public PlayListItemWrapper(string description, string fileName) : base(description, fileName) { }

        public VideoInfo Video { get; set; }

        public Sites.SiteUtilBase Util { get; set; }
    }
}